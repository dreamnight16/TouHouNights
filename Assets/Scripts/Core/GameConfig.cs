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

        public static readonly Color BackgroundColor = new Color(0.043f, 0.051f, 0.071f); // #0B0D12 深蓝黑战场底
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

        // ---- P点 / 得分 / 弹幕射击（东方风格）----
        public const float MaxPower = 1f;              // P点上限（满值即可释放弹幕）
        public const float PowerPerKill = 0.04f;       // 每击杀获得的 P点
        public const float PowerDamageBonus = 0.5f;    // P点满时塔伤害加成（+50%）
        public const int ScorePerGold = 10;            // 得分换算（击杀金币 × 10）
        public const int TargetScore = 6000;           // 评分用的目标得分（得分效率=得分/目标）
        public const float BarrageDuration = 1.6f;     // 弹幕持续时间（秒）
        public const float BarrageTickInterval = 0.12f;// 弹幕发射间隔（秒）
        public const int BarrageSpreadPerTick = 10;    // 每次扫射子弹数
        public const int BarrageHomingPerTick = 6;     // 每次追踪弹数

        // 弹幕用的子弹定义（不属于可放置塔，仅作为 Projectile 的数值载体）。
        public static readonly TowerDefinition BarrageHoming = new TowerDefinition
        {
            Type = TowerType.Gun,
            DisplayName = "弹幕追踪弹",
            Cost = 0,
            Damage = 18f,
            ProjectileSpeed = 10f,
            HomingStrength = 70f,
            SplashRadius = 0.5f,
            SlowFactor = 1f,
            SlowDuration = 0f,
            Color = new Color(1f, 0.4f, 0.6f),
            ProjectileColor = new Color(1f, 0.5f, 0.7f),
        };

        public static readonly TowerDefinition BarrageSpread = new TowerDefinition
        {
            Type = TowerType.Gun,
            DisplayName = "弹幕扫射弹",
            Cost = 0,
            Damage = 12f,
            ProjectileSpeed = 14f,
            HomingStrength = 0f,
            SplashRadius = 0f,
            SlowFactor = 1f,
            SlowDuration = 0f,
            Color = new Color(0.5f, 0.9f, 1f),
            ProjectileColor = new Color(0.55f, 0.9f, 1f),
        };

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

        // ---- 剿灭式连续刷怪时间轴（混合兵种、多红门、随时间变难）----
        public static readonly float AnnihilationHealthRamp = 0.004f;  // 每过 1 秒，敌人血量加成 +0.4%
        public const int AnnihilationMilestoneInterval = 40;           // 每击杀 N 个触发一次里程碑奖励
        public const int AnnihilationMilestoneGold = 20;               // 里程碑奖励金币

        public static readonly SpawnEntry[] AnnihilationSchedule = new SpawnEntry[]
        {
            // 新手期（0~35s）：普通怪、慢节奏
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 0, StartTime = 0f,   Count = 10, SpawnInterval = 1.2f },
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 1, StartTime = 2f,   Count = 10, SpawnInterval = 1.2f },
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 2, StartTime = 8f,   Count = 10, SpawnInterval = 1.0f },

            // 成长期（30~80s）：普通 + 快速混合
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 0, StartTime = 30f,  Count = 12, SpawnInterval = 0.8f },
            new SpawnEntry { EnemyType = EnemyType.Fast,   RouteIndex = 1, StartTime = 32f,  Count = 10, SpawnInterval = 0.6f },
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 2, StartTime = 40f,  Count = 12, SpawnInterval = 0.7f },
            new SpawnEntry { EnemyType = EnemyType.Fast,   RouteIndex = 0, StartTime = 46f,  Count = 8,  SpawnInterval = 0.6f },

            // 熟练期（70~130s）：快速 + 坦克混合
            new SpawnEntry { EnemyType = EnemyType.Fast, RouteIndex = 1, StartTime = 70f,  Count = 14, SpawnInterval = 0.45f },
            new SpawnEntry { EnemyType = EnemyType.Fast, RouteIndex = 2, StartTime = 72f,  Count = 12, SpawnInterval = 0.5f },
            new SpawnEntry { EnemyType = EnemyType.Tank, RouteIndex = 0, StartTime = 82f,  Count = 4,  SpawnInterval = 1.2f },
            new SpawnEntry { EnemyType = EnemyType.Fast, RouteIndex = 0, StartTime = 92f,  Count = 12, SpawnInterval = 0.45f },
            new SpawnEntry { EnemyType = EnemyType.Tank, RouteIndex = 2, StartTime = 100f, Count = 4,  SpawnInterval = 1.1f },

            // 职业期（120~190s）：重压混合
            new SpawnEntry { EnemyType = EnemyType.Tank,   RouteIndex = 1, StartTime = 120f, Count = 5,  SpawnInterval = 1.0f },
            new SpawnEntry { EnemyType = EnemyType.Fast,   RouteIndex = 0, StartTime = 122f, Count = 14, SpawnInterval = 0.4f },
            new SpawnEntry { EnemyType = EnemyType.Normal, RouteIndex = 2, StartTime = 132f, Count = 16, SpawnInterval = 0.4f },
            new SpawnEntry { EnemyType = EnemyType.Tank,   RouteIndex = 0, StartTime = 142f, Count = 5,  SpawnInterval = 0.9f },
            new SpawnEntry { EnemyType = EnemyType.Fast,   RouteIndex = 1, StartTime = 152f, Count = 14, SpawnInterval = 0.4f },
            new SpawnEntry { EnemyType = EnemyType.Tank,   RouteIndex = 2, StartTime = 162f, Count = 6,  SpawnInterval = 0.8f },
        };

        public static int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var entry in AnnihilationSchedule)
                {
                    total += entry.Count;
                }
                return total;
            }
        }
    }
}
