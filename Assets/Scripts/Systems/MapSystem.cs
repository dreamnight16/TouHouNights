using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 明日方舟式开放地图：绘制多条正交路线（每条一个红门）、网格与基地（蓝门），
    /// 计算道路格集合，并用 BFS 从蓝门出发为每个道路格计算到基地的最短格数（距离场）。
    /// </summary>
    public sealed class MapSystem : MonoBehaviour
    {
        private readonly HashSet<Vector2Int> _roadCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, int> _distanceToBase = new Dictionary<Vector2Int, int>();

        public int RouteCount => GameConfig.Routes.Length;

        public void Init(Transform parent)
        {
            BuildRouteVisuals(parent);
            BuildGridVisuals(parent);
            BuildBaseVisual(parent);
            BuildSpawnVisuals(parent);
            ComputeRoadCells();
            ComputeDistanceField();
        }

        public Vector2[] GetRouteWorld(int routeIndex)
        {
            var route = GameConfig.Routes[routeIndex];
            var world = new Vector2[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                world[i] = new Vector2(route[i].x, route[i].y);
            }
            return world;
        }

        public Vector2 GetSpawnPosition(int routeIndex)
        {
            var spawn = GameConfig.Routes[routeIndex][0];
            return new Vector2(spawn.x, spawn.y);
        }

        public bool IsRoadCell(Vector2Int cell)
        {
            return _roadCells.Contains(cell);
        }

        public bool IsBaseCell(Vector2Int cell)
        {
            return cell == GameConfig.BaseCell;
        }

        /// <summary>不可放塔的格子：道路格或基地格。</summary>
        public bool IsCellBlocked(Vector2Int cell)
        {
            return IsRoadCell(cell) || IsBaseCell(cell);
        }

        public int GetDistanceToBase(Vector2Int cell)
        {
            return _distanceToBase.TryGetValue(cell, out var distance) ? distance : int.MaxValue;
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

        private void BuildRouteVisuals(Transform parent)
        {
            var root = new GameObject("RouteVisuals");
            root.transform.SetParent(parent, false);

            for (int r = 0; r < GameConfig.Routes.Length; r++)
            {
                DrawPolyline(GetRouteWorld(r), GameConfig.RouteColors[r], root.transform);
            }
        }

        private static void DrawPolyline(Vector2[] points, Color color, Transform parent)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                float length = Vector2.Distance(a, b);
                int steps = Mathf.CeilToInt(length / 0.25f);
                var dir = (b - a) / length;

                for (int s = 0; s <= steps; s++)
                {
                    var pos = a + dir * (length * s / steps);
                    var go = new GameObject("PathDot");
                    go.transform.SetParent(parent, false);
                    go.transform.position = pos;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteFactory.Square(0.30f, color);
                    sr.sortingOrder = 1;
                }
            }
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
            // 蓝门：基地。
            var go = new GameObject("Base");
            go.transform.SetParent(parent, false);
            go.transform.position = CellToWorld(GameConfig.BaseCell);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(1.2f, new Color(0.20f, 0.55f, 1.00f));
            sr.sortingOrder = 2;
        }

        private void BuildSpawnVisuals(Transform parent)
        {
            // 红门：出怪口。
            for (int r = 0; r < GameConfig.Routes.Length; r++)
            {
                var go = new GameObject("SpawnDoor");
                go.transform.SetParent(parent, false);
                go.transform.position = GetSpawnPosition(r);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Circle(0.55f, new Color(0.95f, 0.28f, 0.22f));
                sr.sortingOrder = 2;
            }
        }

        // ---- 道路格 & BFS 距离场 ----

        private void ComputeRoadCells()
        {
            _roadCells.Clear();

            foreach (var route in GameConfig.Routes)
            {
                for (int i = 0; i < route.Length - 1; i++)
                {
                    AddSegmentCells(route[i], route[i + 1]);
                }
            }
        }

        private void AddSegmentCells(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(b.x - a.x);
            int dy = Mathf.Abs(b.y - a.y);
            int steps = Mathf.Max(dx, dy);

            for (int s = 0; s <= steps; s++)
            {
                float t = steps == 0 ? 0f : (float)s / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
                _roadCells.Add(new Vector2Int(x, y));
            }
        }

        /// <summary>BFS：把道路格建成 4 邻接无权图，从蓝门出发求最短格数。</summary>
        private void ComputeDistanceField()
        {
            _distanceToBase.Clear();

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(GameConfig.BaseCell);
            _distanceToBase[GameConfig.BaseCell] = 0;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int distance = _distanceToBase[cell];

                foreach (var neighbor in OrthogonalNeighbors(cell))
                {
                    if (!_roadCells.Contains(neighbor)) continue;
                    if (_distanceToBase.ContainsKey(neighbor)) continue;

                    _distanceToBase[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        private static IEnumerable<Vector2Int> OrthogonalNeighbors(Vector2Int cell)
        {
            yield return new Vector2Int(cell.x + 1, cell.y);
            yield return new Vector2Int(cell.x - 1, cell.y);
            yield return new Vector2Int(cell.x, cell.y + 1);
            yield return new Vector2Int(cell.x, cell.y - 1);
        }
    }
}
