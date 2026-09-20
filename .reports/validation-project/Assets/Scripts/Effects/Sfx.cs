using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 程序化音效（Unity AudioClip 特性）：用数学函数合成短音效，
    /// 零外部音频资源——点击/放置/爆炸/弹幕/漏怪/击杀全有反馈（参考文章第 7 点「操作反馈」）。
    /// </summary>
    public static class Sfx
    {
        private const int SampleRate = 22050;

        private static AudioSource _source;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static float _lastExplode;
        private static float _lastKill;
        private static float _lastCombo;
        private static float _lastBeat;

        public static void Ensure()
        {
            if (_source != null) return;
            var go = new GameObject("Sfx");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.volume = 0.65f;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public static void Click()
        {
            Play(Get("click", 0.07f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.07f, 3f);
                return (Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 850f * t) * 0.25f) * env;
            }));
        }

        public static void Place()
        {
            Play(Get("place", 0.16f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.16f, 1.6f);
                float f = Mathf.Lerp(190f, 330f, Mathf.Clamp01(t / 0.12f));
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.25f) * env;
            }));
        }

        public static void Explode()
        {
            float now = Time.unscaledTime;
            if (now - _lastExplode < 0.12f) return;
            _lastExplode = now;

            Play(Get("explode", 0.42f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.42f, 2.2f);
                float noise = UnityEngine.Random.value * 2f - 1f;
                return noise * 0.45f * env + Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.5f * env;
            }));
        }

        public static void Barrage()
        {
            Play(Get("barrage", 0.5f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.5f, 1.4f);
                float f = Mathf.Lerp(520f, 1350f, t / 0.5f);
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.3f) * env;
            }));
        }

        public static void Leak()
        {
            Play(Get("leak", 0.34f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.34f, 2.6f);
                float noise = UnityEngine.Random.value * 2f - 1f;
                return Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.62f * env + noise * 0.16f * env;
            }));
        }

        public static void Kill()
        {
            float now = Time.unscaledTime;
            if (now - _lastKill < 0.08f) return;
            _lastKill = now;

            Play(Get("kill", 0.09f, t =>
            {
                float env = Mathf.Pow(1f - t / 0.09f, 2.4f);
                return Mathf.Sin(2f * Mathf.PI * 1180f * t) * 0.5f * env;
            }));
        }

        /// <summary>连击鸣音：三连上升高频琶音（每 10 连击）。</summary>
        public static void Combo()
        {
            float now = Time.unscaledTime;
            if (now - _lastCombo < 0.15f) return;
            _lastCombo = now;

            Play(Get("combo", 0.24f, t =>
            {
                float seg = t / 0.08f;                 // 三段
                int idx = UnityEngine.Mathf.Min(2, (int)seg);
                float f = 905f * UnityEngine.Mathf.Pow(2f, idx * 0.125f); // 小调式上爬
                float env = UnityEngine.Mathf.Pow(1f - (seg - idx), 2f);
                return UnityEngine.Mathf.Sin(2f * UnityEngine.Mathf.PI * f * t) * 0.30f * env;
            }));
        }

        /// <summary>结界低血心音：低频双锤（结界不足时警报）。</summary>
        public static void Heartbeat()
        {
            float now = Time.unscaledTime;
            if (now - _lastBeat < 0.7f) return;
            _lastBeat = now;

            Play(Get("heartbeat", 0.56f, t =>
            {
                float env = 0f;
                float t1 = t - 0f;
                if (t1 >= 0f && t1 < 0.12f) env = 1f - t1 / 0.12f;
                float t2 = t - 0.30f;
                if (t2 >= 0f && t2 < 0.14f) env = UnityEngine.Mathf.Max(env, (1f - t2 / 0.14f) * 0.7f);
                return UnityEngine.Mathf.Sin(2f * UnityEngine.Mathf.PI * 58f * t) * 0.55f * env;
            }));
        }

        // ---- 内部 ----

        private static void Play(AudioClip clip)
        {
            if (clip == null) return;
            Ensure();
            _source.volume = TowerDefense.Core.GameSettings.SfxVolume;
            _source.PlayOneShot(clip);
        }

        private static AudioClip Get(string name, float duration, Func<float, float> synth)
        {
            if (Clips.TryGetValue(name, out var cached)) return cached;

            int n = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(synth(t), -1f, 1f);
            }

            var clip = AudioClip.Create("sfx_" + name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            Clips[name] = clip;
            return clip;
        }
    }
}
