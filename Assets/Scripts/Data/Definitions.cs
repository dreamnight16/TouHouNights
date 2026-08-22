using System;
using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>防御塔类型。</summary>
    public enum TowerType
    {
        Gun,      // 机枪塔：单体、高射速
        Sniper,   // 狙击塔：单体高伤、长射程
        Missile,  // 导弹塔：追踪子弹 + AOE
        Slow      // 减速塔：范围减速
    }

    /// <summary>敌人类型。</summary>
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank
    }

    /// <summary>子弹飞行行为。</summary>
    public enum ProjectileBehavior
    {
        Straight, // 直线飞行（发射瞬间确定方向）
        Homing    // 追踪（带转向加速度）
    }

    /// <summary>
    /// 智能索敌策略（进阶挑战 ★★★★★，三选一）。
    /// 所有塔共享当前全局策略，可在 HUD 中切换。
    /// </summary>
    public enum TargetingPriority
    {
        Nearest,      // 距离塔最近的敌人
        LowestHealth, // 血量最低的敌人
        ClosestToEnd  // 距离终点最近的敌人
    }

    /// <summary>防御塔的可配置数值。</summary>
    [Serializable]
    public class TowerDefinition
    {
        public TowerType Type;
        public string DisplayName;
        public int Cost;

        public float MaxHealth;         // 塔的生命值（可被敌人击毁）
        public int RangeCells;          // 索敌/攻击范围（格子数，Chebyshev 距离，与格子高亮一致）
        public float FireRate;          // 每秒攻击次数
        public float Damage;            // 单次伤害（减速塔为 0）

        public float ProjectileSpeed;   // 子弹速度
        public float HomingStrength;    // 追踪加速度（0 = 直线）
        public float SplashRadius;      // 命中后的 AOE 半径（0 = 单体）

        public float SlowFactor;        // 减速比例（0~1，越小越慢）
        public float SlowDuration;      // 减速持续时间（秒）

        public Color Color;             // 塔身颜色
        public Color ProjectileColor;   // 子弹颜色
    }

    /// <summary>敌人的可配置数值。</summary>
    [Serializable]
    public class EnemyDefinition
    {
        public EnemyType Type;
        public string DisplayName;

        public float MaxHealth;
        public float Speed;
        public float Radius;            // 碰撞半径（用于手动命中检测）

        public int GoldReward;
        public int DamageToBase;        // 到达终点后对基地造成的伤害

        public float AttackDamage;      // 对防御塔的单次伤害
        public int AttackRangeCells;    // 攻击塔的索敌范围（相邻格 = 1）
        public float AttackInterval;    // 攻击间隔（秒）

        public Color Color;
    }

    /// <summary>剿灭式刷怪条目：从开局第 StartTime 秒起，在指定红门按间隔刷 Count 个敌人。</summary>
    [Serializable]
    public class SpawnEntry
    {
        public EnemyType EnemyType;
        public int RouteIndex;          // 走哪条正交路线（红门）
        public float StartTime;         // 开局后第几秒开始
        public int Count;
        public float SpawnInterval;
    }

    /// <summary>结算数据：游戏结束时展示东方 stage 风格战绩 + Phigros 风格评级。</summary>
    public struct GameResult
    {
        public bool Victory;
        public int Score;
        public int TotalKills;
        public int LeakedEnemies;
        public int LivesRemaining;
        public int Rating;
        public string Grade;
    }
}
