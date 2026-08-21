using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Core
{
    /// <summary>
    /// 集中存放所有可调数值（地图、正交路线、塔、敌人、波次、经济）。
    /// 这样设计便于平衡性调整，避免把魔法数字散落在逻辑代码里。
    /// </summary>
    public static class GameConfig
    {
        // ---- 地图 / 相机 ----
        public const float WorldHalfWidth = 9f;
        public const float WorldHalfHeight = 5f;
        public const float GridCellSize = 1f;

        public const float CameraSize = 5.4f;

        public static readonly Color BackgroundColor = new Color(0.06f, 0.07f, 0.10f);
        public static readonly Color GridColor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color GridHoverColor = new Color(0.2f, 1f, 0.3f, 0.28f);
        public static readonly Color GridBlockedColor = new Color(1f, 0.30f, 0.30f, 0.40f);
        public static readonly Color GridRetreatColor = new Color(0.30f, 0.60f, 1f, 0.35f);
        public static readonly Color RangeCellColor = new Color(0.35f, 0.60f, 1f, 0.22f);
        public static readonly Color BaseColor = new Color(0.20f, 0.85f, 0.40f);

        // ---- 经济 / 基地 ----
        public const int StartingGold = 160;
        public const int StartingLives = 10;

        // 塔被打败/主动撤退时的金币返还比例。
        public const float DefeatRefundRatio = 0.2f;   // 被打败返还 20%
        public const float RetreatRefundRatio = 0.5f;  // 主动撤退返还 50%

        // ---- 基地（蓝门）与正交路线（每条一个红门）----
        public static readonly Vector2Int BaseCell = new Vector2Int(8, 0);

        // 路线只由水平/竖直段组成，坐标均为格子中心；每条路线从红门走到蓝门。
        public static readonly Vector2Int[][] Routes = new Vector2Int[][]
        {
            // 路线 0：左上红门
            new Vector2Int[]
            {
                new Vector2Int(-8, 3),
                new Vector2Int(-3, 3),
                new Vector2Int(-3, 0),
                new Vector2Int(8, 0),
            },
            // 路线 1：左下红门
            new Vector2Int[]
            {
                new Vector2Int(-8, -3),
                new Vector2Int(-3, -3),
                new Vector2Int(-3, 0),
                new Vector2Int(8, 0),
            },
            // 路线 2：上方红门
            new Vector2Int[]
            {
                new Vector2Int(1, 4),
                new Vector2Int(1, 1),
                new Vector2Int(5, 1),
                new Vector2Int(5, 0),
                new Vector2Int(8, 0),
            },
        };

        public static readonly Color[] RouteColors = new Color[]
        {
            new Color(0.95f, 0.55f, 0.25f),
            new Color(0.25f, 0.70f, 0.95f),
            new Color(0.75f, 0.45f, 0.95f),
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
                    AttackRangeCells = 1,
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
                    AttackRangeCells = 1,
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
                    AttackRangeCells = 1,
                    AttackInterval = 1.2f,
                    Color = new Color(0.80f, 0.35f, 0.95f),
                }
            },
        };

        // ---- 波次（多个刷怪组，可同时从多个红门出怪；血量随波次线性成长）----
        public static readonly WaveDefinition[] Waves = new WaveDefinition[]
        {
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 0, Count = 3, SpawnInterval = 0.8f },
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 1, Count = 3, SpawnInterval = 0.8f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 2, Count = 4, SpawnInterval = 0.7f },
                    new SpawnGroup { EnemyType = EnemyType.Fast,   RouteIndex = 0, Count = 4, SpawnInterval = 0.6f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Fast, RouteIndex = 1, Count = 6, SpawnInterval = 0.5f },
                    new SpawnGroup { EnemyType = EnemyType.Fast, RouteIndex = 2, Count = 4, SpawnInterval = 0.5f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Tank,   RouteIndex = 0, Count = 2, SpawnInterval = 1.2f },
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 1, Count = 6, SpawnInterval = 0.6f },
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 2, Count = 6, SpawnInterval = 0.6f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Tank, RouteIndex = 2, Count = 2, SpawnInterval = 1.2f },
                    new SpawnGroup { EnemyType = EnemyType.Fast, RouteIndex = 0, Count = 6, SpawnInterval = 0.5f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Fast, RouteIndex = 1, Count = 8, SpawnInterval = 0.45f },
                    new SpawnGroup { EnemyType = EnemyType.Tank, RouteIndex = 0, Count = 2, SpawnInterval = 1.2f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Tank, RouteIndex = 1, Count = 3, SpawnInterval = 1.2f },
                    new SpawnGroup { EnemyType = EnemyType.Tank, RouteIndex = 2, Count = 3, SpawnInterval = 1.2f },
                }
            },
            new WaveDefinition
            {
                Groups = new SpawnGroup[]
                {
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 0, Count = 8, SpawnInterval = 0.45f },
                    new SpawnGroup { EnemyType = EnemyType.Normal, RouteIndex = 1, Count = 8, SpawnInterval = 0.45f },
                    new SpawnGroup { EnemyType = EnemyType.Fast,   RouteIndex = 2, Count = 6, SpawnInterval = 0.45f },
                }
            },
        };
    }
}
