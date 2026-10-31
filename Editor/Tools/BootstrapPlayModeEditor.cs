using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// When enabled, pressing Play always starts in the first Build Settings scene (Bootstrap),
    /// then loads the scene(s) you were editing.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayModeEditor
    {
        const string MenuPath = "Tools/Toolkit/Bootstrap Play Mode";
        const string ScenesKey = BootstrapPlayMode.ScenesKey;
        static string PrefKey => "StuMillman.Toolkit.BootstrapPlayMode.Enabled." + Application.dataPath.GetHashCode();

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set
            {
                if (value == Enabled) return;
                EditorPrefs.SetBool(PrefKey, value);
                UpdateStartScene();
            }
        }

        static BootstrapPlayModeEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += UpdateStartScene;
        }

        [MenuItem(MenuPath, priority = 20)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static string BootstrapPath => EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path;

        static void UpdateStartScene()
        {
            var path = BootstrapPath;
            EditorSceneManager.playModeStartScene = Enabled && !string.IsNullOrEmpty(path)
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(path)
                : null;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    UpdateStartScene();
                    RememberEditedScenes();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    LoadEditedScenes();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    SessionState.EraseString(ScenesKey);
                    break;
            }
        }

        static void RememberEditedScenes()
        {
            SessionState.EraseString(ScenesKey);
            if (EditorSceneManager.playModeStartScene == null)
                return;

            var bootstrap = BootstrapPath;
            var active = SceneManager.GetActiveScene().path;

            // Active scene first, then any other loaded scenes. Unsaved scenes and Bootstrap are skipped.
            var paths = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(s => s.isLoaded && !string.IsNullOrEmpty(s.path) && s.path != bootstrap)
                .Select(s => s.path)
                .OrderBy(p => p == active ? 0 : 1)
                .ToArray();

            if (paths.Length > 0)
                SessionState.SetString(ScenesKey, string.Join("|", paths));
        }

        static void LoadEditedScenes()
        {
            var saved = SessionState.GetString(ScenesKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
                return;

            var paths = saved.Split('|');
            for (var i = 0; i < paths.Length; i++)
            {
                var mode = i == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive;
                EditorSceneManager.LoadSceneInPlayMode(paths[i], new LoadSceneParameters(mode));
            }
        }
    }
}
