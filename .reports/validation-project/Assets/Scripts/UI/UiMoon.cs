using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    // A single procedural motif, shared by menu and pause screen; no imported artwork.
    public sealed class UiMoon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center = rectTransform.rect.center;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .44f;
            float angle = Time.unscaledTime * 2f;
            for (int ring = 0; ring < 3; ring++)
            {
                float r = radius - ring * 14;
                for (int i = 0; i < 120; i++)
                {
                    if (ring == 1 && i % 30 > 20) continue;
                    float a = (i * 3 + angle * (ring == 1 ? -1 : 1)) * Mathf.Deg2Rad;
                    float b = a + 3 * Mathf.Deg2Rad;
                    Color tint = ring == 0 ? BattleUiTheme.Action : BattleUiTheme.Paper;
                    tint.a = ring == 0 ? .5f : .12f;
                    int start = vh.currentVertCount;
                    float thickness = ring == 0 ? 2 : 1;
                    vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, tint, Vector2.zero);
                    vh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, tint, Vector2.zero);
                    vh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (r - thickness), tint, Vector2.zero);
                    vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r - thickness), tint, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
        private void Update() { SetVerticesDirty(); }
    }
}
