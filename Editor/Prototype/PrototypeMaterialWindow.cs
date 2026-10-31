using System;
using System.IO;
using StuMillman.Toolkit.Editor.ProjectSetup;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Prototype
{
    /// <summary>
    /// Creates world-space triplanar prototype materials (grid / checker / plain).
    /// </summary>
    public class PrototypeMaterialWindow : EditorWindow
    {
        public const string ShaderName = "StuMillman/Toolkit/Prototype Triplanar";

        public enum Pattern { Grid = 0, Checker = 1, Plain = 2 }

        [Serializable]
        public struct Preset
        {
            public string name;
            public Color color;

            public Preset(string name, Color color)
            {
                this.name = name;
                this.color = color;
            }
        }

        /// <summary>The classic blockout colours.</summary>
        public static readonly Preset[] DefaultSet =
        {
            new("Floor", new Color(0.25f, 0.25f, 0.27f)),
            new("Wall", new Color(0.62f, 0.62f, 0.64f)),
            new("Interactive", new Color(0.95f, 0.5f, 0.12f)),
            new("Water", new Color(0.2f, 0.45f, 0.85f)),
            new("Vegetation", new Color(0.3f, 0.62f, 0.25f)),
            new("Danger", new Color(0.85f, 0.2f, 0.2f)),
            new("Special", new Color(0.55f, 0.3f, 0.8f)),
        };

        string _name = "Wall";
        Color _color = DefaultSet[1].color;
        Pattern _pattern = Pattern.Grid;
        float _tileSize = 1f;
        float _patternStrength = 1f;
        float _lineWidth = 0.015f;
        bool _minorLines = true;
        float _smoothness = 0.2f;

        [MenuItem("Tools/Toolkit/Prototype Materials", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<PrototypeMaterialWindow>("Prototype Materials");
            window.minSize = new Vector2(320, 360);
        }

        static string Folder
        {
            get
            {
                var s = ProjectSetupSettings.instance;
                return $"Assets/{s.rootFolder}/{s.prototypeMaterialsFolder}".Replace('\\', '/');
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            DrawPresetSwatches();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            _name = EditorGUILayout.TextField("Name", _name);
            _color = EditorGUILayout.ColorField("Color", _color);
            _pattern = (Pattern)EditorGUILayout.EnumPopup("Pattern", _pattern);
            _tileSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Tile Size (m)", _tileSize));

            using (new EditorGUI.DisabledScope(_pattern == Pattern.Plain))
                _patternStrength = EditorGUILayout.Slider("Pattern Strength", _patternStrength, 0f, 1f);

            using (new EditorGUI.DisabledScope(_pattern != Pattern.Grid))
            {
                _lineWidth = EditorGUILayout.Slider("Line Width", _lineWidth, 0.001f, 0.1f);
                _minorLines = EditorGUILayout.Toggle("Minor Lines", _minorLines);
            }

            _smoothness = EditorGUILayout.Slider("Smoothness", _smoothness, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Saved to {Folder}/M_Proto_{SafeName(_name)}.mat", MessageType.None);

            if (GUILayout.Button("Create Material", GUILayout.Height(26)))
            {
                var mat = Create(_name, _color, _pattern, _tileSize, _patternStrength, _lineWidth, _minorLines, _smoothness, overwrite: true);
                if (mat != null)
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Create Default Set"))
                CreateDefaultSet();
        }

        void DrawPresetSwatches()
        {
            const float size = 36f;
            var perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 12f) / (size + 4f)));

            for (var i = 0; i < DefaultSet.Length; i += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var j = i; j < Mathf.Min(i + perRow, DefaultSet.Length); j++)
                    {
                        var preset = DefaultSet[j];
                        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
                        EditorGUI.DrawRect(rect, preset.color);
                        if (GUI.Button(rect, new GUIContent(string.Empty, preset.name), GUIStyle.none))
                        {
                            _name = preset.name;
                            _color = preset.color;
                            GUI.FocusControl(null);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        /// <summary>Creates one grid material per default colour. Existing materials are kept.</summary>
        public static void CreateDefaultSet()
        {
            var created = 0;
            foreach (var preset in DefaultSet)
            {
                if (Create(preset.name, preset.color, Pattern.Grid, 1f, 1f, 0.015f, true, 0.2f, overwrite: false) != null)
                    created++;
            }

            Debug.Log($"[Toolkit] Created {created} prototype material(s) in {Folder}.");
        }

        /// <summary>Creates (or updates) a prototype material. Returns null if skipped or failed.</summary>
        public static Material Create(string name, Color color, Pattern pattern, float tileSize, float patternStrength,
            float lineWidth, bool minorLines, float smoothness, bool overwrite)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Toolkit] Shader '{ShaderName}' not found. Is URP installed?");
                return null;
            }

            var folder = Folder;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            var path = $"{folder}/M_Proto_{SafeName(name)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = mat == null;
            if (!isNew && !overwrite)
                return null;

            if (isNew)
                mat = new Material(shader);
            else
                mat.shader = shader;

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Pattern", (float)pattern);
            mat.SetFloat("_TileSize", tileSize);
            mat.SetFloat("_PatternStrength", patternStrength);
            mat.SetFloat("_LineWidth", lineWidth);
            mat.SetFloat("_MinorLines", minorLines ? 1f : 0f);
            mat.SetFloat("_Smoothness", smoothness);

            if (isNew)
                AssetDatabase.CreateAsset(mat, path);
            else
                EditorUtility.SetDirty(mat);

            AssetDatabase.SaveAssets();
            return mat;
        }

        static string SafeName(string name)
        {
            var id = NameUtil.ToIdentifier(name, "Material");
            return id.TrimStart('_');
        }
    }
}
