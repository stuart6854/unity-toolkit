using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// In builds, moves header children up a level and deletes the header objects.
    /// Headers with components other than Transform are left alone.
    /// </summary>
    public class HierarchyHeaderStripper : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // report is null in Play Mode; only strip in real builds.
            if (report == null)
                return;

            var headers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(t => HierarchyHeaders.IsHeader(t.name, out _))
                .Reverse() // Deepest first, so nested headers are handled before their parents.
                .ToList();

            foreach (var header in headers)
            {
                if (header.GetComponents<Component>().Length > 1)
                {
                    Debug.LogWarning($"[Toolkit] Header '{header.name}' has components, so it was kept in the build.");
                    continue;
                }

                var parent = header.parent;
                var index = header.GetSiblingIndex();

                while (header.childCount > 0)
                {
                    var child = header.GetChild(0);
                    child.SetParent(parent, true);
                    child.SetSiblingIndex(index++);
                }

                Object.DestroyImmediate(header.gameObject);
            }
        }
    }
}
