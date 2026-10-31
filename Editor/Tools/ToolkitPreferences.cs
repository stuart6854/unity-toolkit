using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>Edit > Preferences > Toolkit. Per-user settings stored in EditorPrefs.</summary>
    static class ToolkitPreferences
    {
        [SettingsProvider]
        static SettingsProvider Create() => new("Preferences/Toolkit", SettingsScope.User)
        {
            label = "Toolkit",
            keywords = new[] { "auto save", "autosave", "play mode", "tint", "bootstrap", "hierarchy", "header" },
            guiHandler = _ => DrawGUI(),
        };

        static void DrawGUI()
        {
            EditorGUIUtility.labelWidth = 220f;

            Header("Auto-save");
            AutoSave.TimerEnabled = EditorGUILayout.Toggle("Save every N minutes", AutoSave.TimerEnabled);
            using (new EditorGUI.DisabledScope(!AutoSave.TimerEnabled))
            using (new EditorGUI.IndentLevelScope())
                AutoSave.IntervalMinutes = EditorGUILayout.IntSlider("Interval (minutes)", AutoSave.IntervalMinutes, 1, 60);
            AutoSave.BeforePlay = EditorGUILayout.Toggle("Save before entering Play Mode", AutoSave.BeforePlay);
            AutoSave.OnFocusLost = EditorGUILayout.Toggle("Save when the editor loses focus", AutoSave.OnFocusLost);
            AutoSave.LogSaves = EditorGUILayout.Toggle("Log saves to the Console", AutoSave.LogSaves);
            EditorGUILayout.HelpBox("Untitled scenes are never auto-saved.", MessageType.None);

            Header("Play Mode tint");
            PlayModeTint.Enabled = EditorGUILayout.Toggle("Tint editor in Play Mode", PlayModeTint.Enabled);
            using (new EditorGUI.DisabledScope(!PlayModeTint.Enabled))
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var tint = EditorGUILayout.ColorField("Colour", PlayModeTint.Tint);
                    if (tint != PlayModeTint.Tint)
                        PlayModeTint.Tint = tint;
                    if (GUILayout.Button("Reset", GUILayout.Width(60)))
                        PlayModeTint.Tint = PlayModeTint.DefaultTint;
                }
            }

            Header("Other tools");
            BootstrapPlayModeEditor.Enabled = EditorGUILayout.Toggle("Bootstrap Play Mode", BootstrapPlayModeEditor.Enabled);
            HierarchyHeaders.Enabled = EditorGUILayout.Toggle("Hierarchy headers", HierarchyHeaders.Enabled);
        }

        static void Header(string text)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }
    }
}
