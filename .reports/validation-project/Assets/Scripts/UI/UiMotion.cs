using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    public sealed class UiMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable _control;
        private Image _light;
        private bool _hover, _pressed, _focused;
        private float _value;

        public static void Attach(Selectable control)
        {
            var motion = control.gameObject.AddComponent<UiMotion>();
            motion._control = control;
            motion._light = UiFactory.CreateImage(control.transform, "FocusLight", BattleUiTheme.Action);
            motion._light.raycastTarget = false;
            var rect = motion._light.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(3, 0);
            motion._light.color = Color.clear;
        }

        private void Update()
        {
            bool enabled = _control != null && _control.IsInteractable();
            float target = enabled && (_hover || _focused) ? 1 : 0;
            _value = Mathf.Lerp(_value, target, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
            _light.color = BattleUiTheme.WithAlpha(BattleUiTheme.Action, _value);
            float scale = enabled && _pressed ? .975f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale,
                1 - Mathf.Exp(-22 * Time.unscaledDeltaTime));
        }
        private void OnDisable() { _hover = _pressed = _focused = false; transform.localScale = Vector3.one; }
        public void OnPointerEnter(PointerEventData e) { _hover = true; }
        public void OnPointerExit(PointerEventData e) { _hover = _pressed = false; }
        public void OnPointerDown(PointerEventData e) { _pressed = true; }
        public void OnPointerUp(PointerEventData e) { _pressed = false; }
        public void OnSelect(BaseEventData e) { _focused = true; }
        public void OnDeselect(BaseEventData e) { _focused = _pressed = false; }
    }
}
