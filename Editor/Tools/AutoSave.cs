using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Saves open scenes and assets on a timer, before entering Play Mode and when the editor loses focus.
    /// Untitled scenes are skipped so no Save dialog appears.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSave
    {
        const string Prefix = "StuMillman.Toolkit.AutoSave.";

        static double _nextSave;

        public static bool TimerEnabled
        {
            get => EditorPrefs.GetBool(Prefix + "Timer", true);
            set { if (value == TimerEnabled) return; EditorPrefs.SetBool(Prefix + "Timer", value); ResetTimer(); }
        }

        public static int IntervalMinutes
        {
            get => Mathf.Max(1, EditorPrefs.GetInt(Prefix + "Interval", 5));
            set { if (value == IntervalMinutes) return; EditorPrefs.SetInt(Prefix + "Interval", Mathf.Max(1, value)); ResetTimer(); }
        }

        public static bool BeforePlay
        {
            get => EditorPrefs.GetBool(Prefix + "BeforePlay", true);
            set => EditorPrefs.SetBool(Prefix + "BeforePlay", value);
        }

        public static bool OnFocusLost
        {
            get => EditorPrefs.GetBool(Prefix + "FocusLost", true);
            set => EditorPrefs.SetBool(Prefix + "FocusLost", value);
        }

        public static bool LogSaves
        {
            get => EditorPrefs.GetBool(Prefix + "Log", false);
            set => EditorPrefs.SetBool(Prefix + "Log", value);
        }

        static AutoSave()
        {
            ResetTimer();
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.focusChanged += OnFocusChanged;
        }

        static void ResetTimer() => _nextSave = EditorApplication.timeSinceStartup + IntervalMinutes * 60.0;

        static void Tick()
        {
            if (!TimerEnabled || EditorApplication.timeSinceStartup < _nextSave)
                return;

            ResetTimer();
            Save("timer");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && BeforePlay)
                Save("entering Play Mode");
            else if (state == PlayModeStateChange.EnteredEditMode)
                ResetTimer();
        }

        static void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus && OnFocusLost)
                Save("focus lost");
        }

        /// <summary>Saves dirty, already-saved scenes and all modified assets.</summary>
        public static void Save(string reason)
        {
            // ExitingEditMode is fine; anything later in play mode is not.
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            var savedScenes = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty && !string.IsNullOrEmpty(scene.path) &&
                    EditorSceneManager.SaveScene(scene))
                    savedScenes++;
            }

            AssetDatabase.SaveAssets();

            if (LogSaves)
                Debug.Log($"[Toolkit] Auto-saved ({reason}): {savedScenes} scene(s) and modified assets.");
        }
    }
}
