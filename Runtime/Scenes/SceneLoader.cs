#if TOOLKIT_UNITASK
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StuMillman.Toolkit.Scenes
{
    /// <summary>
    /// Async scene loading with a fade and an optional loading scene. Needs UniTask.
    /// </summary>
    /// <example>
    /// await SceneLoader.Load("Game");
    /// await SceneLoader.Load("Game", loadingScene: "Loading");
    /// </example>
    public static class SceneLoader
    {
        /// <summary>Seconds for each fade. 0 disables fading.</summary>
        public static float FadeDuration = 0.35f;

        /// <summary>Fade colour.</summary>
        public static Color FadeColor = Color.black;

        /// <summary>Loading scene used when Load() isn't given one. Leave empty for none.</summary>
        public static string DefaultLoadingScene;

        /// <summary>Minimum time the loading scene stays visible, so it doesn't flash.</summary>
        public static float MinLoadingSceneTime = 0.5f;

        /// <summary>True while a load is running.</summary>
        public static bool IsLoading { get; private set; }

        /// <summary>Target scene load progress, 0 to 1. Read it from your loading scene UI.</summary>
        public static float Progress { get; private set; }

        /// <summary>Raised with the target scene name when a load starts and when it finishes.</summary>
        public static event Action<string> LoadStarted;
        public static event Action<string> LoadCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsLoading = false;
            Progress = 0f;
            LoadStarted = null;
            LoadCompleted = null;
        }

        /// <summary>Fades out, loads the scene (via a loading scene if set), then fades in.</summary>
        public static async UniTask Load(string sceneName, string loadingScene = null, CancellationToken ct = default)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[Toolkit] SceneLoader is already loading. Ignored request for '{sceneName}'.");
                return;
            }

            IsLoading = true;
            Progress = 0f;
            LoadStarted?.Invoke(sceneName);

            try
            {
                loadingScene = string.IsNullOrEmpty(loadingScene) ? DefaultLoadingScene : loadingScene;
                var useLoadingScene = !string.IsNullOrEmpty(loadingScene);

                await ScreenFader.FadeOut(FadeDuration, FadeColor, ct);

                var loadingShownAt = 0f;
                if (useLoadingScene)
                {
                    await SceneManager.LoadSceneAsync(loadingScene).ToUniTask(cancellationToken: ct);
                    loadingShownAt = Time.unscaledTime;
                    await ScreenFader.FadeIn(FadeDuration, ct);
                }

                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                    throw new ArgumentException($"Scene '{sceneName}' couldn't be loaded. Is it in Build Settings?");

                op.allowSceneActivation = false;
                while (op.progress < 0.9f)
                {
                    Progress = op.progress / 0.9f;
                    await UniTask.Yield(ct);
                }
                Progress = 1f;

                if (useLoadingScene)
                {
                    var remaining = MinLoadingSceneTime - (Time.unscaledTime - loadingShownAt);
                    if (remaining > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.UnscaledDeltaTime, cancellationToken: ct);
                    await ScreenFader.FadeOut(FadeDuration, FadeColor, ct);
                }

                op.allowSceneActivation = true;
                await op.ToUniTask(cancellationToken: ct);
                await ScreenFader.FadeIn(FadeDuration, ct);
                LoadCompleted?.Invoke(sceneName);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
#endif
