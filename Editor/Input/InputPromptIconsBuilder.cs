using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StuMillman.Toolkit.Input;
using UnityEditor;
using UnityEngine;
#if TOOLKIT_UGUI
using TMPro;
using UnityEngine.TextCore;
#endif

namespace StuMillman.Toolkit.Editor.Input
{
    /// <summary>
    /// Creates the InputPromptIcons asset, imports the Kenney Input Prompts pack (CC0) and builds the TMP sprite asset.
    /// </summary>
    public static class InputPromptIconsBuilder
    {
        const string MenuRoot = "Tools/Toolkit/Input Prompts/";
        public const string KenneyUrl = "https://kenney.nl/assets/input-prompts";

        static string Folder
        {
            get
            {
                var s = ProjectSetup.ProjectSetupSettings.instance;
                return $"Assets/{s.rootFolder}/Settings/InputPrompts";
            }
        }

        static string IconsPath => $"{Folder}/Resources/InputPromptIcons.asset";

        [MenuItem(MenuRoot + "Download Kenney Input Prompts (CC0)", priority = 60)]
        static void OpenKenney() => Application.OpenURL(KenneyUrl);

        [MenuItem(MenuRoot + "Import Icons From Folder...", priority = 61)]
        static void ImportFromFolderMenu()
        {
            var abs = EditorUtility.OpenFolderPanel("Select the unzipped Kenney Input Prompts folder (inside Assets)", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            var root = Path.GetFullPath(Directory.GetCurrentDirectory()).Replace('\\', '/') + "/";
            abs = Path.GetFullPath(abs).Replace('\\', '/');
            if (!abs.StartsWith(root + "Assets"))
            {
                EditorUtility.DisplayDialog("Folder must be in Assets",
                    "Copy the unzipped Kenney folder into your project's Assets folder first, then select it.", "OK");
                return;
            }

            ImportFromFolder(abs.Substring(root.Length));
        }

        [MenuItem(MenuRoot + "Select Icons Asset", priority = 62)]
        static void SelectIcons()
        {
            var icons = GetOrCreateIcons();
            Selection.activeObject = icons;
            EditorGUIUtility.PingObject(icons);
        }

        public static InputPromptIcons GetOrCreateIcons()
        {
            var icons = AssetDatabase.LoadAssetAtPath<InputPromptIcons>(IconsPath);
            if (icons != null) return icons;

            Directory.CreateDirectory(Path.GetDirectoryName(IconsPath)!);
            AssetDatabase.Refresh();
            icons = ScriptableObject.CreateInstance<InputPromptIcons>();
            AssetDatabase.CreateAsset(icons, IconsPath);
            AssetDatabase.SaveAssets();
            return icons;
        }

        // ---------------- Kenney import ----------------

        /// <summary>Maps textures in a folder to controls by file name, then builds the sprite asset.</summary>
        public static void ImportFromFolder(string assetFolder)
        {
            var icons = GetOrCreateIcons();
            var found = new Dictionary<(InputDeviceFamily, string), (Texture2D tex, int score)>();

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { assetFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lower = path.ToLowerInvariant();
                if (!lower.EndsWith(".png")) continue;

                var family = FamilyFromPath(lower);
                if (family == null) continue;

                var tokens = Tokens(Path.GetFileNameWithoutExtension(lower));
                var isMouse = tokens.Contains("mouse");
                var core = tokens.Where(t => !Noise.Contains(t)).ToArray();

                foreach (var (control, alias) in Aliases(family.Value, isMouse))
                {
                    if (!alias.SequenceEqual(core)) continue;

                    var score = Score(lower, family.Value);
                    var key = (family.Value, control);
                    if (!found.TryGetValue(key, out var existing) || score > existing.score)
                        found[key] = (AssetDatabase.LoadAssetAtPath<Texture2D>(path), score);
                }
            }

            if (found.Count == 0)
            {
                EditorUtility.DisplayDialog("No icons matched",
                    "No icons in that folder matched known control names. Check it's the Kenney Input Prompts pack " +
                    "(folders/files named keyboard_, mouse_, xbox_, playstation_, switch_).", "OK");
                return;
            }

            Undo.RecordObject(icons, "Import Input Prompt Icons");
            foreach (var ((family, control), (tex, _)) in found)
            {
                var entry = icons.entries.FirstOrDefault(e => e.family == family && string.Equals(e.control, control, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    entry = new InputPromptIcons.Entry { family = family, control = control };
                    icons.entries.Add(entry);
                }
                entry.texture = tex;
            }

            icons.entries = icons.entries.OrderBy(e => e.family).ThenBy(e => e.control).ToList();
            EditorUtility.SetDirty(icons);
            AssetDatabase.SaveAssets();

            BuildSpriteAsset(icons);
            Debug.Log($"[Toolkit] Imported {found.Count} input prompt icon(s) into {IconsPath}.");
            Selection.activeObject = icons;
        }

        static readonly HashSet<string> Noise = new()
        {
            "keyboard", "mouse", "xbox", "playstation", "switch", "nintendo", "steamdeck", "generic",
            "button", "color", "outline", "default", "icon", "key", "series", "trigger", "bumper",
        };

        static string[] Tokens(string name) =>
            name.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);

        static InputDeviceFamily? FamilyFromPath(string lowerPath)
        {
            var file = Path.GetFileName(lowerPath);
            if (file.StartsWith("keyboard") || file.StartsWith("mouse")) return InputDeviceFamily.KeyboardMouse;
            if (file.StartsWith("xbox")) return InputDeviceFamily.Xbox;
            if (file.StartsWith("playstation") || file.StartsWith("ps")) return InputDeviceFamily.PlayStation;
            if (file.StartsWith("switch")) return InputDeviceFamily.Switch;
            if (file.StartsWith("generic") || file.StartsWith("controller")) return InputDeviceFamily.Gamepad;

            if (lowerPath.Contains("keyboard")) return InputDeviceFamily.KeyboardMouse;
            if (lowerPath.Contains("xbox")) return InputDeviceFamily.Xbox;
            if (lowerPath.Contains("playstation")) return InputDeviceFamily.PlayStation;
            if (lowerPath.Contains("switch")) return InputDeviceFamily.Switch;
            return null;
        }

        /// <summary>Prefers the "Default" size, coloured gamepad buttons, and non-outline versions.</summary>
        static int Score(string lowerPath, InputDeviceFamily family)
        {
            var score = 0;
            if (lowerPath.Contains("/default/")) score += 4;
            if (lowerPath.Contains("/double/")) score -= 2;
            if (lowerPath.Contains("outline")) score -= 3;
            if (family != InputDeviceFamily.KeyboardMouse && lowerPath.Contains("color")) score += 2;
            return score;
        }

        /// <summary>(control key, file-name tokens) pairs for a family.</summary>
        static IEnumerable<(string control, string[] alias)> Aliases(InputDeviceFamily family, bool isMouse)
        {
            if (family == InputDeviceFamily.KeyboardMouse)
            {
                if (isMouse)
                {
                    yield return ("Mouse/leftButton", new[] { "left" });
                    yield return ("Mouse/rightButton", new[] { "right" });
                    yield return ("Mouse/middleButton", new[] { "scroll" });
                    yield return ("Mouse/scroll", new[] { "scroll", "vertical" });
                    yield return ("Mouse/delta", new[] { "move" });
                    yield return ("Mouse/delta", Array.Empty<string>());
                    yield break;
                }

                for (var c = 'a'; c <= 'z'; c++)
                    yield return ($"Keyboard/{c}", new[] { c.ToString() });
                for (var d = 0; d <= 9; d++)
                    yield return ($"Keyboard/{d}", new[] { d.ToString() });
                for (var f = 1; f <= 12; f++)
                    yield return ($"Keyboard/f{f}", new[] { $"f{f}" });

                yield return ("Keyboard/space", new[] { "space" });
                yield return ("Keyboard/enter", new[] { "enter" });
                yield return ("Keyboard/enter", new[] { "return" });
                yield return ("Keyboard/escape", new[] { "escape" });
                yield return ("Keyboard/escape", new[] { "esc" });
                yield return ("Keyboard/tab", new[] { "tab" });
                yield return ("Keyboard/backspace", new[] { "backspace" });
                yield return ("Keyboard/leftShift", new[] { "shift" });
                yield return ("Keyboard/rightShift", new[] { "shift" });
                yield return ("Keyboard/leftCtrl", new[] { "ctrl" });
                yield return ("Keyboard/rightCtrl", new[] { "ctrl" });
                yield return ("Keyboard/leftAlt", new[] { "alt" });
                yield return ("Keyboard/rightAlt", new[] { "alt" });
                yield return ("Keyboard/capsLock", new[] { "capslock" });
                yield return ("Keyboard/capsLock", new[] { "caps", "lock" });
                yield return ("Keyboard/delete", new[] { "delete" });
                yield return ("Keyboard/upArrow", new[] { "arrow", "up" });
                yield return ("Keyboard/downArrow", new[] { "arrow", "down" });
                yield return ("Keyboard/leftArrow", new[] { "arrow", "left" });
                yield return ("Keyboard/rightArrow", new[] { "arrow", "right" });
                yield break;
            }

            // Gamepad buttons by position (south/east/west/north) differ per family.
            var (south, east, west, north) = family switch
            {
                InputDeviceFamily.PlayStation => ("cross", "circle", "square", "triangle"),
                InputDeviceFamily.Switch => ("b", "a", "y", "x"),
                _ => ("a", "b", "x", "y"),
            };
            var (lb, rb, lt, rt) = family switch
            {
                InputDeviceFamily.PlayStation => ("l1", "r1", "l2", "r2"),
                InputDeviceFamily.Switch => ("l", "r", "zl", "zr"),
                _ => ("lb", "rb", "lt", "rt"),
            };
            var (start, select) = family switch
            {
                InputDeviceFamily.PlayStation => ("options", "share"),
                InputDeviceFamily.Switch => ("plus", "minus"),
                _ => ("menu", "view"),
            };

            yield return ("Gamepad/buttonSouth", new[] { south });
            yield return ("Gamepad/buttonEast", new[] { east });
            yield return ("Gamepad/buttonWest", new[] { west });
            yield return ("Gamepad/buttonNorth", new[] { north });
            yield return ("Gamepad/leftShoulder", new[] { lb });
            yield return ("Gamepad/rightShoulder", new[] { rb });
            yield return ("Gamepad/leftTrigger", new[] { lt });
            yield return ("Gamepad/rightTrigger", new[] { rt });
            yield return ("Gamepad/start", new[] { start });
            yield return ("Gamepad/select", new[] { select });
            if (family == InputDeviceFamily.PlayStation)
                yield return ("Gamepad/select", new[] { "create" });

            yield return ("Gamepad/leftStick", new[] { "stick", "l" });
            yield return ("Gamepad/rightStick", new[] { "stick", "r" });
            yield return ("Gamepad/leftStickPress", new[] { "stick", "l", "press" });
            yield return ("Gamepad/rightStickPress", new[] { "stick", "r", "press" });
            yield return ("Gamepad/dpad", new[] { "dpad" });
            yield return ("Gamepad/dpad/up", new[] { "dpad", "up" });
            yield return ("Gamepad/dpad/down", new[] { "dpad", "down" });
            yield return ("Gamepad/dpad/left", new[] { "dpad", "left" });
            yield return ("Gamepad/dpad/right", new[] { "dpad", "right" });
        }

        // ---------------- TMP sprite asset ----------------

        /// <summary>Packs all entry textures into one atlas and (re)builds the TMP sprite asset.</summary>
        public static void BuildSpriteAsset(InputPromptIcons icons)
        {
#if TOOLKIT_UGUI
            var withTextures = icons.entries.Where(e => e.texture != null).ToList();
            var unique = withTextures.Select(e => e.texture).Distinct().ToList();
            if (unique.Count == 0)
            {
                Debug.LogWarning("[Toolkit] No icon textures to build a sprite asset from.");
                return;
            }

            var readable = unique.Select(MakeReadableCopy).ToArray();
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var uvRects = atlas.PackTextures(readable, 2, 4096, false);

            var atlasPath = $"{Folder}/InputPromptAtlas.png";
            File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
            foreach (var t in readable) UnityEngine.Object.DestroyImmediate(t);
            var atlasWidth = atlas.width;
            var atlasHeight = atlas.height;
            UnityEngine.Object.DestroyImmediate(atlas);

            AssetDatabase.ImportAsset(atlasPath);
            if (AssetImporter.GetAtPath(atlasPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var atlasTex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

            var assetPath = $"{Folder}/InputPromptSprites.asset";
            AssetDatabase.DeleteAsset(assetPath);

            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            spriteAsset.spriteSheet = atlasTex;

            var names = new Dictionary<Texture2D, string>();
            for (var i = 0; i < unique.Count; i++)
            {
                var tex = unique[i];
                var r = uvRects[i];
                var x = Mathf.RoundToInt(r.x * atlasWidth);
                var y = Mathf.RoundToInt(r.y * atlasHeight);
                var w = Mathf.RoundToInt(r.width * atlasWidth);
                var h = Mathf.RoundToInt(r.height * atlasHeight);

                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(w, h, 0, h * 0.8f, w),
                    glyphRect = new GlyphRect(x, y, w, h),
                    scale = 1f,
                    atlasIndex = 0,
                };
                spriteAsset.spriteGlyphTable.Add(glyph);

                var spriteName = SafeSpriteName(tex.name, i);
                spriteAsset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = spriteName });
                names[tex] = spriteName;
            }

            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "InputPromptSprites Material" };
            material.mainTexture = atlasTex;
            spriteAsset.material = material;

            AssetDatabase.CreateAsset(spriteAsset, assetPath);
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
            spriteAsset.UpdateLookupTables();

            foreach (var e in icons.entries)
                e.spriteName = e.texture != null && names.TryGetValue(e.texture, out var n) ? n : null;
            icons.spriteAsset = spriteAsset;

            EditorUtility.SetDirty(spriteAsset);
            EditorUtility.SetDirty(icons);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Toolkit] Built TMP sprite asset with {unique.Count} sprite(s): {assetPath}");
#else
            Debug.LogWarning("[Toolkit] TextMeshPro (com.unity.ugui) isn't installed, so no sprite asset was built. UI Toolkit prompts still work.");
#endif
        }

        static string SafeSpriteName(string name, int index)
        {
            var clean = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
            return string.IsNullOrEmpty(clean) ? $"icon_{index}" : clean;
        }

        /// <summary>GPU copy so source textures don't need Read/Write enabled.</summary>
        static Texture2D MakeReadableCopy(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }
    }

    [CustomEditor(typeof(InputPromptIcons))]
    class InputPromptIconsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Controls use 'Keyboard/<key>', 'Mouse/<button>' or 'Gamepad/<control>' (Input System control names). " +
                "After changing textures, click Rebuild Sprite Asset so TextMeshPro prompts update.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Sprite Asset"))
                    InputPromptIconsBuilder.BuildSpriteAsset((InputPromptIcons)target);
                if (GUILayout.Button("Get Kenney Icons"))
                    Application.OpenURL(InputPromptIconsBuilder.KenneyUrl);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
