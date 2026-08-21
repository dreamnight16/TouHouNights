using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD 的固定屏幕布局（明日方舟式）：
    /// - 顶栏：生命 / 金币 / 波次 / 状态 + 索敌、开波、重开按钮；
    /// - 底栏：居中排列的防御塔选择卡片；
    /// 中间留给地图。所有矩形均使用 OnGUI 坐标系（左上角原点、y 向下）。
    /// </summary>
    public static class HudLayout
    {
        public const float TopBarHeight = 48f;
        public const float BottomBarHeight = 108f;

        private const float TowerCardWidth = 170f;
        private const float TowerCardGap = 12f;

        public static Rect TopBar => new Rect(0f, 0f, Screen.width, TopBarHeight);
        public static Rect BottomBar => new Rect(0f, Screen.height - BottomBarHeight, Screen.width, BottomBarHeight);

        // 顶栏右侧按钮（从右往左排）。
        public static Rect SpeedButton => new Rect(Screen.width - 536f, 8f, 100f, 32f);
        public static Rect TargetingButton => new Rect(Screen.width - 428f, 8f, 188f, 32f);
        public static Rect StartButton => new Rect(Screen.width - 232f, 8f, 106f, 32f);
        public static Rect RestartButton => new Rect(Screen.width - 118f, 8f, 106f, 32f);

        public static Rect HintLabel => new Rect(14f, Screen.height - 22f, 620f, 18f);

        /// <summary>第 index 张塔选择卡片的位置（底栏居中排列）。</summary>
        public static Rect TowerButton(int index, int count)
        {
            float totalWidth = count * TowerCardWidth + (count - 1) * TowerCardGap;
            float x0 = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - BottomBarHeight + 10f;
            return new Rect(x0 + index * (TowerCardWidth + TowerCardGap), y, TowerCardWidth, 62f);
        }

        /// <summary>判断某个 GUI 坐标是否落在顶栏或底栏上（这些区域点击不应放置塔）。</summary>
        public static bool IsPointerOverGui(Vector2 guiPosition)
        {
            return guiPosition.y <= TopBarHeight || guiPosition.y >= Screen.height - BottomBarHeight;
        }
    }
}
