using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// Fluent Reveal 扫光：鼠标进入时顶部 1px 渐变光带出现并横向往返流动，离开时淡出。
    /// 挂在目标面板/按钮上（组件自动给目标加顶部光带 Image）。
    /// </summary>
    public sealed class RevealLight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _host;
        private Image _light;
        private Coroutine _routine;

        public static void Attach(RectTransform host, Color tint)
        {
            if (host == null || host.GetComponent<RevealLight>() != null) return;
            var fx = host.gameObject.AddComponent<RevealLight>();
            fx._host = host;
            fx._tint = tint;
            var light = UiFactory.CreateImage(host, "RevealLight", new Color(1f, 1f, 1f, 0f));
            light.raycastTarget = false;
            light.sprite = SpriteFactory.GradientTop(Color.white);
            var rt = light.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.sizeDelta = new Vector2(-16f, 3f);
            fx._light = light;
        }

        private Color _tint = new Color(0.45f, 0.72f, 0.96f, 1f);

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopRoutine();
            _routine = StartCoroutine(Enter());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopRoutine();
            _routine = StartCoroutine(Exit());
        }

        private void StopRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator Enter()
        {
            const float fade = 0.18f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                var c = _light.color;
                c.r = _tint.r; c.g = _tint.g; c.b = _tint.b;
                c.a = Mathf.Lerp(c.a, 0.55f, t / fade);
                _light.color = c;
                yield return null;
            }

            // 横向往返扫动
            const float period = 1.6f;
            while (true)
            {
                float k = Time.unscaledTime % period;
                float x = Mathf.Lerp(-70f, 70f, Mathf.Sin(k / period * Mathf.PI * 2f) * 0.5f + 0.5f);
                var rt = _light.rectTransform;
                rt.anchoredPosition = new Vector2(x, -8f);
                yield return null;
            }
        }

        private IEnumerator Exit()
        {
            const float fade = 0.25f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                var c = _light.color;
                c.a = Mathf.Lerp(c.a, 0f, t / fade);
                _light.color = c;
                yield return null;
            }
            var c2 = _light.color;
            c2.a = 0f;
            _light.color = c2;
        }
    }
}
