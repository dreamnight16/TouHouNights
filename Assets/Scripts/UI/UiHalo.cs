using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// UiPanel 的动态辉光层，复制目标几何并通过 CanvasRenderer alpha
    /// 控制淡入淡出、呼吸和闪光，避免逐帧重建网格。
    /// </summary>
    public sealed class UiHalo : MonoBehaviour
    {
        /// <summary>亮起速率。</summary>
        private const float RiseSpeed = 13f;

        /// <summary>熄灭速率，比亮起更慢。</summary>
        private const float FallSpeed = 7f;

        private CanvasRenderer _renderer;
        private float _current;
        private float _target;
        private float _breatheSpeed;
        private float _breatheDepth;
        private float _flash;

        /// <summary>
        /// 添加辉光层，四角形状从目标面板复制。
        /// </summary>
        /// <param name="color">辉光颜色。</param>
        /// <param name="width">向外淡出的宽度。</param>
        public static UiHalo Attach(UiPanel target, Color color, float width = 18f)
        {
            var halo = UiKit.Surface(target.transform, "Halo", UiSurfaceKind.Veil);
            halo.Corners(target.CornerMode, target.CornerSize, target.IsRounded);
            // 只绘制外辉光。
            halo.Flat(Color.clear)
                .Border(Color.clear, 0f)
                .Rim(Color.clear, 0f)
                .Foot(Color.clear, 0f)
                .Glow(color, width);

            UiKit.Stretch(halo.rectTransform);
            halo.raycastTarget = false;

            var component = halo.gameObject.AddComponent<UiHalo>();
            component._renderer = halo.canvasRenderer;
            component._renderer.SetAlpha(0f);
            // 辉光放在文字与图标下方。
            halo.transform.SetSiblingIndex(0);
            return component;
        }

        /// <summary>设置呼吸频率与衰减深度。</summary>
        public UiHalo Breathe(float speed = 2.6f, float depth = .38f)
        {
            _breatheSpeed = speed;
            _breatheDepth = Mathf.Clamp01(depth);
            return this;
        }

        /// <summary>开关辉光；instant 为真时立即应用目标亮度。</summary>
        public UiHalo Set(bool on, bool instant = false)
        {
            _target = on ? 1f : 0f;
            if (instant)
            {
                _current = _target;
                if (_renderer != null) _renderer.SetAlpha(_current);
            }
            return this;
        }

        /// <summary>一次性闪一下（数值变化、被击中、技能就绪）。</summary>
        public void Flash(float amount = .85f)
        {
            _flash = Mathf.Max(_flash, Mathf.Clamp01(amount));
        }

        private void Update()
        {
            if (_renderer == null) return;

            float speed = _target > _current ? RiseSpeed : FallSpeed;
            _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

            float value = _current;
            if (_breatheDepth > 0f && value > .01f)
            {
                // 呼吸仅衰减亮度，峰值保持在目标值内。
                value *= 1f - _breatheDepth * (.5f - .5f * Mathf.Cos(Time.unscaledTime * _breatheSpeed));
            }

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - Time.unscaledDeltaTime * 2.6f);
                value = Mathf.Min(1f, value + _flash * _flash);
            }

            _renderer.SetAlpha(Mathf.Clamp01(value));
        }

        private void OnDisable()
        {
            _current = _target = 0f;
            _flash = 0f;
            if (_renderer != null) _renderer.SetAlpha(0f);
        }
    }
}
