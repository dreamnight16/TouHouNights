using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 开放地图：绘制多种敌人轨迹（每种敌人一条，固定起点/终点）、网格与基地，
    /// 并计算哪些格子被轨迹占据（细线，禁止放塔）。
    /// </summary>
    public sealed class MapSystem : MonoBehaviour
    {
        private readonly HashSet<Vector2Int> _blockedCells = new HashSet<Vector2Int>();

        public Vector2 StartPosition => GameConfig.StartPosition;
        public Vector2 EndPosition => GameConfig.EndPosition;

        public void Init(Transform parent)
        {
            BuildTrajectoryVisuals(parent);
            BuildGridVisuals(parent);
            BuildBaseVisual(parent);
            BuildStartVisual(parent);
            ComputeBlockedCells();
        }

        public Vector2[] GetTrajectory(EnemyType type)
        {
            return GameConfig.Trajectories[type];
        }

        public bool IsCellBlocked(Vector2Int cell)
        {
            return _blockedCells.Contains(cell);
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

        private void BuildTrajectoryVisuals(Transform parent)
        {
            var root = new GameObject("TrajectoryVisuals");
            root.transform.SetParent(parent, false);

            foreach (var pair in GameConfig.Trajectories)
            {
                var color = GameConfig.Enemies[pair.Key].Color;
                color.a = 0.75f;
                DrawPolyline(pair.Value, color, root.transform);
            }
        }

        private static void DrawPolyline(Vector2[] points, Color color, Transform parent)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                float length = Vector2.Distance(a, b);
                int steps = Mathf.CeilToInt(length / 0.2f);
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
            var go = new GameObject("Base");
            go.transform.SetParent(parent, false);
            go.transform.position = EndPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(1.0f, GameConfig.BaseColor);
            sr.sortingOrder = 2;
        }

        private void BuildStartVisual(Transform parent)
        {
            var go = new GameObject("Start");
            go.transform.SetParent(parent, false);
            go.transform.position = StartPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(0.45f, new Color(0.9f, 0.55f, 0.2f));
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

                    foreach (var trajectory in GameConfig.Trajectories.Values)
                    {
                        if (IsNearAnySegment(center, trajectory))
                        {
                            _blockedCells.Add(new Vector2Int(x, y));
                            break;
                        }
                    }
                }
            }
        }

        private static bool IsNearAnySegment(Vector2 p, Vector2[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (DistanceToSegment(p, points[i], points[i + 1]) < GameConfig.TrajectoryBlockRadius)
                {
                    return true;
                }
            }
            return false;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
