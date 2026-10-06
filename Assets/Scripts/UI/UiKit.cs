using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>面板用途。</summary>
    public enum UiSurfaceKind
    {
        /// <summary>信息面板：平涂的石墨底 + 发丝描边。</summary>
        Panel = 0,

        /// <summary>可交互卡片：比 Panel 亮一档的平涂底色。</summary>
        Card = 1,

        /// <summary>通栏遮罩：平涂、无描边。用于暂停与结算的全屏底，也用于按钮自绘底板。</summary>
        Veil = 2,
    }

    /// <summary>按钮语义。</summary>
    public enum UiButtonKind
    {
        /// <summary>主行动：绯色实心。</summary>
        Primary = 0,

        /// <summary>次级行动：暗底、发丝描边和骨白文字。</summary>
        Ghost = 1,

        /// <summary>安静行动：无描边，灰字。用于「返回 / 退出」这类不该抢注意力的入口。</summary>
        Quiet = 2,
    }

    /// <summary>
    /// 战斗和前端界面共用的布局、面板、文本与按钮工厂。
    /// </summary>
    public static class UiKit
    {
        // ================= 布局 =================

        public static void PinTopLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinTopRight(RectTransform r, float right, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(-right, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinTopCenter(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinBottomLeft(RectTransform r, float x, float bottom, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = new Vector2(x, bottom);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinBottomRight(RectTransform r, float right, float bottom, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-right, bottom);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinLeft(RectTransform r, float left, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, .5f);
            r.anchoredPosition = new Vector2(left, y);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void PinRight(RectTransform r, float right, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, .5f);
            r.anchoredPosition = new Vector2(-right, y);
            r.sizeDelta = new Vector2(w, h);
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = Vector2.zero;
        }

        // ================= 面板 =================

        /// <summary>按用途取一块面板。返回 <see cref="UiPanel"/>，可以继续链式覆盖辉光 / 切角。</summary>
        public static UiPanel Surface(Transform parent, string name, UiSurfaceKind kind)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UiPanel));
            go.transform.SetParent(parent, false);
            var panel = go.GetComponent<UiPanel>();

            switch (kind)
            {
                // 面板预设使用平涂底色，层次由明度、描边和辉光区分。

                case UiSurfaceKind.Panel:
                    panel.Flat(BattleUiTheme.Slate)
                         .Border(BattleUiTheme.Edge, BattleUiTheme.Border);
                    break;

                case UiSurfaceKind.Card:
                    panel.Flat(BattleUiTheme.Stone)
                         .Border(BattleUiTheme.Edge, BattleUiTheme.Border);
                    break;

                default: // Veil
                    panel.Flat(BattleUiTheme.Veil)
                         .Border(Color.clear, 0f);
                    break;
            }
            return panel;
        }

        /// <summary>不拦截指针的纯色矩形，用于分隔线、色条和占位。</summary>
        public static Image Rect(Transform parent, string name, Color color)
        {
            var image = UiFactory.CreateImage(parent, name, color);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>发丝分隔线（横）。比 <see cref="Rect"/> 语义更明确，方便统一调粗细。</summary>
        public static Image RuleH(Transform parent, string name, Color color, float x, float y, float w)
        {
            var rule = Rect(parent, name, color);
            PinTopLeft(rule.rectTransform, x, y, w, BattleUiTheme.Hairline);
            return rule;
        }

        /// <summary>发丝分隔线（竖）。</summary>
        public static Image RuleV(Transform parent, string name, Color color, float x, float y, float h)
        {
            var rule = Rect(parent, name, color);
            PinTopLeft(rule.rectTransform, x, y, BattleUiTheme.Hairline, h);
            return rule;
        }

        // ================= 文字 =================

        /// <summary>正文：骨白 / 灰，随锚点摆放，自动缩到框内。</summary>
        public static UiText Label(Transform parent, string name, string value, int size, Color color,
            TextAnchor anchor, float x, float y, float w, float h)
        {
            var text = UiFactory.CreateText(parent, name, value, size, color, anchor);
            PinTopLeft(text.rectTransform, x, y, w, h);
            // 最小字号不得超过当前字号，小字号标签也要保持有效范围。
            text.FitInBox(Mathf.Min(size, Mathf.Max(9, size - 4)));
            return text;
        }

        /// <summary>带字距的小字号标签。</summary>
        public static UiText MicroLabel(Transform parent, string name, string value, Color color,
            float x, float y, float w, float h)
        {
            var text = Label(parent, name, value, BattleUiTheme.Type.Micro, color, TextAnchor.MiddleLeft, x, y, w, h);
            text.SetCharacterSpacing(1.6f);
            return text;
        }

        // ================= 纹样 =================

        /// <summary>封印环：断成 4 段的细环，缺口落在对角线上。</summary>
        public static Image SealRing(Transform parent, string name, float size, Color color,
            float thickness = 0f, int arcs = 4)
        {
            var image = UiFactory.CreateImage(parent, name, color);
            image.sprite = SpriteFactory.SealRing(size, thickness > 0f ? thickness : Mathf.Max(1f, size * .035f), Color.white, arcs);
            image.raycastTarget = false;
            return image;
        }

        // ================= 按钮 =================

        /// <summary>
        /// 按语义取一个按钮。<paramref name="onClick"/> 会自动附带点击音效。
        /// </summary>
        /// <param name="labelSize">标签字号；0 = 按语义取默认档。</param>
        /// <param name="labelInset">标签四周留白。负数 = 按语义取默认值。</param>
        public static Button Button(Transform parent, string name, string label, UiButtonKind kind,
            System.Action onClick, int labelSize = 0, float labelInset = -1f)
        {
            var panel = Surface(parent, name, kind == UiButtonKind.Primary ? UiSurfaceKind.Card : UiSurfaceKind.Panel);
            if (kind == UiButtonKind.Primary)
            {
                // 主行动使用绯色实心和辉光。
                panel.Flat(BattleUiTheme.Scarlet)
                     .Border(BattleUiTheme.Lift(BattleUiTheme.Scarlet, .45f), BattleUiTheme.Border)
                     .Glow(BattleUiTheme.GlowScarlet, 14f);
            }
            else if (kind == UiButtonKind.Quiet)
            {
                panel.Flat(Color.clear)
                     .Border(Color.clear, 0f);
            }

            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = FlatColors();
            if (onClick != null) button.onClick.AddListener(() => { Sfx.Click(); onClick(); });

            int size = labelSize > 0
                ? labelSize
                : (kind == UiButtonKind.Primary ? BattleUiTheme.Type.Heading : BattleUiTheme.Type.Body);
            // 小按钮需要单独指定内缩，避免标签框过窄。
            float inset = labelInset >= 0f ? labelInset : BattleUiTheme.Gap.Md;
            float insetV = Mathf.Min(BattleUiTheme.Gap.Xs, inset);

            var text = UiFactory.CreateText(panel.transform, "Label", label, size,
                kind == UiButtonKind.Primary ? BattleUiTheme.Ink : BattleUiTheme.Bone,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(inset, insetV);
            text.rectTransform.offsetMax = new Vector2(-inset, -insetV);
            text.FitInBox(Mathf.Min(size, Mathf.Max(9, size - 4)));

            UiMotion.Attach(button);
            return button;
        }

        /// <summary>小方块按钮（倍速切换这类）。</summary>
        public static Button Toggle(Transform parent, string name, string label, System.Action onClick,
            int labelSize = 0, float labelInset = -1f)
        {
            return Button(parent, name, label, UiButtonKind.Ghost, onClick, labelSize, labelInset);
        }

        /// <summary>
        /// 全白、零时长的颜色过渡，避免 ColorBlock 乘算改变面板的顶点色。
        /// 状态反馈交给 UiMotion 和面板本体。
        /// </summary>
        public static ColorBlock FlatColors()
        {
            return new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = Color.white,
                pressedColor = Color.white,
                selectedColor = Color.white,
                disabledColor = Color.white,
                colorMultiplier = 1f,
                fadeDuration = 0f,
            };
        }

        // ================= 小工具 =================

        /// <summary>允许在链式表达式里插入一句副作用（例如给创建出来的对象摆位置）。</summary>
        public static T Also<T>(this T value, System.Action<T> action)
        {
            action(value);
            return value;
        }
    }
}
