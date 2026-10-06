using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>战场共用的配色、明暗处理和渲染层序。</summary>
    public static class WorldArt
    {
        // ---- 背景与道路 ----
        public static readonly Color Abyss = Hex("0D1420");        // 战场背景
        public static readonly Color BoardPlate = Hex("182434");   // 可部署棋盘底
        public static readonly Color GridMinor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color GridMajor = new Color(1f, 1f, 1f, 0.11f);
        public static readonly Color Frame = Hex("50657C");        // 取景框 / 角标
        public static readonly Color RoadEdge = Hex("8A7A55");     // 路径描边
        public static readonly Color RoadFill = Hex("3C4C60");     // 路面填充

        // 道路上沿提亮、下沿加深。
        public static readonly Color RoadEdgeLit = new Color(.92f, .82f, .58f, .60f);  // 上沿（朝光）
        public static readonly Color RoadEdgeDark = new Color(0f, 0f, 0f, .45f);       // 下沿（背光）
        public static readonly Color RoadLift = new Color(1f, 1f, 1f, .10f);           // 每格顶面的受光带

        // ---- 夜色天幕 ----
        public static readonly Color SkyHigh = Hex("05070E");      // 天顶
        public static readonly Color SkyMid = Hex("0B1526");       // 中天
        public static readonly Color SkyLow = Hex("26333F");       // 地平（暖灰蓝）
        public static readonly Color Haze = Hex("5A6C7E");         // 地平线雾气
        public static readonly Color Sakura = Hex("FFC4D6");       // 樱瓣
        public static readonly Color GoldDust = Hex("E3C489");     // 金尘

        // ---- 漆器与金线 ----
        public static readonly Color Lacquer = Hex("1A2634");      // 漆板底（明显亮于天幕）
        public static readonly Color LacquerLift = Hex("24313F");  // 漆板受光面
        public static readonly Color GoldLine = new Color(.78f, .66f, .42f, .20f);
        public static readonly Color GoldEdge = new Color(.90f, .78f, .52f, .42f);

        /// <summary>每 4 格一道的主格线，比次格线亮且偏冷青。</summary>
        public static readonly Color GridSurvey = new Color(.36f, .78f, .91f, .17f);

        /// <summary>棋盘上沿向下淡出的辉光。</summary>
        public static readonly Color PlateCrest = new Color(.90f, .78f, .52f, .30f);
        public static readonly Color Lantern = Hex("FFB45A");      // 灯笼暖光
        public static readonly Color LanternDeep = Hex("4A2A12");  // 灯笼暗部

        // ---- 阵营 ----
        public static readonly Color Ally = Hex("4FA3E3");         // 我方主色（蓝）
        public static readonly Color AllyDeep = Hex("12283C");     // 我方暗部
        public static readonly Color AllyBright = Hex("9FD4FF");   // 我方高光
        public static readonly Color AllyGlow = new Color(0.31f, 0.64f, 0.89f, 0.22f);

        public static readonly Color Foe = Hex("FF6B7A");          // 敌方主色（红）
        public static readonly Color FoeDeep = Hex("3A141C");      // 敌方暗部
        public static readonly Color FoeBright = Hex("FFC2C8");    // 敌方高光
        public static readonly Color FoeGlow = new Color(1.00f, 0.42f, 0.48f, 0.24f);

        // ---- 光照 / 描边 ----
        public static readonly Color Ink = Hex("05080C");          // 统一描边（近黑）
        public static readonly Color RimLight = new Color(1f, 1f, 1f, 0.30f);   // 顶部受光
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.32f);     // 落地投影

        // ---- 渲染层序分区 ----
        // 按用途分配排序区间，避免遮挡顺序依赖相机距离。
        public const int LayerSky = -14;        // 天幕渐变
        public const int LayerStars = -13;      // 星野
        public const int LayerHaze = -12;       // 地平线雾
        public const int LayerFloor = -10;      // 地面 / 暗角
        public const int LayerPetals = 9;       // 飘落樱瓣：压在棋盘之上、所有可交互单位之下
        public const int LayerMap = 0;          // 棋盘 0 / 网格 1 / 路径 2~4 / 基地与出怪门 5~8 / 防御塔 3~9
        public const int LayerRange = 10;       // 攻击范围：整体蒙版（压在塔之上、敌人之下）
        public const int LayerRangeLine = 11;   // 攻击范围：边界线 / 四角角标 / 内部格线
        public const int LayerEnemyGlow = 12;   // 敌人：落地辉光（贴地，不随朝向旋转）
        public const int LayerEnemyShadow = 13; // 敌人：落地投影
        public const int LayerEnemyEdge = 14;   // 敌人：剪影描边 / Boss 威胁环
        public const int LayerEnemyBody = 15;   // 敌人：本体
        public const int LayerEnemyCore = 16;   // 敌人：能量芯
        public const int LayerEnemyTop = 17;    // 敌人：顶部受光
        public const int LayerShotGlow = 18;    // 弹丸：拖尾辉光
        public const int LayerShot = 19;        // 弹丸：本体（永远压在敌人之上，弹道不能被挡）
        public const int LayerBar = 20;         // 血条（HealthBarView 内部再 +0/+1/+2）
        public const int LayerBurst = 30;       // 爆发特效

        /// <summary>向黑色混合（取暗部）。</summary>
        public static Color Shade(Color c, float amount)
        {
            return Color.Lerp(c, Color.black, Mathf.Clamp01(amount));
        }

        /// <summary>向白色混合（取亮部 / 高光）。</summary>
        public static Color Tint(Color c, float amount)
        {
            return Color.Lerp(c, Color.white, Mathf.Clamp01(amount));
        }

        /// <summary>保留 RGB，只改 alpha。</summary>
        public static Color Alpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.white;
        }
    }
}
