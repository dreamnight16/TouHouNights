using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 一层**可动画的辉光**，挂在一个 <see cref="UiPanel"/> 上。
    ///
    /// 这套界面表达层级只用两样东西：平涂的**明度台阶**，和**光**。
    /// 「有没有在发光」本身就是层级 —— 该被看见的东西亮起来，其余保持暗的，
    /// 不需要再加倒角、投影、粗描边这些「硬造 3D」的手法去说明谁在上面。
    /// 那些手法在暗底上只会让画面变脏，因为它们在模拟一种这套配色根本给不出的材质。
    ///
    /// 实现和 <see cref="UiMotion"/> 同一套路：一层几何与目标面板完全一致、
    /// 平时全透明的面板，只烘一圈 <c>Glow</c>，靠 <see cref="CanvasRenderer"/> 的 alpha 做插值。
    /// 每帧只改一个 float，绝不重建网格（重建会在每帧产生 GC 抖动）。
    /// </summary>
    public sealed class UiHalo : MonoBehaviour
    {
        /// <summary>亮起的速率。点灯要快 —— 玩家按下之后不该等光。</summary>
        private const float RiseSpeed = 13f;

        /// <summary>熄灭的速率。必须明显慢于亮起，快到对称就会读成「闪了一下」而不是「暗下去」。</summary>
        private const float FallSpeed = 7f;

        private CanvasRenderer _renderer;
        private float _current;
        private float _target;
        private float _breatheSpeed;
        private float _breatheDepth;
        private float _flash;

        /// <summary>
        /// 给一块面板挂一层辉光。四角几何从目标面板复制，所以辉光的轮廓和面板严格重合，
        /// 不会在外面多露出半个像素的直角。
        /// </summary>
        /// <param name="color">辉光色。这里传的颜色决定「发光意味着什么」：绯 = 就绪 / 选中，冷青 = 数据。</param>
        /// <param name="width">向外淡出的宽度。默认 18 已经足够柔和，再大就会糊成一片。</param>
        public static UiHalo Attach(UiPanel target, Color color, float width = 18f)
        {
            var halo = UiKit.Surface(target.transform, "Halo", UiSurfaceKind.Veil);
            halo.Corners(target.CornerMode, target.CornerSize, target.IsRounded);
            // 本体、描边、受光全部留空：这一层唯一画出来的东西就是那圈向外的光。
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
            // 排在文字与图标之前：辉光是**从面板背后透出来的**，不该盖在内容上。
            halo.transform.SetSiblingIndex(0);
            return component;
        }

        /// <summary>缓慢呼吸。只在「正在发生」的元素上用，全场同时呼吸的东西不超过一个。</summary>
        public UiHalo Breathe(float speed = 2.6f, float depth = .38f)
        {
            _breatheSpeed = speed;
            _breatheDepth = Mathf.Clamp01(depth);
            return this;
        }

        /// <summary>开关辉光。界面刚装配出来时可以传 <paramref name="instant"/>，免得开场所有东西一起亮一下。</summary>
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
                // 呼吸只做「变暗」，不做「变亮」：峰值应当就是设定值。
                // 允许它亮过设定值的话，呼吸的波峰会盖过主行动按钮，层级当场反过来。
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
