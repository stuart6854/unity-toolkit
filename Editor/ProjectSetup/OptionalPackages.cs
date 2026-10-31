using System;
using System.Collections.Generic;
using System.IO;
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

        [Tooltip("Scoped registry needed by the package (e.g. npm). Leave empty if not needed.")]
        public string registryName;
        public string registryUrl;
        public string registryScope;

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
                displayName = "UniTask",
                description = "Allocation-free async/await for Unity (Cysharp).",
                packages = { "com.cysharp.unitask=https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask" },
            },
            new OptionalPackage
            {
                displayName = "PrimeTween",
                description = "Fast, allocation-free tweening and animation library. Adds the npm scoped registry.",
                packages = { "com.kyrylokuzyk.primetween" },
                registryName = "npm",
                registryUrl = "https://registry.npmjs.org",
                registryScope = "com.kyrylokuzyk",
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

        /// <summary>Adds scoped registries needed by ticked entries to Packages/manifest.json.</summary>
        public static void EnsureRegistries(ProjectSetupSettings settings)
        {
            foreach (var p in settings.optionalPackages.Where(p => p.enabled && !string.IsNullOrEmpty(p.registryUrl)))
            {
                if (ManifestEditor.EnsureScopedRegistry(p.registryName, p.registryUrl, p.registryScope))
                    Debug.Log($"[Toolkit] Added scoped registry '{p.registryName}' ({p.registryUrl}) for scope '{p.registryScope}'.");
            }
        }

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

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    /// <summary>Small text edits to Packages/manifest.json (Unity has no API for scoped registries).</summary>
    public static class ManifestEditor
    {
        const string ManifestPath = "Packages/manifest.json";

        /// <summary>Adds the registry, or adds the scope to an existing registry with the same URL. Returns true if the file changed.</summary>
        public static bool EnsureScopedRegistry(string name, string url, string scope)
        {
            if (!File.Exists(ManifestPath))
                return false;

            var text = File.ReadAllText(ManifestPath);
            var original = text;
            var urlIndex = FindUrl(text, url);

            if (urlIndex >= 0)
            {
                // Registry exists: add the scope if it isn't already listed.
                var scopesKey = text.IndexOf("\"scopes\"", urlIndex, StringComparison.Ordinal);
                var close = text.IndexOf('}', urlIndex);
                if (scopesKey < 0 || (close >= 0 && scopesKey > close))
                    return false;

                var open = text.IndexOf('[', scopesKey);
                var end = text.IndexOf(']', open);
                if (text.Substring(open, end - open).Contains($"\"{scope}\""))
                    return false;

                text = InsertIntoArray(text, open, $"\"{scope}\"", "        ");
            }
            else
            {
                var entry =
                    "{\n" +
                    $"      \"name\": \"{name}\",\n" +
                    $"      \"url\": \"{url}\",\n" +
                    "      \"scopes\": [\n" +
                    $"        \"{scope}\"\n" +
                    "      ]\n" +
                    "    }";

                var key = text.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
                if (key >= 0)
                {
                    text = InsertIntoArray(text, text.IndexOf('[', key), entry, "    ");
                }
                else
                {
                    var brace = text.IndexOf('{');
                    text = text.Insert(brace + 1, $"\n  \"scopedRegistries\": [\n    {entry}\n  ],");
                }
            }

            if (text == original)
                return false;

            File.WriteAllText(ManifestPath, text);
            return true;
        }

        static int FindUrl(string text, string url)
        {
            var trimmed = url.TrimEnd('/');
            var i = text.IndexOf($"\"{trimmed}\"", StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? i : text.IndexOf($"\"{trimmed}/\"", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Inserts an item at the start of the JSON array whose '[' is at openIndex.</summary>
        static string InsertIntoArray(string text, int openIndex, string item, string indent)
        {
            var next = openIndex + 1;
            while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
            var isEmpty = next < text.Length && text[next] == ']';
            return text.Insert(openIndex + 1, $"\n{indent}{item}{(isEmpty ? "" : ",")}");
        }
    }
}
