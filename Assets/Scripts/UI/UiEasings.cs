using System;
using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 常用补间缓动曲线，输入 t∈[0,1]。Back 和 Elastic 曲线允许输出超调。
    /// </summary>
    public static class UiEasings
    {
        // ---- Sine ----
        public static float InSine(float t) { return 1f - Mathf.Cos(t * Mathf.PI * 0.5f); }
        public static float OutSine(float t) { return Mathf.Sin(t * Mathf.PI * 0.5f); }
        public static float InOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f; }

        // ---- Quad ----
        public static float InQuad(float t) { return t * t; }
        public static float OutQuad(float t) { return 1f - (1f - t) * (1f - t); }
        public static float InOutQuad(float t) { return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f; }

        // ---- Cubic ----
        public static float InCubic(float t) { return t * t * t; }
        public static float OutCubic(float t) { return 1f - Mathf.Pow(1f - t, 3f); }
        public static float InOutCubic(float t) { return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f; }

        // ---- Quart / Quint / Expo ----
        public static float InQuart(float t) { return t * t * t * t; }
        public static float OutQuart(float t) { return 1f - Mathf.Pow(1f - t, 4f); }
        public static float InExpo(float t) { return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f); }
        public static float OutExpo(float t) { return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t); }
        public static float InOutExpo(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
        }

        // ---- Circ ----
        public static float InCirc(float t) { return 1f - Mathf.Sqrt(1f - t * t); }
        public static float OutCirc(float t) { return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f)); }
        public static float InOutCirc(float t)
        {
            return t < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f;
        }

        // ---- Back（回弹超调）----
        public static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float InOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) * 0.5f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
        }

        // ---- Elastic（弹簧）----
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        public static float InElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
        }

        // ---- Bounce ----
        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
        public static float InBounce(float t) { return 1f - OutBounce(1f - t); }

        /// <summary>按名字取曲线（供动画系统统一注册使用）。</summary>
        public static Func<float, float> Get(string name)
        {
            switch (name)
            {
                case "InSine": return InSine;
                case "OutSine": return OutSine;
                case "InOutSine": return InOutSine;
                case "InQuad": return InQuad;
                case "OutQuad": return OutQuad;
                case "InOutQuad": return InOutQuad;
                case "InCubic": return InCubic;
                case "OutCubic": return OutCubic;
                case "InOutCubic": return InOutCubic;
                case "InQuart": return InQuart;
                case "OutQuart": return OutQuart;
                case "InExpo": return InExpo;
                case "OutExpo": return OutExpo;
                case "InOutExpo": return InOutExpo;
                case "InCirc": return InCirc;
                case "OutCirc": return OutCirc;
                case "InOutCirc": return InOutCirc;
                case "InBack": return InBack;
                case "OutBack": return OutBack;
                case "InOutBack": return InOutBack;
                case "OutElastic": return OutElastic;
                case "InElastic": return InElastic;
                case "OutBounce": return OutBounce;
                case "InBounce": return InBounce;
                default: return OutQuad;
            }
        }
    }
}
