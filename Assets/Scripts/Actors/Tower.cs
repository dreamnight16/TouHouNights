using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 防御塔：在射程内锁定敌人并攻击，同时具备生命值（可被敌人击毁）。
    /// - 普通塔：发射子弹（直线 / 追踪 / AOE 由定义决定）；
    /// - 减速塔：不发射子弹，改为对范围内敌人持续施加减速。
    /// 索敌使用 GameManager 的全局智能索敌策略（★★★）。
    /// </summary>
    public sealed class Tower : MonoBehaviour
    {
        private TowerDefinition _definition;
        private Transform _barrel;
        private HealthBarView _healthBar;
        private float _cooldown;
        private float _maxHealth;
        private float _health;

        public TowerDefinition Definition => _definition;
        public TowerType Type => _definition.Type;
        public Vector2Int Cell { get; private set; }
        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _maxHealth > 0f ? _health / _maxHealth : 0f;

        public void Configure(TowerDefinition definition, Vector2Int cell, Vector2 position)
        {
            _definition = definition;
            Cell = cell;
            transform.position = position;
            _cooldown = 0f;
            _maxHealth = definition.MaxHealth;
            _health = _maxHealth;

            // 塔身（圆形底盘）。
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = SpriteFactory.Circle(0.42f, definition.Color);
            bodySr.sortingOrder = 6;

            // 炮管（朝上放置，通过 _barrel.up 旋转指向目标）。
            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            barrel.transform.localScale = new Vector3(0.14f, 0.42f, 1f);
            var barrelSr = barrel.AddComponent<SpriteRenderer>();
            barrelSr.sprite = SpriteFactory.Square(1f, Color.Lerp(definition.Color, Color.white, 0.25f));
            barrelSr.sortingOrder = 7;
            _barrel = barrel.transform;

            _healthBar = HealthBarView.Attach(transform, 0.8f, 0.09f, 0.62f);
            _healthBar.SetRatio(1f);
        }

        private void Update()
        {
            // 减速塔：持续范围减速，不发射子弹。
            if (_definition.Damage <= 0f && _definition.SlowFactor < 1f)
            {
                GameManager.Instance.ApplySlowInRange(
                    transform.position,
                    _definition.RangeCells,
                    _definition.SlowFactor,
                    _definition.SlowDuration);
                return;
            }

            _cooldown -= Time.deltaTime;

            var target = GameManager.Instance.SelectTarget(transform.position, _definition.RangeCells);
            if (target == null)
            {
                return;
            }

            AimAt(target.transform.position);

            if (_cooldown <= 0f)
            {
                Fire(target);
                _cooldown = 1f / Mathf.Max(0.05f, _definition.FireRate);
            }
        }

        public void TakeDamage(float damage)
        {
            if (_health <= 0f) return;

            _health = Mathf.Max(0f, _health - damage);
            _healthBar.SetRatio(_health / _maxHealth);

            if (_health <= 0f)
            {
                GameManager.Instance.OnTowerDefeated(this);
            }
        }

        private void AimAt(Vector3 targetPosition)
        {
            var dir = (Vector2)(targetPosition - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                _barrel.up = dir.normalized;
            }
        }

        private void Fire(Enemy target)
        {
            var muzzle = (Vector2)transform.position + (Vector2)_barrel.up * 0.5f;
            Projectile.Spawn(_definition, muzzle, target, GameManager.Instance.DamageMultiplier);
        }
    }
}
