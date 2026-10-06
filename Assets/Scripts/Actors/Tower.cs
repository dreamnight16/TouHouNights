using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.UI;
using TowerDefense.Util;

namespace TowerDefense.Actors
{
    /// <summary>
    /// 管理塔的攻击、减速、治疗、生命和 BOOM 技能。
    /// 伤害塔按 GameManager 的全局策略索敌；减速和治疗塔持续作用于范围内单位。
    /// </summary>
    public sealed class Tower : MonoBehaviour
    {
        private TowerDefinition _definition;
        private Transform _halo;
        private Transform _turret;       // 炮塔组整体朝向目标
        private Transform _visualRoot;   // 悬浮塔身组（入场/浮空/后坐统一作用；基座与血条保持贴地）
        private SpriteRenderer _muzzle;  // 炮口闪光
        private HealthBarView _healthBar;
        private float _cooldown;
        private float _maxHealth;
        private float _health;
        private bool _isHealer;
        private float _haloT;
        private float _spawnT;            // 入场缩放 0→1
        private float _bobT;              // 浮空呼吸计时
        private float _recoilT;           // 开火后坐
        private float _xp;                // BOOM 经验值（该塔子弹累计造成伤害）
        private float _boomCooldown;      // 距下次可 BOOM 的剩余冷却
        private SpriteRenderer _auraSr;   // 底部职业辉光（BOOM 就绪/爆发时呼吸增亮）
        private float _boomGlowT;         // BOOM 爆发闪光计时
        private const float SpawnDuration = 0.34f;
        private const float AuraRestRatio = 0.30f / 0.55f;   // 静息辉光基线的 alpha（=0.30 峰值）/ 0.55 烘焙峰值

        public TowerDefinition Definition => _definition;
        public TowerType Type => _definition.Type;
        public Vector2Int Cell { get; private set; }
        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _maxHealth > 0f ? _health / _maxHealth : 0f;

        // ---- 二技能 BOOM 状态 ----
        public float Xp => _xp;
        public bool CanBoom => _definition != null && _definition.Damage > 0f;   // 只有能造成伤害的塔可引爆
        public float BoomReadyRatio => CanBoom ? Mathf.Clamp01(_xp / GameConfig.BoomThreshold) : 0f;
        public bool BoomReady => CanBoom && _xp >= GameConfig.BoomThreshold && _boomCooldown <= 0f;

        /// <summary>累加该塔自身子弹造成的伤害（仅伤害塔计入；弹幕/无主弹通过 null 跳过）。</summary>
        public void AddXp(float amount)
        {
            if (CanBoom) _xp += Mathf.Max(0f, amount);
        }

        /// <summary>
        /// 手动引爆 BOOM，造成范围伤害后清空经验并进入冷却。
        /// 塔内校验运行状态、暂停和技能就绪，防止 UI 误触发。
        /// </summary>
        public void TriggerBoom()
        {
            var gm = GameManager.Instance;
            if (gm == null || !CanBoom || !BoomReady) return;
            if (gm.State != GameState.Running || gm.IsPaused) return;

            float boomDamage = _definition.Damage * GameConfig.BoomDamageMultiplier;
            int hits = gm.DamageEnemiesInRadius(transform.position, GameConfig.BoomRadius, boomDamage);

            EffectFactory.SpawnBurst(transform.position, _definition.Color, GameConfig.BoomRadius, 0.55f);
            Sfx.Explode();
            ScreenShake.Shake(0.14f, 0.40f);
            FloatingTextView.SpawnWorld(transform.position + Vector3.up * 0.6f, $"爆 {Mathf.RoundToInt(boomDamage)} ×{hits}", TdTheme.DamageSplash, 1.0f, 15);

            // 消耗全部经验，包括溢出部分；BOOM 自身伤害不再累加经验。
            _xp = 0f;
            _boomCooldown = GameConfig.BoomCooldown;
            _boomGlowT = 0.5f;
        }

        public void Configure(TowerDefinition definition, Vector2Int cell, Vector2 position)
        {
            _definition = definition;
            Cell = cell;
            // 根节点保留视觉高度，底座另行投影到地面。
            transform.position = new Vector3(position.x, position.y, GameConfig.TowerVisualHeight);
            _cooldown = 0f;
            _maxHealth = definition.MaxHealth;
            _health = _maxHealth;
            _isHealer = definition.HealPerSecond > 0f;

            var color = definition.Color;
            var deep = WorldArt.Shade(color, 0.62f);
            var bright = WorldArt.Tint(color, 0.45f);

            // ---- 贴地基座，不随塔身起伏 ----
            var ground = new GameObject("Ground");
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0f, 0f, -GameConfig.TowerVisualHeight);

            AddSprite(ground.transform, "Cast", SpriteFactory.Glow(0.52f, WorldArt.Shadow), 3,
                Vector3.zero, Color.white);
            AddSprite(ground.transform, "Plate", SpriteFactory.Hex(0.47f, deep), 4,
                Vector3.zero, Color.white);
            AddSprite(ground.transform, "Rim", SpriteFactory.Shell(0.47f, 0.045f, color), 5,
                Vector3.zero, Color.white);

            for (int i = 0; i < 4; i++)
            {
                var tick = AddSprite(ground.transform, "Tick",
                    SpriteFactory.RoundedSquare(0.13f, 0.055f, WorldArt.Alpha(color, 0.9f)), 5,
                    new Vector3(0f, 0.57f, 0f), Color.white);
                tick.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            }

            // ---- 悬浮塔身组（缩放/浮空/后坐统一作用；基座与血条保持贴地） ----
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            _visualRoot = visualRoot.transform;

            _auraSr = AddSprite(_visualRoot, "Aura",
                SpriteFactory.Glow(0.60f, WorldArt.Alpha(color, 0.55f)), 5, Vector3.zero, Color.white);
            _auraSr.color = new Color(1f, 1f, 1f, AuraRestRatio);

            // ---- 炮塔组：整组朝向目标 ----
            var turret = new GameObject("Turret");
            turret.transform.SetParent(_visualRoot, false);
            _turret = turret.transform;

            // 描边、本体和顶部受光共用剪影贴图，通过 renderer.color 调整明暗。
            var shape = SilhouetteFor(definition.Type, color);
            AddSprite(_turret, "Outline", shape, 6, Vector3.zero, WorldArt.Ink, 1.16f);
            AddSprite(_turret, "Body", shape, 7, new Vector3(0f, 0.02f, 0f), Color.white);
            AddSprite(_turret, "Top", shape, 8, new Vector3(0f, 0.11f, -0.01f), WorldArt.RimLight, 0.55f);
            AddSprite(_turret, "Core", SpriteFactory.Circle(0.085f, bright), 8,
                new Vector3(0f, -0.05f, -0.02f), Color.white);

            if (_isHealer)
            {
                _halo = AddSprite(_visualRoot, "Halo", SpriteFactory.Shell(0.32f, 0.035f, bright), 8,
                    new Vector3(0f, 0.22f, 0f), Color.white).transform;
                AddSprite(_visualRoot, "Cross",
                    SpriteFactory.Cross(0.22f, 0.075f, WorldArt.Tint(color, 0.75f)), 9,
                    new Vector3(0f, 0.22f, 0f), Color.white);
            }
            else
            {
                // 炮管：随炮塔组一起转向目标
                AddSprite(_turret, "Barrel", SpriteFactory.RoundedSquare(1f, 0.5f, bright), 8,
                    new Vector3(0f, 0.22f, 0f), Color.white, new Vector3(0.13f, 0.42f, 1f));

                _muzzle = AddSprite(_visualRoot, "Muzzle",
                    SpriteFactory.Glow(0.18f, new Color(1f, 1f, 1f, 0.9f)), 9,
                    new Vector3(0f, 0.46f, 0f), Color.white);
                _muzzle.enabled = false;
            }

            _healthBar = HealthBarView.Attach(transform, 0.8f, 0.09f, 0.62f);
            _healthBar.SetRatio(1f);

            // 随机浮空相位，避免群塔动画同步；入场与 BOOM 状态复位。
            _spawnT = 0f;
            _bobT = Random.Range(0f, 6.28f);
            _recoilT = 0f;
            _xp = 0f;
            _boomCooldown = 0f;
            _boomGlowT = 0f;
            transform.localScale = Vector3.one * 0.01f;
        }

        private void Update()
        {
            AnimateVisual();

            _boomCooldown = Mathf.Max(0f, _boomCooldown - Time.deltaTime);

            // 治疗塔：无子弹，持续为范围内友方塔回血 + 光环律动。
            if (_isHealer)
            {
                GameManager.Instance.HealTowersInRange(
                    transform.position,
                    _definition.HealRadiusCells,
                    _definition.HealPerSecond * Time.deltaTime);
                UpdateHalo();
                return;
            }

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

        /// <summary>用剪影区分塔的职业。</summary>
        private static Sprite SilhouetteFor(TowerType type, Color color)
        {
            switch (type)
            {
                case TowerType.Gun:     return SpriteFactory.Arrow(0.30f, 0.46f, color);    // 速射：竖直箭
                case TowerType.Sniper:  return SpriteFactory.Diamond(0.24f, 0.52f, color);  // 狙击：细长针
                case TowerType.Missile: return SpriteFactory.Hex(0.28f, color);             // 爆符：厚重六边
                case TowerType.Slow:    return SpriteFactory.Diamond(0.36f, 0.36f, color);  // 霜华：结晶菱
                case TowerType.Heal:    return SpriteFactory.Hex(0.26f, color);             // 甘霖：柔和六边
                default:                return SpriteFactory.Hex(0.28f, color);
            }
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, int order,
            Vector3 localPos, Color tint, float scale = 1f)
        {
            return AddSprite(parent, name, sprite, order, localPos, tint, Vector3.one * scale);
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, int order,
            Vector3 localPos, Color tint, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = order;
            return sr;
        }

        private void UpdateHalo()
        {
            _haloT += Time.deltaTime * 2.4f;
            float pulse = 1f + 0.10f * Mathf.Sin(_haloT);
            if (_halo != null) _halo.localScale = new Vector3(pulse, pulse, 1f);
        }

        private void AnimateVisual()
        {
            // 入场缩放使用 OutBack 过冲。
            if (_spawnT < 1f)
            {
                _spawnT = Mathf.Min(1f, _spawnT + Time.deltaTime / SpawnDuration);
                transform.localScale = Vector3.one * Mathf.Max(0.01f, UiEasings.OutBack(_spawnT));
            }

            // 浮空呼吸 + 开火后坐（仅作用于悬浮塔身组，基座与血条保持贴地）。
            _bobT += Time.deltaTime;
            _recoilT = Mathf.Max(0f, _recoilT - Time.deltaTime);
            float bob = 0.025f + 0.018f * Mathf.Sin(_bobT * 2.0f);
            float kick = _recoilT > 0f ? Mathf.Sin(_recoilT / 0.12f * Mathf.PI) * 0.05f : 0f;
            if (_visualRoot != null) _visualRoot.localPosition = new Vector3(0f, bob - kick, 0f);

            // 炮口闪光衰减。
            if (_muzzle != null && _muzzle.enabled)
            {
                var c = _muzzle.color;
                c.a -= Time.deltaTime * 11f;
                if (c.a <= 0f) _muzzle.enabled = false;
                else _muzzle.color = c;
            }

            // BOOM 辉光：爆发闪光 → 就绪呼吸增亮 → 静息。
            // 贴图 alpha 峰值为 0.55，renderer 的 alpha 按基线与峰值之比缩放。
            if (_auraSr != null)
            {
                _boomGlowT = Mathf.Max(0f, _boomGlowT - Time.deltaTime);
                float a;
                if (_boomGlowT > 0f)
                {
                    a = 1f;                                        // 爆发瞬间满亮
                }
                else if (BoomReady)
                {
                    a = AuraRestRatio + (1f - AuraRestRatio) * (0.5f + 0.5f * Mathf.Sin(_bobT * 4f)); // 呼吸到峰值
                }
                else
                {
                    a = AuraRestRatio;                             // 静息基线
                }
                _auraSr.color = new Color(1f, 1f, 1f, a);
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

        /// <summary>恢复生命至上限，死亡或满血时跳过。</summary>
        public void Heal(float amount)
        {
            if (_health <= 0f || _health >= _maxHealth) return;

            _health = Mathf.Min(_maxHealth, _health + amount);
            _healthBar.SetRatio(_health / _maxHealth);
        }

        private void AimAt(Vector3 targetPosition)
        {
            if (_turret == null) return;
            var dir = (Vector2)(targetPosition - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                _turret.up = dir.normalized;
            }
        }

        private void Fire(Enemy target)
        {
            var forward = _turret != null ? (Vector2)_turret.up : Vector2.up;
            var muzzle = (Vector2)transform.position + forward * 0.5f;
            Projectile.Spawn(_definition, muzzle, target, GameManager.Instance.DamageMultiplier, this);

            // 开火反馈：塔身下沉后坐 + 炮口点亮闪光。
            _recoilT = 0.12f;
            if (_muzzle != null)
            {
                _muzzle.transform.position = new Vector3(muzzle.x, muzzle.y, transform.position.z);
                _muzzle.color = new Color(1f, 1f, 1f, 1f);
                _muzzle.enabled = true;
            }
        }
    }
}
