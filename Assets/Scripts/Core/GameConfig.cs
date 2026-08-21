using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Core
{
    /// <summary>
    /// 集中存放所有可调数值（地图、轨迹、塔、敌人、波次、经济）。
    /// 这样设计便于平衡性调整，避免把魔法数字散落在逻辑代码里。
    /// </summary>
    public static class GameConfig
    {
        // ---- 地图 / 相机 ----
        public const float WorldHalfWidth = 9f;
        public const float WorldHalfHeight = 5f;
        public const float GridCellSize = 1f;
        public const float TrajectoryBlockRadius = 0.45f; // 轨迹禁放塔的半径（细线，保持地图开放）

        public const float CameraSize = 5.4f;

        public static readonly Color BackgroundColor = new Color(0.06f, 0.07f, 0.10f);
        public static readonly Color GridColor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color GridHoverColor = new Color(0.2f, 1f, 0.3f, 0.28f);
        public static readonly Color GridBlockedColor = new Color(1f, 0.30f, 0.30f, 0.40f);
        public static readonly Color GridRetreatColor = new Color(0.30f, 0.60f, 1f, 0.35f);
        public static readonly Color RangeCellColor = new Color(0.35f, 0.60f, 1f, 0.22f);
        public static readonly Color PathColor = new Color(0.30f, 0.34f, 0.42f);
        public static readonly Color BaseColor = new Color(0.20f, 0.85f, 0.40f);

        // ---- 经济 / 基地 ----
        public const int StartingGold = 160;
        public const int StartingLives = 10;

        // 塔被打败/主动撤退时的金币返还比例。
        public const float DefeatRefundRatio = 0.2f;   // 被打败返还 20%
        public const float RetreatRefundRatio = 0.5f;  // 主动撤退返还 50%

        // ---- 敌人轨迹（开放地图：固定起点/终点，每种敌人走不同路线）----
        public static readonly Vector2 StartPosition = new Vector2(-9.0f, 0.0f);
        public static readonly Vector2 EndPosition = new Vector2(9.0f, 0.0f);

        public static readonly Dictionary<EnemyType, Vector2[]> Trajectories =
            new Dictionary<EnemyType, Vector2[]>
        {
            {
                EnemyType.Normal,
                new Vector2[]
                {
                    new Vector2(-9.0f, 0.0f),
                    new Vector2(-4.0f, 1.0f),
                    new Vector2(0.0f, -1.0f),
                    new Vector2(4.0f, 1.0f),
                    new Vector2(9.0f, 0.0f),
                }
            },
            {
                EnemyType.Fast,
                new Vector2[]
                {
                    new Vector2(-9.0f, 0.0f),
                    new Vector2(-5.0f, -2.0f),
                    new Vector2(-1.0f, 2.0f),
                    new Vector2(3.0f, -2.0f),
                    new Vector2(7.0f, 2.0f),
                    new Vector2(9.0f, 0.0f),
                }
            },
            {
                EnemyType.Tank,
                new Vector2[]
                {
                    new Vector2(-9.0f, 0.0f),
                    new Vector2(-7.0f, 3.0f),
                    new Vector2(-2.0f, 3.0f),
                    new Vector2(2.0f, -3.0f),
                    new Vector2(6.0f, -3.0f),
                    new Vector2(9.0f, 0.0f),
                }
            },
        };

        // ---- 塔的定义 ----
        public static readonly Dictionary<TowerType, TowerDefinition> Towers =
            new Dictionary<TowerType, TowerDefinition>
        {
            {
                TowerType.Gun,
                new TowerDefinition
                {
                    Type = TowerType.Gun,
                    DisplayName = "机枪塔",
                    Cost = 60,
                    MaxHealth = 120f,
                    RangeCells = 3,
                    FireRate = 3.0f,
                    Damage = 12f,
                    ProjectileSpeed = 16f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(0.25f, 0.55f, 1.00f),
                    ProjectileColor = new Color(0.70f, 0.90f, 1.00f),
                }
            },
            {
                TowerType.Sniper,
                new TowerDefinition
                {
                    Type = TowerType.Sniper,
                    DisplayName = "狙击塔",
                    Cost = 100,
                    MaxHealth = 90f,
                    RangeCells = 5,
                    FireRate = 0.7f,
                    Damage = 90f,
                    ProjectileSpeed = 30f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(0.95f, 0.80f, 0.25f),
                    ProjectileColor = new Color(1.00f, 0.95f, 0.55f),
                }
            },
            {
                TowerType.Missile,
                new TowerDefinition
                {
                    Type = TowerType.Missile,
                    DisplayName = "导弹塔",
                    Cost = 130,
                    MaxHealth = 140f,
                    RangeCells = 4,
                    FireRate = 0.8f,
                    Damage = 45f,
                    ProjectileSpeed = 8f,
                    HomingStrength = 45f,     // 追踪加速度
                    SplashRadius = 1.4f,      // AOE 爆炸
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(1.00f, 0.45f, 0.15f),
                    ProjectileColor = new Color(1.00f, 0.70f, 0.25f),
                }
            },
            {
                TowerType.Slow,
                new TowerDefinition
                {
                    Type = TowerType.Slow,
                    DisplayName = "减速塔",
                    Cost = 80,
                    MaxHealth = 110f,
                    RangeCells = 3,
                    FireRate = 1f,
                    Damage = 0f,              // 不造成伤害
                    ProjectileSpeed = 0f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 0.45f,       // 减速到 45%
                    SlowDuration = 0.5f,
                    Color = new Color(0.40f, 0.90f, 0.95f),
                    ProjectileColor = Color.clear,
                }
            },
        };

        // ---- 敌人的定义 ----
        public static readonly Dictionary<EnemyType, EnemyDefinition> Enemies =
            new Dictionary<EnemyType, EnemyDefinition>
        {
            {
                EnemyType.Normal,
                new EnemyDefinition
                {
                    Type = EnemyType.Normal,
                    DisplayName = "普通怪",
                    MaxHealth = 60f,
                    Speed = 2.2f,
                    Radius = 0.30f,
                    GoldReward = 10,
                    DamageToBase = 1,
                    AttackDamage = 12f,
                    AttackRange = 0.8f,
                    AttackInterval = 1.0f,
                    Color = new Color(0.95f, 0.35f, 0.30f),
                }
            },
            {
                EnemyType.Fast,
                new EnemyDefinition
                {
                    Type = EnemyType.Fast,
                    DisplayName = "快速怪",
                    MaxHealth = 35f,
                    Speed = 3.6f,
                    Radius = 0.24f,
                    GoldReward = 12,
                    DamageToBase = 1,
                    AttackDamage = 9f,
                    AttackRange = 0.75f,
                    AttackInterval = 0.8f,
                    Color = new Color(1.00f, 0.85f, 0.25f),
                }
            },
            {
                EnemyType.Tank,
                new EnemyDefinition
                {
                    Type = EnemyType.Tank,
                    DisplayName = "坦克怪",
                    MaxHealth = 200f,
                    Speed = 1.4f,
                    Radius = 0.40f,
                    GoldReward = 30,
                    DamageToBase = 2,
                    AttackDamage = 30f,
                    AttackRange = 0.95f,
                    AttackInterval = 1.2f,
                    Color = new Color(0.80f, 0.35f, 0.95f),
                }
            },
        };

        // ---- 波次（血量随波次线性成长，见 WaveSpawner）----
        public static readonly WaveDefinition[] Waves = new WaveDefinition[]
        {
            new WaveDefinition { EnemyType = EnemyType.Normal, Count = 6,  SpawnInterval = 0.9f },
            new WaveDefinition { EnemyType = EnemyType.Normal, Count = 8,  SpawnInterval = 0.8f },
            new WaveDefinition { EnemyType = EnemyType.Fast,   Count = 8,  SpawnInterval = 0.6f },
            new WaveDefinition { EnemyType = EnemyType.Normal, Count = 12, SpawnInterval = 0.6f },
            new WaveDefinition { EnemyType = EnemyType.Tank,   Count = 4,  SpawnInterval = 1.4f },
            new WaveDefinition { EnemyType = EnemyType.Fast,   Count = 14, SpawnInterval = 0.5f },
            new WaveDefinition { EnemyType = EnemyType.Tank,   Count = 6,  SpawnInterval = 1.2f },
            new WaveDefinition { EnemyType = EnemyType.Normal, Count = 18, SpawnInterval = 0.45f },
        };
    }
}
