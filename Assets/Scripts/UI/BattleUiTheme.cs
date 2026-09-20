using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// 界面视觉令牌 —— 「华贵符卡 · 高对比战术」。
    ///
    /// 本文件是 UI 覆盖层唯一的色值与尺度来源；战场内的颜色归 <c>WorldArt</c>。
    /// 改任何值之前先读这三条铁律：
    ///
    ///   1) 近乎单色。<see cref="Ink"/> → <see cref="Char"/> → <see cref="Slate"/> → <see cref="Stone"/>
    ///      四级灰阶承担全部结构，相邻两级明度必须拉开，禁止「深色叠深色」糊成一片。
    ///   2) 一点绯红。<see cref="Scarlet"/> 是全场唯一的高热色，只表达
    ///      「危险 / 封印 / 就绪 / 正在发生」。任何装饰性用色都不许碰它 ——
    ///      它一亮，玩家就知道该看哪里。
    ///   3) 骨白是结构色，不只是文字色。分隔线、描边、数字优先用它；
    ///      正文用 <see cref="Bone"/>，次级说明降到 <see cref="Ash"/>。
    ///
    /// 排版靠「超大数字 vs 极小米字」的尺度对比 + 大量留白取胜，不靠纹样堆叠。
    /// 字号取 <see cref="Type"/>，间距取 <see cref="Gap"/>，时长取 <see cref="Motion"/> ——
    /// 不要在调用点写魔法数字。
    /// </summary>
    public static class BattleUiTheme
    {
        // ---- 灰阶阶梯（结构）----

        /// <summary>虚空 / 屏幕最底。也是截图验证工具的底色，改名会打断 Editor 侧引用。</summary>
        public static readonly Color Ink = Hex("08090C");

        /// <summary>炭 —— 主面板底（比 Ink 亮一档，保证面板能从底色里浮出来）。</summary>
        public static readonly Color Char = Hex("12141A");

        /// <summary>石墨 —— 抬升面板 / 悬停态。</summary>
        public static readonly Color Slate = Hex("1C1F27");

        /// <summary>板岩 —— 卡片正面 / 按下态。</summary>
        public static readonly Color Stone = Hex("2A2E39");

        // ---- 强调色 ----

        /// <summary>绯 —— 全场唯一高热色。危险、封印、就绪、正在发生，全归它。</summary>
        public static readonly Color Scarlet = Hex("FF3B4E");

        /// <summary>冷青 —— 情报与数据（敌方情报、索敌模式、速度）。安静，不与绯争夺注意力。</summary>
        public static readonly Color Cyan = Hex("5BC8E8");

        /// <summary>灰金 —— 灵力与稀有物。全场出现面积必须极小，否则「华贵」立刻变「俗气」。</summary>
        public static readonly Color Gold = Hex("8A7A55");

        // ---- 文字 ----

        /// <summary>骨白 —— 数字、主文字、结构线。故意不用纯白：暗底上的纯白会产生光晕，久看刺眼。</summary>
        public static readonly Color Bone = Hex("EDEFF2");

        /// <summary>灰 —— 次级说明 / 标签 / 单位后缀。</summary>
        public static readonly Color Ash = Hex("8C93A0");

        // ---- 派生 ----

        /// <summary>极细分隔线：只在需要暗示分组时出现，不构成视觉噪声。</summary>
        public static readonly Color Grid = Alpha(Bone, .07f);

        /// <summary>普通描边：面板与卡片的边界。</summary>
        public static readonly Color Edge = Alpha(Bone, .16f);

        /// <summary>强调描边：当前选中 / 需要关注的那一个。</summary>
        public static readonly Color EdgeStrong = Alpha(Bone, .34f);

        /// <summary>投影。</summary>
        public static readonly Color Shadow = Alpha(Ink, .58f);

        /// <summary>失效 / 不可用。用「退到暗处」表达禁用，而不是换个颜色。</summary>
        public static readonly Color Disabled = Alpha(Ash, .45f);

        /// <summary>绯色外辉光。</summary>
        public static readonly Color GlowScarlet = Alpha(Scarlet, .30f);

        /// <summary>青色外辉光。</summary>
        public static readonly Color GlowCyan = Alpha(Cyan, .26f);

        /// <summary>全屏遮罩（暂停 / 结算）。接近不透明，但仍能透出一点战场。</summary>
        public static readonly Color Veil = Alpha(Ink, .90f);

        // ---- 兼容别名 ----
        // 语义等价于上面的新名字，供尚未迁移到新命名法的调用点使用。
        // 新代码请直接用新名字。

        public static Color SurfaceDeep => Char;
        public static Color Surface => Slate;
        public static Color Paper => Bone;
        public static Color Text => Bone;
        public static Color Muted => Ash;
        public static Color Action => Scarlet;
        public static Color Ready => Scarlet;
        public static Color Danger => Scarlet;

        // ---- 几何常量 ----

        /// <summary>屏幕安全边距。所有贴边元素从这里开始。</summary>
        public const float Safe = 32f;

        /// <summary>发丝线宽。1 个单位在 960 宽参考分辨率下约 1.3 物理像素，是「极细」的下限。</summary>
        public const float Hairline = 1f;

        /// <summary>面板描边宽度。比发丝线略重，用来把面板从底色里切出来。</summary>
        public const float Border = 1f;

        // 角标长度与斜切尺寸两个常量已删。它们的设计用途 —— L 形取景标记、卡片的对角切口 ——
        // 都在这一版里被移除了；常量留着只会给「再切一个角」留一扇方便之门。

        // ---- 尺度 / 间距 / 时长 ----

        /// <summary>字号阶梯。跨度刻意拉大 —— 96 与 10 同屏，是这套排版的身份。</summary>
        public static class Type
        {
            public const int Display = 96;   // 结算评级 / 得分数字
            public const int Hero = 62;      // 标题
            public const int Big = 40;       // 页面主数值
            public const int Title = 27;     // 面板标题
            public const int Heading = 20;   // 卡片名 / 区块名
            public const int Body = 16;      // 正文
            public const int Small = 13;     // 次级说明
            public const int Micro = 10;     // 极小米字：单位后缀 / 编号 / 罗马音
        }

        /// <summary>间距阶梯（8pt 栅格）。不要出现 12 / 20 / 28 这种不在栅格上的值。</summary>
        public static class Gap
        {
            public const float Xs = 4f;
            public const float Sm = 8f;
            public const float Md = 16f;
            public const float Lg = 24f;
            public const float Xl = 32f;
            public const float Xxl = 48f;
        }

        /// <summary>动效时长。快用于反馈，中用于状态切换，慢用于入场。</summary>
        public static class Motion
        {
            public const float Fast = .12f;
            public const float Normal = .22f;
            public const float Slow = .42f;
        }

        // ---- 工具 ----

        /// <summary>保留 RGB，只改 alpha。</summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>向 <see cref="Ink"/> 混合，得到更暗的一档（渐变底部 / 压暗）。</summary>
        public static Color Deepen(Color color, float amount)
        {
            return Color.Lerp(color, Ink, Mathf.Clamp01(amount));
        }

        /// <summary>向 <see cref="Bone"/> 混合，得到更亮的一档（渐变顶部 / 顶部受光）。</summary>
        public static Color Lift(Color color, float amount)
        {
            return Color.Lerp(color, Bone, Mathf.Clamp01(amount));
        }

        private static Color Alpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out var color) ? color : Color.white;
        }
    }
}
