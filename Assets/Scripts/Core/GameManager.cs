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
        Menu,
        Running,   // 建造与战斗并行，无独立阶段
        GameOver,
        Victory
    }

    /// <summary>
    /// 管理经济、生命、单位注册表与全局索敌策略。
    /// 组装相机、地图和 UI，并由协程推进幕次。
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
        private Coroutine _barrage;
        private int _spirit;          // 灵力：部署符卡唯一的资源
        private int _lives;
        private int _score;
        private float _power;
        private int _totalKills;
        private int _spawnedEnemies;
        private int _leakedEnemies;
        private int _combo;
        private int _bestCombo;
        private int _speedIndex = 1; // 指向 SpeedLevels[1] = 1x
        private bool _paused;
        private float _incomeClock;
        private GameObject _battleCanvas;
        private FrontEndUi _frontEnd;
        public bool IsPractice { get; private set; }
        public int StartStage { get; private set; }
        public int CompletedStages { get; private set; }
        public int PracticeSpirit => GameConfig.PracticeStartingSpirit(StartStage);

        public Transform WorldRoot { get; private set; }
        public MapSystem Map { get; private set; }
        public TowerPlacer TowerPlacer { get; private set; }
        public WaveSpawner WaveSpawner { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public TargetingPriority TargetingPriority { get; private set; } = TargetingPriority.Nearest;

        public int Spirit => _spirit;
        public int Lives => _lives;
        public int TotalEnemies => GameConfig.TotalEnemies;
        public float SpeedScale => SpeedLevels[_speedIndex];
        public int SpeedIndex => _speedIndex;
        public bool IsPaused => _paused;
        public bool IsBarrageActive => _barrage != null;
        public int EnemyCount => _enemies.Count;
        public int TowerCount => _towers.Count;
        public bool CanPlaceTower => _towers.Count < GameConfig.MaxTowers;
        public int Score => _score;
        public float Power => _power;
        public bool CanBarrage => _power >= GameConfig.MaxPower;
        public float DamageMultiplier => 1f + _power * GameConfig.PowerDamageBonus;
        public int TotalKills => _totalKills;
        public int LeakedEnemies => _leakedEnemies;
        public int Combo => _combo;
        public int BestCombo => _bestCombo;
        public int BestScore => PlayerPrefs.GetInt("td_best_score", 0);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR
            Screen.SetResolution(1920, 1080, PlayerPrefs.GetInt("td_fullscreen", 1) != 0); // 独立运行默认 1080p 全屏，避免低分辨率
#endif

            EnsureCamera();
            gameObject.AddComponent<BattleMusic>();
            CreateHud();
            CreateWorld();
            ResetState();
            State = GameState.Menu;
            Time.timeScale = 0f;
            WorldRoot.gameObject.SetActive(false);
            _battleCanvas.SetActive(false);
            var menuCanvas = UiFactory.CreateCanvas();
            menuCanvas.name = "FrontEndCanvas";
            menuCanvas.sortingOrder = 20;
            _frontEnd = menuCanvas.gameObject.AddComponent<FrontEndUi>();
            _frontEnd.Init(this);
        }

        private void Update()
        {
            if (State != GameState.Running) return;
            _incomeClock += Time.deltaTime;
            if (_incomeClock >= 1f)
            {
                int ticks = Mathf.FloorToInt(_incomeClock);
                _incomeClock -= ticks;
                AddSpirit(ticks * GameConfig.SpiritRegenPerSecond);
            }
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

            // 使用配置中的透视相机位置和观察点。
            cam.orthographic = false;
            cam.fieldOfView = GameConfig.CameraFov;
            cam.backgroundColor = GameConfig.BackgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = GameConfig.CameraPosition;
            cam.transform.LookAt(GameConfig.CameraLookAt);

            // 屏幕震动和 UI 背景模糊采样。
            if (cam.GetComponent<ScreenShake>() == null)
            {
                cam.gameObject.AddComponent<ScreenShake>();
            }
            if (cam.GetComponent<ScreenBlurFx>() == null)
            {
                cam.gameObject.AddComponent<ScreenBlurFx>();
            }
            // 漂移只改旋转，避免与修改位置的 ScreenShake 冲突。
            if (cam.GetComponent<CameraDrift>() == null)
            {
                cam.gameObject.AddComponent<CameraDrift>();
            }

            QualitySettings.antiAliasing = 8;
            cam.allowMSAA = true;
        }

        private void CreateHud()
        {
            var canvas = UiFactory.CreateCanvas();
            _battleCanvas = canvas.gameObject;
            UiFactory.EnsureEventSystem();
            // 在装配点向 BattleUiRoot 注入依赖。
            var battleUi = canvas.gameObject.AddComponent<BattleUiRoot>();
            battleUi.Init(this);
        }

        private void CreateWorld()
        {
            if (WorldRoot != null)
            {
                Destroy(WorldRoot.gameObject);
            }

            var root = new GameObject("WorldRoot");
            WorldRoot = root.transform;

            // 先创建背景，再创建棋盘。
            WorldAmbience.Create(WorldRoot);
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
            WaveSpawner.OnRoundClear += OnStageCleared;

            // 战场氛围光尘（ParticleSystem）
            AmbientDust.Create(WorldRoot);
        }

        /// <summary>绘制战场暗角；背景由 <see cref="WorldAmbience"/> 提供。</summary>
        private void BuildFloor()
        {
            var vignette = new GameObject("Vignette");
            vignette.transform.SetParent(WorldRoot, false);
            var vsr = vignette.AddComponent<SpriteRenderer>();
            vsr.sprite = SpriteFactory.Vignette(new Color(0f, 0f, 0f, 0.55f));
            vsr.sortingOrder = WorldArt.LayerFloor + 1;
            vignette.transform.localScale = new Vector3(
                GameConfig.WorldHalfWidth * 2f + 4f, GameConfig.WorldHalfHeight * 2f + 4f, 1f);
        }

        private void ResetState()
        {
            _spirit = IsPractice ? PracticeSpirit : GameConfig.StartingSpirit;
            _incomeClock = 0;
            TargetingPriority = TargetingPriority.Nearest;
            _lives = GameConfig.StartingLives;
            _score = 0;
            _power = 0f;
            _totalKills = 0;
            _spawnedEnemies = 0;
            _leakedEnemies = 0;
            _combo = 0;
            _bestCombo = 0;
            CompletedStages = 0;
            State = GameState.Running;
            _enemies.Clear();
            _towers.Clear();
            WaveSpawner.Reset();
            ResetSpeed();
        }

        // ---- Stage 自动推进 ----

        private void StartGame()
        {
            StopGameLoop();
            _gameLoop = StartCoroutine(GameLoop());
        }

        private void OnStageCleared(int _)
        {
            // 失败会重置刷怪器，已通过关数由本局状态单独保存。
            CompletedStages++;
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
            yield return new WaitForSeconds(GameConfig.OpeningDelay); // 留出初始部署时间

            WaveSpawner.StartOperation(StartStage, IsPractice);
            yield return new WaitUntil(() => WaveSpawner.IsOperationComplete && _enemies.Count == 0);

            State = GameState.Victory;
            _paused = false;
            Time.timeScale = 0f; // 结束定格
        }

        // ---- 时间控制（暂停与变速）----

        private static readonly float[] SpeedLevels = { 0.5f, 1f, 2f, 4f };

        private void ResetSpeed()
        {
            _speedIndex = 1; // 1x
            _paused = false;
            Time.timeScale = 1f;
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = _paused ? 0f : SpeedLevels[_speedIndex];
        }

        public void CycleSpeed()
        {
            if (State != GameState.Running) return;
            _speedIndex = (_speedIndex + 1) % SpeedLevels.Length;
            _paused = false;
            ApplyTimeScale();
        }

        /// <summary>直接设置速度档位（HUD 分段按钮用）。</summary>
        public void SetSpeedIndex(int index)
        {
            if (State != GameState.Running) return;
            _speedIndex = Mathf.Clamp(index, 0, SpeedLevels.Length - 1);
            _paused = false;
            ApplyTimeScale();
        }

        public void TogglePause()
        {
            if (State != GameState.Running) return;
            _paused = !_paused;
            ApplyTimeScale();
        }

        // ---- 灵力（部署费用）----

        /// <summary>部署符卡时扣除灵力；灵力不足则拒绝部署。</summary>
        public bool TrySpendSpirit(int amount)
        {
            if (_spirit < amount) return false;
            _spirit -= amount;
            return true;
        }

        /// <summary>
        /// 回复灵力：自然回复 / 击破敌影 / 符卡离场返还都走这里，统一受上限约束。
        /// </summary>
        public void AddSpirit(int amount)
        {
            _spirit = Mathf.Min(GameConfig.MaxSpirit, _spirit + amount);
        }

        // ---- 敌人生命周期 ----

        public void NotifyEnemySpawned(Enemy enemy)
        {
            _enemies.Add(enemy);
            _spawnedEnemies++;
        }

        public void NotifyEnemyRemoved(Enemy enemy)
        {
            _enemies.Remove(enemy);
        }

        /// <summary>
        /// 击破敌影：按敌影类型回复灵力（数值取自 EnemyDefinition.SpiritReward，已按威胁度分级），
        /// 同时推进得分、连击与剿灭里程碑。
        /// </summary>
        public void NotifyEnemyKilled(Enemy enemy, int spirit)
        {
            AddSpirit(spirit);
            _score += spirit * GameConfig.ScorePerSpirit;
            _power = Mathf.Min(GameConfig.MaxPower, _power + GameConfig.PowerPerKill);
            _totalKills++;
            _combo++;
            if (_combo > _bestCombo) _bestCombo = _combo;

            // 每 10 连击额外回复灵力。
            if (_combo > 0 && _combo % 10 == 0)
            {
                AddSpirit(2);
            }

            // 剿灭式里程碑奖励：每击破 N 只敌影额外发一笔灵力。
            if (_totalKills > 0 && _totalKills % GameConfig.AnnihilationMilestoneInterval == 0)
            {
                AddSpirit(GameConfig.AnnihilationMilestoneSpirit);
            }
        }

        public void NotifyEnemyReachedBase(int damage)
        {
            _leakedEnemies++;
            _combo = 0; // 漏怪打断连击
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
            StopBarrage();
            WaveSpawner.Reset();
            _paused = false;
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

        /// <summary>主动撤退符卡，按造价和撤退比例返还灵力。</summary>
        public void RetreatTower(Tower tower)
        {
            if (tower == null) return;
            int refund = Mathf.RoundToInt(tower.Definition.Cost * GameConfig.RetreatRefundRatio);
            AddSpirit(refund);
            FloatingTextView.SpawnWorld(tower.transform.position + Vector3.up * 0.45f,
                "灵力 +" + refund, TdTheme.Spirit, 0.9f, 14);
            RemoveTower(tower);
        }

        /// <summary>符卡被击毁时，按造价和击毁比例返还灵力。</summary>
        public void OnTowerDefeated(Tower tower)
        {
            if (tower == null) return;
            int refund = Mathf.RoundToInt(tower.Definition.Cost * GameConfig.DefeatRefundRatio);
            AddSpirit(refund);
            FloatingTextView.SpawnWorld(tower.transform.position + Vector3.up * 0.45f,
                "灵力 +" + refund, TdTheme.Spirit, 0.9f, 14);
            ScreenShake.Shake(0.09f, 0.35f);
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

        // 返回命中数，供子弹经验值和 BOOM 反馈使用。
        public int DamageEnemiesInRadius(Vector2 center, float radius, float damage)
        {
            _spatialGrid.QueryCircle(center, radius + 0.5f, _queryBuffer);
            int hitCount = 0;
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var enemy = _queryBuffer[i];
                if (Vector2.Distance(center, enemy.transform.position) <= radius + enemy.Radius)
                {
                    enemy.TakeDamage(damage);
                    hitCount++;
                }
            }
            return hitCount;
        }

        public void ApplySlowInRange(Vector2 center, int rangeCells, float factor, float duration)
        {
            _spatialGrid.QueryChebyshev(center, rangeCells, _queryBuffer);
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                _queryBuffer[i].ApplySlow(factor, duration);
            }
        }

        /// <summary>每帧为范围内的非治疗塔恢复生命。</summary>
        public void HealTowersInRange(Vector2 center, int rangeCells, float healing)
        {
            if (healing <= 0f) return;

            foreach (var tower in _towers)
            {
                if (tower == null || tower.Type == TowerType.Heal) continue;
                if (Chebyshev(center, (Vector2)tower.transform.position) > rangeCells) continue;
                tower.Heal(healing);
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

        // ---- 结算 / 评分 ----

        /// <summary>计算结算数据与 Phigros 风格评级（Φ/V/S/A/B/C）。</summary>
        public GameResult GetResult()
        {
            float killRatio = _spawnedEnemies > 0 ? Mathf.Clamp01((float)_totalKills / _spawnedEnemies) : 0f;
            float lifeRatio = GameConfig.StartingLives > 0 ? Mathf.Clamp01((float)_lives / GameConfig.StartingLives) : 0f;
            float comboRatio = _spawnedEnemies > 0 ? Mathf.Clamp01((float)_bestCombo / _spawnedEnemies) : 0f;
            float performance = 0.5f * killRatio + 0.3f * lifeRatio + 0.2f * comboRatio;
            bool perfect = !IsPractice && State == GameState.Victory && CompletedStages == 5
                && _leakedEnemies == 0 && _totalKills == GameConfig.TotalEnemies;

            int rating;
            if (perfect)
            {
                rating = 100;
            }
            else if (IsPractice)
            {
                rating = Mathf.RoundToInt(99f * performance);
            }
            else
            {
                // 表现只影响本档分数，不能跨越已通过关数对应的评级。
                int minimum, maximum;
                switch (CompletedStages)
                {
                    case 0: minimum = 0; maximum = 59; break;
                    case 1: minimum = 60; maximum = 69; break;
                    case 2: minimum = 70; maximum = 79; break;
                    case 3: minimum = 80; maximum = 89; break;
                    case 4: minimum = 90; maximum = 94; break;
                    default: minimum = 95; maximum = 99; break;
                }
                rating = Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, performance));
            }

            // 结界纪录
            bool isNew = !IsPractice && _score > BestScore;
            if (isNew)
            {
                PlayerPrefs.SetInt("td_best_score", _score);
                PlayerPrefs.Save();
            }

            return new GameResult
            {
                Victory = State == GameState.Victory,
                CompletedStages = CompletedStages,
                IsPractice = IsPractice,
                Score = _score,
                TotalKills = _totalKills,
                LeakedEnemies = _leakedEnemies,
                LivesRemaining = _lives,
                Rating = rating,
                Grade = GradeFromRating(rating, perfect),
                BestCombo = _bestCombo,
                NewRecord = isNew,
            };
        }

        private static string GradeFromRating(int rating, bool perfect)
        {
            if (perfect && rating >= 100) return "Φ";
            if (rating >= 90) return "V";
            if (rating >= 80) return "S";
            if (rating >= 70) return "A";
            if (rating >= 60) return "B";
            return "C";
        }

        // ---- 弹幕射击（东方风格）----

        /// <summary>P点攒满后触发弹幕射击：范围扫射 + 跟踪弹，持续数秒。</summary>
        public void TriggerBarrage()
        {
            if (State != GameState.Running || _paused || !CanBarrage) return;
            _power = 0f;
            StopBarrage();
            Sfx.Barrage();
            ScreenShake.Shake(0.05f, 0.30f);
            _barrage = StartCoroutine(BarrageRoutine());
        }

        private void StopBarrage()
        {
            if (_barrage != null)
            {
                StopCoroutine(_barrage);
                _barrage = null;
            }
        }

        private IEnumerator BarrageRoutine()
        {
            float elapsed = 0f;
            float angleOffset = 0f;
            float angleStep = 360f / (GameConfig.BarrageDuration / GameConfig.BarrageTickInterval);

            while (elapsed < GameConfig.BarrageDuration)
            {
                FireBarrageSpread(angleOffset);
                FireBarrageHoming();

                angleOffset += angleStep;
                elapsed += GameConfig.BarrageTickInterval;
                yield return new WaitForSeconds(GameConfig.BarrageTickInterval);
            }

            _barrage = null;
        }

        private void FireBarrageSpread(float angleOffset)
        {
            var origin = Map.CellToWorld(GameConfig.BaseCell);
            int count = GameConfig.BarrageSpreadPerTick;

            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / count) + angleOffset) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Projectile.Spawn(GameConfig.BarrageSpread, origin, direction);
            }
        }

        private void FireBarrageHoming()
        {
            if (_enemies.Count == 0) return;

            var origin = Map.CellToWorld(GameConfig.BaseCell);
            int count = Mathf.Min(GameConfig.BarrageHomingPerTick, _enemies.Count);

            for (int i = 0; i < count; i++)
            {
                var enemy = _enemies[Random.Range(0, _enemies.Count)];
                Projectile.Spawn(GameConfig.BarrageHoming, origin, enemy);
            }
        }

        public void BeginRun(int stage = 0, bool practice = false)
        {
            StopGameLoop();
            StopBarrage();
            if (WaveSpawner != null) WaveSpawner.Reset();
            IsPractice = practice;
            StartStage = practice ? Mathf.Clamp(stage, 0, GameConfig.Rounds.Length - 1) : 0;
            _enemies.Clear();
            _towers.Clear();
            if (WorldRoot != null) WorldRoot.gameObject.SetActive(false);
            CreateWorld();
            ResetState();
            _frontEnd.gameObject.SetActive(false);
            _battleCanvas.SetActive(true);
            GetComponent<BattleMusic>().FollowBattle(StartStage);
            StartGame();
        }

        public void ReturnToMenu()
        {
            StopGameLoop();
            StopBarrage();
            WaveSpawner.Reset();
            State = GameState.Menu;
            _paused = false;
            Time.timeScale = 0f;
            WorldRoot.gameObject.SetActive(false);
            _battleCanvas.SetActive(false);
            _frontEnd.gameObject.SetActive(true);
            _frontEnd.ShowHome();
        }

        public void GrantRoundSupply()
        {
            AddSpirit(GameConfig.RoundClearSpirit);
            foreach (var tower in _towers)
                if (tower != null) tower.Heal(tower.MaxHealth * GameConfig.RoundRepairRatio);
        }

        public void Restart()
        {
            if (State == GameState.Menu) return;
            BeginRun(StartStage, IsPractice);
        }
    }
}
