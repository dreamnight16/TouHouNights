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
    /// 塔的放置交互：鼠标悬停显示格子高亮 + 射程预览，点击合法格子放置所选塔。
    /// 负责占用格子的登记，防止叠塔。
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        private const int MinCellX = -8;
        private const int MaxCellX = 8;
        private const int MinCellY = -4;
        private const int MaxCellY = 4;

        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

        private TowerType _selectedType = TowerType.Gun;
        private SpriteRenderer _hoverCell;
        private SpriteRenderer _rangeGhost;

        public TowerType SelectedType => _selectedType;

        public void Init(Transform parent)
        {
            _hoverCell = CreateSpriteObject(parent, "HoverCell", 1);
            _rangeGhost = CreateSpriteObject(parent, "RangeGhost", 0);

            _hoverCell.sprite = SpriteFactory.Square(0.92f, GameConfig.GridHoverColor);
            _rangeGhost.sprite = SpriteFactory.Circle(0.5f, new Color(1f, 1f, 1f, 0.10f));

            _hoverCell.gameObject.SetActive(false);
            _rangeGhost.gameObject.SetActive(false);
        }

        public void SelectTower(TowerType type)
        {
            _selectedType = type;
        }

        private void Update()
        {
            bool canPlace = GameManager.Instance.State == GameState.Building;
            if (!canPlace)
            {
                _hoverCell.gameObject.SetActive(false);
                _rangeGhost.gameObject.SetActive(false);
                return;
            }

            var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var cell = GameManager.Instance.Map.WorldToCell(mouseWorld);
            bool inBounds = cell.x >= MinCellX && cell.x <= MaxCellX && cell.y >= MinCellY && cell.y <= MaxCellY;
            bool blocked = inBounds && (GameManager.Instance.Map.IsCellBlocked(cell) || _occupiedCells.Contains(cell));

            var definition = GameConfig.Towers[_selectedType];

            if (inBounds)
            {
                _hoverCell.gameObject.SetActive(true);
                _hoverCell.transform.position = GameManager.Instance.Map.CellToWorld(cell);
                _hoverCell.color = blocked ? new Color(1f, 0.3f, 0.3f, 0.4f) : GameConfig.GridHoverColor;

                _rangeGhost.gameObject.SetActive(true);
                _rangeGhost.transform.position = GameManager.Instance.Map.CellToWorld(cell);
                _rangeGhost.transform.localScale = Vector3.one * (definition.Range * 2f);
            }
            else
            {
                _hoverCell.gameObject.SetActive(false);
                _rangeGhost.gameObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0)
                && inBounds
                && !blocked
                && !HudLayout.IsPointerOverUI(Input.mousePosition))
            {
                TryPlace(cell, definition);
            }
        }

        private void TryPlace(Vector2Int cell, TowerDefinition definition)
        {
            if (!GameManager.Instance.TrySpendGold(definition.Cost)) return;

            var position = GameManager.Instance.Map.CellToWorld(cell);
            var go = new GameObject(definition.DisplayName);
            go.transform.SetParent(GameManager.Instance.WorldRoot, false);
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
