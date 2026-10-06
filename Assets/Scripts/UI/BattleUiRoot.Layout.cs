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
            float top = narrow ? 168 : 94;
            float bottom = narrow ? 340 : 166;
            LayoutRect("TopBand").sizeDelta = new Vector2(0, top);
            LayoutRect("BottomBand").sizeDelta = new Vector2(0, bottom);
            PinBottomLeft(LayoutRect("DreamNight"), 24, 4, 200, 14);
            float scale = narrow ? .9f : .8f;
            var mission = LayoutRect("MissionTag");
            mission.localScale = Vector3.one * (narrow ? .85f : .8f);
            PinTopLeft(mission, narrow ? 16 : 24, narrow ? 8 : 10, 300, 84);
            var time = LayoutRect("TimeControls");
            time.localScale = Vector3.one * scale;
            PinTopRight(time, narrow ? 16 : 24, narrow ? 8 : 10, 340, 84);
            var readout = LayoutRect("BattleReadout");
            readout.localScale = Vector3.one * scale;
            PinTopCenter(readout, 0, narrow ? 88 : 10, 448, 84);
            var hand = LayoutRect("DeploymentHand");
            float handScale = Mathf.Min(narrow ? 1 : .82f, (size.x - 48) / 744f);
            hand.localScale = Vector3.one * handScale;
            if (narrow) PinBottomLeft(hand, (size.x - 744 * handScale) / 2, 14, 744, 214);
            else PinBottomRight(hand, 24, 12, 744, 214);
            var power = LayoutRect("BarrageCommand");
            power.localScale = Vector3.one * scale;
            // 窄屏把弹幕读数放在出阵区上方，避免充能轨道穿过部署标题。
            PinBottomLeft(power, narrow ? 18 : 24, narrow ? 206 : 26, 600, 132);
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
                // 相机铺满屏幕，HUD 叠在战场上。
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.fieldOfView = GameConfig.CameraFov;
                camera.aspect = size.x / Mathf.Max(1f, size.y);

                // 在上下栏之间取景；二分查找能容纳地图四角的最小 FOV。
                float yFloor = (bottom + 10f) / size.y;
                float yCeil = 1f - top / size.y;
                float lo = 26f, hi = 105f;
                for (int i = 0; i < 28; i++)
                {
                    float mid = (lo + hi) * .5f;
                    camera.fieldOfView = mid;
                    if (MapFitsBand(camera, yFloor, yCeil)) hi = mid;
                    else lo = mid;
                }
                camera.fieldOfView = hi;
            }
        }

        /// <summary>检查地图四角是否位于上下栏之间，并留出水平边距。</summary>
        private static bool MapFitsBand(Camera camera, float yFloor, float yCeil)
        {
            foreach (var point in new[] { new Vector3(-9, -5, 0), new Vector3(9, -5, 0), new Vector3(-9, 5, 0), new Vector3(9, 5, 0) })
            {
                var p = camera.WorldToViewportPoint(point);
                if (!(p.z > 0) || p.x <= .035f || p.x >= .965f || p.y <= yFloor || p.y >= yCeil) return false;
            }
            return true;
        }
        private void OnDisable()
        {
            var camera = Camera.main;
            if (camera != null) { camera.rect = new Rect(0, 0, 1, 1); camera.ResetAspect(); }
            _layoutSize = Vector2.zero;
        }
    }
}
