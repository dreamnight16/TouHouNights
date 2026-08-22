using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 塔的放置/撤退交互：鼠标悬停显示格子高亮 + 格子化攻击范围预览；
    /// 点击空地放塔，点击已有塔则撤退（返还 50%）。
    /// 使用 legacy Input + EventSystem 判定指针是否落在 UI 上（UGUI）。
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        private const int MinCellX = -8;
        private const int MaxCellX = 8;
        private const int MinCellY = -4;
        private const int MaxCellY = 4;

        private readonly Dictionary<Vector2Int, Tower> _occupiedTowers = new Dictionary<Vector2Int, Tower>();
        private readonly List<SpriteRenderer> _rangeCells = new List<SpriteRenderer>();

        private TowerType _selectedType = TowerType.Gun;
        private SpriteRenderer _hoverCell;

        public TowerType SelectedType => _selectedType;
        public Tower SelectedTower { get; private set; }

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

        public void FreeCell(Vector2Int cell)
        {
            _occupiedTowers.Remove(cell);
            if (SelectedTower != null && SelectedTower.Cell == cell)
            {
                SelectedTower = null;
            }
        }

        public void ClearSelection()
        {
            SelectedTower = null;
        }

        public void RetreatSelected()
        {
            var tower = SelectedTower;
            if (tower == null) return;
            SelectedTower = null;
            GameManager.Instance?.RetreatTower(tower);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            var cam = Camera.main;
            if (gm == null || cam == null) return;

            if (gm.State != GameState.Running)
            {
                HidePreview();
                return;
            }

            if (gm.IsPaused)
            {
                HidePreview(); // 暂停时只能观察，不处理放塔/选塔
                return;
            }

            // 指针在 UI 上时不处理放置/悬停。
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                HidePreview();
                return;
            }

            var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            var cell = gm.Map.WorldToCell(mouseWorld);

            bool inBounds = cell.x >= MinCellX && cell.x <= MaxCellX && cell.y >= MinCellY && cell.y <= MaxCellY;
            bool mapBlocked = inBounds && gm.Map.IsCellBlocked(cell);
            bool occupied = inBounds && _occupiedTowers.ContainsKey(cell);
            bool atLimit = !gm.CanPlaceTower;

            var definition = GameConfig.Towers[_selectedType];

            if (inBounds)
            {
                _hoverCell.gameObject.SetActive(true);
                _hoverCell.transform.position = gm.Map.CellToWorld(cell);

                if (occupied)
                {
                    _hoverCell.color = GameConfig.GridRetreatColor;
                    if (_occupiedTowers.TryGetValue(cell, out var occupiedTower))
                    {
                        UpdateRangePreview(cell, occupiedTower.Definition.RangeCells);
                    }
                }
                else
                {
                    _hoverCell.color = (mapBlocked || atLimit) ? GameConfig.GridBlockedColor : GameConfig.GridHoverColor;
                    UpdateRangePreview(cell, definition.RangeCells);
                }
            }
            else
            {
                HidePreview();
            }

            if (Input.GetMouseButtonDown(0) && inBounds)
            {
                if (_occupiedTowers.TryGetValue(cell, out var tower))
                {
                    SelectedTower = tower; // 选中查看，不立即撤退
                }
                else if (!mapBlocked && !atLimit)
                {
                    SelectedTower = null;
                    TryPlace(cell, definition);
                }
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

        private void HideRangePreview()
        {
            for (int i = 0; i < _rangeCells.Count; i++)
            {
                _rangeCells[i].gameObject.SetActive(false);
            }
        }

        private void HidePreview()
        {
            _hoverCell.gameObject.SetActive(false);
            HideRangePreview();
        }

        private void TryPlace(Vector2Int cell, TowerDefinition definition)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlaceTower || !gm.TrySpendGold(definition.Cost)) return;

            var position = gm.Map.CellToWorld(cell);
            var go = new GameObject(definition.DisplayName);
            go.transform.SetParent(gm.WorldRoot, false);
            var tower = go.AddComponent<Tower>();
            tower.Configure(definition, cell, position);

            _occupiedTowers.Add(cell, tower);
            gm.NotifyTowerSpawned(tower);

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
