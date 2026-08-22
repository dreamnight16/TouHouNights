using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI 视觉主题：方舟式「锐角工业 HUD + 冷描边」+ Phigros 式「评级色」。
    /// 所有矩形使用 CanvasScaler 的 1280×720 参考分辨率逻辑单位。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        public const float TopBarHeight = 54f;
        public const float BottomBarHeight = 88f;
        public const float TowerCardWidth = 170f;
        public const float TowerCardHeight = 64f;
        public const float TowerCardGap = 12f;

        // 战场 / 面板 / 描边（Fluent 亚克力：半透明 + 柔和浅描边）
        public static readonly Color BattleBg = Hex("0B0D12");
        public static readonly Color PanelBg = new Color(0.10f, 0.13f, 0.18f, 0.62f);  // 亚克力半透明
        public static readonly Color PanelLine = new Color(1f, 1f, 1f, 0.16f);          // 柔和浅描边
        public static readonly Color CardBg = new Color(0.16f, 0.20f, 0.27f, 0.78f);
        public static readonly Color CardSelected = new Color(0.15f, 0.42f, 0.45f, 0.88f); // 青调选中
        public static readonly Color CardDisabled = new Color(0.12f, 0.13f, 0.16f, 0.70f);
        public static readonly Color ButtonBg = new Color(0.20f, 0.26f, 0.35f, 0.85f);
        public static readonly Color ButtonHover = new Color(0.27f, 0.34f, 0.45f, 0.92f);

        // 文字 / 强调
        public static readonly Color Ink = Hex("E7EAF0");
        public static readonly Color InkDim = Hex("8A93A6");
        public static readonly Color Accent = Hex("4CD6E0");
        public static readonly Color EnemyRed = Hex("E5484D");
        public static readonly Color Gold = Hex("F0B33C");
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.66f);

        // 进度条
        public static readonly Color PowerBar = Accent;
        public static readonly Color PowerBarBg = new Color(1f, 1f, 1f, 0.12f);

        // Phigros 评级色
        public static readonly Color GradePhi = Hex("F5D76E");
        public static readonly Color GradeV = Hex("4CD6E0");
        public static readonly Color GradeS = Hex("5FE39B");
        public static readonly Color GradeA = Hex("6BA8F0");
        public static readonly Color GradeB = Hex("F0A86B");
        public static readonly Color GradeC = Hex("9AA1AE");

        // 字号
        public const int FontSize = 18;
        public const int FontSizeSmall = 14;
        public const int FontSizeCardTitle = 17;
        public const int FontSizeCardSub = 12;
        public const int FontSizeGrade = 120;
        public const int FontSizeTitle = 30;

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.white;
        }
    }
}
