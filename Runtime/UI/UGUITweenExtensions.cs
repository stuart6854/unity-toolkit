#if TOOLKIT_PRIMETWEEN
using PrimeTween;
using UnityEngine;

namespace StuMillman.Toolkit.UI
{
    /// <summary>
    /// PrimeTween helpers for UGUI. All use unscaled time by default so they work in pause menus.
    /// </summary>
    public static class UGUITweenExtensions
    {
        /// <summary>Fades in and enables interaction at the end.</summary>
        public static Tween FadeIn(this CanvasGroup group, float duration = 0.25f, Ease ease = Ease.OutQuad, bool unscaled = true)
        {
            group.gameObject.SetActive(true);
            return Tween.Custom(group.alpha, 1f, duration, a => group.alpha = a, ease, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    group.interactable = true;
                    group.blocksRaycasts = true;
                });
        }

        /// <summary>Disables interaction, fades out, and optionally deactivates the GameObject.</summary>
        public static Tween FadeOut(this CanvasGroup group, float duration = 0.25f, bool deactivate = false, Ease ease = Ease.OutQuad, bool unscaled = true)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
            return Tween.Custom(group.alpha, 0f, duration, a => group.alpha = a, ease, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    if (deactivate) group.gameObject.SetActive(false);
                });
        }

        /// <summary>Slides in from an offset (e.g. new Vector2(0, -200) = from below) to its current position.</summary>
        public static Tween SlideIn(this RectTransform rect, Vector2 fromOffset, float duration = 0.3f, Ease ease = Ease.OutCubic, bool unscaled = true)
        {
            var target = rect.anchoredPosition;
            return Tween.Custom(target + fromOffset, target, duration, p => rect.anchoredPosition = p, ease, useUnscaledTime: unscaled);
        }

        /// <summary>Slides out by an offset, then puts the position back so it can slide in again.</summary>
        public static Tween SlideOut(this RectTransform rect, Vector2 toOffset, float duration = 0.25f, Ease ease = Ease.InCubic, bool unscaled = true)
        {
            var start = rect.anchoredPosition;
            return Tween.Custom(start, start + toOffset, duration, p => rect.anchoredPosition = p, ease, useUnscaledTime: unscaled)
                .OnComplete(() => rect.anchoredPosition = start);
        }

        /// <summary>Scales up from 0 with a slight overshoot.</summary>
        public static Tween PopIn(this Transform transform, float duration = 0.3f, bool unscaled = true)
        {
            var target = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            return Tween.Custom(Vector3.zero, target, duration, s => transform.localScale = s, Ease.OutBack, useUnscaledTime: unscaled);
        }

        /// <summary>Scales down to 0, then restores the scale and optionally deactivates.</summary>
        public static Tween PopOut(this Transform transform, float duration = 0.2f, bool deactivate = true, bool unscaled = true)
        {
            var start = transform.localScale;
            return Tween.Custom(start, Vector3.zero, duration, s => transform.localScale = s, Ease.InBack, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    transform.localScale = start;
                    if (deactivate) transform.gameObject.SetActive(false);
                });
        }

        /// <summary>Shakes the anchored position, e.g. for an invalid input.</summary>
        public static Tween Shake(this RectTransform rect, float strength = 10f, float duration = 0.3f, bool unscaled = true)
        {
            var start = rect.anchoredPosition;
            return Tween.Custom(1f, 0f, duration, k =>
                {
                    rect.anchoredPosition = start + Random.insideUnitCircle * (strength * k);
                }, Ease.Linear, useUnscaledTime: unscaled)
                .OnComplete(() => rect.anchoredPosition = start);
        }
    }
}
#endif
