using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Draws GameObjects named like "--- Managers ---" as coloured header bars in the legacy (IMGUI) Hierarchy.
    /// Unity 6.6's new Hierarchy window is handled by <see cref="HierarchyHeadersView"/>.
    /// Headers are removed from builds by <see cref="HierarchyHeaderStripper"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyHeaders
    {
        const string MenuPath = "Tools/Toolkit/Hierarchy Headers";
        const string PrefKey = "StuMillman.Toolkit.HierarchyHeaders.Enabled";

        static readonly Color[] Palette =
        {
            new(0.20f, 0.33f, 0.52f), // blue
            new(0.22f, 0.45f, 0.32f), // green
            new(0.52f, 0.30f, 0.22f), // rust
            new(0.42f, 0.26f, 0.50f), // purple
            new(0.18f, 0.43f, 0.47f), // teal
            new(0.50f, 0.42f, 0.18f), // olive
            new(0.50f, 0.22f, 0.32f), // wine
            new(0.32f, 0.34f, 0.38f), // slate
        };

        static GUIStyle _style;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set
            {
                EditorPrefs.SetBool(PrefKey, value);
                EditorApplication.RepaintHierarchyWindow();
#if UNITY_6000_6_OR_NEWER
                HierarchyHeadersView.RefreshAll();
#endif
            }
        }

        static HierarchyHeaders()
        {
#if UNITY_6000_3_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnItemGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnItemGUI;
#endif
        }

        [MenuItem(MenuPath, priority = 21)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        /// <summary>True for names like "--- Managers ---". Returns the title without dashes.</summary>
        public static bool IsHeader(string name, out string title)
        {
            title = null;
            if (string.IsNullOrEmpty(name) || !name.StartsWith("---"))
                return false;

            title = name.Trim('-', ' ');
            return true;
        }

#if UNITY_6000_3_OR_NEWER
        static void OnItemGUI(EntityId id, Rect rect) =>
            DrawItem(EditorUtility.EntityIdToObject(id) as GameObject, rect);
#else
        static void OnItemGUI(int instanceId, Rect rect) =>
            DrawItem(EditorUtility.InstanceIDToObject(instanceId) as GameObject, rect);
#endif

        static void DrawItem(GameObject go, Rect rect)
        {
            if (!Enabled || Event.current.type != EventType.Repaint)
                return;

            if (go == null || !IsHeader(go.name, out var title))
                return;

            var color = ColorFor(title);
            if (Selection.Contains(go))
                color = Color.Lerp(color, Color.white, 0.25f);
            if (!go.activeInHierarchy)
                color.a = 0.5f;

            // Full row width (from just right of the visibility/pick columns).
            const float left = 32f;
            var bar = new Rect(left, rect.y, rect.xMax - left + 16f, rect.height);
            EditorGUI.DrawRect(bar, color);

            _style ??= new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            GUI.Label(bar, title.ToUpperInvariant(), _style);
        }

        /// <summary>Lowers saturation and brightness so bars don't overpower the Hierarchy.</summary>
        static Color Mute(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            return Color.HSVToRGB(h, s * 0.7f, v * 0.85f);
        }

        internal static Color ColorFor(string title)
        {
            // Stable FNV-1a hash so a name always gets the same colour.
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in title.ToLowerInvariant())
                    hash = (hash ^ c) * 16777619u;
                return Mute(Palette[hash % (uint)Palette.Length]);
            }
        }
    }
}
