using System;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 结界终端 UI 工厂（全新体系）：玻璃面板 / 终端按钮 / 符卡 / 环印 / 徽章。
    /// 底图元复用矢量组件（RoundedRectImage、RadialProgressImage、UiText、UiIcon），保证锐利与材质统一。
    /// </summary>
    public static class TdKit
    {
        /// <summary>部署廊里符卡的固定顺序（同时是快捷键 1-5 的顺序）。</summary>
        public static readonly TowerType[] SpellOrder =
        {
            TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow, TowerType.Heal
        };

        /// <summary>符卡在 <see cref="SpellOrder"/> 中的下标（用于取符卡名/英文名）。</summary>
        public static int SpellIndex(TowerType type)
        {
            switch (type)
            {
                case TowerType.Sniper: return 1;
                case TowerType.Missile: return 2;
                case TowerType.Slow: return 3;
                case TowerType.Heal: return 4;
                default: return 0;
            }
        }

        /// <summary>职业语义标签：状态优先于数值——先告诉玩家「这是什么角色」。</summary>
        public static string RoleOf(TowerDefinition def)
        {
            if (def.HealPerSecond > 0f) return "医疗";
            if (def.Damage <= 0f) return "辅助";
            return def.SplashRadius > 0f ? "群攻" : "单体";
        }

        /// <summary>终端玻璃面板（矢量圆角 + 1px 冷描边 + 亚克力材质）。</summary>
        public static RoundedRectImage Glass(Transform parent, string name, float radius, Color bg, Color edge = default, float edgeW = 1f, bool acrylic = true)
        {
            if (edge == default) edge = TdTheme.Edge;
            var panel = UiFactory.CreateVectorPanel(parent, name, radius, bg, edge, edgeW);
            return panel;
        }

        /// <summary>终端按钮（矢量圆角描边 + 点击回弹 + 按需扫光）。</summary>
        public static Button Button(Transform parent, string name, string label, float radius, Color bg, Action onClick, int fontSize = TdTheme.Small, bool reveal = false)
        {
            var b = UiFactory.CreateButton(parent, name, label, bg, fontSize, onClick);
            var rri = b.GetComponent<RoundedRectImage>();
            if (rri != null)
            {
                rri.cornerRadius = radius;
                rri.borderWidth = 1f;
                rri.borderColor = new Color(1f, 1f, 1f, 0.18f);
            }
            if (reveal)
            {
                RevealLight.Attach(b.GetComponent<RectTransform>(), new Color(0.42f, 0.70f, 0.95f, 1f));
            }
            return b;
        }

        /// <summary>环形符印（径向进度环）。</summary>
        public static RadialProgressImage Ring(Transform parent, string name, float radius, float thickness, Color fill, Color track)
        {
            var ring = new GameObject(name, typeof(RectTransform)).AddComponent<RadialProgressImage>();
            ring.transform.SetParent(parent, false);
            ring.radius = radius;
            ring.thickness = thickness;
            ring.fillColor = fill;
            ring.trackColor = track;
            ring.raycastTarget = false;
            return ring;
        }

        /// <summary>终端文本。</summary>
        public static UiText Text(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool shadow = false)
        {
            return UiFactory.CreateText(parent, name, content, fontSize, color, align, shadow);
        }

        /// <summary>圆形符印徽章（实心圆 + 内环，结界/威胁类读数的载体）。</summary>
        public static Image CircleBadge(Transform parent, string name, float size, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            img.sprite = SpriteFactory.Circle(0.5f, color);
            return img;
        }

        /// <summary>小圆点符号（符印装饰/状态点）。</summary>
        public static Image Dot(Transform parent, string name, float size, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            img.sprite = SpriteFactory.Circle(0.5f, color);
            return img;
        }

        /// <summary>细横线（轨道基线/分隔）。</summary>
        public static Image Line(Transform parent, string name, float width, float height, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>光晕（呼吸光常用）。</summary>
        public static Image Glow(Transform parent, string name, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            img.sprite = SpriteFactory.Glow(0.5f, Color.white);
            return img;
        }

        /// <summary>顶部内部高光：在面板顶缘铺一条「上亮下透」的渐变，制造内部光感（方舟式立体感）。</summary>
        public static Image Sheen(Transform parent, string name, float inset, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, Color.white);
            img.raycastTarget = false;
            img.sprite = SpriteFactory.GradientTop(color);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -inset);
            rt.sizeDelta = new Vector2(0f, 18f);
            return img;
        }

        /// <summary>发光强调轨：贴在带顶/底缘的一条青蓝光轨（全局强调色的具象化）。</summary>
        public static Image AccentLine(Transform parent, string name, bool bottom, Color color)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            if (bottom)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 2f);
            return img;
        }

        /// <summary>
        /// L 型折线角标（方舟 / Sci-fi HUD 的标志性细节）：在父元素四角各画一组「横 + 竖」短棒，
        /// 叠加在面板之上，制造截取框 / 仪器套环的精密感。返回承载根 RectTransform；运行时用
        /// <see cref="TintCorners"/> 整体染色（例如选中时提亮）。
        /// </summary>
        public static RectTransform Corners(Transform parent, string name, Color color, float len = 9f, float thick = 2f)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            UiFactory.Stretch(rootRect);

            Vector2[] pivots = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            foreach (var pivot in pivots)
            {
                CornerBar(root.transform, name + "_H", color, new Vector2(len, thick), pivot);
                CornerBar(root.transform, name + "_V", color, new Vector2(thick, len), pivot);
            }
            return rootRect;
        }

        /// <summary>把角标根下的所有线段子图元染成同一颜色（选择/悬停状态切换）。</summary>
        public static void TintCorners(RectTransform cornersRoot, Color color)
        {
            if (cornersRoot == null) return;
            var bars = cornersRoot.GetComponentsInChildren<Image>(true);
            foreach (var bar in bars) bar.color = color;
        }

        private static Image CornerBar(Transform parent, string name, Color color, Vector2 size, Vector2 pivot)
        {
            var img = UiFactory.CreateImage(parent, name, color);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = pivot;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return img;
        }
    }
}
