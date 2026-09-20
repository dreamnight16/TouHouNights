using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 世界空间的血条（三层：底 + 延迟条 + 当前填充）：
    /// - 底层：黑色背景轨道；
    /// - 延迟条（白色/灰色）：受攻击时缓慢追赶当前血量，让玩家直观看到「这一发打了多少伤害」；
    /// - 填充条：当前血量，即时变化，颜色随血量从绿→黄→红。
    /// 参考 design.md 第 18 节：受到攻击时白色/灰色延迟条稍晚下降。
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        private Transform _fill;
        private SpriteRenderer _fillSr;
        private Transform _delayed;
        private SpriteRenderer _delayedSr;
        private float _width;
        private float _height;

        private float _currentRatio = 1f;   // 当前血量比例（即时）
        private float _delayedRatio = 1f;  // 延迟条比例（缓慢追赶）
        private float _delaySpeed = 3.5f;  // 延迟条追赶速度（比例/秒）

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

            // 底层：黑色背景轨道
            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = SpriteFactory.RoundedSquare(1f, 0.30f, new Color(0f, 0f, 0f, 0.78f));
            bgSr.sortingOrder = WorldArt.LayerBar;
            bg.transform.localScale = new Vector3(width, height, 1f);

            // 延迟条（白色/浅灰）：受击后缓慢追赶，制造「伤害可视化」效果
            var delayed = new GameObject("Delayed");
            delayed.transform.SetParent(transform, false);
            var delayedSr = delayed.AddComponent<SpriteRenderer>();
            delayedSr.sprite = SpriteFactory.RoundedSquare(1f, 0.30f, new Color(0.85f, 0.88f, 0.92f, 0.55f));
            delayedSr.sortingOrder = WorldArt.LayerBar + 1;
            _delayed = delayed.transform;
            _delayedSr = delayedSr;

            // 当前填充条：即时反映血量，颜色随血量变化
            var fill = new GameObject("Fill");
            fill.transform.SetParent(transform, false);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = SpriteFactory.RoundedSquare(1f, 0.30f, Color.white);
            fillSr.sortingOrder = WorldArt.LayerBar + 2;
            _fill = fill.transform;
            _fillSr = fillSr;

            SetRatio(1f);
        }

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _currentRatio = ratio;
            ApplyFill(ratio);
        }

        private void Update()
        {
            // 延迟条缓慢追赶当前血量（只在血量下降时延迟，回血时立即同步）
            if (_delayedRatio > _currentRatio + 0.001f)
            {
                _delayedRatio = Mathf.MoveTowards(_delayedRatio, _currentRatio, _delaySpeed * Time.deltaTime);
                ApplyDelayed(_delayedRatio);
            }
            else if (_delayedRatio < _currentRatio - 0.001f)
            {
                // 回血时延迟条立即追上（不产生误导）
                _delayedRatio = _currentRatio;
                ApplyDelayed(_delayedRatio);
            }
        }

        private void ApplyFill(float ratio)
        {
            _fill.localScale = new Vector3(_width * ratio, _height, 1f);
            _fill.localPosition = new Vector3(-_width * (1f - ratio) * 0.5f, 0f, 0f);
            _fillSr.color = HealthColor(ratio);
        }

        private void ApplyDelayed(float ratio)
        {
            _delayed.localScale = new Vector3(_width * ratio, _height, 1f);
            _delayed.localPosition = new Vector3(-_width * (1f - ratio) * 0.5f, 0f, 0f);
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
