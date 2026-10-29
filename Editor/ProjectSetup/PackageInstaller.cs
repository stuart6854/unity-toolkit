using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class PackageInstaller
    {
        static AddAndRemoveRequest _request;

        public static bool IsBusy => _request != null && !_request.IsCompleted;

        /// <summary>Installs and removes packages in a single Package Manager request.</summary>
        public static void Apply(IEnumerable<string> install, IEnumerable<string> remove)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[Toolkit] A package operation is already running.");
                return;
            }

            var toAdd = Clean(install)
                .Where(id => PackageInfo.FindForPackageName(NameOf(id)) == null)
                .ToArray();

            // Never remove something we're installing, and only remove direct dependencies.
            var addNames = new HashSet<string>(Clean(install).Select(NameOf));
            var toRemove = Clean(remove)
                .Select(NameOf)
                .Where(name => !addNames.Contains(name))
                .Where(name => PackageInfo.FindForPackageName(name) is { isDirectDependency: true })
                .ToArray();

            if (toAdd.Length == 0 && toRemove.Length == 0)
            {
                Debug.Log("[Toolkit] Packages already up to date.");
                return;
            }

            if (toAdd.Length > 0) Debug.Log($"[Toolkit] Installing: {string.Join(", ", toAdd)}");
            if (toRemove.Length > 0) Debug.Log($"[Toolkit] Removing: {string.Join(", ", toRemove)}");

            _request = Client.AddAndRemove(toAdd, toRemove);
            EditorApplication.update += Poll;
        }

        static IEnumerable<string> Clean(IEnumerable<string> ids) =>
            (ids ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct();

        static string NameOf(string id) => id.Split('@')[0];

        static void Poll()
        {
            if (_request == null || !_request.IsCompleted)
                return;

            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
                Debug.Log("[Toolkit] Package changes applied.");
            else
                Debug.LogError($"[Toolkit] Package changes failed: {_request.Error?.message}");

            _request = null;
        }
    }
}
