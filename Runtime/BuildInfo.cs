using System;
using System.Globalization;
using UnityEngine;

namespace StuMillman.Toolkit
{
    /// <summary>
    /// Version, build number, git commit/branch and build time, stamped in at build time.
    /// In the editor, values are read live (the build number is the last one used).
    /// </summary>
    public class BuildInfo : ScriptableObject
    {
        internal const string ResourceName = "StuMillmanToolkit.BuildInfo";

        [SerializeField] internal string version;
        [SerializeField] internal int buildNumber;
        [SerializeField] internal string commit;
        [SerializeField] internal string branch;
        [SerializeField] internal bool isDirty;
        [SerializeField] internal string buildTimeUtc;

        /// <summary>Player Settings version, e.g. "0.3.0".</summary>
        public string Version => version;

        /// <summary>Goes up by 1 on every build.</summary>
        public int BuildNumber => buildNumber;

        /// <summary>Short git commit hash, or "unknown" if git wasn't available.</summary>
        public string Commit => commit;

        /// <summary>Git branch name, or "unknown".</summary>
        public string Branch => branch;

        /// <summary>True if there were uncommitted changes when the build was made.</summary>
        public bool IsDirty => isDirty;

        /// <summary>When the build was made (UTC).</summary>
        public DateTime BuildTimeUtc =>
            DateTime.TryParse(buildTimeUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t)
                ? t
                : default;

        /// <summary>e.g. "v0.3.0 (42)".</summary>
        public string ShortString => $"v{version} ({buildNumber})";

        /// <summary>e.g. "v0.3.0 (42) a1b2c3d-dirty [main] 2026-10-29 14:03 UTC".</summary>
        public override string ToString() =>
            $"{ShortString} {commit}{(isDirty ? "-dirty" : "")} [{branch}] {BuildTimeUtc:yyyy-MM-dd HH:mm} UTC";

        /// <summary>Set by the editor so Play Mode gets live values.</summary>
        internal static Func<BuildInfo> EditorProvider;

        static BuildInfo _current;

        /// <summary>Info for the running build.</summary>
        public static BuildInfo Current
        {
            get
            {
#if UNITY_EDITOR
                if (EditorProvider != null)
                    return EditorProvider();
#endif
                if (_current == null)
                    _current = Resources.Load<BuildInfo>(ResourceName);

                if (_current == null)
                {
                    // Built without the toolkit's build step (shouldn't normally happen).
                    _current = CreateInstance<BuildInfo>();
                    _current.version = Application.version;
                    _current.commit = _current.branch = "unknown";
                }

                return _current;
            }
        }

        internal static BuildInfo Create(string version, int buildNumber, string commit, string branch,
            bool isDirty, DateTime buildTimeUtc)
        {
            var info = CreateInstance<BuildInfo>();
            info.version = version;
            info.buildNumber = buildNumber;
            info.commit = commit;
            info.branch = branch;
            info.isDirty = isDirty;
            info.buildTimeUtc = buildTimeUtc.ToString("o", CultureInfo.InvariantCulture);
            return info;
        }
    }
}
