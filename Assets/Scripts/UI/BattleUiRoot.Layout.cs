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
            // 竖屏带子从 176/360 收到 168/340：内容实际只需要这么多，
            // 每收 10px，FOV 循环就让地图长大一圈 —— 竖屏最大的问题一直是中间那段死天空。
            float top = narrow ? 168 : 94;
            float bottom = narrow ? 340 : 166;
            LayoutRect("TopBand").sizeDelta = new Vector2(0, top);
            LayoutRect("BottomBand").sizeDelta = new Vector2(0, bottom);
            // 右下角的制作者暗记：极小、贴边、永远不参与布局博弈。
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
            // 弹幕读数横躺在底栏左半边，和右边的出阵区一起构成一整行。
            var power = LayoutRect("BarrageCommand");
            power.localScale = Vector3.one * scale;
            // 竖屏时出阵区是**居中**的（上面那行），所以弹幕读数不能只让开 7 个单位 ——
            // 那条 600 宽的充能轨会正好从「灵力 SPIRIT / 出阵」那行字中间穿过去。
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
                // 战场铺满全屏。上一版把相机 rect 裁进上下两条栏之间（只有 64% 高），
                // 于是天空和星野被切成一条带子，上下各留一条纯黑 —— 那两条黑边唯一的作用
                // 就是「给 UI 腾地方」。屏幕有三分之一是死的，战场看着像控件里的预览窗口。
                //
                // 东方和方舟都不这么做：战场铺满，UI 浮在战场**之上**，
                // 只在贴着屏幕边缘的地方给一层渐变暗角保证文字可读。
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.fieldOfView = GameConfig.CameraFov;
                camera.aspect = size.x / Mathf.Max(1f, size.y);

                // 但地图不能躲到出阵卡底下 —— 那里要放塔，看不见就没法玩。
                // 所以把「可玩区」的判定框收在卡片条上方。
                //
                // 取景用二分而不是老的「一路拉远直到塞下」：老算法只保证地图**放得进**，
                // 地图本来就放得进时它什么都不做 —— 于是竖屏里地图头顶悬着两百多像素的死天空。
                // 现在反过来，在塞得进的前提下把 FOV 收到最小，地图顶到判定框边缘为止。
                // 16:9 下结果和旧值接近，竖屏 / 4:3 能多吃掉一整段留白。
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

        /// <summary>地图四角都在「可玩区」内 = 取景合格。可玩区比 UI 带略宽，留了边距容错。</summary>
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
