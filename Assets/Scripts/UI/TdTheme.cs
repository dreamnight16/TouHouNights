using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 设计令牌（本次重写后的唯一色值 / 尺寸来源）。
    ///
    /// 世界观术语不变：结界(=生命) / 灵力(=金币) / 敌影(=敌人) / 符卡(=塔) / P点 / 弹幕 / 时之流速(=倍速)。
    ///
    /// 三条约束（任何新增 UI 都必须遵守）：
    ///   1. 只允许引用令牌，禁止在组件里写死色值和裸数字。
    ///   2. 间距走 8pt 栅格；圆角只取 0 / 4 / 6 / 10 四档。
    ///   3. 界面元素一律用「拉伸锚点 + 局部偏移」定位，不使用相对参考分辨率的绝对 x，
    ///      这样任何窗口比例下都不会被裁切。
    ///
    /// 字体不在这里：<see cref="UiFont"/> 单独负责中文字形的解析与验证。
    /// </summary>
    public static class TdTheme
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        // ---- 三层架构的尺寸 ----
        public const float TopBarH = 64f;      // 战局状态层
        public const float BottomBarH = 112f;  // 快速操作层（比卡片 86 + 悬停上浮 6 + 上下留白更宽裕）
        public const float CardW = 118f;
        public const float CardH = 86f;
        public const float CardGap = 14f;
        /// <summary>底栏内容的竖直中心（距屏幕下缘）。底栏内所有元素都对齐到这条基线。</summary>
        public const float BottomAxisY = 64f;

        // ---- 底色与面板 ----
        public static readonly Color Void = Hex("0B1219");
        public static readonly Color Glass = new Color(0.090f, 0.140f, 0.190f, 0.86f);
        public static readonly Color GlassDeep = new Color(0.060f, 0.095f, 0.135f, 0.92f);
        public static readonly Color GlassLight = new Color(0.120f, 0.170f, 0.225f, 0.92f);
        public static readonly Color Panel = Hex("16212C");
        public static readonly Color PanelEdge = Hex("2B3E52");
        public static readonly Color Rail = new Color(1f, 1f, 1f, 0.10f);   // 贯穿全屏的细导轨线
        public static readonly Color Divider = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Edge = new Color(1f, 1f, 1f, 0.26f);       // 0.14 太淡，面板没有边界感
        public static readonly Color EdgeStrong = new Color(1f, 1f, 1f, 0.50f); // 0.38 在 hover 时仍不够明显

        // ---- 卡片 / 按钮 ----
        public static readonly Color CardBg = new Color(0.100f, 0.150f, 0.205f, 0.92f);
        public static readonly Color CardSel = new Color(0.090f, 0.215f, 0.310f, 0.96f);
        public static readonly Color CardOff = new Color(0.080f, 0.100f, 0.128f, 0.88f);
        public static readonly Color SlotEdge = new Color(1f, 1f, 1f, 0.22f);   // 0.10 太淡，面板边界感不足
        public static readonly Color Chip = new Color(0.070f, 0.110f, 0.155f, 0.90f);
        public static readonly Color BtnBg = new Color(0.100f, 0.145f, 0.195f, 0.97f);
        public static readonly Color BtnActive = new Color(0.085f, 0.225f, 0.325f, 0.98f);

        // ---- 语义主色 ----
        public static readonly Color Star = Hex("27CFF5");      // 方舟青蓝 · 我方 / 可操作 / P点（重构主强调）
        public static readonly Color StarSoft = new Color(0.31f, 0.64f, 0.89f, 0.45f);
        public static readonly Color Spirit = Hex("F5C542");    // 星尘金 · 灵力 / 费用
        public static readonly Color Barrier = Hex("FF6A5A");   // 神樱红 · 结界 / 危险
        public static readonly Color Glass2 = Hex("6FC3E8");    // 冰蓝 · 辅助信息

        // ---- 方舟风格重构新增令牌（炭灰分层 + 青蓝强调体系）----
        public static readonly Color Accent = Hex("27CFF5");        // 全局强调（= Star）
        public static readonly Color AccentBright = Hex("5FE0FF");  // 强调高亮
        public static readonly Color AccentDeep = Hex("15799B");    // 强调暗部
        public static readonly Color Surface0 = new Color(0.055f, 0.070f, 0.094f, 0.97f); // 最深底（带背板）
        public static readonly Color Surface1 = new Color(0.078f, 0.098f, 0.129f, 0.97f); // 带底（炭灰玻璃）
        public static readonly Color Surface2 = new Color(0.110f, 0.137f, 0.180f, 0.98f); // 抬起面板
        public static readonly Color Surface3 = new Color(0.140f, 0.176f, 0.227f, 0.98f); // 卡面（最亮层）
        public static readonly Color Hairline = new Color(1f, 1f, 1f, 0.16f);   // 发丝描边
        public static readonly Color HairlineStrong = new Color(1f, 1f, 1f, 0.30f); // 强描边 / hover
        public static readonly Color Sheen = new Color(1f, 1f, 1f, 0.22f);      // 顶部内部高光
        public static readonly Color GlowCyan = new Color(0.153f, 0.804f, 0.961f, 0.45f);
        public static readonly Color GlowGold = new Color(1f, 0.824f, 0.290f, 0.42f);
        public static readonly Color GlowDanger = new Color(1f, 0.353f, 0.302f, 0.40f);

        // ---- 文字 ----
        public static readonly Color Ink = Hex("E8F2FA");
        public static readonly Color InkDim = Hex("93A6B8");
        public static readonly Color InkFaint = Hex("6B7E91");
        public static readonly Color OnStar = new Color(0.03f, 0.08f, 0.13f, 1f);

        // ---- 世界飘字 ----
        public static readonly Color DamageText = new Color(0.97f, 0.95f, 0.88f, 1f);
        public static readonly Color DamageSplash = Hex("FF8A4A");
        public static readonly Color GoldText = Hex("F5C542");
        public static readonly Color LifeLossText = Hex("FF6A5A");


        // ---- 评级 ----
        public static readonly Color GradePhi = Hex("F5D76E");
        public static readonly Color GradeV = Hex("57B8E8");
        public static readonly Color GradeS = Hex("8FCBE8");
        public static readonly Color GradeA = Hex("6BA8F0");
        public static readonly Color GradeB = Hex("F0A86B");
        public static readonly Color GradeC = Hex("9AA1AE");

        // ---- 字号 ----
        public const int Tiny = 12;
        public const int Small = 14;
        public const int Body = 16;
        public const int Big = 22;
        public const int Huge = 30;
        public const int GradeSize = 160;

        /// <summary>语义色：白=信息、灰=不可用、蓝=可操作、黄=注意、红=危险、绿=成功。</summary>
        public static class Sem
        {
            public static readonly Color Info = Hex("E8F2FA");
            public static readonly Color Disabled = Hex("6B7E91");
            public static readonly Color Action = Hex("4FA3E3");
            public static readonly Color Caution = Hex("F5A623");
            public static readonly Color Danger = Hex("FF6A5A");
            public static readonly Color Success = Hex("5FD3A6");
            public static readonly Color Neutral = Hex("6FC3E8");
        }

        /// <summary>间距（8pt 栅格）。</summary>
        public static class Space
        {
            public const float Xs = 4f;
            public const float Sm = 8f;
            public const float Md = 12f;
            public const float Lg = 16f;
            public const float Xl = 24f;
            public const float Xxl = 32f;
        }

        /// <summary>动效时长（秒）。</summary>
        public static class Motion
        {
            public const float Fast = 0.15f;
            public const float Base = 0.22f;
            public const float Slow = 0.40f;
            public const float Hold = 1.15f;
        }

        /// <summary>圆角：0 / 4 / 6 / 10 四档。</summary>
        public static class Radius
        {
            public const float None = 0f;
            public const float Sm = 4f;
            public const float Card = 6f;
            public const float Panel = 10f;
        }

        public const float GridUnit = 8f;

        // 投影令牌（给面板/按钮增加深度层次，解决「贴在一起没有立体度」的粗糙感）
        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.20f);
        public static readonly Vector2 ShadowDistance = new Vector2(0f, -3f);

        /// <summary>战场安全区（避开上下两条带）。</summary>
        public const float SafeTop = TopBarH + Space.Sm;
        public const float SafeBottom = BottomBarH + Space.Sm;

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.white;
        }
    }
}
