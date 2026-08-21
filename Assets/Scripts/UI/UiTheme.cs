using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI 视觉主题：集中配色、字号、布局尺寸，保证整体观感统一。
    /// 所有矩形使用 CanvasScaler 的 1280×720 参考分辨率逻辑单位。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        public const float TopBarHeight = 56f;
        public const float BottomBarHeight = 124f;

        public const float TowerCardWidth = 170f;
        public const float TowerCardHeight = 96f;
        public const float TowerCardGap = 12f;

        // 配色
        public static readonly Color PanelBg = new Color(0.09f, 0.10f, 0.14f, 0.95f);
        public static readonly Color CardBg = new Color(0.20f, 0.26f, 0.35f, 1f);
        public static readonly Color CardSelected = new Color(0.20f, 0.62f, 0.35f, 1f);
        public static readonly Color CardDisabled = new Color(0.13f, 0.15f, 0.18f, 1f);
        public static readonly Color ButtonBg = new Color(0.22f, 0.28f, 0.38f, 1f);
        public static readonly Color ButtonHover = new Color(0.28f, 0.36f, 0.48f, 1f);

        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextDim = new Color(0.72f, 0.76f, 0.84f, 1f);
        public static readonly Color Accent = new Color(0.55f, 0.85f, 1f, 1f);

        public static readonly Color PowerBar = new Color(0.55f, 0.85f, 1f, 1f);
        public static readonly Color PowerBarBg = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color LivesBar = new Color(0.30f, 1f, 0.35f, 1f);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.62f);

        // 字号
        public const int FontSize = 20;
        public const int FontSizeSmall = 15;
        public const int FontSizeCardTitle = 18;
        public const int FontSizeCardSub = 13;
        public const int FontSizeGrade = 96;
        public const int FontSizeTitle = 30;
    }
}
