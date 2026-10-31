using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Sets Unity's built-in "Playmode tint" preference (Preferences > Colors) to a chosen colour.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeTint
    {
        const string UnityKey = "Playmode tint";
        const string EnabledKey = "StuMillman.Toolkit.PlayModeTint.Enabled";
        const string ColorKey = "StuMillman.Toolkit.PlayModeTint.Color";

        static readonly Color UnityDefault = new(0.8f, 0.8f, 0.8f, 1f);
        public static readonly Color DefaultTint = new(0.85f, 0.68f, 0.68f, 1f);

        static PlayModeTint()
        {
            EditorApplication.delayCall += Apply;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set
            {
                if (value == Enabled) return;
                EditorPrefs.SetBool(EnabledKey, value);
                Apply();
            }
        }

        public static Color Tint
        {
            get => ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(ColorKey, ""), out var c) ? c : DefaultTint;
            set
            {
                EditorPrefs.SetString(ColorKey, ColorUtility.ToHtmlStringRGBA(value));
                Apply();
            }
        }

        /// <summary>Writes the tint to Unity's preference and refreshes the editor's cached copy.</summary>
        public static void Apply()
        {
            var c = Enabled ? Tint : UnityDefault;
            var value = string.Join(";", UnityKey,
                c.r.ToString(CultureInfo.InvariantCulture),
                c.g.ToString(CultureInfo.InvariantCulture),
                c.b.ToString(CultureInfo.InvariantCulture),
                c.a.ToString(CultureInfo.InvariantCulture));

            if (EditorPrefs.GetString(UnityKey, "") == value)
                return;

            EditorPrefs.SetString(UnityKey, value);
            ReloadCachedPref();
        }

        // Unity caches the colour in HostView.kPlayModeDarken (a PrefColor). Marking it as not loaded
        // makes it re-read EditorPrefs. If this internal field changes, the tint applies after a restart.
        static void ReloadCachedPref()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var hostView = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView");
            var pref = hostView?.GetField("kPlayModeDarken", flags)?.GetValue(null);
            pref?.GetType()
                .GetField("m_Loaded", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(pref, false);
        }
    }
}
