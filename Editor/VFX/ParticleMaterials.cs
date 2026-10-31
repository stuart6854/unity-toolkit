using System.IO;
using StuMillman.Toolkit.Editor.ProjectSetup;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StuMillman.Toolkit.Editor.VFX
{
    /// <summary>
    /// URP "Particles/Unlit" materials using the generated particle textures.
    /// </summary>
    public static class ParticleMaterials
    {
        public const string ShaderName = "Universal Render Pipeline/Particles/Unlit";

        public enum Blend { Alpha, Additive }

        public class Set
        {
            public Material Glow;        // Additive glow (flashes, magic, fire core)
            public Material Spark;       // Additive streak (sparks, embers)
            public Material Ring;        // Additive ring (shockwaves, pickups)
            public Material SoftAlpha;   // Alpha soft circle (dust, fireball)
            public Material Smoke;       // Alpha smoke with soft particles
        }

        public static string Folder(ProjectSetupSettings s) =>
            $"Assets/{s.rootFolder}/{s.particleMaterialsFolder}".Replace('\\', '/');

        /// <summary>Creates any missing particle textures and materials, and returns them.</summary>
        public static Set EnsureAll(ProjectSetupSettings settings)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Toolkit] Shader '{ShaderName}' not found. Is URP installed?");
                return null;
            }

            TextureSetup.CreateParticleTextures(settings);

            var folder = Folder(settings);
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            Texture2D Tex(string name) =>
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureSetup.Folder(settings)}/{name}.png");

            var set = new Set
            {
                Glow = GetOrCreate(folder, "M_Particle_Glow", shader, Tex(TextureSetup.Glow), Blend.Additive, false),
                Spark = GetOrCreate(folder, "M_Particle_Spark", shader, Tex(TextureSetup.Spark), Blend.Additive, false),
                Ring = GetOrCreate(folder, "M_Particle_Ring", shader, Tex(TextureSetup.Ring), Blend.Additive, false),
                SoftAlpha = GetOrCreate(folder, "M_Particle_SoftAlpha", shader, Tex(TextureSetup.SoftCircle), Blend.Alpha, false),
                Smoke = GetOrCreate(folder, "M_Particle_Smoke", shader, Tex(TextureSetup.Smoke), Blend.Alpha, true),
            };

            AssetDatabase.SaveAssets();
            return set;
        }

        static Material GetOrCreate(string folder, string name, Shader shader, Texture2D texture, Blend blend, bool soft)
        {
            var path = $"{folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;

            mat = new Material(shader) { name = name };
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            Configure(mat, blend, soft);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>
        /// Sets the same properties, keywords and render state URP's material inspector would.
        /// </summary>
        public static void Configure(Material mat, Blend blend, bool softParticles)
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", blend == Blend.Additive ? 2f : 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (blend == Blend.Additive)
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.One);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            }
            else
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            }

            mat.SetFloat("_SoftParticlesEnabled", softParticles ? 1f : 0f);
            mat.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            mat.SetFloat("_SoftParticlesFarFadeDistance", 1f);
            if (softParticles) mat.EnableKeyword("_SOFTPARTICLES_ON");
            else mat.DisableKeyword("_SOFTPARTICLES_ON");
        }
    }
}
