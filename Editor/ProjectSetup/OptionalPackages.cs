using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    /// <summary>Optional packages shown as tick boxes in the Project Setup window.</summary>
    [Serializable]
    public class OptionalPackage
    {
        public bool enabled;
        public string displayName;
        [TextArea] public string description;

        [Tooltip("Packages to install. Use 'name' or 'name@version', or 'name=gitUrl' for git packages.")]
        public List<string> packages = new();

        [Tooltip("For Asset Store packages that can't be installed automatically: the page to open.")]
        public string assetStoreUrl;

        [Tooltip("Full type name used to check whether a manually imported package is present.")]
        public string detectType;

        public static List<OptionalPackage> Defaults() => new()
        {
            new OptionalPackage
            {
                displayName = "Alchemy",
                description = "Open-source inspector attributes: [Button], [ShowIf], [ReadOnly], groups, tabs and more.",
                packages = { "com.annulusgames.alchemy=https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy" },
            },
            new OptionalPackage
            {
                displayName = "ALINE (Asset Store)",
                description = "Paid drawing library by Aron Granberg. Installs its dependencies (Burst, Mathematics, Collections); import ALINE itself from Package Manager > My Assets.",
                packages = { "com.unity.burst", "com.unity.mathematics", "com.unity.collections" },
                assetStoreUrl = "https://assetstore.unity.com/search#q=ALINE",
                detectType = "Drawing.Draw",
            },
        };

        public bool IsManualAndMissing =>
            !string.IsNullOrEmpty(detectType) &&
            !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(detectType, false) != null);
    }

    public static class OptionalPackages
    {
        /// <summary>Package IDs from all ticked optional entries.</summary>
        public static IEnumerable<string> EnabledIds(ProjectSetupSettings settings) =>
            settings.optionalPackages.Where(p => p.enabled).SelectMany(p => p.packages);

        /// <summary>Reminds the user about ticked packages that must be imported by hand.</summary>
        public static void RemindManualImports(ProjectSetupSettings settings)
        {
            foreach (var p in settings.optionalPackages.Where(p => p.enabled && p.IsManualAndMissing))
            {
                Debug.LogWarning($"[Toolkit] {p.displayName} must be imported manually from Package Manager > My Assets.");
                if (EditorUtility.DisplayDialog($"Import {p.displayName}",
                        $"{p.displayName} can't be installed automatically. Import it from Package Manager > My Assets.",
                        "Open Package Manager", "Later"))
                {
                    UnityEditor.PackageManager.UI.Window.Open(string.Empty);
                }
            }
        }
    }
}
