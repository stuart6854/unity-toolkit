using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class AssemblySetup
    {
        public static string RootName(ProjectSetupSettings settings) =>
            NameUtil.ToIdentifier(string.IsNullOrWhiteSpace(settings.productName)
                ? PlayerSettings.productName
                : settings.productName);

        public static void Create(ProjectSetupSettings settings)
        {
            var root = RootName(settings);
            var baseDir = $"Assets/{settings.rootFolder}/{settings.scriptsFolder}";
            var runtimeName = $"{root}.Runtime";
            var editorName = $"{root}.Editor";
            var refs = settings.asmdefReferences.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList();

            Write($"{baseDir}/Runtime", runtimeName, root, refs, editorOnly: false);
            Write($"{baseDir}/Editor", editorName, $"{root}.Editor", new[] { runtimeName }.Concat(refs), editorOnly: true);

            AssetDatabase.Refresh();
        }

        static void Write(string dir, string name, string rootNamespace, IEnumerable<string> references, bool editorOnly)
        {
            Directory.CreateDirectory(dir);

            // A folder can only hold one asmdef.
            var existing = Directory.GetFiles(dir, "*.asmdef");
            if (existing.Length > 0)
            {
                Debug.Log($"[Toolkit] {dir} already has {Path.GetFileName(existing[0])}, skipped.");
                return;
            }

            var refsJson = string.Join(",\n", references.Select(r => $"        \"{r}\""));
            var platforms = editorOnly ? "\n        \"Editor\"\n    " : "";
            var json =
$@"{{
    ""name"": ""{name}"",
    ""rootNamespace"": ""{rootNamespace}"",
    ""references"": [
{refsJson}
    ],
    ""includePlatforms"": [{platforms}],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}
";
            var path = $"{dir}/{name}.asmdef";
            File.WriteAllText(path, json);
            Debug.Log($"[Toolkit] Created {path}");
        }
    }
}
