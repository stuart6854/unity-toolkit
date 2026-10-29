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

        [Tooltip("Folder (relative to the root folder) where scenes are created.")]
        public string scenesFolder = "Scenes";

        [Tooltip("Scenes to create. The first one is opened and placed first in Build Settings.")]
        public List<SceneEntry> scenes = DefaultScenes();

        [Tooltip("Empty header objects added to each scene, shown as '--- Name ---'.")]
        public List<string> organiserNames = new(DefaultOrganisers);

        [Tooltip("Delete Assets/Scenes/SampleScene.unity and remove it from Build Settings.")]
        public bool deleteSampleScene = true;

        static List<SceneEntry> DefaultScenes() => new()
        {
            new SceneEntry("Bootstrap"),
            new SceneEntry("MainMenu"),
            new SceneEntry("Game"),
        };

        static readonly string[] DefaultOrganisers = { "Managers", "Environment", "UI", "Dynamic" };

        static readonly string[] DefaultPackagesToRemove =
        {
            "com.unity.collab-proxy",
            "com.unity.visualscripting",
            "com.unity.multiplayer.center",
            "com.unity.modules.cloth",
            "com.unity.modules.vehicles",
            "com.unity.modules.vr",
            "com.unity.modules.wind",
            "com.unity.modules.umbra",
        };

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
            scenesFolder = "Scenes";
            scenes = DefaultScenes();
            organiserNames = new List<string>(DefaultOrganisers);
            deleteSampleScene = true;
        }

        public void SaveSettings() => Save(true);
    }
}
