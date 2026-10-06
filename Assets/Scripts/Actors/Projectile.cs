using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 直线与追踪子弹，按距离检测命中，支持单体和范围伤害。
    /// 目标失效时，追踪弹尝试重锁定；无法重锁定则淡出。
    /// 无目标的扫射弹持续沿发射方向飞行。
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
        private SpriteRenderer _glowSr;

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
        private bool _directional; // 无目标的直线弹（弹幕扫射用）
        private Tower _source;     // 伤害计入发射塔的 BOOM 经验；无主弹为 null
        private float _trailTimer; // 拖尾落点计时

        private static Projectile CreateNew()
        {
            var go = new GameObject("Projectile");

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sortingOrder = WorldArt.LayerShotGlow;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = WorldArt.LayerShot;
            var projectile = go.AddComponent<Projectile>();
            projectile._sprite = sr;
            projectile._glowSr = glowSr;
            return projectile;
        }

        public static Projectile Spawn(TowerDefinition definition, Vector2 position, Enemy target)
        {
            return Spawn(definition, position, target, 1f, null);
        }

        /// <summary>
        /// 生成带目标的子弹，并应用伤害倍率。
        /// source 为发射塔；null 表示无主弹，不计入 BOOM 经验。
        /// </summary>
        public static Projectile Spawn(TowerDefinition definition, Vector2 position, Enemy target, float damageMultiplier, Tower source = null)
        {
            var p = Pool.Get();
            p.transform.SetParent(GameManager.Instance.WorldRoot, true);
            p.transform.position = new Vector3(position.x, position.y, GameConfig.ProjectileVisualHeight);
            p.Configure(definition, target, damageMultiplier, source);
            return p;
        }

        /// <summary>生成一枚朝指定方向飞行的直线弹（弹幕扫射用）。</summary>
        public static Projectile Spawn(TowerDefinition definition, Vector2 position, Vector2 direction)
        {
            var p = Pool.Get();
            p.transform.SetParent(GameManager.Instance.WorldRoot, true);
            p.transform.position = new Vector3(position.x, position.y, GameConfig.ProjectileVisualHeight);
            p.ConfigureDirectional(definition, direction);
            return p;
        }

        private void Configure(TowerDefinition definition, Enemy target, float damageMultiplier, Tower source)
        {
            _definition = definition;
            _target = target;
            _directional = false;
            _source = source;
            _damage = definition.Damage * damageMultiplier;
            _splashRadius = definition.SplashRadius;
            _speed = definition.ProjectileSpeed;
            _homingStrength = definition.HomingStrength;
            _lifetime = MaxLifetime;
            _fading = false;
            _fadeTimer = 0f;
            _trailTimer = 0f;

            _sprite.sprite = SpriteFactory.Diamond(0.085f, 0.20f, definition.ProjectileColor);
            _glowSr.sprite = SpriteFactory.Glow(0.24f, new Color(definition.ProjectileColor.r, definition.ProjectileColor.g, definition.ProjectileColor.b, 0.45f));

            var direction = target != null
                ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized
                : Vector2.up;
            _velocity = direction * _speed;
        }

        private void ConfigureDirectional(TowerDefinition definition, Vector2 direction)
        {
            _definition = definition;
            _target = null;
            _directional = true;
            _source = null;   // 弹幕扫射：无主弹，不计入 BOOM 经验值
            _damage = definition.Damage;
            _splashRadius = definition.SplashRadius;
            _speed = definition.ProjectileSpeed;
            _homingStrength = 0f;
            _lifetime = MaxLifetime;
            _fading = false;
            _fadeTimer = 0f;
            _trailTimer = 0f;

            _sprite.sprite = SpriteFactory.Diamond(0.085f, 0.20f, definition.ProjectileColor);
            _glowSr.sprite = SpriteFactory.Glow(0.24f, new Color(definition.ProjectileColor.r, definition.ProjectileColor.g, definition.ProjectileColor.b, 0.45f));
            _velocity = direction.normalized * _speed;
        }

        private void Update()
        {
            if (_fading)
            {
                UpdateFade();
                return;
            }

            _trailTimer -= Time.deltaTime;
            if (_trailTimer <= 0f)
            {
                _trailTimer = 0.055f;
                Effects.TrailDot.Spawn((Vector2)transform.position, _definition.ProjectileColor, 0.75f);
            }

            if (_directional)
            {
                UpdateDirectional();
                return;
            }

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Despawn();
                return;
            }

            // 目标失效后，只有追踪弹尝试重锁定。
            if (_target == null || !_target.IsAlive)
            {
                _target = TryRetarget();
                if (_target == null)
                {
                    BeginFade();
                    return;
                }
            }

            if (_homingStrength > 0f)
            {
                var desired = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized * _speed;
                _velocity = Vector2.MoveTowards(_velocity, desired, _homingStrength * Time.deltaTime);
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);
            FaceVelocity();

            float hitDistance = HitRadius + _target.Radius;
            if (Vector2.Distance(transform.position, _target.transform.position) <= hitDistance)
            {
                Impact(_target);
            }
        }

        /// <summary>弹体朝向飞行方向。</summary>
        private void FaceVelocity()
        {
            if (_velocity.sqrMagnitude < 0.0001f) return;
            transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f);
        }

        private void UpdateDirectional()
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            FaceVelocity();

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Despawn();
                return;
            }

            // 扫射弹命中：碰到最近敌人即结算（可带 AOE）。
            var hit = GameManager.Instance.GetNearestEnemy(transform.position, HitRadius + 0.35f);
            if (hit != null)
            {
                Impact(hit);
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
            int shownDamage = Mathf.RoundToInt(_damage);

            if (_splashRadius > 0f)
            {
                // 范围伤害按命中数累加发射塔的 BOOM 经验。
                int hit = GameManager.Instance.DamageEnemiesInRadius(transform.position, _splashRadius, _damage);
                _source?.AddXp(_damage * hit);
                EffectFactory.SpawnBurst(transform.position, _definition.ProjectileColor, _splashRadius, 0.35f);
                Sfx.Explode();
                ScreenShake.Shake(0.07f, 0.22f);
                FloatingTextView.SpawnWorld(transform.position, $"爆炸 {shownDamage}", TdTheme.DamageSplash, 0.8f, 14);
            }
            else
            {
                primary.TakeDamage(_damage);
                _source?.AddXp(_damage);
                EffectFactory.SpawnBurst(transform.position, _definition.ProjectileColor, 0.25f, 0.2f);
                FloatingTextView.SpawnWorld(primary.transform.position + Vector3.up * 0.5f, "-" + shownDamage, TdTheme.DamageText, 0.7f, 13);
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
            transform.localRotation = Quaternion.identity;
            _source = null;
            transform.SetParent(null); // 脱离场景根，避免被 WorldRoot 销毁时一并回收
            Pool.Release(this);
        }
    }
}
