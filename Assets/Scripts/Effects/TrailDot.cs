using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 对象池复用的弹道拖尾光点，生成后缩小并淡出。
    /// </summary>
    public sealed class TrailDot : MonoBehaviour
    {
        private static readonly ObjectPool<TrailDot> Pool = new ObjectPool<TrailDot>(CreateNew, 48);

        private SpriteRenderer _sr;
        private float _elapsed;
        private readonly float _life = 0.28f;
        private float _size = 1f;
        private Color _color;

        public static void Spawn(Vector2 pos, Color color, float size)
        {
            var dot = Pool.Get();
            dot.transform.position = new Vector3(pos.x, pos.y, 0f);
            dot.Setup(color, size);
        }

        private static TrailDot CreateNew()
        {
            var go = new GameObject("TrailDot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            sr.sprite = SpriteFactory.Circle(0.05f, Color.white);
            var dot = go.AddComponent<TrailDot>();
            dot._sr = sr;
            return dot;
        }

        private void Setup(Color color, float size)
        {
            _elapsed = 0f;
            _color = color;
            _size = size;
            _sr.color = new Color(color.r, color.g, color.b, 0.45f);
            transform.localScale = Vector3.one * size;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float k = _elapsed / _life;
            if (k >= 1f)
            {
                Pool.Release(this);
                return;
            }

            var c = _color;
            c.a = (1f - k) * (1f - k) * 0.45f;
            _sr.color = c;
            transform.localScale = Vector3.one * (_size * (1f - k * 0.6f));
        }
    }
}
