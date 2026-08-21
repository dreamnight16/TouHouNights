using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 敌人：沿预设路径点向终点移动，具备血量、减速、死亡与到达终点逻辑。
    /// 采用对象池复用；不使用物理引擎，移动与命中检测均在 Update 中手动完成，
    /// 从而避免层/标签/Rigidbody 配置，保证工程「开箱即跑」。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Enemy : MonoBehaviour
    {
        private static readonly ObjectPool<Enemy> Pool = new ObjectPool<Enemy>(CreateNew, 32);

        private SpriteRenderer _sprite;
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
        private int _damageToBase;
        private int _goldReward;

        public bool IsAlive => _health > 0f;
        public float Radius => _definition != null ? _definition.Radius : 0.3f;
        public float HealthRatio => _maxHealth > 0f ? _health / _maxHealth : 0f;

        /// <summary>
        /// 到基地的最短距离（用于「距离终点最近」索敌策略）。
        /// 直接用 MapSystem 预计算的 BFS 距离场（O(1) 查表），替代逐段求和。
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
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            var enemy = go.AddComponent<Enemy>();
            enemy._sprite = sr;
            enemy._healthBar = HealthBarView.Attach(go.transform, 0.7f, 0.09f, 0.5f);
            return enemy;
        }

        public static Enemy Spawn(EnemyDefinition definition, float healthScale, Vector2[] path, Vector2 spawnPos)
        {
            var enemy = Pool.Get();
            enemy.transform.SetParent(GameManager.Instance.WorldRoot, true);
            enemy.transform.position = spawnPos;
            enemy.Configure(definition, healthScale, path);
            GameManager.Instance.NotifyEnemySpawned(enemy);
            return enemy;
        }

        private void Configure(EnemyDefinition definition, float healthScale, Vector2[] path)
        {
            _definition = definition;
            _path = path;
            _waypointIndex = 0;

            _maxHealth = definition.MaxHealth * healthScale;
            _health = _maxHealth;
            _speed = definition.Speed;
            _damageToBase = definition.DamageToBase;
            _goldReward = definition.GoldReward;
            _slowFactor = 1f;
            _slowTimer = 0f;
            _attackCooldown = 0f;

            _sprite.sprite = SpriteFactory.Circle(definition.Radius, definition.Color);
            _healthBar.SetRatio(1f);
        }

        private void Update()
        {
            if (!IsAlive || _path == null || _path.Length == 0) return;

            TickSlow();
            MoveTowardsNextWaypoint();
            TryAttackTower();
        }

        private void MoveTowardsNextWaypoint()
        {
            var target = _path[_waypointIndex];
            var position = (Vector2)transform.position;
            var delta = target - position;
            float step = _speed * _slowFactor * Time.deltaTime;

            if (delta.sqrMagnitude <= step * step)
            {
                transform.position = target;
                _waypointIndex++;
                if (_waypointIndex >= _path.Length)
                {
                    ReachBase();
                }
            }
            else
            {
                transform.position = position + delta.normalized * step;
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
            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            var tower = GameManager.Instance.GetNearestTower(transform.position, _definition.AttackRangeCells);
            if (tower == null) return;

            tower.TakeDamage(_definition.AttackDamage);
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
            GameManager.Instance.NotifyEnemyKilled(this, _goldReward);
            EffectFactory.SpawnBurst(transform.position, _definition.Color, 0.5f, 0.3f);
            Despawn();
        }

        private void ReachBase()
        {
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
