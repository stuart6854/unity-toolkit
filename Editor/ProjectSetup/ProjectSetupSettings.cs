using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    /// <summary>
    /// Per-project settings for the Project Setup window.
    /// Saved to ProjectSettings/ so they can be committed with the project.
    /// </summary>
    [FilePath("ProjectSettings/StuMillmanToolkit/ProjectSetupSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ProjectSetupSettings : ScriptableSingleton<ProjectSetupSettings>
    {
        [Serializable]
        public class SceneEntry
        {
            public string name;
            public bool cameraAndLight = true;
            public bool organisers = true;
            public bool globalVolume = true;
            public bool testGeometry = true;

            public SceneEntry(string name) => this.name = name;
        }

        [Serializable]
        public class MaterialEntry
        {
            public string name;
            public Color color = Color.white;

            public MaterialEntry(string name, Color color)
            {
                this.name = name;
                this.color = color;
            }
        }

        [Tooltip("Root folder created under Assets/.")]
        public string rootFolder;

        [Tooltip("Folders created under the root folder. Use '/' for nested folders.")]
        public List<string> folders = new();

        [Tooltip("Add an empty .gitkeep file to each folder so git keeps empty folders.")]
        public bool addGitKeep;

        [Tooltip("Folder (relative to the root folder) where default materials are created.")]
        public string materialsFolder;

        public List<MaterialEntry> materials = new();

        [Tooltip("Package IDs to install, e.g. com.unity.cinemachine or com.unity.cinemachine@3.1.2.")]
        public List<string> packages = new();

        [Tooltip("Packages and built-in modules to remove, e.g. com.unity.collab-proxy or com.unity.modules.cloth.")]
        public List<string> packagesToRemove = new(DefaultPackagesToRemove);

        [Tooltip("Folder (relative to the root folder) where prototype materials are created.")]
        public string prototypeMaterialsFolder = "Art/Materials/Prototype";

        [Tooltip("Folder (relative to the root folder) where utility textures are created.")]
        public string texturesFolder = "Art/Textures/Common";

        [Tooltip("2x2 white, black, grey and flat normal.")]
        public bool solidTextures = true;

        [Tooltip("White noise, RGB noise and tileable Perlin noise.")]
        public bool noiseTextures = true;

        [Tooltip("Size of the noise textures in pixels.")]
        public int noiseSize = 256;

        [Tooltip("Seed for the noise textures. Change it to get different noise.")]
        public int noiseSeed = 12345;

        [Tooltip("Tileable 1m grid, checker and UV test textures.")]
        public bool prototypeTextures = true;

        [Tooltip("Horizontal, vertical and radial gradients.")]
        public bool gradientTextures = true;

        [Tooltip("Folder (relative to the root folder) where scenes are created.")]
        public string scenesFolder = "Scenes";

        [Tooltip("Scenes to create. The first one is opened and placed first in Build Settings.")]
        public List<SceneEntry> scenes = DefaultScenes();

        [Tooltip("Empty header objects added to each scene, shown as '--- Name ---'.")]
        public List<string> organiserNames = new(DefaultOrganisers);

        [Tooltip("Delete Assets/Scenes/SampleScene.unity and remove it from Build Settings.")]
        public bool deleteSampleScene = true;

        [Tooltip("Company name. Leave empty to keep the current Player Settings value.")]
        public string companyName;

        [Tooltip("Product name. Also used for the bundle ID, assembly names and root namespace.")]
        public string productName;

        public bool setCompanyAndProduct = true;
        public bool setLinearColorSpace = true;

        [Tooltip("Disables domain and scene reload on Play. Static fields are not reset between plays.")]
        public bool setFastEnterPlayMode = true;

        [Tooltip("Sets Active Input Handling to the Input System package only. Needs an editor restart.")]
        public bool setInputSystemOnly = true;

        [Tooltip("Overwrite existing .gitignore / .gitattributes in the project root.")]
        public bool overwriteGitFiles;

        [Tooltip("Scripts folder (relative to the root folder). Runtime and Editor asmdefs go in its Runtime/ and Editor/ subfolders.")]
        public string scriptsFolder = "Scripts";

        [Tooltip("Assemblies referenced by the project's asmdefs. Missing ones are ignored by Unity.")]
        public List<string> asmdefReferences = new(DefaultAsmdefReferences);

        static readonly string[] DefaultAsmdefReferences =
        {
            "Unity.InputSystem",
            "Unity.Cinemachine",
            "Unity.AI.Navigation",
            "Unity.RenderPipelines.Core.Runtime",
            "Unity.RenderPipelines.Universal.Runtime",
            "UnityEngine.UI",
            "Unity.TextMeshPro",
        };

        static List<SceneEntry> DefaultScenes() => new()
        {
            new SceneEntry("Bootstrap"),
            new SceneEntry("MainMenu"),
            new SceneEntry("Game"),
        };

        static readonly string[] DefaultOrganisers = { "Managers", "Environment", "UI", "Dynamic" };

        static readonly string[] DefaultPackagesToRemove =
        {
            // Older Unity 6 template packages
            "com.unity.collab-proxy",
            "com.unity.visualscripting",
            "com.unity.multiplayer.center",
            "com.unity.modules.cloth",
            "com.unity.modules.vehicles",
            "com.unity.modules.vr",
            "com.unity.modules.wind",
            "com.unity.modules.umbra",
            // 2D
            "com.unity.modules.physics2d",
            "com.unity.modules.physicscore2d",
            "com.unity.modules.tilemap",
            "com.unity.modules.vectorgraphics",
            // Platform / services
            "com.unity.modules.adaptiveperformance",
            "com.unity.modules.androidjni",
            "com.unity.modules.unityanalytics",
            "com.unity.modules.xr",
            // Media / content
            "com.unity.modules.video",
            "com.unity.modules.assetbundle",
            "com.unity.modules.unitywebrequestassetbundle",
            "com.unity.modules.unitywebrequestaudio",
            "com.unity.modules.unitywebrequesttexture",
            "com.unity.modules.unitywebrequestwww",
            // Terrain
            "com.unity.modules.terrain",
            "com.unity.modules.terrainphysics",
            "com.unity.modules.tetgen",
        };

        const int CurrentSettingsVersion = 3;

        [SerializeField, HideInInspector] int settingsVersion;

        /// <summary>
        /// Adds new default entries to settings saved by an older toolkit version.
        /// Keeps the user's own entries.
        /// </summary>
        public void Migrate()
        {
            if (settingsVersion >= CurrentSettingsVersion)
                return;

            packagesToRemove ??= new List<string>();

            if (settingsVersion < 2)
            {
                foreach (var id in DefaultPackagesToRemove)
                    if (!packagesToRemove.Contains(id))
                        packagesToRemove.Add(id);
            }

            if (settingsVersion < 3)
            {
                // The screenshot tool needs this module.
                packagesToRemove.Remove("com.unity.modules.screencapture");
                if (string.IsNullOrEmpty(companyName)) companyName = PlayerSettings.companyName;
                if (string.IsNullOrEmpty(productName)) productName = PlayerSettings.productName;
            }

            settingsVersion = CurrentSettingsVersion;
            SaveSettings();
        }

        void OnEnable()
        {
            // A fresh instance (no saved file yet) gets the defaults.
            if (string.IsNullOrEmpty(rootFolder))
                ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            rootFolder = "_Project";
            folders = new List<string>
            {
                "Art/Materials",
                "Art/Models",
                "Art/Textures",
                "Art/Animations",
                "Audio",
                "Prefabs",
                "Scenes",
                "Scripts/Runtime",
                "Scripts/Editor",
                "ScriptableObjects",
                "Settings",
                "UI",
            };
            addGitKeep = true;
            materialsFolder = "Art/Materials/Defaults";
            materials = new List<MaterialEntry>
            {
                new("M_White", Color.white),
                new("M_LightGrey", new Color(0.75f, 0.75f, 0.75f)),
                new("M_Grey", new Color(0.5f, 0.5f, 0.5f)),
                new("M_DarkGrey", new Color(0.25f, 0.25f, 0.25f)),
                new("M_Black", new Color(0.05f, 0.05f, 0.05f)),
                new("M_Red", new Color(0.85f, 0.2f, 0.2f)),
                new("M_Green", new Color(0.3f, 0.75f, 0.3f)),
                new("M_Blue", new Color(0.25f, 0.45f, 0.9f)),
                new("M_Yellow", new Color(0.95f, 0.8f, 0.2f)),
            };
            packages = new List<string>
            {
                "com.unity.inputsystem",
                "com.unity.cinemachine",
                "com.unity.ai.navigation",
            };
            packagesToRemove = new List<string>(DefaultPackagesToRemove);
            prototypeMaterialsFolder = "Art/Materials/Prototype";
            texturesFolder = "Art/Textures/Common";
            solidTextures = true;
            noiseTextures = true;
            noiseSize = 256;
            noiseSeed = 12345;
            prototypeTextures = true;
            gradientTextures = true;
            scenesFolder = "Scenes";
            scenes = DefaultScenes();
            organiserNames = new List<string>(DefaultOrganisers);
            deleteSampleScene = true;
            companyName = PlayerSettings.companyName;
            productName = PlayerSettings.productName;
            setCompanyAndProduct = true;
            setLinearColorSpace = true;
            setFastEnterPlayMode = true;
            setInputSystemOnly = true;
            overwriteGitFiles = false;
            scriptsFolder = "Scripts";
            asmdefReferences = new List<string>(DefaultAsmdefReferences);
            settingsVersion = CurrentSettingsVersion;
        }

        public void SaveSettings() => Save(true);
    }
}
