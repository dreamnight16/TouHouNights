using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 塔的放置交互：鼠标悬停显示格子高亮 + 格子化攻击范围预览，点击合法格子放置所选塔。
    /// 鼠标输入全部走 IMGUI 的 <see cref="Event"/>，不依赖旧版 Input 类，
    /// 因此不受 Unity 6 的「Active Input Handling（旧/新 Input System）」影响。
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        private const int MinCellX = -8;
        private const int MaxCellX = 8;
        private const int MinCellY = -4;
        private const int MaxCellY = 4;

        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();
        private readonly List<SpriteRenderer> _rangeCells = new List<SpriteRenderer>();

        private TowerType _selectedType = TowerType.Gun;
        private SpriteRenderer _hoverCell;

        public TowerType SelectedType => _selectedType;

        public void Init(Transform parent)
        {
            _hoverCell = CreateSpriteObject(parent, "HoverCell", 2);
            _hoverCell.sprite = SpriteFactory.Square(0.94f, GameConfig.GridHoverColor);
            _hoverCell.gameObject.SetActive(false);

            BuildRangeCellPool(parent);
        }

        public void SelectTower(TowerType type)
        {
            _selectedType = type;
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            var cam = Camera.main;
            if (gm == null || cam == null) return;

            // Event.current.mousePosition 是 GUI 坐标（左上角原点、y 向下）。
            var guiPosition = Event.current.mousePosition;

            if (gm.State != GameState.Building)
            {
                HidePreview();
                return;
            }

            // 转换为屏幕坐标（左下角原点）再映射到世界坐标。
            var screenPosition = new Vector3(guiPosition.x, Screen.height - guiPosition.y, 0f);
            var mouseWorld = cam.ScreenToWorldPoint(screenPosition);

            var cell = gm.Map.WorldToCell(mouseWorld);
            bool inBounds = cell.x >= MinCellX && cell.x <= MaxCellX && cell.y >= MinCellY && cell.y <= MaxCellY;
            bool blocked = inBounds && (gm.Map.IsCellBlocked(cell) || _occupiedCells.Contains(cell));

            var definition = GameConfig.Towers[_selectedType];

            if (inBounds)
            {
                _hoverCell.gameObject.SetActive(true);
                _hoverCell.transform.position = gm.Map.CellToWorld(cell);
                _hoverCell.color = blocked ? GameConfig.GridBlockedColor : GameConfig.GridHoverColor;

                UpdateRangePreview(cell, definition.RangeCells);
            }
            else
            {
                HidePreview();
            }

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && inBounds
                && !blocked
                && !HudLayout.IsPointerOverGui(guiPosition))
            {
                TryPlace(cell, definition);
            }
        }

        private void BuildRangeCellPool(Transform parent)
        {
            int maxRange = 0;
            foreach (var tower in GameConfig.Towers.Values)
            {
                maxRange = Mathf.Max(maxRange, tower.RangeCells);
            }

            int side = maxRange * 2 + 1;
            for (int i = 0; i < side * side; i++)
            {
                var sr = CreateSpriteObject(parent, "RangeCell", 1);
                sr.sprite = SpriteFactory.Square(0.94f, GameConfig.RangeCellColor);
                sr.gameObject.SetActive(false);
                _rangeCells.Add(sr);
            }
        }

        private void UpdateRangePreview(Vector2Int center, int rangeCells)
        {
            var map = GameManager.Instance.Map;
            int index = 0;

            for (int dx = -rangeCells; dx <= rangeCells; dx++)
            {
                for (int dy = -rangeCells; dy <= rangeCells; dy++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dy);
                    if (cell.x < MinCellX || cell.x > MaxCellX || cell.y < MinCellY || cell.y > MaxCellY) continue;
                    if (index >= _rangeCells.Count) break;

                    var sr = _rangeCells[index++];
                    sr.gameObject.SetActive(true);
                    sr.transform.position = map.CellToWorld(cell);
                }
            }

            for (int i = index; i < _rangeCells.Count; i++)
            {
                _rangeCells[i].gameObject.SetActive(false);
            }
        }

        private void HidePreview()
        {
            _hoverCell.gameObject.SetActive(false);
            for (int i = 0; i < _rangeCells.Count; i++)
            {
                _rangeCells[i].gameObject.SetActive(false);
            }
        }

        private void TryPlace(Vector2Int cell, TowerDefinition definition)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.TrySpendGold(definition.Cost)) return;

            var position = gm.Map.CellToWorld(cell);
            var go = new GameObject(definition.DisplayName);
            go.transform.SetParent(gm.WorldRoot, false);
            var tower = go.AddComponent<Tower>();
            tower.Configure(definition, position);

            _occupiedCells.Add(cell);
            EffectFactory.SpawnBurst(position, definition.Color, 0.5f, 0.25f);
        }

        private static SpriteRenderer CreateSpriteObject(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
