#if TOOLKIT_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if TOOLKIT_UGUI
using UnityEngine.UI;
#endif

namespace StuMillman.Toolkit.Scenes
{
    /// <summary>
    /// Full-screen fade overlay that survives scene loads. Created on first use.
    /// Without UGUI (com.unity.ugui) installed, fades are skipped.
    /// </summary>
    public static class ScreenFader
    {
#if TOOLKIT_UGUI
        static CanvasGroup _group;
        static Image _image;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _group = null;
            _image = null;
        }

        static void Ensure()
        {
            if (_group != null)
                return;

            var go = new GameObject("[Toolkit] Screen Fader");
            Object.DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var imageGo = new GameObject("Overlay", typeof(RectTransform));
            imageGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _image = imageGo.AddComponent<Image>();
        }
#endif

        /// <summary>Fades the screen to the colour. Blocks input while covered.</summary>
        public static async UniTask FadeOut(float duration, Color color, CancellationToken ct = default)
        {
#if TOOLKIT_UGUI
            Ensure();
            _image.color = color;
            _group.blocksRaycasts = true;
            await Fade(1f, duration, ct);
#else
            await UniTask.CompletedTask;
#endif
        }

        /// <summary>Fades the overlay away.</summary>
        public static async UniTask FadeIn(float duration, CancellationToken ct = default)
        {
#if TOOLKIT_UGUI
            Ensure();
            await Fade(0f, duration, ct);
            _group.blocksRaycasts = false;
#else
            await UniTask.CompletedTask;
#endif
        }

#if TOOLKIT_UGUI
        static async UniTask Fade(float target, float duration, CancellationToken ct)
        {
            var start = _group.alpha;
            if (duration <= 0f)
            {
                _group.alpha = target;
                return;
            }

            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(start, target, t / duration);
                await UniTask.Yield(ct);
            }

            _group.alpha = target;
        }
#endif
    }
}
#endif
