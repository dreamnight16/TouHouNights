using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Actors;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 均匀网格空间哈希（ICPC 式优化）：
    /// 把敌人按所在格子分桶，范围查询从「每塔每帧 O(N) 全量扫描」降为
    /// 「只遍历覆盖格子，O(1) 摊销 + 候选集内的常数级过滤」。
    /// 每帧由 GameManager 重建一次，保证索敌/AOE/减速查询的高效与正确。
    /// </summary>
    public sealed class SpatialGrid
    {
        private const float CellSize = 1f;

        private readonly Dictionary<Vector2Int, List<Enemy>> _buckets =
            new Dictionary<Vector2Int, List<Enemy>>();

        /// <summary>每帧重建：清空后按敌人当前位置重新分桶。</summary>
        public void Rebuild(IReadOnlyList<Enemy> enemies)
        {
            _buckets.Clear();

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                var key = WorldToCell(enemy.transform.position);
                if (!_buckets.TryGetValue(key, out var list))
                {
                    list = new List<Enemy>(4);
                    _buckets[key] = list;
                }
                list.Add(enemy);
            }
        }

        /// <summary>查询圆形范围内的存活敌人（欧氏距离）。</summary>
        public void QueryCircle(Vector2 center, float radius, List<Enemy> results)
        {
            results.Clear();
            float radiusSqr = radius * radius;

            int r = Mathf.CeilToInt(radius);
            var centerCell = WorldToCell(center);

            for (int x = centerCell.x - r; x <= centerCell.x + r; x++)
            {
                for (int y = centerCell.y - r; y <= centerCell.y + r; y++)
                {
                    if (!_buckets.TryGetValue(new Vector2Int(x, y), out var list)) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        var enemy = list[i];
                        if (enemy == null || !enemy.IsAlive) continue;
                        if ((center - (Vector2)enemy.transform.position).sqrMagnitude <= radiusSqr)
                        {
                            results.Add(enemy);
                        }
                    }
                }
            }
        }

        /// <summary>查询 Chebyshev 范围内（格子距离）的存活敌人。</summary>
        public void QueryChebyshev(Vector2 center, int cells, List<Enemy> results)
        {
            results.Clear();
            var centerCell = WorldToCell(center);

            for (int x = centerCell.x - cells; x <= centerCell.x + cells; x++)
            {
                for (int y = centerCell.y - cells; y <= centerCell.y + cells; y++)
                {
                    if (!_buckets.TryGetValue(new Vector2Int(x, y), out var list)) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        var enemy = list[i];
                        if (enemy == null || !enemy.IsAlive) continue;
                        if (Chebyshev(center, enemy.transform.position) <= cells)
                        {
                            results.Add(enemy);
                        }
                    }
                }
            }
        }

        private static float Chebyshev(Vector2 a, Vector2 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static Vector2Int WorldToCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / CellSize),
                Mathf.FloorToInt(position.y / CellSize));
        }
    }
}
