using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 悬停显示格子和攻击范围；点击空地部署，点击已有塔选中查看。
    /// 撤退由所选塔的操作按钮触发；使用 legacy Input 和 EventSystem 排除 UI 操作。
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        private const int MinCellX = -8;
        private const int MaxCellX = 8;
        private const int MinCellY = -4;
        private const int MaxCellY = 4;

        private readonly Dictionary<Vector2Int, Tower> _occupiedTowers = new Dictionary<Vector2Int, Tower>();

        private TowerType _selectedType = TowerType.Gun;
        private SpriteRenderer _hoverCell;
        private SpriteRenderer _selectionRing;
        private HudRangeIndicator _rangeIndicator;

        public TowerType SelectedType => _selectedType;
        public Tower SelectedTower { get; private set; }

        public void Init(Transform parent)
        {
            _hoverCell = CreateSpriteObject(parent, "HoverCell", 2);
            _hoverCell.sprite = SpriteFactory.RoundedSquare(0.9f, 0.3f, GameConfig.GridHoverColor);
            _hoverCell.gameObject.SetActive(false);

            _selectionRing = CreateSpriteObject(parent, "SelectionRing", 3);
            _selectionRing.sprite = SpriteFactory.Shell(0.66f, 0.075f, Color.white);
            _selectionRing.gameObject.SetActive(false);

            var rangeGo = new GameObject("RangeIndicator");
            rangeGo.transform.SetParent(parent, false);
            _rangeIndicator = rangeGo.AddComponent<HudRangeIndicator>();
            _rangeIndicator.Init();
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

            if (SelectedTower != null && gm != null)
            {
                _selectionRing.gameObject.SetActive(true);
                var towerPos = SelectedTower.transform.position;
                _selectionRing.transform.position = new Vector3(towerPos.x, towerPos.y, 0f); // 地面投影
                _selectionRing.color = SelectedTower.Definition.Color;
            }
            else if (_selectionRing != null)
            {
                _selectionRing.gameObject.SetActive(false);
            }

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

            // 指针在相机视口外或 UI 上时不处理放置和悬停。
            if (!cam.pixelRect.Contains(Input.mousePosition))
            {
                HidePreview();
                return;
            }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                HidePreview();
                return;
            }

            // 透视相机拾取需与地面 z=0 平面求交。
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.forward, Vector3.zero);
            if (!ground.Raycast(ray, out float groundDist))
            {
                HidePreview();
                return;
            }
            var mouseWorld = ray.GetPoint(groundDist);
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
                var centerWorld = (Vector2)_hoverCell.transform.position;

                if (occupied)
                {
                    _hoverCell.color = GameConfig.GridRetreatColor;
                    if (_occupiedTowers.TryGetValue(cell, out var occupiedTower))
                    {
                        _rangeIndicator.Show(centerWorld, occupiedTower.Definition.RangeCells,
                            occupiedTower.Definition.Color);
                    }
                }
                else
                {
                    _hoverCell.color = (mapBlocked || atLimit) ? GameConfig.GridBlockedColor : GameConfig.GridHoverColor;
                    _rangeIndicator.Show(centerWorld, definition.RangeCells, definition.Color);
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
                    TryDeploy(cell, definition.Type);
                }
            }
        }

        private void HidePreview()
        {
            _hoverCell.gameObject.SetActive(false);
            _rangeIndicator?.Hide();
        }

        public bool TryDeploy(Vector2Int cell, TowerType type)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Running || gm.IsPaused || !gm.CanPlaceTower) return false;
            if (cell.x < MinCellX || cell.x > MaxCellX || cell.y < MinCellY || cell.y > MaxCellY) return false;
            if (gm.Map.IsCellBlocked(cell) || _occupiedTowers.ContainsKey(cell)) return false;
            if (!GameConfig.Towers.TryGetValue(type, out var definition) || !gm.TrySpendSpirit(definition.Cost)) return false;

            var position = gm.Map.CellToWorld(cell);
            var go = new GameObject(definition.DisplayName);
            go.transform.SetParent(gm.WorldRoot, false);
            var tower = go.AddComponent<Tower>();
            tower.Configure(definition, cell, position);

            _occupiedTowers.Add(cell, tower);
            gm.NotifyTowerSpawned(tower);

            Sfx.Place();
            EffectFactory.SpawnBurst(position, definition.Color, 0.5f, 0.25f);
            return true;
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
