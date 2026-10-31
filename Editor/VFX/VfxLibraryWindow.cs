using System.Collections.Generic;
using System.IO;
using System.Linq;
using StuMillman.Toolkit.Editor.ProjectSetup;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.VFX
{
    /// <summary>Tools > Toolkit > VFX Library: creates starter particle effect prefabs.</summary>
    public class VfxLibraryWindow : EditorWindow
    {
        readonly HashSet<string> _selected = new(VfxLibrary.All.Select(e => e.Name));
        bool _useTint;
        Color _tint = Color.white;
        bool _overwrite;
        Vector2 _scroll;

        [MenuItem("Tools/Toolkit/VFX Library", priority = 2)]
        public static void Open()
        {
            var window = GetWindow<VfxLibraryWindow>("VFX Library");
            window.minSize = new Vector2(340, 420);
        }

        static string Folder
        {
            get
            {
                var s = ProjectSetupSettings.instance;
                return $"Assets/{s.rootFolder}/{s.vfxFolder}".Replace('\\', '/');
            }
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var group in VfxLibrary.All.GroupBy(e => e.Category))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                foreach (var effect in group)
                {
                    var on = _selected.Contains(effect.Name);
                    var exists = File.Exists(PathFor(effect));
                    var label = exists ? $"{effect.Name}  (exists)" : effect.Name;
                    var now = EditorGUILayout.ToggleLeft(new GUIContent(label, effect.Description), on);
                    if (now != on)
                    {
                        if (now) _selected.Add(effect.Name);
                        else _selected.Remove(effect.Name);
                    }
                    EditorGUILayout.LabelField(effect.Description, EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All")) _selected.UnionWith(VfxLibrary.All.Select(e => e.Name));
                if (GUILayout.Button("Select None")) _selected.Clear();
            }

            _useTint = EditorGUILayout.Toggle(new GUIContent("Tint", "Multiplies each effect's colours, e.g. blue sparks or green fire."), _useTint);
            using (new EditorGUI.DisabledScope(!_useTint))
                _tint = EditorGUILayout.ColorField("Tint Colour", _tint);
            _overwrite = EditorGUILayout.Toggle("Overwrite Existing", _overwrite);

            EditorGUILayout.HelpBox($"Prefabs go to {Folder}. Particle textures and materials are created if missing.", MessageType.None);

            using (new EditorGUI.DisabledScope(_selected.Count == 0))
            {
                if (GUILayout.Button($"Create {_selected.Count} Effect(s)", GUILayout.Height(28)))
                    Create(VfxLibrary.All.Where(e => _selected.Contains(e.Name)), _useTint, _tint, _overwrite);
            }
        }

        static string PathFor(VfxLibrary.Effect effect) => $"{Folder}/VFX_{effect.Name}.prefab";

        /// <summary>Builds the effects and saves them as prefabs.</summary>
        public static void Create(IEnumerable<VfxLibrary.Effect> effects, bool useTint, Color tint, bool overwrite)
        {
            var mats = ParticleMaterials.EnsureAll(ProjectSetupSettings.instance);
            if (mats == null) return;

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var context = new VfxLibrary.Context { Mats = mats, UseTint = useTint, Tint = tint };
            var created = 0;
            GameObject last = null;

            foreach (var effect in effects)
            {
                var path = PathFor(effect);
                if (File.Exists(path) && !overwrite) continue;

                var go = effect.Build(context);
                last = PrefabUtility.SaveAsPrefabAsset(go, path);
                DestroyImmediate(go);
                created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Toolkit] Created {created} VFX prefab(s) in {Folder}.");
            if (last != null) EditorGUIUtility.PingObject(last);
        }
    }
}
