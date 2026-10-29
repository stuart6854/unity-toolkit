namespace StuMillman.Toolkit
{
    /// <summary>
    /// Info about the editor's Bootstrap Play Mode (Tools > Toolkit > Bootstrap Play Mode).
    /// </summary>
    public static class BootstrapPlayMode
    {
        internal const string ScenesKey = "StuMillman.Toolkit.BootstrapPlayMode.Scenes";

        /// <summary>
        /// True when Play was pressed in another scene, and the editor will load that scene after Bootstrap.
        /// Bootstrap code should skip loading its own first scene (e.g. MainMenu) when this is true.
        /// Always false in builds.
        /// </summary>
        public static bool WillLoadEditedScene
        {
            get
            {
#if UNITY_EDITOR
                return !string.IsNullOrEmpty(UnityEditor.SessionState.GetString(ScenesKey, string.Empty));
#else
                return false;
#endif
            }
        }
    }
}
