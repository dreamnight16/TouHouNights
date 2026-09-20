using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    public sealed partial class BattleUiRoot
    {
        private Vector2 _layoutSize;
        private RectTransform LayoutRect(string path) => transform.Find(path) as RectTransform;
        public void RefreshLayout()
        {
            var size = ((RectTransform)transform).rect.size;
            if (size.x < 1 || size.y < 1 || size == _layoutSize) return;
            _layoutSize = size;
            bool narrow = size.x / size.y < 1.25f;
            float top = narrow ? 176 : 94;
            float bottom = narrow ? 360 : 166;
            LayoutRect("TopBand").sizeDelta = new Vector2(0, top);
            LayoutRect("BottomBand").sizeDelta = new Vector2(0, bottom);
            LayoutRect("LeftSafetyRail").gameObject.SetActive(false);
            LayoutRect("BottomSafetyRail").gameObject.SetActive(false);
            LayoutRect("DreamNight").gameObject.SetActive(false);
            float scale = narrow ? .9f : .8f;
            var mission = LayoutRect("MissionTag");
            mission.localScale = Vector3.one * scale;
            PinTopLeft(mission, 24, 10, 240, 84);
            var time = LayoutRect("TimeControls");
            time.localScale = Vector3.one * scale;
            PinTopRight(time, 24, 10, 312, 84);
            var readout = LayoutRect("BattleReadout");
            readout.localScale = Vector3.one * scale;
            PinTopCenter(readout, 0, narrow ? 90 : 10, 448, 84);
            var hand = LayoutRect("DeploymentHand");
            float handScale = Mathf.Min(narrow ? 1 : .82f, (size.x - 48) / 744f);
            hand.localScale = Vector3.one * handScale;
            if (narrow) PinBottomLeft(hand, (size.x - 744 * handScale) / 2, 20, 744, 182);
            else PinBottomRight(hand, 24, 12, 744, 182);
            var power = LayoutRect("BarrageCommand");
            power.localScale = Vector3.one * .72f;
            PinBottomLeft(power, 24, narrow ? 204 : 12, 224, 182);
            PinBottomLeft(LayoutRect("UnitDossier"), 24, bottom + 16, 304, 280);
            PinTopRight(LayoutRect("EnemyIntel"), 24, top + 16, 236, 126);
            PinTopCenter(LayoutRect("AlertDirector"), 0, top + 18, 480, 68);
            PinLeft(LayoutRect("PauseScreen/Menu"), narrow ? (size.x - 330) / 2 : size.x * .12f, narrow ? 70 : 0, 330, 392);
            PinRight(LayoutRect("PauseScreen/Brief"), narrow ? (size.x - 330) / 2 : size.x * .12f, narrow ? -260 : 0, 330, 200);
            LayoutRect("PauseScreen/PauseWatermark").gameObject.SetActive(!narrow);
            PinTopLeft(LayoutRect("ResultScreen/ReportLabel"), 32, 32, size.x - 64, 32);
            PinTopLeft(LayoutRect("ResultScreen/ReportDivider"), 32, 80, size.x - 64, 1);
            PinTopLeft(LayoutRect("ResultScreen/Title"), 48, 112, size.x - 96, 64);
            PinLeft(LayoutRect("ResultScreen/Grade"), 48, narrow ? 140 : 0, narrow ? size.x - 96 : 300, 220);
            PinRight(LayoutRect("ResultScreen/Stats"), 48, narrow ? -140 : 0, narrow ? size.x - 96 : 400, 260);
            float bw = Mathf.Min(270, (size.x - 112) / 2);
            PinBottomLeft(LayoutRect("ResultScreen/Retry"), 48, 40, bw, 54);
            PinBottomRight(LayoutRect("ResultScreen/Home"), 48, 40, bw, 54);
            var camera = Camera.main;
            if (camera != null)
            {
                camera.rect = new Rect(0, bottom / size.y, 1, Mathf.Max(.1f, (size.y - top - bottom) / size.y));
                camera.fieldOfView = GameConfig.CameraFov;
                camera.aspect = size.x / Mathf.Max(1, size.y - top - bottom);
                for (int i = 0; i < 45; i++)
                {
                    bool fits = true;
                    foreach (var point in new[] { new Vector3(-9, -5, 0), new Vector3(9, -5, 0), new Vector3(-9, 5, 0), new Vector3(9, 5, 0) })
                    {
                        var p = camera.WorldToViewportPoint(point);
                        fits &= p.z > 0 && p.x > .04f && p.x < .96f && p.y > .04f && p.y < .96f;
                    }
                    if (fits || camera.fieldOfView >= 105) break;
                    camera.fieldOfView += 1.5f;
                }
            }
        }
        private void OnDisable()
        {
            var camera = Camera.main;
            if (camera != null) { camera.rect = new Rect(0, 0, 1, 1); camera.ResetAspect(); }
            _layoutSize = Vector2.zero;
        }
    }
}
