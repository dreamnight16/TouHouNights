using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// 按钮的悬停、按下和聚焦反馈。高亮层复制 UiPanel 的几何，
    /// 通过 CanvasRenderer alpha 淡入淡出，避免逐帧修改顶点色并重建网格。
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
        /// 添加交互反馈，高亮层复制同一对象上 UiPanel 的四角形状。
        /// </summary>
        /// <param name="accent">高亮描边与外辉光的颜色，默认使用骨白强调边。</param>
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

            // 高亮层放在文字下方，避免覆盖内容。
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
