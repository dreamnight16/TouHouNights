using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// 按钮的悬停 / 按下 / 聚焦反馈。
    ///
    /// 上一版是「加一条左侧亮条 + 整体缩放」。这一版改成：在按钮本体上盖一层
    /// **几何完全相同、平时全透明**的高亮层（描边 + 外辉光），靠 CanvasRenderer 的
    /// alpha 淡入淡出。
    ///
    /// 为什么不直接改 <see cref="UiPanel"/> 自己的颜色：那些颜色是烘进顶点里的，
    /// 每改一次就要重建整张网格 —— 而悬停是一个持续几十帧的插值。
    /// 淡入一层现成的网格则完全不需要重建。
    /// </summary>
    public sealed class UiMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private const float PressedScale = .975f;

        private Selectable _control;
        private UiPanel _highlight;
        private bool _hover, _pressed, _focused;
        private float _value;

        /// <summary>
        /// 给一个 Selectable 装上悬停反馈。它会自动寻找同一物体上的 <see cref="UiPanel"/>
        /// 并复制其四角几何 —— 高亮层的轮廓因此和本体严格重合，不会在外面露出一圈多余的直角。
        /// </summary>
        /// <param name="accent">高亮描边与外辉光的颜色。默认骨白强调边；主行动按钮建议传绯色。</param>
        public static UiMotion Attach(Selectable control, Color? accent = null)
        {
            var motion = control.gameObject.AddComponent<UiMotion>();
            motion._control = control;
            motion._highlight = BuildHighlight(control, accent ?? BattleUiTheme.EdgeStrong);
            return motion;
        }

        private static UiPanel BuildHighlight(Selectable control, Color accent)
        {
            var source = control.GetComponent<UiPanel>();
            var highlight = UiKit.Surface(control.transform, "Highlight", UiSurfaceKind.Veil);

            if (source != null)
            {
                highlight.Corners(source.CornerMode, source.CornerSize, source.IsRounded);
            }

            highlight.Flat(Color.clear)
                     .Border(accent, BattleUiTheme.Border)
                     .Rim(Color.clear, 0f)
                     .Foot(Color.clear, 0f)
                     .Glow(BattleUiTheme.WithAlpha(accent, accent.a * .5f), 11f);

            UiKit.Stretch((RectTransform)highlight.transform);
            highlight.raycastTarget = false;

            // 排在本体之后、文字之前 —— 压在本体上才有意义，盖住文字就变成一层雾。
            highlight.transform.SetSiblingIndex(Mathf.Min(1, control.transform.childCount - 1));
            highlight.canvasRenderer.SetAlpha(0f);
            return highlight;
        }

        private void Update()
        {
            bool interactable = _control != null && _control.IsInteractable();
            float target = interactable && (_hover || _focused) ? 1f : 0f;
            _value = Mathf.Lerp(_value, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));

            if (_highlight != null) _highlight.canvasRenderer.SetAlpha(_value);

            float scale = interactable && _pressed ? PressedScale : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale,
                1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
        }

        private void OnDisable()
        {
            _hover = _pressed = _focused = false;
            _value = 0f;
            if (_highlight != null) _highlight.canvasRenderer.SetAlpha(0f);
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e) { _hover = true; }
        public void OnPointerExit(PointerEventData e) { _hover = _pressed = false; }
        public void OnPointerDown(PointerEventData e) { _pressed = true; }
        public void OnPointerUp(PointerEventData e) { _pressed = false; }
        public void OnSelect(BaseEventData e) { _focused = true; }
        public void OnDeselect(BaseEventData e) { _focused = _pressed = false; }
    }
}
