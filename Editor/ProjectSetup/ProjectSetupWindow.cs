using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public class ProjectSetupWindow : EditorWindow
    {
        SerializedObject _so;
        Vector2 _scroll;

        bool _doFolders = true;
        bool _doMaterials = true;
        bool _doScenes = true;
        bool _doPackages = true;

        [MenuItem("Tools/Toolkit/Project Setup", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ProjectSetupWindow>("Project Setup");
            window.minSize = new Vector2(380, 420);
        }

        void OnEnable()
        {
            var settings = ProjectSetupSettings.instance;
            settings.hideFlags &= ~HideFlags.NotEditable;
            _so = new SerializedObject(settings);
        }

        void OnGUI()
        {
            var settings = ProjectSetupSettings.instance;
            _so.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            Section("Folders", ref _doFolders, () =>
            {
                Prop("rootFolder");
                Prop("folders");
                Prop("addGitKeep");
                if (GUILayout.Button("Create Folders"))
                    FolderSetup.CreateFolders(settings);
            });

            Section("Default Materials (URP)", ref _doMaterials, () =>
            {
                Prop("materialsFolder");
                Prop("materials");
                if (GUILayout.Button("Create Materials"))
                    MaterialSetup.CreateMaterials(settings);
            });

            Section("Scenes", ref _doScenes, () =>
            {
                Prop("scenesFolder");
                Prop("scenes");
                Prop("organiserNames");
                Prop("deleteSampleScene");
                if (GUILayout.Button("Create Scenes"))
                    SceneSetup.CreateScenes(settings);
            });

            Section("Packages", ref _doPackages, () =>
            {
                Prop("packages");
                Prop("packagesToRemove");
                using (new EditorGUI.DisabledScope(PackageInstaller.IsBusy))
                {
                    if (GUILayout.Button(PackageInstaller.IsBusy ? "Applying..." : "Apply Package Changes"))
                        PackageInstaller.Apply(settings.packages, settings.packagesToRemove);
                }
            });

            EditorGUILayout.EndScrollView();

            if (_so.ApplyModifiedProperties())
                settings.SaveSettings();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to Defaults"))
                {
                    settings.ResetToDefaults();
                    settings.SaveSettings();
                    _so.Update();
                }

                using (new EditorGUI.DisabledScope(PackageInstaller.IsBusy))
                {
                    if (GUILayout.Button("Run Selected", GUILayout.Height(28)))
                        RunSelected(settings);
                }
            }
        }

        void RunSelected(ProjectSetupSettings settings)
        {
            if (_doFolders) FolderSetup.CreateFolders(settings);
            if (_doMaterials) MaterialSetup.CreateMaterials(settings);
            if (_doScenes) SceneSetup.CreateScenes(settings);
            // Packages last: installing triggers a domain reload.
            if (_doPackages) PackageInstaller.Apply(settings.packages, settings.packagesToRemove);
        }

        void Section(string title, ref bool enabled, System.Action body)
        {
            EditorGUILayout.Space();
            enabled = EditorGUILayout.ToggleLeft(title, enabled, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
                body();
        }

        void Prop(string name) => EditorGUILayout.PropertyField(_so.FindProperty(name), true);
    }
}
