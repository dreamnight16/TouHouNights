using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// 战斗界面的基础控件和定位函数，委托给 UiKit 与 UiFactory。
    /// </summary>
    public sealed partial class BattleUiRoot
    {
        private static UiPanel Surface(Transform parent, string name, UiSurfaceKind kind)
        {
            return UiKit.Surface(parent, name, kind);
        }

        private static Image Image(Transform parent, string name, Color color)
        {
            return UiFactory.CreateImage(parent, name, color);
        }

        private static UiText Text(Transform p, string n, string v, int s, Color c, TextAnchor a,
            float x, float y, float w, float h)
        {
            return UiKit.Label(p, n, v, s, c, a, x, y, w, h);
        }

        /// <summary>小米字：全大写标签 / 单位后缀。和超大数字同屏才有尺度对比。</summary>
        private static UiText Micro(Transform p, string n, string v, Color c, float x, float y, float w, float h)
        {
            return UiKit.MicroLabel(p, n, v, c, x, y, w, h);
        }

        // ---- 定位（Layout.cs 直接用这几个名字）----

        private static void PinTopLeft(RectTransform r, float x, float y, float w, float h) => UiKit.PinTopLeft(r, x, y, w, h);
        private static void PinTopRight(RectTransform r, float right, float y, float w, float h) => UiKit.PinTopRight(r, right, y, w, h);
        private static void PinTopCenter(RectTransform r, float x, float y, float w, float h) => UiKit.PinTopCenter(r, x, y, w, h);
        private static void PinBottomLeft(RectTransform r, float x, float b, float w, float h) => UiKit.PinBottomLeft(r, x, b, w, h);
        private static void PinBottomRight(RectTransform r, float right, float b, float w, float h) => UiKit.PinBottomRight(r, right, b, w, h);
        private static void PinLeft(RectTransform r, float left, float y, float w, float h) => UiKit.PinLeft(r, left, y, w, h);
        private static void PinRight(RectTransform r, float right, float y, float w, float h) => UiKit.PinRight(r, right, y, w, h);
    }
}
