using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// 矢量圆角面板 v2（继承 Image，自绘 mesh）：
    /// - 圆角/描边/进度全部由顶点几何构建（纯矢量，任何缩放物理级锐利）
    /// - 屏幕级抗锯齿：外缘一圈按 canvas.scaleFactor 计算的 1 物理像素羽化带
    ///   （外扩顶点 alpha=0 到边缘 alpha=1 线性过渡），任何分辨率下无边缘锯齿
    /// - 弧线段数 24/角（视觉正圆弧，无多边形感）
    /// - 进度条截断端同样自动抗锯齿
    /// 材质使用 UI_Acrylic：顶光/底影/磨砂/毛玻璃由 shader 提供。
    /// </summary>
    public sealed class RoundedRectImage : Image
    {
        private const int SegmentsPerCorner = 24;

        /// <summary>
        /// 直角主题全局开关：为真时无视各面板/按钮传入的 cornerRadius，一律渲染为锐利直角矩形。
        /// 单一来源控制「直角边 + 玻璃」的整体语言（呼应主题：方舟式终端，统一直角）。
        /// </summary>
        public static bool ForceSquareEdges = true;

        [SerializeField] private float _cornerRadius = 10f;
        [SerializeField] private float _borderWidth = 1f;
        [SerializeField] private Color _borderColor = new Color(1f, 1f, 1f, 0.14f);
        [SerializeField] private float _progress = 1f;
        private float _lastScale = -1f;

        public float cornerRadius
        {
            get { return _cornerRadius; }
            set { if (Mathf.Abs(_cornerRadius - value) > 0.01f) { _cornerRadius = Mathf.Max(0f, value); SetVerticesDirty(); } }
        }

        public float borderWidth
        {
            get { return _borderWidth; }
            set { if (Mathf.Abs(_borderWidth - value) > 0.01f) { _borderWidth = Mathf.Max(0f, value); SetVerticesDirty(); } }
        }

        public Color borderColor
        {
            get { return _borderColor; }
            set { if (_borderColor != value) { _borderColor = value; SetVerticesDirty(); } }
        }

        /// <summary>进度 0~1（矢量填充量，端部直角+抗锯齿）。</summary>
        public float Progress
        {
            get { return _progress; }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Abs(_progress - value) < 0.0005f) return;
                _progress = value;
                SetVerticesDirty();
            }
        }

        private void Update()
        {
            // 画布缩放（分辨率/缩放器）变化时重建网格，保证羽化带始终 = 1 物理像素
            var c = canvas;
            if (c == null) return;
            if (Mathf.Abs(_lastScale - c.scaleFactor) > 0.001f)
            {
                _lastScale = c.scaleFactor;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            if (w < 2f || h < 2f) return;

            float r = Mathf.Clamp(_cornerRadius, 0f, Mathf.Min(w, h) * 0.5f);
            if (ForceSquareEdges) r = 0f;
            float aa = ComputeAaWidth();
            float f = Mathf.Clamp01(_progress);

            var outline = BuildOutline(w, h, r, f);
            if (outline.Count < 4)
            {
                return;
            }

            int n = outline.Count;
            var normal = new Vector2[n];
            var outer = new Vector2[n];
            var outerAa = new Vector2[n];
            var inner = new Vector2[n];
            var center = ComputeCentroid(outline);

            for (int i = 0; i < n; i++)
            {
                var prev = outline[(i - 1 + n) % n];
                var next = outline[(i + 1) % n];
                var tangent = next - prev;
                if (tangent.sqrMagnitude < 1e-6f)
                {
                    tangent = next - outline[i];
                }
                normal[i] = tangent.sqrMagnitude < 1e-6f ? Vector2.up : new Vector2(tangent.y, -tangent.x).normalized;

                outer[i] = outline[i];
                outerAa[i] = outline[i] + normal[i] * aa;
                inner[i] = _borderWidth > 0.01f ? outline[i] - normal[i] * Mathf.Min(_borderWidth, Mathf.Min(w, h) * 0.25f) : outline[i];
            }

            var bg = color;
            var border = _borderColor;
            var borderAa = new Color(border.r, border.g, border.b, 0f);

            // 外缘抗锯齿羽化带（alpha 0 到 1）
            for (int i = 0; i < n; i++)
            {
                int a0 = AddVertex(vh, outerAa[i], borderAa, w, h);
                int b0 = AddVertex(vh, outerAa[(i + 1) % n], borderAa, w, h);
                int c0 = AddVertex(vh, outer[(i + 1) % n], border, w, h);
                int d0 = AddVertex(vh, outer[i], border, w, h);
                vh.AddTriangle(a0, b0, c0);
                vh.AddTriangle(a0, c0, d0);
            }

            // 描边环（外=描边色 到 内=底色）
            if (_borderWidth > 0.01f)
            {
                for (int i = 0; i < n; i++)
                {
                    int o0 = AddVertex(vh, outer[i], border, w, h);
                    int o1 = AddVertex(vh, outer[(i + 1) % n], border, w, h);
                    int i1 = AddVertex(vh, inner[(i + 1) % n], bg, w, h);
                    int i0 = AddVertex(vh, inner[i], bg, w, h);
                    vh.AddTriangle(o0, o1, i1);
                    vh.AddTriangle(o0, i1, i0);
                }
            }

            // 主体（扇形三角化）
            int centerV = AddVertex(vh, center, bg, w, h);
            for (int i = 0; i < n; i++)
            {
                int a = AddVertex(vh, inner[i], bg, w, h);
                int b = AddVertex(vh, inner[(i + 1) % n], bg, w, h);
                vh.AddTriangle(centerV, a, b);
            }
        }

        private float ComputeAaWidth()
        {
            var c = canvas;
            float scale = c != null ? c.scaleFactor : 1f;
            return Mathf.Max(0.25f, 1f / Mathf.Max(0.001f, scale));
        }

        private static List<Vector2> BuildOutline(float w, float h, float r, float f)
        {
            float hw = w * 0.5f;
            float hh = h * 0.5f;

            // 直角主题（r 近 0）：直接四角矩形，避免 r=0 时圆角循环退化为 96 个重合顶点的退化三角形。
            if (r < 0.01f)
            {
                var rect = new List<Vector2>
                {
                    new Vector2(-hw, -hh),
                    new Vector2(hw, -hh),
                    new Vector2(hw, hh),
                    new Vector2(-hw, hh),
                };
                if (f < 0.999f)
                {
                    for (int i = 0; i < rect.Count; i++)
                    {
                        rect[i] = new Vector2((rect[i].x + hw) * f - hw, rect[i].y);
                    }
                }
                return rect;
            }

            var pts = new List<Vector2>(SegmentsPerCorner * 4 + 4);

            var centers = new[]
            {
                new Vector2(hw - r, -hh + r),
                new Vector2(hw - r, hh - r),
                new Vector2(-hw + r, hh - r),
                new Vector2(-hw + r, -hh + r),
            };

            for (int ci = 0; ci < 4; ci++)
            {
                for (int s = 0; s < SegmentsPerCorner; s++)
                {
                    float a = (ci * 90f + s * (90f / SegmentsPerCorner) - 90f) * Mathf.Deg2Rad;
                    pts.Add(centers[ci] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            }

            if (f < 0.999f)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    pts[i] = new Vector2((pts[i].x + hw) * f - hw, pts[i].y);
                }
                RemoveNearDuplicates(pts, Mathf.Max(0.05f, r * 0.02f));
            }

            return pts;
        }

        private static void RemoveNearDuplicates(List<Vector2> pts, float tolerance)
        {
            float tol2 = tolerance * tolerance;
            for (int i = pts.Count - 1; i > 0; i--)
            {
                if ((pts[i] - pts[i - 1]).sqrMagnitude < tol2)
                {
                    pts.RemoveAt(i);
                }
            }
        }

        private static Vector2 ComputeCentroid(List<Vector2> pts)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < pts.Count; i++)
            {
                sum += pts[i];
            }
            return sum / Mathf.Max(1, pts.Count);
        }

        private static int AddVertex(VertexHelper vh, Vector2 pos, Color col, float w, float h)
        {
            int idx = vh.currentVertCount;
            vh.AddVert(pos, col, new Vector2(pos.x / w + 0.5f, pos.y / h + 0.5f));
            return idx;
        }
    }
}
