using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Core
{
    /// <summary>
    /// 集中存放所有可调数值（地图、正交路线、塔、敌人、Stage、经济）。
    /// 这样设计便于平衡性调整，避免把魔法数字散落在逻辑代码里。
    /// </summary>
    public static class GameConfig
    {
        // ---- 地图 / 相机 ----
        public const float WorldHalfWidth = 9f;
        public const float WorldHalfHeight = 5f;
        public const float GridCellSize = 1f;

        // ---- 轴测相机（参考明日方舟斜俯视 3/4 视角）----
        // 绕 X 轴前倾 50°，地面在屏幕上纵向压缩，配合单位的 Z 高度（视觉高度）产生立体感。
        // ---- 透视相机（近大远小：屏幕下方=相机近处=大，上方=远处=小）----
        // 要点：相机位于地图下方（y 为负）前方，视线微微上仰看向地图——
        // 这样地图下缘（近）自然放大、上缘（远）缩小，类似「坐在第一排看展板」。
        public const float CameraFov = 50f;                       // 垂直视场角
        public static readonly Vector3 CameraPosition = new Vector3(0f, -3.9f, 13f);
        public static readonly Vector3 CameraLookAt = new Vector3(0f, -1.0f, 0f);   // 视野中心（负值让地图略偏上，为底部 HUD 让位）

        // ---- 视觉高度（单位在透视相机下的“身高”，仅视觉，不参与碰撞）----
        public const float TowerVisualHeight = 0.30f;
        public const float EnemyVisualHeight = 0.22f;
        public const float ProjectileVisualHeight = 0.26f;

        // 配色与 WorldArt / TdTheme 同一套：深蓝灰底 + 天蓝我方 + 珊瑚红敌方 + 琥珀强调。
        public static readonly Color BackgroundColor = new Color(0.039f, 0.059f, 0.082f); // #0A0F15
        public static readonly Color GridColor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color GridHoverColor = new Color(0.23f, 0.66f, 0.88f, 0.32f);
        public static readonly Color GridBlockedColor = new Color(1f, 0.42f, 0.35f, 0.42f);
        public static readonly Color GridRetreatColor = new Color(0.34f, 0.72f, 0.96f, 0.38f);
        public static readonly Color RangeCellColor = new Color(0.23f, 0.66f, 0.88f, 0.22f);
        public static readonly Color BaseColor = new Color(0.23f, 0.66f, 0.88f);

        // ---- 灵力 SPIRIT（部署费用 · 参考明日方舟的 DP）----
        // 灵力是部署符卡唯一的资源，累计逻辑由「三个进项 + 两个返还口」组成：
        //   ① 初始灵力  —— 开局先给一笔，方舟式「只够先落一两个单位」，逼出「先放谁」的取舍；
        //   ② 自然回复  —— 每秒稳定进账，长线主力（方舟 DP 自然回费 1/s）；
        //   ③ 击破回复  —— 按敌影威胁度分级回收，越难缠回得越多（见 Enemies[type].SpiritReward）；
        //   ④ 撤退符卡  —— 按造价返还一部分已投入的灵力，允许换阵（方舟不返还，本作刻意放宽）；
        //   ⑤ 符卡被击毁 —— 只返还更少，作为「没守住」的惩罚。
        public const int StartingSpirit = 150;        // 初始灵力：方舟式开局，先落两个低费单位
        public const int SpiritRegenPerSecond = 1;    // 自然回复：灵力 / 秒
        public const int MaxSpirit = 1500;            // 灵力上限：防止无限囤积，让「花在哪」形成取舍

        // 符卡离场时的灵力返还比例（按造价折算）。
        public const float DefeatRefundRatio = 0.2f;   // 被击毁返还 20%
        public const float RetreatRefundRatio = 0.5f;  // 主动撤退返还 50%

        // ---- 基地 ----
        public const int StartingLives = 10;
        public const int MaxTowers = 8;       // 部署位上限（参考方舟：关卡内最多上场 8 名）

        // ---- P点 / 得分 / 弹幕射击（东方风格）----
        public const float MaxPower = 1f;              // P点上限（满值即可释放弹幕）
        public const float PowerPerKill = 0.04f;       // 每击杀获得的 P点
        public const float PowerDamageBonus = 0.5f;    // P点满时塔伤害加成（+50%）
        public const int ScorePerSpirit = 10;          // 得分换算（击破灵力 × 10）
        public const int TargetScore = 6000;           // 评分用的目标得分（得分效率=得分/目标）
        public const float BarrageDuration = 1.6f;     // 弹幕持续时间（秒）
        public const float BarrageTickInterval = 0.12f;// 弹幕发射间隔（秒）
        public const int BarrageSpreadPerTick = 10;    // 每次扫射子弹数
        public const int BarrageHomingPerTick = 6;     // 每次追踪弹数

        // 弹幕用的子弹定义（不属于可放置塔，仅作为 Projectile 的数值载体）。
        public static readonly TowerDefinition BarrageHoming = new TowerDefinition
        {
            Type = TowerType.Gun,
            DisplayName = "灵力追踪型",
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
            DisplayName = "范围扩大型",
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

        // ---- 二技能 BOOM（爆）：塔的"经验值"= 该塔自身子弹累计造成伤害；达标后可手动引爆 ----
        public const float BoomThreshold = 400f;            // 触发 BOOM 所需累计伤害（XP）
        public const float BoomRadius = 2.4f;              // 爆炸范围（世界单位）
        public const float BoomDamageMultiplier = 5f;      // BOOM 命中伤害 = 基础伤害 × 该倍率
        public const float BoomCooldown = 3f;              // BOOM 之间的最短间隔（秒），防狂点连爆

        // ---- 基地（蓝门）与正交路线（每条一个红门）----
        public static readonly Vector2Int BaseCell = new Vector2Int(8, 0);

        // 路线只由水平/竖直段组成，坐标均为格子中心；每条路线从红门走到蓝门。
        public static readonly Vector2Int[][] Routes = new Vector2Int[][]
        {
            // 三条路线在不同区域折返，仅在终点前汇合。
            new Vector2Int[] { new Vector2Int(-8, 3), new Vector2Int(-4, 3), new Vector2Int(-4, 1), new Vector2Int(3, 1), new Vector2Int(3, 3), new Vector2Int(7, 3), new Vector2Int(7, 0), BaseCell },
            new Vector2Int[] { new Vector2Int(-8, -3), new Vector2Int(-5, -3), new Vector2Int(-5, -1), new Vector2Int(1, -1), new Vector2Int(1, -3), new Vector2Int(6, -3), new Vector2Int(6, 0), BaseCell },
            new Vector2Int[] { new Vector2Int(0, 4), new Vector2Int(0, 1), new Vector2Int(-2, 1), new Vector2Int(-2, -1), new Vector2Int(4, -1), new Vector2Int(4, 0), BaseCell },
        };

        public static readonly Color[] RouteColors = new Color[]
        {
            new Color(0.96f, 0.65f, 0.29f),
            new Color(0.31f, 0.73f, 0.94f),
            new Color(0.69f, 0.51f, 0.90f),
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
                    DisplayName = "博丽御札",
                    Cost = 60,
                    MaxHealth = 120f,
                    RangeCells = 2,
                    FireRate = 3.0f,
                    Damage = 12f,
                    ProjectileSpeed = 16f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(0.29f, 0.66f, 0.88f),
                    ProjectileColor = new Color(0.62f, 0.86f, 1.00f),
                }
            },
            {
                TowerType.Sniper,
                new TowerDefinition
                {
                    Type = TowerType.Sniper,
                    DisplayName = "银刃飞刀",
                    Cost = 100,
                    MaxHealth = 90f,
                    RangeCells = 3,
                    FireRate = 0.7f,
                    Damage = 90f,
                    ProjectileSpeed = 30f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(0.91f, 0.70f, 0.24f),
                    ProjectileColor = new Color(1.00f, 0.89f, 0.52f),
                }
            },
            {
                TowerType.Missile,
                new TowerDefinition
                {
                    Type = TowerType.Missile,
                    DisplayName = "七曜追星",
                    Cost = 130,
                    MaxHealth = 140f,
                    RangeCells = 3,
                    FireRate = 0.8f,
                    Damage = 45f,
                    ProjectileSpeed = 8f,
                    HomingStrength = 45f,     // 追踪加速度
                    SplashRadius = 1.4f,      // AOE 爆炸
                    SlowFactor = 1f,
                    SlowDuration = 0f,
                    Color = new Color(0.95f, 0.47f, 0.23f),
                    ProjectileColor = new Color(1.00f, 0.72f, 0.38f),
                }
            },
            {
                TowerType.Slow,
                new TowerDefinition
                {
                    Type = TowerType.Slow,
                    DisplayName = "冰霜结界",
                    Cost = 80,
                    MaxHealth = 110f,
                    RangeCells = 2,
                    FireRate = 1f,
                    Damage = 0f,              // 不造成伤害
                    ProjectileSpeed = 0f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 0.45f,       // 减速到 45%
                    SlowDuration = 0.5f,
                    Color = new Color(0.34f, 0.77f, 0.91f),
                    ProjectileColor = Color.clear,
                }
            },
            {
                TowerType.Heal,
                new TowerDefinition
                {
                    Type = TowerType.Heal,
                    DisplayName = "祈愿之阵",
                    Cost = 100,
                    MaxHealth = 150f,
                    RangeCells = 2,
                    FireRate = 1f,
                    Damage = 0f,              // 不造成伤害
                    ProjectileSpeed = 0f,
                    HomingStrength = 0f,
                    SplashRadius = 0f,
                    SlowFactor = 1f,          // 不减速（避免被识别为霜华结界）
                    SlowDuration = 0f,
                    HealPerSecond = 22f,      // 每秒为范围内友方塔修复的生命
                    HealRadiusCells = 2,      // 生效半径（格子数，Chebyshev）
                    Color = new Color(0.37f, 0.83f, 0.65f),
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
                    DisplayName = "普通",
                    MaxHealth = 60f,
                    Speed = 2.2f,
                    Radius = 0.30f,
                    SpiritReward = 10,
                    DamageToBase = 1,
                    AttackDamage = 12f,
                    AttackRangeCells = 1,
                    AttackInterval = 1.0f,
                    Color = new Color(0.94f, 0.42f, 0.35f),
                }
            },
            {
                EnemyType.Fast,
                new EnemyDefinition
                {
                    Type = EnemyType.Fast,
                    DisplayName = "高速",
                    MaxHealth = 35f,
                    Speed = 3.6f,
                    Radius = 0.24f,
                    SpiritReward = 12,
                    DamageToBase = 1,
                    AttackDamage = 9f,
                    AttackRangeCells = 1,
                    AttackInterval = 0.8f,
                    Color = new Color(0.96f, 0.77f, 0.26f),
                }
            },
            {
                EnemyType.Tank,
                new EnemyDefinition
                {
                    Type = EnemyType.Tank,
                    DisplayName = "重装",
                    MaxHealth = 200f,
                    Speed = 1.4f,
                    Radius = 0.40f,
                    SpiritReward = 30,
                    DamageToBase = 2,
                    AttackDamage = 30f,
                    AttackRangeCells = 1,
                    AttackInterval = 1.2f,
                    Color = new Color(0.66f, 0.48f, 0.88f),
                }
            },
            {
                EnemyType.Boss,
                new EnemyDefinition
                {
                    Type = EnemyType.Boss,
                    DisplayName = "Extra",
                    MaxHealth = 800f,
                    Speed = 0.9f,
                    Radius = 0.52f,
                    SpiritReward = 100,
                    DamageToBase = 3,
                    AttackDamage = 50f,
                    AttackRangeCells = 1,
                    AttackInterval = 1.5f,
                    Color = new Color(1.00f, 0.35f, 0.35f),
                }
            },
        };

        // 难度来自波次组合；避免血量、速度与攻击同时以二次曲线膨胀。
        public static float StageHealthScale(int stage) => .85f + .10f * Mathf.Clamp(stage, 0, 4);
        public static float StageDamageScale(int stage) => .65f + .075f * Mathf.Clamp(stage, 0, 4);
        public static float StageSpeedScale(int stage) => .75f + .025f * Mathf.Clamp(stage, 0, 4);
        public const float OpeningDelay = 8f;
        public const float RoundIntroDelay = 5f;
        public const float RoundGap = 10f;
        public const int RoundClearSpirit = 80;              // 幕次清场补给
        public const float RoundRepairRatio = .4f;
        public const int AnnihilationMilestoneInterval = 40;
        public const int AnnihilationMilestoneSpirit = 20;   // 每剿灭 N 只敌影额外发一笔灵力
        // 练习模式：从初始灵力起步，越靠后的幕次给越多，方便直接跳幕试阵。
        public static int PracticeStartingSpirit(int stage) => StartingSpirit + Mathf.Clamp(stage, 0, 4) * 180;
        public static int RoundEnemyCount(int stage)
        {
            int count = 0;
            foreach (var entry in Rounds[stage].Entries) count += entry.Count;
            return count;
        }

        // 1x 下每幕约 100–150 秒；波组间有呼吸，后段集中攻势配合音乐展开。
        // 所有时刻均相对幕次横幅结束；倍速是玩家主动选择的加速。
        private static SpawnEntry Group(EnemyType type, int route, float time, int count, float interval)
            => new SpawnEntry { EnemyType = type, RouteIndex = route, StartTime = time, Count = count, SpawnInterval = interval };
        public static readonly RoundDef[] Rounds =
        {
            new RoundDef { Title = "淡水珍珠之泪", Eng = "STAGE 1", Entries = new[] {
                Group(EnemyType.Normal, 0, 0, 4, 3),
                Group(EnemyType.Normal, 1, 18, 4, 3),
                Group(EnemyType.Normal, 2, 36, 4, 3),
                Group(EnemyType.Normal, 0, 54, 5, 2),
                Group(EnemyType.Normal, 1, 72, 5, 2),
                Group(EnemyType.Fast, 2, 90, 4, 2.5f) } },
            new RoundDef { Title = "柳树下的首级", Eng = "STAGE 2", Entries = new[] {
                Group(EnemyType.Normal, 0, 0, 6, 2),
                Group(EnemyType.Fast, 1, 18, 5, 2),
                Group(EnemyType.Normal, 2, 36, 6, 2),
                Group(EnemyType.Tank, 0, 54, 2, 4),
                Group(EnemyType.Fast, 1, 70, 6, 1.8f),
                Group(EnemyType.Normal, 2, 88, 8, 1.8f) } },
            new RoundDef { Title = "十五夜的妖兽", Eng = "STAGE 3", Entries = new[] {
                Group(EnemyType.Fast, 1, 0, 6, 2),
                Group(EnemyType.Normal, 0, 18, 6, 2),
                Group(EnemyType.Tank, 2, 36, 3, 3),
                Group(EnemyType.Fast, 0, 52, 6, 1.8f),
                Group(EnemyType.Fast, 1, 70, 6, 1.8f),
                Group(EnemyType.Tank, 2, 86, 3, 3),
                Group(EnemyType.Fast, 0, 94, 5, 1.8f) } },
            new RoundDef { Title = "风暴中的不协和音", Eng = "STAGE 4", Entries = new[] {
                Group(EnemyType.Tank, 0, 0, 3, 3.5f),
                Group(EnemyType.Normal, 1, 18, 7, 2),
                Group(EnemyType.Fast, 2, 36, 7, 1.8f),
                Group(EnemyType.Tank, 1, 54, 3, 3),
                Group(EnemyType.Normal, 0, 72, 8, 1.8f),
                Group(EnemyType.Tank, 2, 90, 3, 3),
                Group(EnemyType.Fast, 1, 98, 5, 2) } },
            new RoundDef { Title = "万物反转颠倒的世界", Eng = "STAGE 5", Entries = new[] {
                Group(EnemyType.Normal, 0, 0, 8, 2),
                Group(EnemyType.Tank, 1, 20, 3, 3.5f),
                Group(EnemyType.Fast, 2, 38, 8, 1.8f),
                Group(EnemyType.Tank, 0, 56, 3, 3.5f),
                Group(EnemyType.Normal, 1, 74, 8, 1.8f),
                Group(EnemyType.Tank, 2, 92, 3, 3.5f),
                Group(EnemyType.Boss, 1, 112, 1, 1) } },
        };

        public static int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var round in Rounds)
                {
                    foreach (var entry in round.Entries)
                    {
                        total += entry.Count;
                    }
                }
                return total;
            }
        }
    }
}
