using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 相机震动，幅度按剩余时间比例的平方衰减，衰减进度使用缩放时间。
    /// </summary>
    public sealed class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Current { get; private set; }

        private Vector3 _basePosition;
        private float _intensity;
        private float _elapsed;
        private float _duration;

        private void Awake()
        {
            Current = this;
            _basePosition = transform.position;
        }

        public static void Shake(float intensity, float duration)
        {
            if (Current == null || !TowerDefense.Core.GameSettings.CameraMotion) return;
            Current.Play(intensity, duration);
        }

        private void Play(float intensity, float duration)
        {
            _intensity = Mathf.Max(_intensity * (1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.001f, _duration))), intensity);
            _duration = Mathf.Max(duration, 0.05f);
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_duration <= 0f) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                transform.position = _basePosition;
                _duration = 0f;
                _intensity = 0f;
                return;
            }

            float t = 1f - _elapsed / _duration;
            float amp = _intensity * t * t; // 平方衰减，落点平滑
            var offset = new Vector3(
                (Mathf.PerlinNoise(Time.unscaledTime * 37f, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, Time.unscaledTime * 37f) - 0.5f) * 2f,
                0f) * amp;
            transform.position = _basePosition + offset;
        }
    }
}
