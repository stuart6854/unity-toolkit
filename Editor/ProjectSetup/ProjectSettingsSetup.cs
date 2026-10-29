using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    [InitializeOnLoad]
    public static class ProjectSettingsSetup
    {
        const string RestartPendingKey = "StuMillman.Toolkit.RestartPending";

        static ProjectSettingsSetup()
        {
            // After a domain reload (e.g. packages installed), ask about any pending restart.
            EditorApplication.delayCall += PromptRestartIfPending;
        }

        public static void Apply(ProjectSetupSettings settings)
        {
            if (settings.setCompanyAndProduct)
            {
                if (!string.IsNullOrWhiteSpace(settings.companyName))
                    PlayerSettings.companyName = settings.companyName.Trim();
                if (!string.IsNullOrWhiteSpace(settings.productName))
                    PlayerSettings.productName = settings.productName.Trim();

                var id = $"com.{NameUtil.ToBundlePart(PlayerSettings.companyName)}.{NameUtil.ToBundlePart(PlayerSettings.productName)}";
                foreach (var target in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS })
                    PlayerSettings.SetApplicationIdentifier(target, id);

                Debug.Log($"[Toolkit] Company '{PlayerSettings.companyName}', product '{PlayerSettings.productName}', bundle ID '{id}'.");
            }

            if (settings.setLinearColorSpace && PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Debug.Log("[Toolkit] Color space set to Linear.");
            }

            if (settings.setFastEnterPlayMode)
            {
                // Older Unity 6 versions also need the "enabled" flag; newer ones removed it.
                typeof(EditorSettings)
                    .GetProperty("enterPlayModeOptionsEnabled", BindingFlags.Public | BindingFlags.Static)
                    ?.SetValue(null, true);
                EditorSettings.enterPlayModeOptions =
                    EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
                Debug.Log("[Toolkit] Enter Play Mode: domain and scene reload disabled.");
            }

            if (settings.setInputSystemOnly && SetInputSystemOnly())
                SessionState.SetBool(RestartPendingKey, true);

            AssetDatabase.SaveAssets();
        }

        static bool SetInputSystemOnly()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            var prop = asset != null ? new SerializedObject(asset).FindProperty("activeInputHandler") : null;
            if (prop == null)
            {
                Debug.LogWarning("[Toolkit] Could not find the Active Input Handling setting.");
                return false;
            }

            const int inputSystemOnly = 1; // 0 = Input Manager, 1 = Input System, 2 = Both
            if (prop.intValue == inputSystemOnly)
                return false;

            prop.intValue = inputSystemOnly;
            prop.serializedObject.ApplyModifiedProperties();
            Debug.Log("[Toolkit] Active Input Handling set to Input System. An editor restart is needed.");
            return true;
        }

        /// <summary>Shows the restart dialog if a setting needs it and no package job is running.</summary>
        public static void PromptRestartIfPending()
        {
            if (!SessionState.GetBool(RestartPendingKey, false) || PackageInstaller.IsBusy)
                return;

            SessionState.EraseBool(RestartPendingKey);
            if (EditorUtility.DisplayDialog("Restart required",
                    "Active Input Handling was changed to the Input System. Restart the editor now?",
                    "Restart", "Later"))
            {
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
        }
    }
}
