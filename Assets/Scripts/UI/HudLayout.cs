using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD 的固定屏幕布局。集中定义矩形，供 HUD 绘制与「点击是否落在 UI 上」判断共用。
    /// 注意：OnGUI 使用左上角原点（y 向下），而 Input.mousePosition 是左下角原点。
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

        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return InteractiveRegion.Contains(guiPosition);
        }
    }
}
