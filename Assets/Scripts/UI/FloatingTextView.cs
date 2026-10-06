using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 飘字池：把世界坐标的反馈（伤害数字 / 金币奖励 / 生命损失）换算到屏幕坐标，
    /// 在 UI 画布上浮动淡出。对象池复用，未激活时零成本；
    /// 暂停/结束时 Time.timeScale=0，飘字随之冻结（不会出现「暂停还在跳数字」）。
    /// </summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        private static FloatingTextView _layer;
        private static RectTransform _layerRect;
        private static readonly ObjectPool<FloatingTextView> Pool = new ObjectPool<FloatingTextView>(CreateNew, 0);

        private UiText _text;
        private float _elapsed;
        private float _lifetime;
        private float _riseSpeed;
        private float _driftSpeed;
        private float _startScale;

        /// <summary>挂到 HUD 画布并预热飘字池。</summary>
        public static void Init(Transform canvasRoot)
        {
            if (_layer != null) return;

            var go = new GameObject("FloatingTextLayer", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            _layerRect = go.GetComponent<RectTransform>();
            UiFactory.Stretch(_layerRect);
            _layer = go.AddComponent<FloatingTextView>();

            for (int i = 0; i < 32; i++)
            {
                var view = Pool.Get();
                Pool.Release(view);
            }
        }

        /// <summary>在世界坐标处弹出一行飘字（任意 UI 状态均可安全调用）。</summary>
        public static void SpawnWorld(Vector3 worldPos, string text, Color color, float lifetime = 0.8f, int fontSize = 14)
        {
            if (_layer == null || Camera.main == null) return;

            var view = Pool.Get();
            var screen = Camera.main.WorldToScreenPoint(worldPos);
            if (screen.z < 0f)
            {
                Pool.Release(view);
                return;
            }
            screen.z = 0f;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_layerRect, screen, null, out var local))
            {
                Pool.Release(view);
                return;
            }

            view.Play(local, text, color, lifetime, fontSize);
        }

        private static FloatingTextView CreateNew()
        {
            var text = UiFactory.CreateText(_layer.transform, "Float", string.Empty, 14, Color.white, TextAnchor.MiddleCenter, true);

            var view = text.gameObject.AddComponent<FloatingTextView>();
            view._text = text;
            return view;
        }

        private void Play(Vector2 localPos, string text, Color color, float lifetime, int fontSize)
        {
            _text.text = text;
            _text.color = color;
            _text.fontSize = fontSize;

            _elapsed = 0f;
            _lifetime = Mathf.Max(0.2f, lifetime);
            _riseSpeed = Random.Range(42f, 62f);
            _driftSpeed = Random.Range(-16f, 16f);
            _startScale = Random.Range(0.75f, 0.95f);

            var rt = (RectTransform)transform;
            rt.anchoredPosition = localPos;
            rt.localScale = Vector3.one * _startScale;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_text == null) return; // 图层根节点自身也挂了本组件，仅容器职责

            _elapsed += Time.deltaTime;
            float t = _elapsed / _lifetime;
            if (t >= 1f)
            {
                Pool.Release(this);
                return;
            }

            var rt = (RectTransform)transform;
            rt.anchoredPosition += new Vector2(_driftSpeed * Time.deltaTime, _riseSpeed * Time.deltaTime);

            // 开场快速放大到位，主体时段保持 1，结尾淡出
            float pop = Mathf.Lerp(_startScale, 1f, Mathf.Clamp01(t * 4f));
            rt.localScale = Vector3.one * pop;

            var c = _text.color;
            c.a = 1f - Mathf.Clamp01((t - 0.55f) / 0.45f);
            _text.color = c;
        }
    }
}
