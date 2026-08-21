using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Data;
using TowerDefense.Systems;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Core
{
    public enum GameState
    {
        Building,
        WaveActive,
        GameOver,
        Victory
    }

    /// <summary>
    /// 游戏总控：持有经济、生命、波次、敌人注册表与全局索敌策略，
    /// 并负责相机、地图、UI、刷怪器、放置器的组装与重启。
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<Enemy> _enemies = new List<Enemy>();

        private int _gold;
        private int _lives;
        private int _waveIndex;

        public Transform WorldRoot { get; private set; }
        public MapSystem Map { get; private set; }
        public TowerPlacer TowerPlacer { get; private set; }
        public WaveSpawner WaveSpawner { get; private set; }

        public GameState State { get; private set; } = GameState.Building;
        public TargetingPriority TargetingPriority { get; private set; } = TargetingPriority.Nearest;

        public int Gold => _gold;
        public int Lives => _lives;
        public int WaveIndex => _waveIndex;
        public int TotalWaves => GameConfig.Waves.Length;
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
        }

        private void Update()
        {
            if (State != GameState.WaveActive) return;

            if (WaveSpawner.IsWaveComplete && _enemies.Count == 0)
            {
                State = _waveIndex >= TotalWaves ? GameState.Victory : GameState.Building;
            }
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
            State = GameState.Building;
            _enemies.Clear();
            WaveSpawner.Reset();
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
                State = GameState.GameOver;
            }
        }

        // ---- 索敌 / 伤害 ----

        public Enemy SelectTarget(Vector2 position, float range)
        {
            Enemy best = null;

            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(position, enemy.transform.position) > range) continue;

                if (best == null || IsBetter(enemy, best, position))
                {
                    best = enemy;
                }
            }

            return best;
        }

        public Enemy GetNearestEnemy(Vector2 position, float range)
        {
            Enemy best = null;
            float bestDistance = range * range;

            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                float sqrDistance = (position - (Vector2)enemy.transform.position).sqrMagnitude;
                if (sqrDistance <= bestDistance)
                {
                    bestDistance = sqrDistance;
                    best = enemy;
                }
            }

            return best;
        }

        public void DamageEnemiesInRadius(Vector2 center, float radius, float damage)
        {
            var snapshot = new List<Enemy>(_enemies);
            foreach (var enemy in snapshot)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(center, enemy.transform.position) <= radius + enemy.Radius)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        public void ApplySlowInRadius(Vector2 center, float radius, float factor, float duration)
        {
            var snapshot = new List<Enemy>(_enemies);
            foreach (var enemy in snapshot)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(center, enemy.transform.position) <= radius + enemy.Radius)
                {
                    enemy.ApplySlow(factor, duration);
                }
            }
        }

        private bool IsBetter(Enemy candidate, Enemy current, Vector2 position)
        {
            switch (TargetingPriority)
            {
                case TargetingPriority.LowestHealth:
                    return candidate.HealthRatio < current.HealthRatio;
                case TargetingPriority.ClosestToEnd:
                    return candidate.DistanceToEnd < current.DistanceToEnd;
                case TargetingPriority.Nearest:
                default:
                    return (candidate.transform.position - (Vector3)position).sqrMagnitude
                         < (current.transform.position - (Vector3)position).sqrMagnitude;
            }
        }

        // ---- 流程控制 ----

        public void CycleTargetingPriority()
        {
            int count = System.Enum.GetValues(typeof(TargetingPriority)).Length;
            int next = ((int)TargetingPriority + 1) % count;
            TargetingPriority = (TargetingPriority)next;
        }

        public void StartNextWave()
        {
            if (State != GameState.Building || _waveIndex >= TotalWaves) return;

            State = GameState.WaveActive;
            WaveSpawner.StartWave(_waveIndex);
            _waveIndex++;
        }

        public void Restart()
        {
            _enemies.Clear();
            CreateWorld();
            ResetState();
        }
    }
}
