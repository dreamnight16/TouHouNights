using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>结界作战档案 UI 的独立视觉令牌。此主题不依赖旧 HUD 的青蓝玻璃样式。</summary>
    public static class BattleUiTheme
    {
        public static readonly Color Ink = Hex("0D1420");
        public static readonly Color Surface = Hex("243143");
        public static readonly Color SurfaceDeep = Hex("151F2D");
        public static readonly Color Paper = Hex("E6ECEB");
        public static readonly Color Text = Hex("E6ECEB");
        public static readonly Color Muted = Hex("A0ACB0");
        public static readonly Color Action = Hex("F18D76");
        public static readonly Color Ready = Hex("88D8CD");
        public static readonly Color Danger = Hex("E56B64");
        public static readonly Color Disabled = Hex("70767B");
        public static readonly Color Grid = WithAlpha(Text, 0.12f);
        public static readonly Color Edge = WithAlpha(Text, 0.22f);
        public static readonly Color Shadow = WithAlpha(Ink, 0.52f);

        public const float Safe = 32f;
        public const float Hairline = 2f;
        public const float Corner = 7f;

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out var color) ? color : Color.white;
        }
    }
}
