using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 入场：从一段偏移滑到位，同时淡入。整套 HUD 的每一块都挂一个，<c>delay</c> 错峰，
    /// 于是战斗开始时界面是**依次落位**的，而不是「啪」一下全部出现在屏幕上。
    ///
    /// 这是整套界面里唯一一处「有编排」的地方，也是它读起来像游戏而不是像一块面板的原因：
    /// 层级不全靠静态样式表达，一部分是靠「谁先出现、谁后出现」讲出来的。
    ///
    /// 位置在 <c>anchoredPosition</c> 上做，所以对 <see cref="UiKit.PinTopLeft"/> 这类
    /// 锚点定位的元件天然可用 —— 不需要知道它被钉在屏幕的哪一边，偏移量由调用方按方向给。
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

        /// <summary>
        /// 播一次入场。重复调用会从当前位置重来（重开一局时用得上）。
        /// </summary>
        /// <param name="from">相对目标位置的**偏移**，不是绝对坐标。左上角的面板给 (-20, 0) 就是从左边滑进来。</param>
        /// <param name="delay">错峰延迟（秒）。同一排元件按顺序 +0.05 递增即可。</param>
        public static UiEntrance Play(RectTransform target, Vector2 from, float delay = 0f, float duration = .42f)
        {
            if (target == null) return null;

            var entrance = target.GetComponent<UiEntrance>();
            if (entrance == null) entrance = target.gameObject.AddComponent<UiEntrance>();

            entrance._rect = target;
            entrance._group = target.GetComponent<CanvasGroup>();
            if (entrance._group == null) entrance._group = target.gameObject.AddComponent<CanvasGroup>();

            // 目标位置在第一次调用时记下来。之后再播（重开一局）也回到同一个落点，
            // 否则会「每次重开都往下挪一截」。
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
            // OutCubic：起步快、落位慢。匀速滑动看起来像「被拖过去的」，缓出才像「自己停下的」。
            _rect.anchoredPosition = Vector2.LerpUnclamped(_from, _to, UiEasings.OutCubic(t));
            // 透明度比位移早一步走完：光比物体先到位，这是「亮起来」而不是「滑进来」。
            if (_group != null) _group.alpha = Mathf.Clamp01(t * 1.8f);

            if (t < 1f) return;

            _rect.anchoredPosition = _to;
            if (_group != null) _group.alpha = 1f;
            enabled = false;
        }
    }
}
