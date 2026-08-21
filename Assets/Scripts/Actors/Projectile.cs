using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 子弹：支持直线与追踪（Homing）两种飞行方式。
    /// - ★  追踪：带转向加速度，实时追向目标；
    /// - ★★ 目标死亡后：追踪弹尝试就近重锁定，直线弹平滑淡出消失；
    /// - ★★★ 命中后产生 AOE 爆炸，对范围内所有敌人造成伤害。
    /// 命中检测不使用物理引擎，直接按距离判断。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly ObjectPool<Projectile> Pool = new ObjectPool<Projectile>(CreateNew, 64);

        private const float RetargetRadius = 3.0f;
        private const float HitRadius = 0.12f;
        private const float MaxLifetime = 4.0f;
        private const float FadeDuration = 0.25f;

        private SpriteRenderer _sprite;

        private TowerDefinition _definition;
        private Enemy _target;
        private Vector2 _velocity;
        private float _damage;
        private float _splashRadius;
        private float _speed;
        private float _homingStrength;
        private float _lifetime;

        private bool _fading;
        private float _fadeTimer;

        private static Projectile CreateNew()
        {
            var go = new GameObject("Projectile");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            var projectile = go.AddComponent<Projectile>();
            projectile._sprite = sr;
            return projectile;
        }

        public static Projectile Spawn(TowerDefinition definition, Vector2 position, Enemy target)
        {
            var p = Pool.Get();
            p.transform.SetParent(GameManager.Instance.WorldRoot, true);
            p.transform.position = position;
            p.Configure(definition, target);
            return p;
        }

        private void Configure(TowerDefinition definition, Enemy target)
        {
            _definition = definition;
            _target = target;
            _damage = definition.Damage;
            _splashRadius = definition.SplashRadius;
            _speed = definition.ProjectileSpeed;
            _homingStrength = definition.HomingStrength;
            _lifetime = MaxLifetime;
            _fading = false;
            _fadeTimer = 0f;

            _sprite.sprite = SpriteFactory.Circle(0.09f, definition.ProjectileColor);

            var direction = target != null
                ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized
                : Vector2.up;
            _velocity = direction * _speed;
        }

        private void Update()
        {
            if (_fading)
            {
                UpdateFade();
                return;
            }

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Despawn();
                return;
            }

            // ★★ 目标死亡处理：追踪弹就近重锁定，否则平滑消失。
            if (_target == null || !_target.IsAlive)
            {
                _target = TryRetarget();
                if (_target == null)
                {
                    BeginFade();
                    return;
                }
            }

            // ★ 追踪：带转向加速度的实时追踪。
            if (_homingStrength > 0f)
            {
                var desired = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized * _speed;
                _velocity = Vector2.MoveTowards(_velocity, desired, _homingStrength * Time.deltaTime);
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);

            // 命中检测（手动距离判断）。
            float hitDistance = HitRadius + _target.Radius;
            if (Vector2.Distance(transform.position, _target.transform.position) <= hitDistance)
            {
                Impact(_target);
            }
        }

        private Enemy TryRetarget()
        {
            // 只有追踪弹会重新寻找新目标。
            if (_homingStrength <= 0f) return null;
            return GameManager.Instance.GetNearestEnemy(transform.position, RetargetRadius);
        }

        private void Impact(Enemy primary)
        {
            if (_splashRadius > 0f)
            {
                // ★★★ AOE 爆炸：范围内所有敌人受同等伤害。
                GameManager.Instance.DamageEnemiesInRadius(transform.position, _splashRadius, _damage);
                EffectFactory.SpawnBurst(transform.position, _definition.ProjectileColor, _splashRadius, 0.35f);
            }
            else
            {
                primary.TakeDamage(_damage);
                EffectFactory.SpawnBurst(transform.position, _definition.ProjectileColor, 0.25f, 0.2f);
            }

            Despawn();
        }

        private void BeginFade()
        {
            _fading = true;
            _fadeTimer = 0f;
        }

        private void UpdateFade()
        {
            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / FadeDuration);

            var color = _sprite.color;
            color.a = 1f - t;
            _sprite.color = color;

            transform.localScale = Vector3.one * (1f - t);

            if (t >= 1f)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            _sprite.color = Color.white;
            transform.localScale = Vector3.one;
            transform.SetParent(null); // 脱离场景根，避免被 WorldRoot 销毁时一并回收
            Pool.Release(this);
        }
    }
}
