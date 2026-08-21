using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD 的固定屏幕布局。集中定义矩形，供 HUD 绘制与「点击是否落在 UI 上」判断共用。
    /// 所有矩形均使用 OnGUI 坐标系（左上角原点、y 向下）。
    /// </summary>
    public static class HudLayout
    {
        public static readonly Rect Panel = new Rect(12f, 12f, 320f, 150f);
        public static readonly Rect StartButton = new Rect(12f, 172f, 200f, 44f);
        public static readonly Rect RestartButton = new Rect(12f, 224f, 200f, 44f);
        public static readonly Rect TargetingButton = new Rect(12f, 276f, 200f, 40f);

        public static Rect TowerButton(int index)
        {
            return new Rect(344f, 12f + index * 52f, 230f, 46f);
        }

        public static readonly Rect InteractiveRegion = new Rect(12f, 12f, 562f, 304f);

        /// <summary>判断某个 GUI 坐标（左上角原点）是否落在 HUD 交互区域上。</summary>
        public static bool IsPointerOverGui(Vector2 guiPosition)
        {
            return InteractiveRegion.Contains(guiPosition);
        }
    }
}
