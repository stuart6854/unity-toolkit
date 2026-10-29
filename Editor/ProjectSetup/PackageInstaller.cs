using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class PackageInstaller
    {
        static ListRequest _listRequest;
        static AddAndRemoveRequest _changeRequest;
        static string[] _install;
        static string[] _remove;

        public static bool IsBusy => _listRequest != null || _changeRequest != null;

        /// <summary>Installs and removes packages in a single Package Manager request.</summary>
        public static void Apply(IEnumerable<string> install, IEnumerable<string> remove)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[Toolkit] A package operation is already running.");
                return;
            }

            _install = Clean(install).ToArray();
            _remove = Clean(remove).Select(NameOf).ToArray();

            // Ask the Package Manager for the project's direct dependencies (what's in manifest.json).
            _listRequest = Client.List(true, false);
            EditorApplication.update += PollList;
        }

        static void PollList()
        {
            if (!_listRequest.IsCompleted)
                return;

            EditorApplication.update -= PollList;
            var list = _listRequest;
            _listRequest = null;

            if (list.Status != StatusCode.Success)
            {
                Debug.LogError($"[Toolkit] Could not list packages: {list.Error?.message}");
                ProjectSettingsSetup.PromptRestartIfPending();
                return;
            }

            var installed = new HashSet<string>(
                list.Result.Where(p => p.isDirectDependency).Select(p => p.name));

            var toAdd = _install.Where(id => !installed.Contains(NameOf(id))).ToArray();

            // Never remove something we're installing, and only remove what the manifest lists.
            var addNames = new HashSet<string>(_install.Select(NameOf));
            var toRemove = _remove
                .Where(name => !addNames.Contains(name))
                .Where(installed.Contains)
                .ToArray();

            var notInstalled = _remove.Where(name => !installed.Contains(name)).ToArray();
            if (notInstalled.Length > 0)
                Debug.Log($"[Toolkit] Not in manifest, skipping removal of: {string.Join(", ", notInstalled)}");

            if (toAdd.Length == 0 && toRemove.Length == 0)
            {
                Debug.Log("[Toolkit] Packages already up to date.");
                ProjectSettingsSetup.PromptRestartIfPending();
                return;
            }

            if (toAdd.Length > 0) Debug.Log($"[Toolkit] Installing: {string.Join(", ", toAdd)}");
            if (toRemove.Length > 0) Debug.Log($"[Toolkit] Removing: {string.Join(", ", toRemove)}");

            _changeRequest = Client.AddAndRemove(toAdd, toRemove);
            EditorApplication.update += PollChange;
        }

        static void PollChange()
        {
            if (!_changeRequest.IsCompleted)
                return;

            EditorApplication.update -= PollChange;

            if (_changeRequest.Status == StatusCode.Success)
                Debug.Log("[Toolkit] Package changes applied.");
            else
                Debug.LogError($"[Toolkit] Package changes failed: {_changeRequest.Error?.message}");

            _changeRequest = null;
            ProjectSettingsSetup.PromptRestartIfPending();
        }

        static IEnumerable<string> Clean(IEnumerable<string> ids) =>
            (ids ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct();

        static string NameOf(string id) => id.Split('@')[0];
    }
}
