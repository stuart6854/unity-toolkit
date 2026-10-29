using System.IO;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class FolderSetup
    {
        public static void CreateFolders(ProjectSetupSettings settings)
        {
            var root = Path.Combine("Assets", settings.rootFolder);
            var created = 0;

            foreach (var folder in settings.folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                var path = Path.Combine(root, folder.Trim().Trim('/'));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    created++;
                }

                if (settings.addGitKeep)
                {
                    var keep = Path.Combine(path, ".gitkeep");
                    if (!File.Exists(keep))
                        File.WriteAllText(keep, string.Empty);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Toolkit] Folder setup done. Created {created} new folder(s) under {root}.");
        }
    }
}
