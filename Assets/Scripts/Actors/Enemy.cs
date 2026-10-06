using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 沿预设路线移动，管理血量、减速和攻击，并在死亡或抵达基地时回收。
    /// 使用对象池；移动与碰撞判定不依赖物理引擎。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Enemy : MonoBehaviour
    {
        private static readonly ObjectPool<Enemy> Pool = new ObjectPool<Enemy>(CreateNew, 32);

        /// <summary>Boss 生成时触发（供 HUD 显示预警横幅）。</summary>
        public static event System.Action<Enemy> OnBossSpawned;

        /// <summary>Boss 被击破时触发（供 HUD 显示击破横幅）。</summary>
        public static event System.Action<Enemy> OnBossKilled;

        private SpriteRenderer _sprite;       // 落地辉光（贴地，不随朝向旋转）
        private SpriteRenderer _outlineSr;
        private SpriteRenderer _bodySr;
        private SpriteRenderer _highlightSr;
        private SpriteRenderer _shadowSr;
        private SpriteRenderer _coreSr;
        private SpriteRenderer _crownSr;      // Boss 专属：外环威胁标记
        private Transform _spin;              // 朝向组：随移动方向旋转
        private Vector2 _facing = Vector2.up;
        private HealthBarView _healthBar;

        private EnemyDefinition _definition;
        private Vector2[] _path;
        private int _waypointIndex;

        private float _maxHealth;
        private float _health;
        private float _speed;
        private float _slowFactor = 1f;
        private float _slowTimer;
        private float _attackCooldown;
        private float _attackDamage;
        private int _damageToBase;
        private int _spiritReward;    // 击破后回复的灵力
        private float _spawnT = 1f;   // 入场缩放 0→1（1=已登场）
        private float _animT;         // 行进脉冲计时

        public bool IsAlive => _health > 0f;
        public float Radius => _definition != null ? _definition.Radius : 0.3f;
        public float HealthRatio => _maxHealth > 0f ? _health / _maxHealth : 0f;

        // ---- UI 只读数据 ----
        public EnemyDefinition Definition => _definition;
        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public bool IsBoss => _definition != null && _definition.Type == EnemyType.Boss;

        /// <summary>减速仍在生效。</summary>
        public bool IsSlowed => _slowTimer > 0f && _slowFactor < 0.999f;

        /// <summary>
        /// 到基地的最短距离（用于「距离终点最近」索敌策略）。
        /// 优先读取地图的 BFS 距离场；无地图时沿剩余路线求和。
        /// </summary>
        public float DistanceToEnd
        {
            get
            {
                var map = GameManager.Instance != null ? GameManager.Instance.Map : null;
                if (map != null)
                {
                    return map.GetDistanceToBase(map.WorldToCell(transform.position));
                }
                return ComputeRouteDistance();
            }
        }

        private float ComputeRouteDistance()
        {
            if (_path == null || _waypointIndex >= _path.Length) return 0f;
            float d = Vector2.Distance(transform.position, _path[_waypointIndex]);
            for (int i = _waypointIndex + 1; i < _path.Length; i++)
            {
                d += Vector2.Distance(_path[i - 1], _path[i]);
            }
            return d;
        }

        private static Enemy CreateNew()
        {
            var go = new GameObject("Enemy");

            // 落地辉光独立于朝向组，保持贴地且不随敌人旋转。
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var sr = glow.AddComponent<SpriteRenderer>();
            sr.sortingOrder = WorldArt.LayerEnemyGlow;

            // 地面投影位于 z=0。
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sortingOrder = WorldArt.LayerEnemyShadow;

            // ---- 朝向组：描边 / 本体 / 芯 / 高光 全部随移动方向旋转 ----
            var spin = new GameObject("Spin");
            spin.transform.SetParent(go.transform, false);

            var outline = new GameObject("Outline");
            outline.transform.SetParent(spin.transform, false);
            var outlineSr = outline.AddComponent<SpriteRenderer>();
            outlineSr.sortingOrder = WorldArt.LayerEnemyEdge;

            // Boss 威胁环（与描边同层，非 Boss 时隐藏）
            var crown = new GameObject("Crown");
            crown.transform.SetParent(spin.transform, false);
            var crownSr = crown.AddComponent<SpriteRenderer>();
            crownSr.sortingOrder = WorldArt.LayerEnemyEdge;

            var body = new GameObject("Body");
            body.transform.SetParent(spin.transform, false);
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sortingOrder = WorldArt.LayerEnemyBody;

            var core = new GameObject("Core");
            core.transform.SetParent(spin.transform, false);
            var coreSr = core.AddComponent<SpriteRenderer>();
            coreSr.sortingOrder = WorldArt.LayerEnemyCore;

            var highlight = new GameObject("Highlight");
            highlight.transform.SetParent(spin.transform, false);
            var highlightSr = highlight.AddComponent<SpriteRenderer>();
            highlightSr.sortingOrder = WorldArt.LayerEnemyTop;

            var enemy = go.AddComponent<Enemy>();
            enemy._sprite = sr;
            enemy._outlineSr = outlineSr;
            enemy._bodySr = bodySr;
            enemy._crownSr = crownSr;
            enemy._highlightSr = highlightSr;
            enemy._shadowSr = shadowSr;
            enemy._coreSr = coreSr;
            enemy._spin = spin.transform;
            enemy._healthBar = HealthBarView.Attach(go.transform, 0.7f, 0.09f, 0.5f);
            return enemy;
        }

        public static Enemy Spawn(EnemyDefinition definition, float healthScale, Vector2[] path, Vector2 spawnPos,
            float speedScale = 1f, float damageScale = 1f)
        {
            var enemy = Pool.Get();
            enemy.transform.SetParent(GameManager.Instance.WorldRoot, true);
            enemy.transform.position = spawnPos;
            enemy.Configure(definition, healthScale, speedScale, damageScale, path);
            GameManager.Instance.NotifyEnemySpawned(enemy);

            if (definition.Type == EnemyType.Boss)
            {
                OnBossSpawned?.Invoke(enemy);
            }

            return enemy;
        }

        private void Configure(EnemyDefinition definition, float healthScale, float speedScale, float damageScale, Vector2[] path)
        {
            _definition = definition;
            _path = path;
            _waypointIndex = 0;

            _maxHealth = definition.MaxHealth * healthScale;
            _health = _maxHealth;
            _speed = definition.Speed * speedScale;
            _damageToBase = definition.DamageToBase;
            _attackDamage = definition.AttackDamage * damageScale;
            _spiritReward = definition.SpiritReward;
            _slowFactor = 1f;
            _slowTimer = 0f;
            _attackCooldown = 0f;

            transform.position = new Vector3(transform.position.x, transform.position.y, GameConfig.EnemyVisualHeight);

            float r = definition.Radius;
            var color = definition.Color;
            var bright = WorldArt.Tint(color, 0.45f);

            // 落地辉光（贴地，不旋转）
            _sprite.transform.localPosition = new Vector3(0f, 0f, -GameConfig.EnemyVisualHeight);
            _sprite.sprite = SpriteFactory.Glow(r * 1.45f, WorldArt.Alpha(color, 0.20f));

            // 地面柔影
            _shadowSr.transform.localPosition = new Vector3(0f, 0f, -GameConfig.EnemyVisualHeight);
            _shadowSr.transform.localScale = new Vector3(1f, 0.6f, 1f);
            _shadowSr.sprite = SpriteFactory.Circle(r * 0.78f, WorldArt.Shadow);

            // 描边、本体和顶部受光共用剪影贴图，通过 renderer.color 调整明暗。
            var shape = SilhouetteFor(definition.Type, color);
            _outlineSr.sprite = shape;
            _outlineSr.transform.localScale = Vector3.one * 1.16f;
            _outlineSr.color = WorldArt.Ink;

            _bodySr.sprite = shape;
            _bodySr.color = Color.white;

            _highlightSr.sprite = shape;
            _highlightSr.transform.localPosition = new Vector3(0f, r * 0.30f, -0.01f);
            _highlightSr.transform.localScale = Vector3.one * 0.52f;
            _highlightSr.color = WorldArt.RimLight;

            // 能量芯
            _coreSr.sprite = SpriteFactory.Circle(r * 0.26f, bright);
            _coreSr.transform.localPosition = new Vector3(0f, -r * 0.10f, -0.02f);
            _coreSr.color = Color.white;

            // Boss 专属威胁环。
            bool boss = definition.Type == EnemyType.Boss;
            _crownSr.gameObject.SetActive(boss);
            if (boss)
            {
                _crownSr.sprite = SpriteFactory.Shell(r * 1.75f, 0.055f, bright);
                _crownSr.color = Color.white;
            }

            // 初始朝向沿路线第一段。
            _facing = _path != null && _path.Length >= 2
                ? ((Vector2)_path[1] - (Vector2)_path[0]).normalized
                : Vector2.up;
            if (_spin != null)
            {
                _spin.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg - 90f);
            }

            _healthBar.SetRatio(1f);

            // 入场：从 0.15 过冲放大登场；行进脉冲计时归零。
            _spawnT = 0f;
            _animT = 0f;
            transform.localScale = Vector3.one * 0.15f;
        }

        private void Update()
        {
            if (!IsAlive || _path == null || _path.Length == 0) return;

            AnimateVisual();
            TickSlow();
            MoveTowardsNextWaypoint();
            TryAttackTower();
        }

        /// <summary>入场缩放和行进脉冲只影响显示，不改变碰撞半径。</summary>
        private void AnimateVisual()
        {
            _animT += Time.deltaTime;

            if (_spawnT < 1f)
            {
                _spawnT = Mathf.Min(1f, _spawnT + Time.deltaTime / 0.20f);
                transform.localScale = Vector3.one * Mathf.Max(0.15f, UiEasings.OutBack(_spawnT));
            }
            else
            {
                float pulse = 1f + 0.022f * Mathf.Sin(_animT * 3.2f);
                transform.localScale = Vector3.one * pulse;
            }

            // 减速时本体泛青。
            if (_bodySr != null)
            {
                _bodySr.color = IsSlowed
                    ? Color.Lerp(Color.white, new Color(0.55f, 0.85f, 1f, 1f), 0.55f)
                    : Color.white;
            }

            if (_spin != null && _facing.sqrMagnitude > 0.0001f)
            {
                float target = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg - 90f;
                float cur = _spin.localEulerAngles.z;
                float t = 1f - Mathf.Exp(-14f * Time.deltaTime);
                _spin.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(cur, target, t));
            }
        }

        /// <summary>用剪影区分敌人类型。</summary>
        private static Sprite SilhouetteFor(EnemyType type, Color color)
        {
            switch (type)
            {
                case EnemyType.Fast: return SpriteFactory.Arrow(0.28f, 0.58f, color);
                case EnemyType.Tank: return SpriteFactory.Hex(0.42f, color);
                case EnemyType.Boss: return SpriteFactory.Arrow(0.68f, 0.92f, color);
                default:             return SpriteFactory.Arrow(0.42f, 0.58f, color);
            }
        }

        private void MoveTowardsNextWaypoint()
        {
            var target = _path[_waypointIndex];
            var position = (Vector2)transform.position;
            var delta = target - position;
            float step = _speed * _slowFactor * Time.deltaTime;

            if (delta.sqrMagnitude > 0.0001f) _facing = delta.normalized;

            if (delta.sqrMagnitude <= step * step)
            {
                transform.position = new Vector3(target.x, target.y, transform.position.z); // 保留视觉高度
                _waypointIndex++;
                if (_waypointIndex >= _path.Length)
                {
                    ReachBase();
                }
            }
            else
            {
                var next = position + delta.normalized * step;
                transform.position = new Vector3(next.x, next.y, transform.position.z); // 保留视觉高度
            }
        }

        private void TickSlow()
        {
            if (_slowTimer <= 0f) return;
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f)
            {
                _slowTimer = 0f;
                _slowFactor = 1f; // 减速结束，恢复原速
            }
        }

        private void TryAttackTower()
        {
            if (!IsAlive) return;

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            var tower = GameManager.Instance.GetNearestTower(transform.position, _definition.AttackRangeCells);
            if (tower == null) return;

            tower.TakeDamage(_attackDamage);
            _attackCooldown = _definition.AttackInterval;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _health = Mathf.Max(0f, _health - damage);
            _healthBar.SetRatio(HealthRatio);

            if (_health <= 0f)
            {
                Die();
            }
        }

        public void ApplySlow(float factor, float duration)
        {
            if (!IsAlive) return;
            _slowFactor = Mathf.Min(_slowFactor, factor);
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        private void Die()
        {
            GameManager.Instance.NotifyEnemyKilled(this, _spiritReward);
            Sfx.Kill();

            // Boss 死亡时更大的爆炸 + 屏幕震动 + 击破横幅
            bool isBoss = _definition.Type == EnemyType.Boss;
            float burstRadius = isBoss ? 1.5f : 0.5f;
            float burstDur = isBoss ? 0.8f : 0.3f;
            EffectFactory.SpawnBurst(transform.position, _definition.Color, burstRadius, burstDur);
            if (isBoss)
            {
                Sfx.Explode();
                ScreenShake.Shake(0.15f, 0.50f);
                OnBossKilled?.Invoke(this);
            }

            FloatingTextView.SpawnWorld(transform.position + Vector3.up * 0.55f, "灵力 +" + _spiritReward,
                TdTheme.GoldText, 0.9f, isBoss ? 18 : 14);
            Despawn();
        }

        private void ReachBase()
        {
            _health = 0f; // 标记死亡，避免同帧继续攻击塔
            Sfx.Leak();
            ScreenShake.Shake(0.12f, 0.35f);
            GameManager.Instance.NotifyEnemyReachedBase(_damageToBase);
            Despawn();
        }

        private void Despawn()
        {
            GameManager.Instance.NotifyEnemyRemoved(this);
            transform.SetParent(null); // 脱离场景根，避免被 WorldRoot 销毁时一并回收
            Pool.Release(this);
        }
    }
}
