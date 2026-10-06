using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 绘制棋盘、路线、基地和出怪口，并计算道路格集合与 BFS 距离场。
    /// 玩法数据与绘制分开维护，战场配色使用 <see cref="WorldArt"/>。
    /// </summary>
    public sealed class MapSystem : MonoBehaviour
    {
        // 棋盘范围：覆盖所有路线与基地的格子中心范围。
        private const int BoardMinX = -8, BoardMaxX = 8;
        private const int BoardMinY = -4, BoardMaxY = 4;

        private readonly HashSet<Vector2Int> _roadCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, int> _distanceToBase = new Dictionary<Vector2Int, int>();
        private readonly List<SpriteRenderer> _doorGlows = new List<SpriteRenderer>();
        private SpriteRenderer _baseGlow;
        private Transform _baseSpin;
        private float _pulseTime;
        private float _doorFlashTimer;    // 出怪门闪烁计时（波次开始时触发）

        public int RouteCount => GameConfig.Routes.Length;

        public void Init(Transform parent)
        {
            ComputeRoadCells();
            BuildBoard(parent);
            BuildDeploymentTiles(parent);
            BuildRouteVisuals(parent);
            BuildBaseVisual(parent);
            BuildSpawnVisuals(parent);
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

        /// <summary>触发出怪门闪烁（波次开始时由 HUD 调用）。</summary>
        public void PulseDoors()
        {
            _doorFlashTimer = 1.2f;
        }

        private void Update()
        {
            // 出怪口 / 基地光晕呼吸
            _pulseTime += Time.deltaTime;
            float breath = 0.5f + 0.5f * Mathf.Sin(_pulseTime * 2.2f);

            // 出怪门闪烁衰减
            _doorFlashTimer = Mathf.Max(0f, _doorFlashTimer - Time.deltaTime);
            float flash = _doorFlashTimer > 0f
                ? (_doorFlashTimer / 1.2f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f))
                : 0f;

            for (int i = 0; i < _doorGlows.Count; i++)
            {
                var c = _doorGlows[i].color;
                c.a = 0.16f + 0.12f * breath + 0.30f * flash;
                _doorGlows[i].color = c;
            }

            if (_baseGlow != null)
            {
                var c = _baseGlow.color;
                c.a = 0.12f + 0.10f * breath;
                _baseGlow.color = c;
            }

            if (_baseSpin != null) _baseSpin.Rotate(0f, 0f, -18f * Time.deltaTime);
        }

        // ---- 绘制 ----

        /// <summary>绘制棋盘底、主次网格和取景框。</summary>
        private void BuildBoard(Transform parent)
        {
            var root = new GameObject("Board");
            root.transform.SetParent(parent, false);

            float x0 = BoardMinX - 0.5f, x1 = BoardMaxX + 0.5f;
            float y0 = BoardMinY - 0.5f, y1 = BoardMaxY + 0.5f;
            float w = x1 - x0, h = y1 - y0;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;

            var plate = CreateSprite(root.transform, "Plate", 0);
            plate.sprite = SpriteFactory.Square(1f, WorldArt.Lacquer);
            plate.transform.position = new Vector3(cx, cy, 0f);
            plate.transform.localScale = new Vector3(w, h, 1f);

            // 噪声贴图使用 Tiled 平铺，避免随棋盘尺寸拉伸。
            var grain = CreateSprite(root.transform, "PlateGrain", 0);
            grain.sprite = SpriteFactory.Mottle(7717, 5, .55f);
            grain.drawMode = SpriteDrawMode.Tiled;
            grain.tileMode = SpriteTileMode.Continuous;
            grain.size = new Vector2(w, h);
            grain.color = WorldArt.Alpha(WorldArt.LacquerLift, .085f);
            grain.transform.position = new Vector3(cx, cy, 0f);

            // 道路层盖住网格；每 4 格使用更亮的主格线。
            for (int x = BoardMinX; x <= BoardMaxX + 1; x++)
                Line(root.transform, "GridV", x - .5f, cy, .012f, h, x % 4 == 0 ? 2 : 1,
                    x % 4 == 0 ? WorldArt.GridSurvey : WorldArt.GoldLine);
            for (int y = BoardMinY; y <= BoardMaxY + 1; y++)
                Line(root.transform, "GridH", cx, y - .5f, w, .012f, y % 4 == 0 ? 2 : 1,
                    y % 4 == 0 ? WorldArt.GridSurvey : WorldArt.GoldLine);

            var crest = CreateSprite(root.transform, "PlateGlow", 3);
            crest.sprite = SpriteFactory.GradientTop(WorldArt.PlateCrest);
            crest.transform.position = new Vector3(cx, y1 - .5f, 0f);
            crest.transform.localScale = new Vector3(w, 1.0f, 1f);

            float t = 0.04f;
            Line(root.transform, "EdgeT", cx, y1, w, t, 2);
            Line(root.transform, "EdgeB", cx, y0, w, t, 2);
            Line(root.transform, "EdgeL", x0, cy, t, h, 2);
            Line(root.transform, "EdgeR", x1, cy, t, h, 2);

            const float corner = 1.05f;
            // Corner 贴图是「左下角 L」，顺时针转 90° 依次变成右下 / 右上 / 左上。
            Bracket(root.transform, "C_LB", x0, y0, corner, 0f);
            Bracket(root.transform, "C_RB", x1, y0, corner, 90f);
            Bracket(root.transform, "C_RT", x1, y1, corner, 180f);
            Bracket(root.transform, "C_LT", x0, y1, corner, -90f);
        }

        /// <summary>绘制棋盘外的鸟居和灯笼，不占用部署格。</summary>
        private void BuildDeploymentTiles(Transform parent)
        {
            var root = new GameObject("ShrineCourtyard").transform;
            root.SetParent(parent, false);

            var vermilion = new Color(.56f, .23f, .29f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 8.85f;
                Line(root, "ToriiPillar", x, .0f, .12f, 1.2f, 3, vermilion);
            }
            Line(root, "BoundaryLintel", 0, 4.72f, 17.8f, .08f, 3,
                WorldArt.Alpha(vermilion, .65f));

            for (int i = -3; i <= 3; i++)
            {
                float x = i * 2.45f;
                var glow = CreateSprite(root, "LanternGlow", 2);
                glow.sprite = SpriteFactory.Glow(1f, Color.white);
                glow.color = WorldArt.Alpha(WorldArt.Lantern, .30f);
                glow.transform.position = new Vector3(x, 5.35f, 0f);
                glow.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                var lamp = CreateSprite(root, "Lantern", 3);
                lamp.sprite = SpriteFactory.Sphere(.13f, WorldArt.Ink);
                lamp.transform.position = new Vector3(x, 5.35f, 0f);

                var core = CreateSprite(root, "LanternCore", 4);
                core.sprite = SpriteFactory.Sphere(.095f, WorldArt.Lantern);
                core.transform.position = new Vector3(x, 5.35f, 0f);
            }
        }

        /// <summary>道路格铺石，边沿仅绘制在路面外侧，交叉口不叠加轮廓。</summary>
        private void BuildRouteVisuals(Transform parent)
        {
            var root = new GameObject("RouteVisuals");
            root.transform.SetParent(parent, false);

            var stone = SpriteFactory.Square(1f, Color.white);
            var lift = SpriteFactory.GradientTop(WorldArt.RoadLift);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var cell in _roadCells)
            {
                var road = CreateSprite(root.transform, "PathStone", 3);
                road.sprite = stone;
                road.color = WorldArt.RoadFill;
                road.transform.position = CellToWorld(cell);
                road.transform.localScale = new Vector3(.99f, .99f, 1);

                var glow = CreateSprite(root.transform, "PathLift", 4);
                glow.sprite = lift;
                glow.transform.position = CellToWorld(cell) + new Vector2(0f, .34f);
                glow.transform.localScale = new Vector3(.99f, .32f, 1f);

                foreach (var direction in directions)
                {
                    if (_roadCells.Contains(cell + direction)) continue;
                    bool horizontal = direction.y != 0;
                    // 上沿提亮、下沿加深，左右沿使用普通描边色。
                    Color edge = direction.y > 0 ? WorldArt.RoadEdgeLit
                        : direction.y < 0 ? WorldArt.RoadEdgeDark
                        : WorldArt.RoadEdge;
                    Line(root.transform, "PathBorder", cell.x + direction.x * .49f,
                        cell.y + direction.y * .49f, horizontal ? 1f : .025f,
                        horizontal ? .025f : 1f, 5, edge);
                }
            }
            for (int r = 0; r < GameConfig.Routes.Length; r++)
                DrawDirectionArrows(root.transform, GetRouteWorld(r), GameConfig.RouteColors[r]);
        }

        /// <summary>沿路线按固定间距绘制朝行进方向的箭头。</summary>
        private static void DrawDirectionArrows(Transform parent, Vector2[] points, Color color)
        {
            const float spacing = 2.4f;
            float minLen = GameConfig.GridCellSize * 0.45f;
            var arrow = SpriteFactory.Arrow(0.15f, 0.26f, color);

            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var delta = b - a;
                float length = delta.magnitude;
                if (length < 0.001f) continue;

                var dir = delta / length;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                // 每段两端各留半格，避免箭头压在转角/门上
                float usable = length - minLen * 2f;
                if (usable < spacing * 0.6f) continue;   // 短段不放箭头，避免与转角重叠

                int count = Mathf.FloorToInt(usable / spacing) + 1;
                float start = (length - (count - 1) * spacing) * 0.5f;   // 在段内居中分布

                for (int k = 0; k < count; k++)
                {
                    var pos = a + dir * (start + k * spacing);
                    var sr = CreateSprite(parent, "DirArrow", 4);
                    sr.sprite = arrow;
                    sr.transform.position = pos;
                    sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        /// <summary>绘制基地的六边形核心、外环和自转刻度。</summary>
        private void BuildBaseVisual(Transform parent)
        {
            var go = new GameObject("Base");
            go.transform.SetParent(parent, false);
            go.transform.position = CellToWorld(GameConfig.BaseCell);

            _baseGlow = CreateSprite(go.transform, "Glow", 2);
            _baseGlow.sprite = SpriteFactory.Glow(1.25f, WorldArt.AllyGlow);
            _baseGlow.transform.localPosition = Vector3.zero;

            var plate = CreateSprite(go.transform, "Plate", 5);
            plate.sprite = SpriteFactory.Hex(0.62f, WorldArt.AllyDeep);

            var ring = CreateSprite(go.transform, "Ring", 6);
            ring.sprite = SpriteFactory.Shell(0.60f, 0.055f, WorldArt.Ally);

            var core = CreateSprite(go.transform, "Core", 7);
            core.sprite = SpriteFactory.Hex(0.30f, WorldArt.AllyBright);

            // 刻度组绕基地核心缓转。
            _baseSpin = new GameObject("Spin").transform;
            _baseSpin.SetParent(go.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var tick = CreateSprite(_baseSpin, "Tick", 6);
                tick.sprite = SpriteFactory.RoundedSquare(0.16f, 0.07f, WorldArt.Ally);
                tick.transform.localPosition = new Vector3(0f, 0.82f, 0f);
                tick.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            }
        }

        /// <summary>绘制出怪口及指向路线首段的箭头。</summary>
        private void BuildSpawnVisuals(Transform parent)
        {
            for (int r = 0; r < GameConfig.Routes.Length; r++)
            {
                var go = new GameObject("SpawnDoor");
                go.transform.SetParent(parent, false);
                go.transform.position = GetSpawnPosition(r);

                var glow = CreateSprite(go.transform, "Glow", 2);
                glow.sprite = SpriteFactory.Glow(1.05f, WorldArt.FoeGlow);
                _doorGlows.Add(glow);

                var plate = CreateSprite(go.transform, "Plate", 5);
                plate.sprite = SpriteFactory.Hex(0.46f, WorldArt.FoeDeep);

                var ring = CreateSprite(go.transform, "Ring", 6);
                ring.sprite = SpriteFactory.Shell(0.46f, 0.05f, WorldArt.Foe);

                // 方向箭头：指向路线第一段的行进方向
                var route = GameConfig.Routes[r];
                Vector2 dir = Vector2.up;
                if (route.Length >= 2)
                {
                    dir = (CellToWorld(route[1]) - CellToWorld(route[0])).normalized;
                }
                var arrow = CreateSprite(go.transform, "Arrow", 7);
                arrow.sprite = SpriteFactory.Arrow(0.26f, 0.34f, WorldArt.FoeBright);
                arrow.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                arrow.transform.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            }
        }

        // ---- 绘制小工具 ----

        private static SpriteRenderer Line(Transform parent, string name, float x, float y, float w, float h,
            int order, Color? color = null)
        {
            var sr = CreateSprite(parent, name, order);
            sr.sprite = SpriteFactory.Square(1f, color ?? WorldArt.Frame);
            sr.transform.position = new Vector3(x, y, 0f);
            sr.transform.localScale = new Vector3(w, h, 1f);
            return sr;
        }

        private static void Bracket(Transform parent, string name, float x, float y, float size, float rotation)
        {
            var sr = CreateSprite(parent, name, 2);
            sr.sprite = SpriteFactory.Corner(size, 0.055f, WorldArt.Frame);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            // Corner 贴图的直角顶点在左下角（本地 -o,-o），但 pivot 在贴图中心，
            // 所以要把「中心相对直角顶点的偏移」旋转后补偿回去，直角才精确落在棋盘角上。
            float o = size * 0.5f;
            float rad = rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            sr.transform.position = new Vector3(x + c * o - s * o, y + s * o + c * o, 0f);
        }

        private static SpriteRenderer CreateSprite(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            // 本类只画地图层的内容，所以统一抬到地图带内，调用方只写带内相对层号。
            sr.sortingOrder = WorldArt.LayerMap + sortingOrder;
            return sr;
        }

        // ---- 道路格 & BFS 距离场（玩法数据，与绘制无关）----

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
