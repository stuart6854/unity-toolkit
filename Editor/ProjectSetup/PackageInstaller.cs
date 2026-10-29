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

        public static void Install(IEnumerable<string> packageIds)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[Toolkit] A package install is already running.");
                return;
            }

            var toAdd = packageIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .Where(id => PackageInfo.FindForPackageName(id.Split('@')[0]) == null)
                .ToArray();

            if (toAdd.Length == 0)
            {
                Debug.Log("[Toolkit] All packages are already installed.");
                return;
            }

            Debug.Log($"[Toolkit] Installing: {string.Join(", ", toAdd)}");
            _request = Client.AddAndRemove(toAdd, null);
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null || !_request.IsCompleted)
                return;

            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
                Debug.Log("[Toolkit] Packages installed.");
            else
                Debug.LogError($"[Toolkit] Package install failed: {_request.Error?.message}");

            _request = null;
        }
    }
}
