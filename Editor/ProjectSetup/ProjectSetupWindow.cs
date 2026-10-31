using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public class ProjectSetupWindow : EditorWindow
    {
        SerializedObject _so;
        Vector2 _scroll;

        bool _doFolders = true;
        bool _doGitFiles = true;
        bool _doProjectSettings = true;
        bool _doAssemblies = true;
        bool _doMaterials = true;
        bool _doTextures = true;
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
            settings.Migrate();
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

            Section("Git Files (.gitignore / .gitattributes)", ref _doGitFiles, () =>
            {
                Prop("overwriteGitFiles");
                if (GUILayout.Button("Create Git Files"))
                    GitFilesSetup.Create(settings);
            });

            Section("Default Materials (URP)", ref _doMaterials, () =>
            {
                Prop("materialsFolder");
                Prop("materials");
                Prop("prototypeMaterialsFolder");
                if (GUILayout.Button("Create Materials"))
                {
                    MaterialSetup.CreateMaterials(settings);
                    Prototype.PrototypeMaterialWindow.CreateDefaultSet();
                }
                if (GUILayout.Button("Open Prototype Material Generator"))
                    Prototype.PrototypeMaterialWindow.Open();
            });

            Section("Utility Textures", ref _doTextures, () =>
            {
                Prop("texturesFolder");
                Prop("solidTextures");
                Prop("noiseTextures");
                Prop("noiseSize");
                Prop("noiseSeed");
                Prop("prototypeTextures");
                Prop("gradientTextures");
                if (GUILayout.Button("Create Textures"))
                    TextureSetup.CreateTextures(settings);
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

            Section("Project Settings", ref _doProjectSettings, () =>
            {
                Prop("companyName");
                Prop("productName");
                Prop("setCompanyAndProduct");
                Prop("setLinearColorSpace");
                Prop("setFastEnterPlayMode");
                Prop("setInputSystemOnly");
                if (GUILayout.Button("Apply Project Settings"))
                {
                    ProjectSettingsSetup.Apply(settings);
                    ProjectSettingsSetup.PromptRestartIfPending();
                }
            });

            Section("Assembly Definitions", ref _doAssemblies, () =>
            {
                Prop("scriptsFolder");
                Prop("asmdefReferences");
                var root = AssemblySetup.RootName(settings);
                EditorGUILayout.HelpBox($"Creates {root}.Runtime and {root}.Editor (namespace {root}).", MessageType.None);
                if (GUILayout.Button("Create Assembly Definitions"))
                    AssemblySetup.Create(settings);
            });

            Section("Packages", ref _doPackages, () =>
            {
                Prop("packages");
                DrawOptionalPackages();
                Prop("packagesToRemove");
                using (new EditorGUI.DisabledScope(PackageInstaller.IsBusy))
                {
                    if (GUILayout.Button(PackageInstaller.IsBusy ? "Applying..." : "Apply Package Changes"))
                        ApplyPackages(settings);
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
            if (_doGitFiles) GitFilesSetup.Create(settings);
            if (_doMaterials)
            {
                MaterialSetup.CreateMaterials(settings);
                Prototype.PrototypeMaterialWindow.CreateDefaultSet();
            }
            if (_doTextures) TextureSetup.CreateTextures(settings);
            if (_doScenes) SceneSetup.CreateScenes(settings);
            if (_doProjectSettings) ProjectSettingsSetup.Apply(settings);
            if (_doAssemblies) AssemblySetup.Create(settings);
            // Packages last: installing triggers a domain reload.
            if (_doPackages) ApplyPackages(settings);
            // Restart prompt waits until packages finish (or shows now if nothing is running).
            ProjectSettingsSetup.PromptRestartIfPending();
        }

        static void ApplyPackages(ProjectSetupSettings settings)
        {
            OptionalPackages.EnsureRegistries(settings);
            PackageInstaller.Apply(
                settings.packages.Concat(OptionalPackages.EnabledIds(settings)),
                settings.packagesToRemove);
            OptionalPackages.RemindManualImports(settings);
        }

        bool _editOptional;

        void DrawOptionalPackages()
        {
            EditorGUILayout.LabelField("Optional Packages", EditorStyles.miniBoldLabel);
            var list = _so.FindProperty("optionalPackages");

            for (var i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var enabled = entry.FindPropertyRelative("enabled");
                var name = entry.FindPropertyRelative("displayName").stringValue;
                var description = entry.FindPropertyRelative("description").stringValue;
                var url = entry.FindPropertyRelative("assetStoreUrl").stringValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    enabled.boolValue = EditorGUILayout.ToggleLeft(name, enabled.boolValue, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(url) && GUILayout.Button("Asset Store", EditorStyles.miniButton, GUILayout.Width(80)))
                        Application.OpenURL(url);
                }

                if (!string.IsNullOrEmpty(description))
                    EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            }

            _editOptional = EditorGUILayout.Foldout(_editOptional, "Edit optional package list", true);
            if (_editOptional)
                EditorGUILayout.PropertyField(list, true);
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
