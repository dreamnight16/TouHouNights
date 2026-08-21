using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 负责构建地图：绘制路径、起点/终点、网格线，并计算哪些格子被路径占据（禁止放塔）。
    /// </summary>
    public sealed class MapSystem : MonoBehaviour
    {
        private Vector2[] _waypoints;
        private readonly HashSet<Vector2Int> _blockedCells = new HashSet<Vector2Int>();

        public Vector2[] Waypoints => _waypoints;
        public Vector2 StartPosition => _waypoints.Length > 0 ? _waypoints[0] : Vector2.zero;
        public Vector2 EndPosition => _waypoints.Length > 0 ? _waypoints[_waypoints.Length - 1] : Vector2.zero;

        public void Init(Transform parent)
        {
            _waypoints = GameConfig.Path;
            BuildPathVisuals(parent);
            BuildGridVisuals(parent);
            BuildBaseVisual(parent);
            ComputeBlockedCells();
        }

        public bool IsCellBlocked(Vector2Int cell)
        {
            if (_blockedCells.Contains(cell)) return true;
            return false;
        }

        public Vector2 CellToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * GameConfig.GridCellSize, cell.y * GameConfig.GridCellSize);
        }

        public Vector2Int WorldToCell(Vector2 world)
        {
            return new Vector2Int(
                Mathf.RoundToInt(world.x / GameConfig.GridCellSize),
                Mathf.RoundToInt(world.y / GameConfig.GridCellSize));
        }

        // ---- 绘制 ----

        private void BuildPathVisuals(Transform parent)
        {
            var root = new GameObject("PathVisuals");
            root.transform.SetParent(parent, false);

            for (int i = 0; i < _waypoints.Length - 1; i++)
            {
                var a = _waypoints[i];
                var b = _waypoints[i + 1];
                float length = Vector2.Distance(a, b);
                int steps = Mathf.CeilToInt(length / 0.2f);
                var dir = (b - a) / length;

                for (int s = 0; s <= steps; s++)
                {
                    var pos = a + dir * (length * s / steps);
                    var go = new GameObject("PathDot");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = pos;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteFactory.Square(0.55f, GameConfig.PathColor);
                    sr.sortingOrder = 1;
                }
            }

            // 起点标记。
            var start = new GameObject("Start");
            start.transform.SetParent(root.transform, false);
            start.transform.position = StartPosition;
            var startSr = start.AddComponent<SpriteRenderer>();
            startSr.sprite = SpriteFactory.Circle(0.45f, new Color(0.9f, 0.55f, 0.2f));
            startSr.sortingOrder = 2;
        }

        private void BuildGridVisuals(Transform parent)
        {
            var root = new GameObject("GridVisuals");
            root.transform.SetParent(parent, false);

            int minX = -8, maxX = 8, minY = -4, maxY = 4;

            for (int x = minX; x <= maxX; x++)
            {
                var line = new GameObject("VLine");
                line.transform.SetParent(root.transform, false);
                line.transform.position = new Vector3(x, 0f, 0f);
                line.transform.localScale = new Vector3(0.02f, (maxY - minY) + 1f, 1f);
                var sr = line.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Square(1f, GameConfig.GridColor);
                sr.sortingOrder = 0;
            }

            for (int y = minY; y <= maxY; y++)
            {
                var line = new GameObject("HLine");
                line.transform.SetParent(root.transform, false);
                line.transform.position = new Vector3(0f, y, 0f);
                line.transform.localScale = new Vector3((maxX - minX) + 1f, 0.02f, 1f);
                var sr = line.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Square(1f, GameConfig.GridColor);
                sr.sortingOrder = 0;
            }
        }

        private void BuildBaseVisual(Transform parent)
        {
            var go = new GameObject("Base");
            go.transform.SetParent(parent, false);
            go.transform.position = EndPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(1.0f, GameConfig.BaseColor);
            sr.sortingOrder = 2;
        }

        // ---- 阻挡判定 ----

        private void ComputeBlockedCells()
        {
            _blockedCells.Clear();

            for (int x = -8; x <= 8; x++)
            {
                for (int y = -4; y <= 4; y++)
                {
                    var center = new Vector2(x, y);
                    for (int i = 0; i < _waypoints.Length - 1; i++)
                    {
                        if (DistanceToSegment(center, _waypoints[i], _waypoints[i + 1]) < GameConfig.PathCorridorWidth)
                        {
                            _blockedCells.Add(new Vector2Int(x, y));
                            break;
                        }
                    }
                }
            }
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
