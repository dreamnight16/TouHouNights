using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 攻击范围指示（new.md 第 8 节：「攻击范围一定要做得漂亮 —— 非常干净的几何图形」）。
    ///
    /// 重构前：范围内每一格都铺一张半透明圆角方块 → 格子越多越糊，读起来是「一坨蓝」。
    /// 重构后：范围被表达为一个几何体 ——
    ///   ① 极淡的整体蒙版（告诉玩家「这片地」）
    ///   ② 四条细边线（告诉玩家「边界在哪」—— 这是玩家真正要看的信息）
    ///   ③ 四角 L 型角标（方舟 / Sci-fi HUD 的标志性细节，让范围像仪器取景框）
    ///   ④ 内部格线（保留「格子」语义，但極淡，不抢戏）
    /// 只消耗 13 + 池化格线个固定对象，不随范围增大而反复创建。
    /// </summary>
    public sealed class HudRangeIndicator : MonoBehaviour
    {
        private const int FillOrder = WorldArt.LayerRange;
        private const int LineOrder = WorldArt.LayerRangeLine;

        private const float EdgeThickness = 0.05f;
        private const float BracketThickness = 0.085f;
        private const float BracketLength = 0.46f;
        private const float GridThickness = 0.022f;

        // 语义化透明度：面 → 线 → 角，依次增强，让「边界」成为视觉主体。
        private const float FillAlpha = 0.085f;
        private const float GridAlpha = 0.14f;
        private const float EdgeAlpha = 0.55f;
        private const float BracketAlpha = 0.92f;

        private SpriteRenderer _fill;
        private readonly List<SpriteRenderer> _edges = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _brackets = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _gridLines = new List<SpriteRenderer>();

        private bool _shown;
        private float _pulse;

        /// <summary>在自身节点下构建固定数量的几何图元（只建一次，之后只切换显示与尺寸）。</summary>
        public void Init()
        {
            _fill = CreateBar(transform, "RangeFill", FillOrder);

            for (int i = 0; i < 4; i++)
            {
                _edges.Add(CreateBar(transform, "RangeEdge", LineOrder));
            }

            for (int i = 0; i < 8; i++)
            {
                _brackets.Add(CreateBar(transform, "RangeBracket", LineOrder));
            }

            int maxRange = 0;
            foreach (var tower in GameConfig.Towers.Values)
            {
                maxRange = Mathf.Max(maxRange, tower.RangeCells);
            }
            // 内部格线：每个方向 2 * range 条，两个方向共 4 * range 条。
            for (int i = 0; i < Mathf.Max(0, maxRange) * 4; i++)
            {
                _gridLines.Add(CreateBar(transform, "RangeGridLine", LineOrder));
            }

            Hide();
        }

        /// <summary>以 <paramref name="centerWorld"/> 为中心显示一个边长 (2*range+1) 格的范围框。</summary>
        public void Show(Vector2 centerWorld, int rangeCells, Color tint)
        {
            if (_fill == null) return;

            float cell = GameConfig.GridCellSize;
            float half = (rangeCells + 0.5f) * cell;
            float size = half * 2f;

            Color fill = new Color(tint.r, tint.g, tint.b, FillAlpha);
            Color edge = new Color(tint.r, tint.g, tint.b, EdgeAlpha);
            Color grid = new Color(tint.r, tint.g, tint.b, GridAlpha);

            // ① 整体蒙版：一张 1×1 白方片缩放而成，硬边直角 —— 刻意保留「几何感」
            _fill.gameObject.SetActive(true);
            _fill.color = fill;
            _fill.transform.position = new Vector3(centerWorld.x, centerWorld.y, 0f);
            _fill.transform.localScale = new Vector3(size, size, 1f);

            // ② 四条边线
            Place(_edges[0], centerWorld + new Vector2(0f, half), new Vector2(size, EdgeThickness), edge);
            Place(_edges[1], centerWorld - new Vector2(0f, half), new Vector2(size, EdgeThickness), edge);
            Place(_edges[2], centerWorld - new Vector2(half, 0f), new Vector2(EdgeThickness, size), edge);
            Place(_edges[3], centerWorld + new Vector2(half, 0f), new Vector2(EdgeThickness, size), edge);

            // ③ 四角 L 型角标：横向短棒 + 纵向短棒，各自从角点向内延伸
            int b = 0;
            for (int sy = 1; sy >= -1; sy -= 2)
            {
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var corner = centerWorld + new Vector2(sx * half, sy * half);
                    Place(_brackets[b++], corner - new Vector2(sx * BracketLength * 0.5f, 0f),
                        new Vector2(BracketLength, BracketThickness), edge);
                    Place(_brackets[b++], corner - new Vector2(0f, sy * BracketLength * 0.5f),
                        new Vector2(BracketThickness, BracketLength), edge);
                }
            }

            // ④ 内部格线：保留格子语义
            int used = 0;
            for (int k = -rangeCells; k < rangeCells && used < _gridLines.Count; k++)
            {
                float offset = (k + 0.5f) * cell;
                Place(_gridLines[used++], centerWorld + new Vector2(offset, 0f),
                    new Vector2(GridThickness, size), grid);
            }
            for (int k = -rangeCells; k < rangeCells && used < _gridLines.Count; k++)
            {
                float offset = (k + 0.5f) * cell;
                Place(_gridLines[used++], centerWorld + new Vector2(0f, offset),
                    new Vector2(size, GridThickness), grid);
            }
            for (int i = used; i < _gridLines.Count; i++)
            {
                _gridLines[i].gameObject.SetActive(false);
            }

            _shown = true;
        }

        public void Hide()
        {
            if (_fill != null) _fill.gameObject.SetActive(false);
            for (int i = 0; i < _edges.Count; i++) _edges[i].gameObject.SetActive(false);
            for (int i = 0; i < _brackets.Count; i++) _brackets[i].gameObject.SetActive(false);
            for (int i = 0; i < _gridLines.Count; i++) _gridLines[i].gameObject.SetActive(false);
            _shown = false;
        }

        private void Update()
        {
            if (!_shown) return;

            // 角标呼吸：唯一允许的持续动效 —— 让「当前预览的范围」有生命感，但不喧宾夺主。
            _pulse += Time.unscaledDeltaTime;
            float a = BracketAlpha * (0.78f + 0.22f * Mathf.Sin(_pulse * 2.2f));
            for (int i = 0; i < _brackets.Count; i++)
            {
                var sr = _brackets[i];
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, a);
            }
        }

        private static void Place(SpriteRenderer sr, Vector2 center, Vector2 size, Color color)
        {
            sr.gameObject.SetActive(true);
            sr.color = color;
            sr.transform.position = new Vector3(center.x, center.y, 0f);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private SpriteRenderer CreateBar(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(1f, Color.white);
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
