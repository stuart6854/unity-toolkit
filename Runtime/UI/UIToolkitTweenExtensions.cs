#if TOOLKIT_PRIMETWEEN && TOOLKIT_UITOOLKIT
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

namespace StuMillman.Toolkit.UI
{
    /// <summary>
    /// PrimeTween helpers for UI Toolkit VisualElements. All use unscaled time by default.
    /// </summary>
    public static class UIToolkitTweenExtensions
    {
        static float Opacity(VisualElement e) => e.resolvedStyle.opacity;

        /// <summary>Shows the element and fades it in.</summary>
        public static Tween FadeIn(this VisualElement e, float duration = 0.25f, Ease ease = Ease.OutQuad, bool unscaled = true)
        {
            e.style.display = DisplayStyle.Flex;
            e.pickingMode = PickingMode.Position;
            return Tween.Custom(Opacity(e), 1f, duration, a => e.style.opacity = a, ease, useUnscaledTime: unscaled);
        }

        /// <summary>Fades out and optionally hides the element (display: none).</summary>
        public static Tween FadeOut(this VisualElement e, float duration = 0.25f, bool hide = true, Ease ease = Ease.OutQuad, bool unscaled = true)
        {
            return Tween.Custom(Opacity(e), 0f, duration, a => e.style.opacity = a, ease, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    if (hide) e.style.display = DisplayStyle.None;
                });
        }

        /// <summary>Slides in from an offset in pixels (e.g. new Vector2(0, 200) = from below).</summary>
        public static Tween SlideIn(this VisualElement e, Vector2 fromOffset, float duration = 0.3f, Ease ease = Ease.OutCubic, bool unscaled = true)
        {
            e.style.display = DisplayStyle.Flex;
            return Tween.Custom(fromOffset, Vector2.zero, duration, p => SetTranslate(e, p), ease, useUnscaledTime: unscaled);
        }

        /// <summary>Slides out by an offset in pixels, then optionally hides and resets the element.</summary>
        public static Tween SlideOut(this VisualElement e, Vector2 toOffset, float duration = 0.25f, bool hide = true, Ease ease = Ease.InCubic, bool unscaled = true)
        {
            return Tween.Custom(Vector2.zero, toOffset, duration, p => SetTranslate(e, p), ease, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    if (!hide) return;
                    e.style.display = DisplayStyle.None;
                    SetTranslate(e, Vector2.zero);
                });
        }

        /// <summary>Scales up from 0 with a slight overshoot.</summary>
        public static Tween PopIn(this VisualElement e, float duration = 0.3f, bool unscaled = true)
        {
            e.style.display = DisplayStyle.Flex;
            return Tween.Custom(0f, 1f, duration, s => SetScale(e, s), Ease.OutBack, useUnscaledTime: unscaled);
        }

        /// <summary>Scales down to 0, then optionally hides the element and resets its scale.</summary>
        public static Tween PopOut(this VisualElement e, float duration = 0.2f, bool hide = true, bool unscaled = true)
        {
            return Tween.Custom(1f, 0f, duration, s => SetScale(e, s), Ease.InBack, useUnscaledTime: unscaled)
                .OnComplete(() =>
                {
                    if (!hide) return;
                    e.style.display = DisplayStyle.None;
                    SetScale(e, 1f);
                });
        }

        /// <summary>Shakes the element, e.g. for an invalid input.</summary>
        public static Tween Shake(this VisualElement e, float strength = 10f, float duration = 0.3f, bool unscaled = true)
        {
            return Tween.Custom(1f, 0f, duration, k => SetTranslate(e, Random.insideUnitCircle * (strength * k)),
                    Ease.Linear, useUnscaledTime: unscaled)
                .OnComplete(() => SetTranslate(e, Vector2.zero));
        }

        static void SetTranslate(VisualElement e, Vector2 p) => e.style.translate = new Translate(p.x, p.y);
        static void SetScale(VisualElement e, float s) => e.style.scale = new Scale(new Vector2(s, s));
    }
}
#endif
