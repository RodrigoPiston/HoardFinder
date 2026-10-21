using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Stateless animation helpers. Animations are functions of (now - start), using unscaled time so they also run
    /// while the game is paused, and OnGUI's several events per frame all see the same value.
    /// </summary>
    internal static class Anim
    {
        public static float Now => Time.unscaledTime;

        /// <summary>0 before <paramref name="start"/>, 1 after start + duration.</summary>
        public static float Progress(float start, float duration)
        {
            return duration <= 0f ? 1f : Mathf.Clamp01((Now - start) / duration);
        }

        public static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Overshoots slightly before settling: a small "pop".</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>Frame-rate independent exponential approach toward <paramref name="target"/>.</summary>
        public static float Approach(float current, float target, float sharpness)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));
        }

        /// <summary>
        /// Scales GUI drawing around <paramref name="pivot"/> on top of <paramref name="baseMatrix"/>.
        /// </summary>
        public static Matrix4x4 ScaleAround(Matrix4x4 baseMatrix, Vector2 pivot, float scale)
        {
            Matrix4x4 toPivot = Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0f));
            Matrix4x4 back = Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0f));
            return baseMatrix * toPivot * Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * back;
        }
    }
}
