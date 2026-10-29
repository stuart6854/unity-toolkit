using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
#endif

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Main toolbar dropdown (Unity 6.3+) listing Build Settings scenes.
    /// Older versions use Tools > Toolkit > Switch Scene... instead.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSwitcher
    {
        const string ElementPath = "Toolkit/Scene Switcher";

        static SceneSwitcher()
        {
#if UNITY_6000_3_OR_NEWER
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) => RefreshToolbar();
            EditorSceneManager.sceneOpened += (_, _) => RefreshToolbar();
            EditorBuildSettings.sceneListChanged += RefreshToolbar;
            EditorApplication.playModeStateChanged += _ => RefreshToolbar();
#endif
        }

#if UNITY_6000_3_OR_NEWER
        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateToolbarElement()
        {
            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            var name = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(name)) name = "Untitled";

            var content = new MainToolbarContent(name, icon, "Switch scene (Build Settings)");
            return new MainToolbarDropdown(content, rect => BuildMenu().DropDown(rect));
        }

        static void RefreshToolbar() => MainToolbar.Refresh(ElementPath);
#endif

        [MenuItem("Tools/Toolkit/Switch Scene...", priority = 22)]
        static void ShowMenu() => BuildMenu().ShowAsContext();

        static GenericMenu BuildMenu()
        {
            var menu = new GenericMenu();
            var activePath = SceneManager.GetActiveScene().path;
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
                return menu;
            }

            for (var i = 0; i < scenes.Length; i++)
            {
                var path = scenes[i].path;
                var label = $"{i}: {Path.GetFileNameWithoutExtension(path)}";
                if (!scenes[i].enabled) label += " (disabled)";

                if (EditorApplication.isPlaying || !File.Exists(path))
                    menu.AddDisabledItem(new GUIContent(label), path == activePath);
                else
                    menu.AddItem(new GUIContent(label), path == activePath, () => Open(path));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Build Profiles..."), false,
                () => EditorApplication.ExecuteMenuItem("File/Build Profiles"));
            return menu;
        }

        static void Open(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
