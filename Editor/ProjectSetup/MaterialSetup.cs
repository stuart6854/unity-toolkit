using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class MaterialSetup
    {
        const string UrpLit = "Universal Render Pipeline/Lit";

        public static void CreateMaterials(ProjectSetupSettings settings)
        {
            var shader = ResolveShader();
            if (shader == null)
            {
                Debug.LogError("[Toolkit] No suitable shader found. Is URP installed?");
                return;
            }

            if (shader.name != UrpLit)
                Debug.LogWarning($"[Toolkit] URP is not the active pipeline. Using '{shader.name}' instead.");

            var folder = Path.Combine("Assets", settings.rootFolder, settings.materialsFolder).Replace('\\', '/');
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            var created = 0;
            foreach (var entry in settings.materials)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                    continue;

                var path = $"{folder}/{entry.name.Trim()}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                    continue; // Never overwrite existing materials.

                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", entry.color);
                else
                    mat.color = entry.color;

                AssetDatabase.CreateAsset(mat, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Toolkit] Material setup done. Created {created} material(s) in {folder}.");
        }

        static Shader ResolveShader()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultShader != null)
                return pipeline.defaultShader;

            return Shader.Find(UrpLit) ?? Shader.Find("Standard");
        }
    }
}
