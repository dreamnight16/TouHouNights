using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Effects;

namespace TowerDefense.UI
{
    /// <summary>
    /// 按钮点击回弹（Fluent 弹簧动效）：按下 0.94 → 回弹 1.05 → 稳定 1。
    /// 由 UiFactory.CreateButton 自动挂载，无需手动接入。
    /// </summary>
    public sealed class UiButtonFx : MonoBehaviour
    {
        private Button _button;

        public static void Attach(Button button)
        {
            if (button == null || button.GetComponent<UiButtonFx>() != null) return;
            var fx = button.gameObject.AddComponent<UiButtonFx>();
            fx._button = button;
        }

        private void Start()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(Punch);
            }
        }

        private void Punch()
        {
            Sfx.Click();
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var rt = (RectTransform)transform;

            // 按下
            const float press = 0.06f;
            float t = 0f;
            while (t < press)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, t / press);
                yield return null;
            }

            // 回弹
            const float spring = 0.14f;
            t = 0f;
            while (t < spring)
            {
                t += Time.unscaledDeltaTime;
                float p = t / spring;
                rt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.05f, p);
                yield return null;
            }

            // 稳定
            const float settle = 0.10f;
            t = 0f;
            while (t < settle)
            {
                t += Time.unscaledDeltaTime;
                float p = t / settle;
                rt.localScale = Vector3.one * Mathf.Lerp(1.05f, 1f, p);
                yield return null;
            }

            rt.localScale = Vector3.one;
        }
    }
}
