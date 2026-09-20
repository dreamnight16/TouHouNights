using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// 径向进度仪表（矢量自绘）：外弧为轨道、内弧为进度弧（顶部起顺时针），
    /// 内外缘各带 1 物理像素抗锯齿羽化带（与 RoundedRectImage 同一套 AA 方案），任何缩放锐利。
    /// 用于顶栏 P 能量环 / 结算评分环。
    /// </summary>
    public sealed class RadialProgressImage : Image
    {
        [SerializeField] private float _radius = 30f;
        [SerializeField] private float _thickness = 4f;
        [SerializeField] private Color _trackColor = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color _fillColor = new Color(0.34f, 0.65f, 0.88f, 1f);
        [SerializeField] private float _progress = 0f;
        private float _lastScale = -1f;

        public float radius { get => _radius; set { _radius = Mathf.Max(2f, value); SetVerticesDirty(); } }
        public float thickness { get => _thickness; set { _thickness = Mathf.Max(1f, value); SetVerticesDirty(); } }
        public Color trackColor { get => _trackColor; set { _trackColor = value; SetVerticesDirty(); } }
        public Color fillColor { get => _fillColor; set { _fillColor = value; SetVerticesDirty(); } }

        public float Progress
        {
            get => _progress;
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

            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            if (size < 8f) return;

            float r = Mathf.Min(_radius, size - _thickness * 0.5f - 1f);
            float hw = _thickness * 0.5f;
            float aa = 1f / Mathf.Max(0.001f, canvas != null ? canvas.scaleFactor : 1f);

            // 轨道（整圆）
            DrawArc(vh, 0f, 1f, r, hw, aa, _trackColor);

            // 进度弧
            if (_progress > 0.002f)
            {
                DrawArc(vh, 0f, _progress, r, hw, aa, _fillColor);
            }
        }

        private void DrawArc(VertexHelper vh, float t0, float t1, float r, float hw, float aa, Color col)
        {
            float start = -90f;
            float sweep = (t1 - t0) * 360f;
            float a0 = start + t0 * 360f;

            int segs = Mathf.Clamp(Mathf.CeilToInt(sweep / 6f), 4, 90);
            var colAa = new Color(col.r, col.g, col.b, 0f);

            float rIn = r - hw;
            float rOut = r + hw;
            float rAaIn = rIn - aa;
            float rAaOut = rOut + aa;

            // 顶点色：从外到内 = colAa(0), col, col, colAa(0)
            var vPrev = new int[4];

            for (int s = 0; s <= segs; s++)
            {
                float a = (a0 + sweep * s / segs) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

                int v0 = AddVertex(vh, dir * rAaIn, colAa);
                int v1 = AddVertex(vh, dir * rIn, col);
                int v2 = AddVertex(vh, dir * rOut, col);
                int v3 = AddVertex(vh, dir * rAaOut, colAa);

                if (s > 0)
                {
                    vh.AddTriangle(vPrev[0], v0, v1);
                    vh.AddTriangle(vPrev[0], v1, vPrev[1]);
                    vh.AddTriangle(vPrev[1], v1, v2);
                    vh.AddTriangle(vPrev[1], v2, vPrev[2]);
                    vh.AddTriangle(vPrev[2], v2, v3);
                    vh.AddTriangle(vPrev[2], v3, vPrev[3]);
                }

                vPrev[0] = v0;
                vPrev[1] = v1;
                vPrev[2] = v2;
                vPrev[3] = v3;
            }
        }

        private static int AddVertex(VertexHelper vh, Vector2 pos, Color col)
        {
            int idx = vh.currentVertCount;
            vh.AddVert(pos, col, new Vector2(0.5f, 0.5f));
            return idx;
        }
    }
}
