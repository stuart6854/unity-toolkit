using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace StuMillman.Toolkit.Editor.Build
{
    /// <summary>
    /// Before each build: bumps the build number, reads git info and writes a temporary
    /// BuildInfo asset into Resources. The asset is deleted after the build.
    /// </summary>
    [InitializeOnLoad]
    public class BuildInfoBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string GeneratedFolder = "Assets/StuMillmanToolkit.Generated";
        const string ResourcesFolder = GeneratedFolder + "/Resources";
        const string AssetPath = ResourcesFolder + "/" + BuildInfo.ResourceName + ".asset";
        const string BuildNumberFile = "ProjectSettings/StuMillmanToolkit/BuildNumber.txt";

        static BuildInfo _editorInfo;
        static double _editorInfoTime;

        public int callbackOrder => -100;

        static BuildInfoBuilder()
        {
            BuildInfo.EditorProvider = GetEditorInfo;
            // Removes the asset if a previous build failed before cleanup ran.
            EditorApplication.delayCall += DeleteGenerated;
        }

        /// <summary>The last build number used. Edit ProjectSettings/StuMillmanToolkit/BuildNumber.txt to change it.</summary>
        public static int BuildNumber
        {
            get => File.Exists(BuildNumberFile) && int.TryParse(File.ReadAllText(BuildNumberFile).Trim(), out var n) ? n : 0;
            private set
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BuildNumberFile)!);
                File.WriteAllText(BuildNumberFile, value + "\n");
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Read git first, so bumping the build number doesn't make the tree look dirty.
            var git = GitInfo.Read();

            var number = BuildNumber + 1;
            BuildNumber = number;
            PlayerSettings.Android.bundleVersionCode = number;
            PlayerSettings.iOS.buildNumber = number.ToString();
            PlayerSettings.macOS.buildNumber = number.ToString();

            var info = BuildInfo.Create(PlayerSettings.bundleVersion, number, git.Commit, git.Branch, git.IsDirty, DateTime.UtcNow);

            DeleteGenerated();
            Directory.CreateDirectory(ResourcesFolder);
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(info, AssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Toolkit] Build info: {info}");
        }

        public void OnPostprocessBuild(BuildReport report) => DeleteGenerated();

        static void DeleteGenerated()
        {
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.DeleteAsset(GeneratedFolder);
        }

        static BuildInfo GetEditorInfo()
        {
            // Refresh at most every 30 seconds so git isn't run on every access.
            if (_editorInfo == null || EditorApplication.timeSinceStartup - _editorInfoTime > 30)
            {
                var git = GitInfo.Read();
                _editorInfo = BuildInfo.Create(PlayerSettings.bundleVersion, BuildNumber, git.Commit, git.Branch, git.IsDirty, DateTime.UtcNow);
                _editorInfo.hideFlags = UnityEngine.HideFlags.DontSave;
                _editorInfoTime = EditorApplication.timeSinceStartup;
            }

            return _editorInfo;
        }
    }

    internal readonly struct GitInfo
    {
        public readonly string Commit;
        public readonly string Branch;
        public readonly bool IsDirty;

        GitInfo(string commit, string branch, bool isDirty)
        {
            Commit = commit;
            Branch = branch;
            IsDirty = isDirty;
        }

        public static GitInfo Read()
        {
            var commit = Run("rev-parse --short HEAD");
            var branch = Run("rev-parse --abbrev-ref HEAD");
            var status = Run("status --porcelain");
            return new GitInfo(
                string.IsNullOrEmpty(commit) ? "unknown" : commit,
                string.IsNullOrEmpty(branch) ? "unknown" : branch,
                !string.IsNullOrEmpty(status));
        }

        static string Run(string args)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo("git", args)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (process == null) return null;

                var output = process.StandardOutput.ReadToEnd();
                return process.WaitForExit(3000) && process.ExitCode == 0 ? output.Trim() : null;
            }
            catch
            {
                return null; // git not installed or not a repo.
            }
        }
    }
}
