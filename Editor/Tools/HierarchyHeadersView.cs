#if UNITY_6000_6_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Header bars for Unity 6.6's UI Toolkit Hierarchy window, which doesn't raise the IMGUI row callback.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyHeadersView
    {
        const string BarName = "stumillman-toolkit-header";

        static HierarchyHeadersView()
        {
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            HierarchyWindow.BindViewItem += OnBindViewItem;
        }

        static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            if (!Apply(item))
                item.schedule.Execute(() => Apply(item)); // Row not attached yet; try again next frame.
        }

        // Elements we hid on each recycled row, so they can be shown again on rebind.
        static readonly Dictionary<VisualElement, List<VisualElement>> Hidden = new();

        /// <summary>Clears and (re)adds the bar. Returns false if the row element couldn't be found yet.</summary>
        static bool Apply(HierarchyViewItem item)
        {
            var row = FindRow(item);
            Clear(item, row);

            if (!HierarchyHeaders.Enabled || item.Handler is not HierarchyGameObjectHandler handler)
                return true;

            var go = handler.GetGameObject(item.Node);
            if (go == null || !HierarchyHeaders.IsHeader(go.name, out var title))
                return true;

            var bar = new Label(title.ToUpperInvariant())
            {
                name = BarName,
                pickingMode = PickingMode.Ignore, // Clicks still select/expand the row.
            };
            bar.style.position = Position.Absolute;
            bar.style.left = 0;
            bar.style.right = 0;
            bar.style.top = 0;
            bar.style.bottom = 0;
            bar.style.backgroundColor = HierarchyHeaders.ColorFor(title);
            bar.style.color = new Color(1f, 1f, 1f, 0.9f);
            bar.style.unityTextAlign = TextAnchor.MiddleCenter;
            bar.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.style.opacity = go.activeInHierarchy ? 1f : 0.5f;

            // Behind the row's contents, so the expand/collapse arrow stays on top.
            (row ?? item).Insert(0, bar);

            // Hide everything else on the row: the name, the icon and the other columns.
            var hidden = new List<VisualElement>();
            if (row != null)
                hidden.AddRange(row.Children().Where(cell => cell != bar && !cell.Contains(item)));
            if (item.Icon != null)
                hidden.Add(item.Icon);
            hidden.AddRange(item.Query<Label>().Where(l => l.text == go.name).ToList());

            foreach (var e in hidden)
                e.style.visibility = Visibility.Hidden;
            Hidden[item] = hidden;

            return row != null;
        }

        static void Clear(HierarchyViewItem item, VisualElement row)
        {
            item.Q(BarName)?.RemoveFromHierarchy();
            row?.Q(BarName)?.RemoveFromHierarchy();

            if (Hidden.Remove(item, out var hidden))
                foreach (var e in hidden)
                    e.style.visibility = StyleKeyword.Null;
        }

        /// <summary>The list row that holds all the column cells for this item.</summary>
        static VisualElement FindRow(VisualElement item)
        {
            for (var e = item; e != null; e = e.parent)
            {
                if (e.ClassListContains("unity-multi-column-view__row-container") ||
                    e.ClassListContains("unity-collection-view__item"))
                    return e;
            }
            return null;
        }

        /// <summary>Re-applies headers to all visible rows (e.g. after toggling the feature).</summary>
        public static void RefreshAll()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            foreach (var item in window.rootVisualElement.Query<HierarchyViewItem>().ToList())
                Apply(item);
        }
    }
}
#endif
