using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 一次性扩散淡出特效（命中 / 爆炸 / 死亡）。
    /// 三层组合：闪光内核（快速白闪）+ 扩散环（Shell 描边外扩）+ 主体扩散（实心圆淡出）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BurstEffect : MonoBehaviour
    {
        private SpriteRenderer _sprite;
        private SpriteRenderer _ring;
        private SpriteRenderer _flash;
        private float _duration;
        private float _elapsed;

        public void Init(Color color, float radius, float duration)
        {
            _sprite = GetComponent<SpriteRenderer>();
            _sprite.sprite = SpriteFactory.Circle(radius, color);
            _sprite.sortingOrder = WorldArt.LayerBurst;
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
            transform.localScale = Vector3.one * 0.3f;

            if (_ring == null)
            {
                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(transform, false);
                _ring = ringGo.AddComponent<SpriteRenderer>();
                _ring.sortingOrder = WorldArt.LayerBurst + 1;
            }
            _ring.sprite = SpriteFactory.Shell(0.5f, 0.08f, color);
            _ring.color = new Color(1f, 1f, 1f, 0.85f);
            _ring.transform.localScale = Vector3.one * 0.3f;

            if (_flash == null)
            {
                var flashGo = new GameObject("Flash");
                flashGo.transform.SetParent(transform, false);
                _flash = flashGo.AddComponent<SpriteRenderer>();
                _flash.sortingOrder = WorldArt.LayerBurst + 2;
            }
            _flash.sprite = SpriteFactory.Circle(radius * 0.5f, new Color(1f, 1f, 1f, 0.92f));
            _flash.transform.localScale = Vector3.one * 0.3f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            float scale = Mathf.Lerp(0.3f, 1f, t);
            transform.localScale = Vector3.one * scale;

            var color = _sprite.color;
            color.a = 1f - t;
            _sprite.color = color;

            // 扩散环比主体更早淡出。
            if (_ring != null)
            {
                float ringT = Mathf.Clamp01(t * 1.3f);
                float ringScale = Mathf.Lerp(0.3f, 1.5f, ringT);
                _ring.transform.localScale = Vector3.one * ringScale;
                var rc = _ring.color;
                rc.a = (1f - ringT) * 0.85f;
                _ring.color = rc;
            }

            // 闪光只持续总时长的前 20%。
            if (_flash != null)
            {
                float flashT = Mathf.Clamp01(t / 0.2f);
                float flashScale = Mathf.Lerp(0.3f, 0.8f, flashT);
                _flash.transform.localScale = Vector3.one * flashScale;
                var fc = _flash.color;
                fc.a = (1f - flashT) * 0.92f;
                _flash.color = fc;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
