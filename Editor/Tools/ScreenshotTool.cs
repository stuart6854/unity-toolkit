using System;
using System.IO;
using StuMillman.Toolkit.Editor.ProjectSetup;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>Saves Game view screenshots to &lt;Project&gt;/Screenshots.</summary>
    public static class ScreenshotTool
    {
        const string MenuRoot = "Tools/Toolkit/Screenshot/";
        const string ModuleId = "com.unity.modules.screencapture";

        public static string Folder => Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");

        [MenuItem(MenuRoot + "Capture Game View _F12", priority = 40)]
        static void Capture1() => Capture(1);

        [MenuItem(MenuRoot + "Capture Game View x2", priority = 41)]
        static void Capture2() => Capture(2);

        [MenuItem(MenuRoot + "Capture Game View x4", priority = 42)]
        static void Capture4() => Capture(4);

        [MenuItem(MenuRoot + "Open Screenshots Folder", priority = 60)]
        static void OpenFolder()
        {
            Directory.CreateDirectory(Folder);
            EditorUtility.RevealInFinder(Folder);
        }

        public static void Capture(int scale)
        {
#if TOOLKIT_SCREENCAPTURE
            Directory.CreateDirectory(Folder);
            var suffix = scale > 1 ? $"_x{scale}" : string.Empty;
            var file = $"{NameUtil.ToIdentifier(PlayerSettings.productName)}_{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.png";
            var path = Path.Combine(Folder, file);

            ScreenCapture.CaptureScreenshot(path, scale);

            // In edit mode the file is only written when the Game view repaints.
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
                EditorWindow.GetWindow(gameViewType, false, null, false).Repaint();

            Debug.Log($"[Toolkit] Screenshot saved: {path}");
#else
            if (EditorUtility.DisplayDialog("Screen Capture module missing",
                    $"Screenshots need the built-in module '{ModuleId}', which isn't installed. Install it now?",
                    "Install", "Cancel"))
            {
                Client.Add(ModuleId);
            }
#endif
        }
    }
}
