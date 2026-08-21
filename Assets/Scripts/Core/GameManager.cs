using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.Systems;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Core
{
    public enum GameState
    {
        Running,   // 建造与战斗并行，无独立阶段
        GameOver,
        Victory
    }

    /// <summary>
    /// 游戏总控：持有经济、生命、敌人/塔注册表、空间哈希、懒删除堆与全局索敌策略，
    /// 负责相机、地图、UI、刷怪器、放置器的组装；波次由协程自动推进（无需手动开波）。
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<Tower> _towers = new List<Tower>();
        private readonly SpatialGrid _spatialGrid = new SpatialGrid();
        private readonly MinHeap<Enemy> _lowestHealthHeap = new MinHeap<Enemy>(enemy => enemy.HealthRatio);
        private readonly MinHeap<Enemy> _closestToEndHeap = new MinHeap<Enemy>(enemy => enemy.DistanceToEnd);
        private readonly List<Enemy> _queryBuffer = new List<Enemy>();
        private readonly List<Enemy> _heapRecycle = new List<Enemy>();

        private Coroutine _gameLoop;
        private int _gold;
        private int _lives;
        private int _waveIndex;

        public Transform WorldRoot { get; private set; }
        public MapSystem Map { get; private set; }
        public TowerPlacer TowerPlacer { get; private set; }
        public WaveSpawner WaveSpawner { get; private set; }

        public GameState State { get; private set; } = GameState.Running;
        public TargetingPriority TargetingPriority { get; private set; } = TargetingPriority.Nearest;

        public int Gold => _gold;
        public int Lives => _lives;
        public int WaveIndex => _waveIndex;
        public int TotalWaves => GameConfig.Waves.Length;
        public int CurrentWave => _waveIndex + 1; // 1-based，与 HUD 显示一致
        public bool IsDoubleSpeed { get; private set; }
        public int EnemyCount => _enemies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureCamera();
            CreateHud();
            CreateWorld();
            ResetState();
            StartGame();
        }

        private void Update()
        {
            // 每帧重建空间哈希与懒删除堆，供索敌/AOE/减速/子弹重锁定查询使用。
            _spatialGrid.Rebuild(_enemies);
            _lowestHealthHeap.Rebuild(_enemies);
            _closestToEndHeap.Rebuild(_enemies);
        }

        // ---- 组装 ----

        private void EnsureCamera()
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }

            // 无论场景中是否已有相机，都强制配置为俯视正交相机。
            cam.orthographic = true;
            cam.orthographicSize = GameConfig.CameraSize;
            cam.backgroundColor = GameConfig.BackgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void CreateHud()
        {
            if (GetComponent<HudController>() == null)
            {
                gameObject.AddComponent<HudController>();
            }
        }

        private void CreateWorld()
        {
            if (WorldRoot != null)
            {
                Destroy(WorldRoot.gameObject);
            }

            var root = new GameObject("WorldRoot");
            WorldRoot = root.transform;

            BuildFloor();

            var mapGo = new GameObject("MapSystem");
            mapGo.transform.SetParent(WorldRoot, false);
            Map = mapGo.AddComponent<MapSystem>();
            Map.Init(WorldRoot);

            var placerGo = new GameObject("TowerPlacer");
            placerGo.transform.SetParent(WorldRoot, false);
            TowerPlacer = placerGo.AddComponent<TowerPlacer>();
            TowerPlacer.Init(WorldRoot);

            var spawnerGo = new GameObject("WaveSpawner");
            spawnerGo.transform.SetParent(WorldRoot, false);
            WaveSpawner = spawnerGo.AddComponent<WaveSpawner>();
        }

        private void BuildFloor()
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(WorldRoot, false);
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(1f, new Color(0.09f, 0.10f, 0.14f));
            sr.sortingOrder = -10;
            floor.transform.localScale = new Vector3(GameConfig.WorldHalfWidth * 2f + 4f, GameConfig.WorldHalfHeight * 2f + 4f, 1f);
        }

        private void ResetState()
        {
            _gold = GameConfig.StartingGold;
            _lives = GameConfig.StartingLives;
            _waveIndex = 0;
            State = GameState.Running;
            _enemies.Clear();
            _towers.Clear();
            WaveSpawner.Reset();
            SetSpeed(false);
        }

        // ---- 波次自动推进 ----

        private void StartGame()
        {
            StopGameLoop();
            _gameLoop = StartCoroutine(GameLoop());
        }

        private void StopGameLoop()
        {
            if (_gameLoop != null)
            {
                StopCoroutine(_gameLoop);
                _gameLoop = null;
            }
        }

        private IEnumerator GameLoop()
        {
            yield return new WaitForSeconds(2f); // 开局缓冲，期间仍可建塔

            for (int i = 0; i < TotalWaves; i++)
            {
                _waveIndex = i;
                WaveSpawner.StartWave(i);
                yield return new WaitUntil(() => WaveSpawner.IsWaveComplete && _enemies.Count == 0);
                yield return new WaitForSeconds(1.2f); // 波间短暂缓冲，仍可建塔
            }

            State = GameState.Victory;
            Time.timeScale = 0f; // 结束定格
        }

        // ---- 速度 ----

        private void SetSpeed(bool doubleSpeed)
        {
            IsDoubleSpeed = doubleSpeed;
            Time.timeScale = doubleSpeed ? 2f : 1f;
        }

        public void ToggleSpeed()
        {
            if (State != GameState.Running) return;
            SetSpeed(!IsDoubleSpeed);
        }

        // ---- 经济 ----

        public bool TrySpendGold(int amount)
        {
            if (_gold < amount) return false;
            _gold -= amount;
            return true;
        }

        public void AddGold(int amount)
        {
            _gold += amount;
        }

        // ---- 敌人生命周期 ----

        public void NotifyEnemySpawned(Enemy enemy)
        {
            _enemies.Add(enemy);
        }

        public void NotifyEnemyRemoved(Enemy enemy)
        {
            _enemies.Remove(enemy);
        }

        public void NotifyEnemyKilled(Enemy enemy, int reward)
        {
            AddGold(reward);
        }

        public void NotifyEnemyReachedBase(int damage)
        {
            _lives -= damage;
            if (_lives <= 0)
            {
                _lives = 0;
                OnGameOver();
            }
        }

        private void OnGameOver()
        {
            State = GameState.GameOver;
            StopGameLoop();
            WaveSpawner.Reset();
            Time.timeScale = 0f;
        }

        // ---- 塔生命周期 ----

        public void NotifyTowerSpawned(Tower tower)
        {
            _towers.Add(tower);
        }

        public void NotifyTowerRemoved(Tower tower)
        {
            _towers.Remove(tower);
        }

        public Tower GetNearestTower(Vector2 position, int rangeCells)
        {
            Tower best = null;
            float bestSqr = float.MaxValue;

            foreach (var tower in _towers)
            {
                if (tower == null) continue;
                var p = (Vector2)tower.transform.position;
                if (Chebyshev(position, p) > rangeCells) continue;

                float sqr = (position - p).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = tower;
                }
            }

            return best;
        }

        /// <summary>主动撤退塔，返还 50% 金币。</summary>
        public void RetreatTower(Tower tower)
        {
            if (tower == null) return;
            AddGold(Mathf.RoundToInt(tower.Definition.Cost * GameConfig.RetreatRefundRatio));
            RemoveTower(tower);
        }

        /// <summary>塔被敌人击毁，返还 20% 金币。</summary>
        public void OnTowerDefeated(Tower tower)
        {
            if (tower == null) return;
            AddGold(Mathf.RoundToInt(tower.Definition.Cost * GameConfig.DefeatRefundRatio));
            RemoveTower(tower);
        }

        private void RemoveTower(Tower tower)
        {
            _towers.Remove(tower);
            TowerPlacer.FreeCell(tower.Cell);
            EffectFactory.SpawnBurst(tower.transform.position, tower.Definition.Color, 0.6f, 0.3f);
            Destroy(tower.gameObject);
        }

        // ---- 索敌 / 伤害 ----

        public Enemy SelectTarget(Vector2 position, int rangeCells)
        {
            switch (TargetingPriority)
            {
                case TargetingPriority.LowestHealth:
                    return SelectByHeap(_lowestHealthHeap, position, rangeCells);
                case TargetingPriority.ClosestToEnd:
                    return SelectByHeap(_closestToEndHeap, position, rangeCells);
                case TargetingPriority.Nearest:
                default:
                    return SelectNearest(position, rangeCells);
            }
        }

        private Enemy SelectNearest(Vector2 position, int rangeCells)
        {
            _spatialGrid.QueryChebyshev(position, rangeCells, _queryBuffer);

            Enemy best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var enemy = _queryBuffer[i];
                float sqr = (position - (Vector2)enemy.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }
            return best;
        }

        private Enemy SelectByHeap(MinHeap<Enemy> heap, Vector2 position, int rangeCells)
        {
            _heapRecycle.Clear();
            Enemy result = null;

            while (heap.Count > 0)
            {
                var top = heap.Peek();
                if (top == null || !top.IsAlive)
                {
                    heap.Pop(); // 懒删除：跳过已死敌人
                    continue;
                }

                if (Chebyshev(position, top.transform.position) <= rangeCells)
                {
                    result = top;
                    break;
                }

                _heapRecycle.Add(heap.Pop()); // 出界项暂存，稍后回插
            }

            for (int i = 0; i < _heapRecycle.Count; i++)
            {
                heap.Push(_heapRecycle[i]);
            }
            _heapRecycle.Clear();

            return result;
        }

        public Enemy GetNearestEnemy(Vector2 position, float range)
        {
            _spatialGrid.QueryCircle(position, range, _queryBuffer);

            Enemy best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var enemy = _queryBuffer[i];
                float sqr = (position - (Vector2)enemy.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }
            return best;
        }

        public void DamageEnemiesInRadius(Vector2 center, float radius, float damage)
        {
            _spatialGrid.QueryCircle(center, radius + 0.5f, _queryBuffer);
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var enemy = _queryBuffer[i];
                if (Vector2.Distance(center, enemy.transform.position) <= radius + enemy.Radius)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        public void ApplySlowInRange(Vector2 center, int rangeCells, float factor, float duration)
        {
            _spatialGrid.QueryChebyshev(center, rangeCells, _queryBuffer);
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                _queryBuffer[i].ApplySlow(factor, duration);
            }
        }

        private static float Chebyshev(Vector2 a, Vector2 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        // ---- 流程控制 ----

        public void CycleTargetingPriority()
        {
            int count = System.Enum.GetValues(typeof(TargetingPriority)).Length;
            int next = ((int)TargetingPriority + 1) % count;
            TargetingPriority = (TargetingPriority)next;
        }

        public void Restart()
        {
            StopGameLoop();
            _enemies.Clear();
            _towers.Clear();
            CreateWorld();
            ResetState();
            StartGame();
        }
    }
}
