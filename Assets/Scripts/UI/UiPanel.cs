using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>面板四角的处理方式。</summary>
    public enum UiCorner
    {
        /// <summary>锐利直角。默认值 —— 这套视觉的地基。</summary>
        Sharp = 0,

        /// <summary>四角一致斜切：签名式切口。</summary>
        Chamfer = 1,

        /// <summary>只有左上 / 右下斜切，另外两角保持直角。不对称，最像「做过设计」的那一种。</summary>
        Diagonal = 2,
    }

    /// <summary>
    /// 一块面板，一个 Graphic。
    ///
    /// 存在的理由：上一版每块面板都是 <c>Image</c> + <c>Shadow</c> + <c>Outline</c> 三个组件。
    /// 那是三层**硬偏移** —— 暗影其实是复制一份本体再位移，转角和斜边处会露出原形状的边；
    /// 而且颜色只能靠 <c>Image.color</c> 一处平涂，做不出「上亮下暗」的体积。
    /// 更贵的是布局开销：三个组件各自持有一份网格，任何尺寸变化都要重建三次。
    ///
    /// 本组件在 <see cref="OnPopulateMesh"/> 里一次性烘出五层，合并进同一个网格：
    ///   ① 外辉光（向外淡出） ② 描边环 ③ 渐变本体 ④ 顶部受光 ⑤ 底部压暗
    ///
    /// 几何上只有一件事需要理解：**轮廓生成器同时接受「内缩量」和「角生长量」**。
    /// 圆角半径按生长量线性增加、斜切切口按 0.586·生长量 增加、直角保持直角 ——
    /// 这恰好是「多边形 ⊕ 半径 g 的圆盘」的闵可夫斯基和，所以辉光/描边环的每一条边
    /// 都严格等距于本体轮廓，不会出现粗细不均的豁口。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiPanel : MaskableGraphic
    {
        /// <summary>45° 斜切在向外等距偏移时，切口长度相对偏移量的增长系数（2-√2）。</summary>
        private const float ChamferGrow = 0.586f;

        [SerializeField] private UiCorner _corner = UiCorner.Sharp;
        [SerializeField] private float _cornerSize;
        [SerializeField] private bool _rounded;

        [SerializeField] private Color _bodyTop = BattleUiTheme.Char;
        [SerializeField] private Color _bodyBottom = BattleUiTheme.Ink;

        [SerializeField] private Color _borderColor = BattleUiTheme.Edge;
        [SerializeField] private float _borderWidth = BattleUiTheme.Border;

        [SerializeField] private Color _rimColor;
        [SerializeField] private float _rimHeight = 28f;

        [SerializeField] private Color _footColor;
        [SerializeField] private float _footHeight = 10f;

        [SerializeField] private Color _glowColor;
        [SerializeField] private float _glowWidth;

        // OnPopulateMesh 不会被重入（同一帧内 Unity 不会并行重建网格），静态缓冲安全且免去每帧 GC。
        private static readonly List<Vector2> RingOuter = new List<Vector2>(96);
        private static readonly List<Vector2> RingInner = new List<Vector2>(96);
        private static readonly List<Vector2> BodyPoints = new List<Vector2>(96);

        // ---- 读取：让 UiMotion 能复制同样的几何做一层悬停高亮 ----

        public UiCorner CornerMode => _corner;
        public float CornerSize => _cornerSize;
        public bool IsRounded => _rounded;

        // ---- 装配 API ----

        /// <summary>四角形状。size 同时是圆角半径与斜切边长（<see cref="UiCorner.Sharp"/> 下被忽略）。</summary>
        public UiPanel Corners(UiCorner corner, float size, bool rounded = false)
        {
            _corner = corner;
            _cornerSize = size;
            _rounded = rounded;
            SetVerticesDirty();
            return this;
        }

        /// <summary>本体垂直渐变。上亮下暗才有体积，平涂一定像色块。</summary>
        public UiPanel Body(Color top, Color bottom)
        {
            _bodyTop = top;
            _bodyBottom = bottom;
            SetVerticesDirty();
            return this;
        }

        /// <summary>单色本体（仍是平涂，仅在确实不需要体积时使用，例如纯色遮罩）。</summary>
        public UiPanel Flat(Color color)
        {
            return Body(color, color);
        }

        /// <summary>描边环（等亮度平涂）。width 传 0 可关闭。</summary>
        public UiPanel Border(Color color, float width)
        {
            _borderColor = color;
            _borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
            return this;
        }

        /// <summary>沿顶部内缘的一条向下淡出的受光带。这是「金属/玻璃」感的主要来源。</summary>
        public UiPanel Rim(Color color, float height = 28f)
        {
            _rimColor = color;
            _rimHeight = Mathf.Max(0f, height);
            SetVerticesDirty();
            return this;
        }

        /// <summary>沿底部内缘的一条向上淡出的压暗带。和 <see cref="Rim"/> 配对，面板才有「被加工过」的立体边。</summary>
        public UiPanel Foot(Color color, float height = 10f)
        {
            _footColor = color;
            _footHeight = Mathf.Max(0f, height);
            SetVerticesDirty();
            return this;
        }

        /// <summary>向外淡出的辉光。绯色辉光是「就绪 / 正在发生」的统一语言。</summary>
        public UiPanel Glow(Color color, float width)
        {
            _glowColor = color;
            _glowWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
            return this;
        }

        // ---- 网格 ----

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width < 1f || rect.height < 1f) return;

            float limit = Mathf.Min(rect.width, rect.height) * .5f;
            float corner = Mathf.Clamp(_cornerSize, 0f, limit);
            float border = Mathf.Clamp(_borderWidth, 0f, Mathf.Max(0f, limit - corner));
            float glow = Mathf.Clamp(_glowWidth, 0f, 96f);

            // 分段数在三条环之间共享 —— 点数不一致就没法对着连线。
            int steps = _rounded ? Mathf.Clamp(Mathf.RoundToInt((corner + glow) * .6f) + 3, 4, 20) : 1;

            // ① 外辉光：外缘完全透明，内缘留 55% —— 形成一条贴边的亮边而不是一团糊光。
            if (glow > .01f && _glowColor.a > 0f)
            {
                Outline(rect, -glow, corner, _rounded, _corner, glow, steps, RingOuter);
                Outline(rect, 0f, corner, _rounded, _corner, 0f, steps, RingInner);
                Ring(vh, RingOuter, WithAlpha(_glowColor, 0f), RingInner, WithAlpha(_glowColor, _glowColor.a * .55f));
            }

            if (border > .01f && _borderColor.a > 0f)
            {
                Outline(rect, 0f, corner, _rounded, _corner, 0f, steps, RingOuter);
                Outline(rect, border, corner, _rounded, _corner, -border, steps, RingInner);
                Ring(vh, RingOuter, _borderColor, RingInner, _borderColor);
            }

            Outline(rect, border, corner, _rounded, _corner, -border, steps, BodyPoints);
            Fan(vh, BodyPoints, _bodyTop, _bodyBottom, rect.yMax, rect.yMin);

            // ④ 顶部受光 + 底部压暗：一对，缺一个面板就会显得「贴」在背景上而不是「立」在上面。
            EdgeBand(vh, rect, corner, border, _rimColor, _rimHeight, true);
            EdgeBand(vh, rect, corner, border, _footColor, _footHeight, false);
        }

        /// <summary>
        /// 生成一圈闭合轮廓（绝对坐标，绕矩形中心逆时针）。
        /// inset 为正表示内缩；cornerGrowth 为正表示角沿外法线生长（辉光），为负表示收缩（描边内侧）。
        /// 直角在两个方向上都保持直角，所以辉光/描边的直角边永远是干净的直线。
        /// </summary>
        private static void Outline(Rect r, float inset, float corner, bool rounded, UiCorner mode,
            float cornerGrowth, int steps, List<Vector2> points)
        {
            points.Clear();

            float hw = Mathf.Max(.01f, r.width * .5f - inset);
            float hh = Mathf.Max(.01f, r.height * .5f - inset);
            float c = Mathf.Clamp(corner, 0f, Mathf.Min(hw, hh));
            Vector2 center = r.center;

            // 四个角连成一个从 180° 走到 540° 的连续扇区：第 i 个角覆盖 [180° + i·90°, 180° + (i+1)·90°]。
            // 顶点顺序依次是 左下 → 右下 → 右上 → 左上，正是逆时针。
            for (int i = 0; i < 4; i++)
            {
                float sx = (i == 1 || i == 2) ? 1f : -1f;
                float sy = i >= 2 ? 1f : -1f;
                var sharpVertex = new Vector2(sx * hw, sy * hh);

                if (c <= .01f || (mode == UiCorner.Diagonal && i != 1 && i != 3))
                {
                    points.Add(center + sharpVertex);
                    continue;
                }

                float cut = c + cornerGrowth * (rounded ? 1f : ChamferGrow);
                cut = Mathf.Clamp(cut, 0f, Mathf.Min(hw, hh));
                if (cut <= .01f)
                {
                    points.Add(center + sharpVertex);
                    continue;
                }

                // 圆弧中心到角顶点恒为 cut，所以偏移前后圆心不动，半径/切口各自按生长量变化。
                var arcCenter = new Vector2(sx * (hw - cut), sy * (hh - cut));
                for (int s = 0; s <= steps; s++)
                {
                    float angle = Mathf.PI * (1f + i * .5f + (float)s / steps * .5f);
                    points.Add(center + arcCenter + new Vector2(Mathf.Cos(angle) * cut, Mathf.Sin(angle) * cut));
                }
            }
        }

        /// <summary>在两条点数相同的轮廓之间铺一圈四边形。UI 着色器不做背面剔除，所以绕序无关。</summary>
        private static void Ring(VertexHelper vh, List<Vector2> outer, Color outerColor,
            List<Vector2> inner, Color innerColor)
        {
            int n = outer.Count;
            if (n < 3 || inner.Count != n) return;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int b = vh.currentVertCount;
                vh.AddVert(outer[i], outerColor, Vector2.zero);
                vh.AddVert(inner[i], innerColor, Vector2.zero);
                vh.AddVert(inner[j], innerColor, Vector2.zero);
                vh.AddVert(outer[j], outerColor, Vector2.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b, b + 2, b + 3);
            }
        }

        /// <summary>凸多边形扇形填充，逐顶点按 y 做垂直渐变。</summary>
        private static void Fan(VertexHelper vh, List<Vector2> points, Color top, Color bottom, float yMax, float yMin)
        {
            int n = points.Count;
            if (n < 3) return;

            var centroid = Vector2.zero;
            for (int i = 0; i < n; i++) centroid += points[i];
            centroid /= n;

            float span = Mathf.Max(.0001f, yMax - yMin);
            int b = vh.currentVertCount;
            vh.AddVert(centroid, Gradient(centroid.y, top, bottom, yMax, span), Vector2.zero);
            for (int i = 0; i < n; i++)
            {
                vh.AddVert(points[i], Gradient(points[i].y, top, bottom, yMax, span), Vector2.zero);
            }
            for (int i = 0; i < n; i++)
            {
                vh.AddTriangle(b, b + 1 + i, b + 1 + (i + 1) % n);
            }
        }

        private static Color Gradient(float y, Color top, Color bottom, float yMax, float span)
        {
            return Color.Lerp(bottom, top, Mathf.Clamp01((y - (yMax - span)) / span));
        }

        /// <summary>
        /// 内缘的一条渐变窄带：<paramref name="fromTop"/> 为真时从上边缘向下淡出（顶部受光），
        /// 为假时从下边缘向上淡出（底部压暗）。水平方向按角尺寸收进，避免在斜切角外侧露头。
        /// </summary>
        private static void EdgeBand(VertexHelper vh, Rect rect, float corner, float border,
            Color color, float requestedHeight, bool fromTop)
        {
            if (color.a <= 0f || requestedHeight <= 0f) return;

            // 受光带按**比例**封顶，不按绝对像素。20px 的受光铺在 136px 的卡片上是「一条边」，
            // 铺在 48px 的按钮上就是整块按钮的 42% —— 同一份颜色于是从「光」变成了「粉色渐变的
            // 上半截」，按钮看起来像两截拼的。封到 22% 之后，大面板的受光几乎不变，
            // 而小按钮自动退回成一条边。调用点不必逐个去调数值。
            float height = Mathf.Min(requestedHeight, rect.height * .22f);
            float insetX = Mathf.Max(corner, 6f) + border;
            float x0 = rect.xMin + insetX;
            float x1 = rect.xMax - insetX;
            if (x1 - x0 < 2f || height < .5f) return;

            float inner = fromTop ? rect.yMax - border : rect.yMin + border;
            float outer = fromTop ? inner - height : inner + height;
            Color faded = WithAlpha(color, 0f);

            int b = vh.currentVertCount;
            vh.AddVert(new Vector2(x0, inner), color, Vector2.zero);
            vh.AddVert(new Vector2(x1, inner), color, Vector2.zero);
            vh.AddVert(new Vector2(x1, outer), faded, Vector2.zero);
            vh.AddVert(new Vector2(x0, outer), faded, Vector2.zero);
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b, b + 2, b + 3);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
