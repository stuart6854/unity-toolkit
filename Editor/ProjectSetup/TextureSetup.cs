using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    /// <summary>
    /// Generates common utility textures (solids, noise, prototype grids, gradients) as PNGs.
    /// Existing files are never overwritten.
    /// </summary>
    public static class TextureSetup
    {
        class Spec
        {
            public string Name;
            public Func<Texture2D> Make;
            public Action<TextureImporter> Import;
        }

        public static string Folder(ProjectSetupSettings settings) =>
            $"Assets/{settings.rootFolder}/{settings.texturesFolder}".Replace('\\', '/');

        public static void CreateTextures(ProjectSetupSettings settings)
        {
            var specs = new List<Spec>();
            if (settings.solidTextures) AddSolids(specs);
            if (settings.noiseTextures) AddNoise(specs, settings.noiseSize, settings.noiseSeed);
            if (settings.prototypeTextures) AddPrototype(specs);
            if (settings.gradientTextures) AddGradients(specs);
            if (settings.particleTextures) AddParticles(specs, settings.noiseSeed);
            Generate(Folder(settings), specs);
        }

        /// <summary>Creates only the particle textures (used by the VFX Library).</summary>
        public static void CreateParticleTextures(ProjectSetupSettings settings)
        {
            var specs = new List<Spec>();
            AddParticles(specs, settings.noiseSeed);
            Generate(Folder(settings), specs);
        }

        static void Generate(string folder, List<Spec> specs)
        {
#if TOOLKIT_IMAGECONVERSION
            Directory.CreateDirectory(folder);

            var created = new List<(string path, Spec spec)>();
            foreach (var spec in specs)
            {
                var path = $"{folder}/{spec.Name}.png";
                if (File.Exists(path))
                    continue;

                var tex = spec.Make();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                created.Add((path, spec));
            }

            AssetDatabase.Refresh();

            foreach (var (path, spec) in created)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                spec.Import(importer);
                importer.SaveAndReimport();
            }

            Debug.Log($"[Toolkit] Texture setup done. Created {created.Count} texture(s) in {folder}.");
#else
            Debug.LogError("[Toolkit] Texture setup needs the built-in module 'com.unity.modules.imageconversion'.");
#endif
        }

        // ---------- Solids (2x2) ----------

        static void AddSolids(List<Spec> specs)
        {
            specs.Add(Solid("T_White", new Color32(255, 255, 255, 255), false));
            specs.Add(Solid("T_Black", new Color32(0, 0, 0, 255), false));
            specs.Add(Solid("T_Grey", new Color32(128, 128, 128, 255), false));
            specs.Add(Solid("T_FlatNormal", new Color32(128, 128, 255, 255), true));
        }

        static Spec Solid(string name, Color32 color, bool normal) => new()
        {
            Name = name,
            Make = () => Fill(2, 2, (_, _) => color),
            Import = i =>
            {
                i.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                i.mipmapEnabled = false;
                i.filterMode = FilterMode.Point;
                i.textureCompression = TextureImporterCompression.Uncompressed;
            },
        };

        // ---------- Noise ----------

        static void AddNoise(List<Spec> specs, int size, int seed)
        {
            specs.Add(new Spec
            {
                Name = "T_Noise_White",
                Make = () =>
                {
                    var rng = new System.Random(seed);
                    return Fill(size, size, (_, _) =>
                    {
                        var v = (byte)rng.Next(256);
                        return new Color32(v, v, v, 255);
                    });
                },
                Import = i => DataImport(i, FilterMode.Point),
            });

            specs.Add(new Spec
            {
                Name = "T_Noise_RGB",
                Make = () =>
                {
                    var rng = new System.Random(seed + 1);
                    return Fill(size, size, (_, _) =>
                        new Color32((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256), 255));
                },
                Import = i => DataImport(i, FilterMode.Point),
            });

            specs.Add(new Spec
            {
                Name = "T_Noise_Perlin",
                Make = () => PerlinTexture(size, seed),
                Import = i => DataImport(i, FilterMode.Bilinear),
            });
        }

        static void DataImport(TextureImporter i, FilterMode filter)
        {
            i.textureType = TextureImporterType.Default;
            i.sRGBTexture = false;
            i.mipmapEnabled = false;
            i.filterMode = filter;
            i.wrapMode = TextureWrapMode.Repeat;
            i.textureCompression = TextureImporterCompression.Uncompressed;
        }

        /// <summary>Tileable fractal Perlin noise (5 octaves), stretched to the full 0-255 range.</summary>
        static Texture2D PerlinTexture(int size, int seed)
        {
            const int basePeriod = 4;
            const int octaves = 5;
            var values = new float[size * size];
            float min = float.MaxValue, max = float.MinValue;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float sum = 0f, amp = 1f;
                var period = basePeriod;
                for (var o = 0; o < octaves; o++)
                {
                    sum += amp * PeriodicPerlin(u * period, v * period, period, seed + o * 101);
                    amp *= 0.5f;
                    period *= 2;
                }

                values[y * size + x] = sum;
                min = Mathf.Min(min, sum);
                max = Mathf.Max(max, sum);
            }

            var i = 0;
            return Fill(size, size, (_, _) =>
            {
                var b = (byte)Mathf.RoundToInt(Mathf.InverseLerp(min, max, values[i++]) * 255f);
                return new Color32(b, b, b, 255);
            });
        }

        static float PeriodicPerlin(float x, float y, int period, int seed)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;

            float Dot(int ix, int iy, float dx, float dy)
            {
                var angle = Hash(Mod(ix, period), Mod(iy, period), seed) * Mathf.PI * 2f;
                return Mathf.Cos(angle) * dx + Mathf.Sin(angle) * dy;
            }

            var n00 = Dot(x0, y0, fx, fy);
            var n10 = Dot(x0 + 1, y0, fx - 1, fy);
            var n01 = Dot(x0, y0 + 1, fx, fy - 1);
            var n11 = Dot(x0 + 1, y0 + 1, fx - 1, fy - 1);

            float sx = Fade(fx), sy = Fade(fy);
            return Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy);
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        static int Mod(int a, int m) => (a % m + m) % m;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // ---------- Prototype ----------

        static void AddPrototype(List<Spec> specs)
        {
            specs.Add(new Spec { Name = "T_Proto_Grid", Make = GridTexture, Import = ColorImport });
            specs.Add(new Spec { Name = "T_Proto_Checker", Make = CheckerTexture, Import = ColorImport });
            specs.Add(new Spec { Name = "T_Proto_UVTest", Make = UVTestTexture, Import = ColorImport });
        }

        static void ColorImport(TextureImporter i)
        {
            i.textureType = TextureImporterType.Default;
            i.sRGBTexture = true;
            i.mipmapEnabled = true;
            i.filterMode = FilterMode.Trilinear;
            i.anisoLevel = 4;
            i.wrapMode = TextureWrapMode.Repeat;
        }

        /// <summary>One tile = 1m: thick border lines, with thin lines every 0.25m.</summary>
        static Texture2D GridTexture()
        {
            const int size = 512;
            var bg = new Color32(70, 70, 74, 255);
            var minor = new Color32(95, 95, 100, 255);
            var major = new Color32(150, 150, 156, 255);

            return Fill(size, size, (x, y) =>
            {
                bool Edge(int p, int width) => p < width || p >= size - width;
                if (Edge(x, 3) || Edge(y, 3)) return major;
                if (x % (size / 4) < 1 || y % (size / 4) < 1) return minor;
                return bg;
            });
        }

        static Texture2D CheckerTexture()
        {
            const int size = 512, cells = 8;
            var a = new Color32(90, 90, 94, 255);
            var b = new Color32(130, 130, 136, 255);
            return Fill(size, size, (x, y) => ((x / (size / cells)) + (y / (size / cells))) % 2 == 0 ? a : b);
        }

        /// <summary>Red = U, green = V, with a checker to show stretching and a darker border.</summary>
        static Texture2D UVTestTexture()
        {
            const int size = 512, cells = 8;
            return Fill(size, size, (x, y) =>
            {
                float u = x / (size - 1f), v = y / (size - 1f);
                var dark = ((x / (size / cells)) + (y / (size / cells))) % 2 == 0;
                var k = dark ? 0.65f : 1f;
                var border = x % (size / cells) == 0 || y % (size / cells) == 0;
                if (border) k = 0.3f;
                return new Color(u * k, v * k, 0.35f * k, 1f);
            });
        }

        // ---------- Gradients ----------

        static void AddGradients(List<Spec> specs)
        {
            specs.Add(new Spec
            {
                Name = "T_Gradient_Horizontal",
                Make = () => Fill(256, 4, (x, _) => Grey(x / 255f)),
                Import = GradientImport,
            });
            specs.Add(new Spec
            {
                Name = "T_Gradient_Vertical",
                Make = () => Fill(4, 256, (_, y) => Grey(y / 255f)),
                Import = GradientImport,
            });
            specs.Add(new Spec
            {
                Name = "T_Gradient_Radial",
                Make = () => Fill(256, 256, (x, y) =>
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(128f, 128f)) / 128f;
                    return Grey(1f - Mathf.Clamp01(d)); // White centre, black edge.
                }),
                Import = GradientImport,
            });
        }

        static void GradientImport(TextureImporter i)
        {
            i.textureType = TextureImporterType.Default;
            i.sRGBTexture = false;
            i.mipmapEnabled = false;
            i.filterMode = FilterMode.Bilinear;
            i.wrapMode = TextureWrapMode.Clamp;
            i.textureCompression = TextureImporterCompression.Uncompressed;
        }

        // ---------- Particles ----------

        public const string SoftCircle = "T_Particle_SoftCircle";
        public const string Glow = "T_Particle_Glow";
        public const string Spark = "T_Particle_Spark";
        public const string Smoke = "T_Particle_Smoke";
        public const string Ring = "T_Particle_Ring";

        static void AddParticles(List<Spec> specs, int seed)
        {
            const int size = 128;

            // Alpha only; colour comes from the particle system.
            Color32 White(float alpha) => new(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
            float Dist(int x, int y) => Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);

            specs.Add(new Spec
            {
                Name = SoftCircle,
                Make = () => Fill(size, size, (x, y) => White(Mathf.SmoothStep(1f, 0f, Dist(x, y)))),
                Import = ParticleImport,
            });

            specs.Add(new Spec
            {
                Name = Glow,
                Make = () => Fill(size, size, (x, y) =>
                {
                    var d = Mathf.Clamp01(Dist(x, y));
                    return White(Mathf.Pow(1f - d, 3f));
                }),
                Import = ParticleImport,
            });

            specs.Add(new Spec
            {
                Name = Spark,
                Make = () => Fill(size, size / 4, (x, y) =>
                {
                    // Horizontal streak: bright core, soft edges, tapering towards both ends.
                    var u = (x + 0.5f) / size * 2f - 1f;
                    var v = (y + 0.5f) / (size / 4f) * 2f - 1f;
                    var along = 1f - u * u;
                    var across = Mathf.Exp(-v * v * 6f);
                    return White(along * across);
                }),
                Import = ParticleImport,
            });

            specs.Add(new Spec
            {
                Name = Smoke,
                Make = () =>
                {
                    var noise = PerlinTexture(size, seed + 7);
                    var px = noise.GetPixels32();
                    UnityEngine.Object.DestroyImmediate(noise);
                    var i = 0;
                    return Fill(size, size, (x, y) =>
                    {
                        var n = px[i++].r / 255f;
                        var falloff = Mathf.SmoothStep(1f, 0f, Dist(x, y));
                        return White(falloff * Mathf.Lerp(0.35f, 1f, n));
                    });
                },
                Import = ParticleImport,
            });

            specs.Add(new Spec
            {
                Name = Ring,
                Make = () => Fill(size, size, (x, y) =>
                {
                    var d = Dist(x, y);
                    return White(Mathf.Exp(-Mathf.Pow((d - 0.8f) / 0.08f, 2f)));
                }),
                Import = ParticleImport,
            });
        }

        static void ParticleImport(TextureImporter i)
        {
            i.textureType = TextureImporterType.Default;
            i.sRGBTexture = true;
            i.alphaSource = TextureImporterAlphaSource.FromInput;
            i.alphaIsTransparency = true;
            i.mipmapEnabled = true;
            i.wrapMode = TextureWrapMode.Clamp;
            i.filterMode = FilterMode.Bilinear;
        }

        // ---------- Helpers ----------

        static Color32 Grey(float t)
        {
            var b = (byte)Mathf.RoundToInt(Mathf.Clamp01(t) * 255f);
            return new Color32(b, b, b, 255);
        }

        /// <summary>Creates a texture by calling pixel(x, y) row by row, bottom to top.</summary>
        static Texture2D Fill(int width, int height, Func<int, int, Color32> pixel)
        {
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = pixel(x, y);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
