using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>一次性扩散淡出特效（命中 / 爆炸 / 死亡）。</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BurstEffect : MonoBehaviour
    {
        private SpriteRenderer _sprite;
        private float _duration;
        private float _elapsed;
        private float _maxRadius;

        public void Init(Color color, float radius, float duration)
        {
            _sprite = GetComponent<SpriteRenderer>();
            _sprite.sprite = SpriteFactory.Circle(radius, color);
            _sprite.sortingOrder = 30;
            _maxRadius = radius;
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
            transform.localScale = Vector3.one * 0.3f;
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

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
