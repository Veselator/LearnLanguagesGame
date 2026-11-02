using System;
using System.Collections;
using UnityEngine;

namespace LearnLanguages.UI.Common
{
    /// <summary>
    /// Non-linear curves for interface animations: t goes from 0 to 1, the result starts at 0 and ends at 1.
    /// </summary>
    public static class UiEasing
    {
        private const float Overshoot = 1.70158f;
        /// <summary>A long frame must not swallow an animation: time advances by at most this per frame.</summary>
        private const float MaxFrameStep = 1f / 20f;

        public static float OutCubic(float t)
        {
            var inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        /// <summary>Flies past the target and comes back.</summary>
        public static float OutBack(float t)
        {
            var shifted = t - 1f;
            return 1f + (Overshoot + 1f) * shifted * shifted * shifted + Overshoot * shifted * shifted;
        }

        /// <summary>Takes a swing back before leaving.</summary>
        public static float InBack(float t) => (Overshoot + 1f) * t * t * t - Overshoot * t * t;

        /// <summary>Up and back to 0: a pulse.</summary>
        public static float Pulse(float t) => Mathf.Sin(t * Mathf.PI);

        /// <summary>Oscillation fading out to 0: a shake.</summary>
        public static float DampedShake(float t, float oscillations) =>
            Mathf.Sin(t * oscillations * 2f * Mathf.PI) * (1f - t) * (1f - t);

        /// <summary>Runs on unscaled time, so a paused game does not stop the interface.</summary>
        public static IEnumerator Animate(float duration, Action<float> step)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep))
            {
                step(elapsed / duration);
                yield return null;
            }
            step(1f);
        }
    }
}
