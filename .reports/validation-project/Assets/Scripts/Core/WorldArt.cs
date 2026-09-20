using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// 世界层视觉令牌（与 UI 侧的 <c>TdTheme</c> 平级，但只服务战场内的东西）。
    ///
    /// 存在的理由：地图、塔、敌人以前各自在代码里写死色值和尺寸，结果
    ///   ① 明暗层级塌陷（深蓝叠深蓝，看不清）；
    ///   ② 形状语言不统一（球 / 圆 / 方随意混搭，像几何体堆料）；
    ///   ③ 没有统一的光照与描边规则，缺少"工业设计感"。
    ///
    /// 本层定死三条规则，所有世界单位一律遵守：
    ///   - 价值层级：底(4%) → 棋盘(8%) → 网格(12%) → 路径(18%) → 建筑(30%) → 单位(55%~85%)。
    ///     相邻层级必须拉开明度，不允许"深色上叠深色"。
    ///   - 统一描边：任何单位先铺一层放大 1.14 倍的近黑剪影，再叠本体色，保证不糊在背景里。
    ///   - 统一打光：光来自左上 45°，一律「顶部缩小上移提亮 + 落地压扁投影」。
    ///
    /// 形状语言：六边形 = 平台/建筑，箭形 = 有朝向的机动单位，菱形 = 尖锐/精准职业，
    /// 圆环 = 范围/光环，L 角标 = 取景与锁定。
    /// </summary>
    public static class WorldArt
    {
        // ---- 明度阶梯（括号内为近似灰度）----
        public static readonly Color Abyss = Hex("0D1420");        // 4%  战场之外的虚空
        public static readonly Color BoardPlate = Hex("182434");   // 8%  可部署棋盘底
        public static readonly Color GridMinor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color GridMajor = new Color(1f, 1f, 1f, 0.11f);
        public static readonly Color Frame = Hex("50657C");        // 取景框 / 角标
        public static readonly Color RoadEdge = Hex("496174");     // 路径描边（比路面亮一档）
        public static readonly Color RoadFill = Hex("2A3B4D");     // 18% 路径路面

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
        // 同层并列的两个 SpriteRenderer 之间没有稳定遮挡关系（会随相机距离漂移而抖），
        // 所以按职能分段：新增任何渲染体都必须落在对应区间，禁止随手填一个数字。
        public const int LayerFloor = -10;      // 地面 / 暗角
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
