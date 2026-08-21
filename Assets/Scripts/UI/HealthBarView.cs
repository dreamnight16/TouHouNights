using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 世界空间的血条：用两个 SpriteRenderer（底 + 填充）拼成，避免每帧新建 UI 开销。
    /// 挂在敌人头顶，随敌人移动。
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        private Transform _fill;
        private SpriteRenderer _fillSr;
        private float _width;
        private float _height;

        public static HealthBarView Attach(Transform parent, float width, float height, float yOffset)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var view = go.AddComponent<HealthBarView>();
            view.Build(width, height);
            return view;
        }

        private void Build(float width, float height)
        {
            _width = width;
            _height = height;

            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = SpriteFactory.Square(1f, new Color(0f, 0f, 0f, 0.75f));
            bgSr.sortingOrder = 20;
            bg.transform.localScale = new Vector3(width, height, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(transform, false);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = SpriteFactory.Square(1f, new Color(0.30f, 1f, 0.35f, 0.95f));
            fillSr.sortingOrder = 21;
            _fill = fill.transform;
            _fillSr = fillSr;

            SetRatio(1f);
        }

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _fill.localScale = new Vector3(_width * ratio, _height, 1f);
            // 左对齐，血条从左往右缩短。
            _fill.localPosition = new Vector3(-_width * (1f - ratio) * 0.5f, 0f, 0f);
            _fillSr.color = HealthColor(ratio);
        }

        private static Color HealthColor(float ratio)
        {
            var green = new Color(0.30f, 1f, 0.35f, 0.95f);
            var yellow = new Color(1f, 0.85f, 0.25f, 0.95f);
            var red = new Color(1f, 0.30f, 0.30f, 0.95f);

            if (ratio >= 0.5f)
            {
                return Color.Lerp(yellow, green, (ratio - 0.5f) * 2f);
            }
            return Color.Lerp(red, yellow, ratio * 2f);
        }
    }
}
