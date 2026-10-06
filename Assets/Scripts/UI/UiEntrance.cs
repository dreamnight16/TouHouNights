using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 使用 anchoredPosition 从相对偏移滑到目标位置，同时淡入。
    /// delay 用于错开多个元素的入场时间。
    /// </summary>
    public sealed class UiEntrance : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _from;
        private Vector2 _to;
        private float _delay;
        private float _duration = .42f;
        private float _time;

        /// <summary>播放入场；重复调用仍使用第一次记录的目标位置。</summary>
        /// <param name="from">相对目标位置的偏移。</param>
        /// <param name="delay">入场延迟，单位秒。</param>
        public static UiEntrance Play(RectTransform target, Vector2 from, float delay = 0f, float duration = .42f)
        {
            if (target == null) return null;

            var entrance = target.GetComponent<UiEntrance>();
            if (entrance == null) entrance = target.gameObject.AddComponent<UiEntrance>();

            entrance._rect = target;
            entrance._group = target.GetComponent<CanvasGroup>();
            if (entrance._group == null) entrance._group = target.gameObject.AddComponent<CanvasGroup>();

            // 保留首次目标，避免重复播放时累积偏移。
            entrance._to = entrance._settled ? entrance._to : target.anchoredPosition;
            entrance._settled = true;
            entrance._from = entrance._to + from;
            entrance._delay = Mathf.Max(0f, delay);
            entrance._duration = Mathf.Max(.01f, duration);
            entrance._time = -entrance._delay;

            target.anchoredPosition = entrance._from;
            entrance._group.alpha = 0f;
            entrance.enabled = true;
            return entrance;
        }

        private bool _settled;

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            if (_time < 0f) return;

            float t = Mathf.Clamp01(_time / _duration);
            _rect.anchoredPosition = Vector2.LerpUnclamped(_from, _to, UiEasings.OutCubic(t));
            // 淡入先于位移完成。
            if (_group != null) _group.alpha = Mathf.Clamp01(t * 1.8f);

            if (t < 1f) return;

            _rect.anchoredPosition = _to;
            if (_group != null) _group.alpha = 1f;
            enabled = false;
        }
    }
}
