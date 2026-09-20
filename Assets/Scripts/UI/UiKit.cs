using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 面板用途。只有三种 —— 这个数字是刻意的。
    /// Chrome / Well 两个成员连同它们的预设已被删除：全项目一次都没用过，
    /// 留着只会让「这块该用哪种面」变成一个可以选错的问题。
    /// </summary>
    public enum UiSurfaceKind
    {
        /// <summary>信息面板：平涂的石墨底 + 发丝描边。</summary>
        Panel = 0,

        /// <summary>可交互卡片：平涂的板岩底 —— 比 <see cref="Panel"/> 亮一档，靠这一点浮起来。</summary>
        Card = 1,

        /// <summary>通栏遮罩：平涂、无描边。用于暂停与结算的全屏底，也用于按钮自绘底板。</summary>
        Veil = 2,
    }

    /// <summary>按钮语义。</summary>
    public enum UiButtonKind
    {
        /// <summary>主行动：绯色实心。一屏之内最多出现一个。</summary>
        Primary = 0,

        /// <summary>次级行动：无填充，发丝描边，骨白文字。默认值。</summary>
        Ghost = 1,

        /// <summary>安静行动：无描边，灰字。用于「返回 / 退出」这类不该抢注意力的入口。</summary>
        Quiet = 2,
    }

    /// <summary>
    /// UI 装配的公共零件库。所有界面（战斗 / 标题 / 练习 / 设置）都从这里取件，
    /// 保证「同一件事只有一种做法」—— 面板只有一种做法、按钮只有一种做法、小米字只有一种做法。
    ///
    /// 想要新外观，先在这里加一个零件；不要在调用点临时拼一个 —— 那正是上一版
    /// 退化成「工程技术样品」的原因：十几个面板各自手搓，长出了十几种不同的边。
    /// </summary>
    public static class UiKit
    {
        // ================= 布局 =================
        // 一律用锚点定位，禁止写死参考分辨率坐标 —— 竖屏 / 超宽屏都靠这几个函数兜住。

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
                // 一条规则管全部三种面：**面是平的**。
                //
                // 上一版这里写着「面板是平的」，可 Card 预设实际给了它三样东西 ——
                // Stone→Char 的上下渐变、一个对角斜切、一条顶部受光加一条底部压暗。
                // 四条装饰同时在否认那句注释。
                //
                // 渐变和切角本质上是**对材质的模仿**：它们假装这块平面是一块被光照到的
                // 实体，用受光面与背光面伪造出厚度。可这套界面不在任何一个有光源的空间里，
                // 它浮在夜空之上 —— 于是那些「受光」既说不出光源在哪，也不对应任何状态。
                // 玩家读到的不是「这块更亮」，而是「这里有一道没人要的亮边」。
                //
                // 层次改由两样东西表达，它们都是真的：
                //   · **明度台阶** —— Ink → Char → Slate → Stone，平涂，亮一档就是高一层；
                //   · **辉光与动效** —— UiHalo 的明暗随时间变化，能呼吸、能淡入淡出。
                // Metro 的做法也正是这两条：颜色只用来回答「这是哪一层」，
                // 运动只用来回答「什么变了」。
                //
                // 唯一留下的是发丝描边。它不模仿材质，它只说明这块面的**边界在哪** ——
                // 面板浮在流动的星空上，没有边就没有形，那是信息，不是装饰。

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

        /// <summary>纯色矩形（分隔线、色条、占位）。需要渐变或有边界的块请用 <see cref="Surface"/>。</summary>
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
            // 自动缩放下限必须严格小于上限：Micro 档只有 10pt，减 4 会得到 6，
            // 再被 Mathf.Max 抬到 11 就变成 min > max，TMP 会直接取 min 渲染 —— 米字全部偷偷放大一号。
            text.FitInBox(Mathf.Min(size, Mathf.Max(9, size - 4)));
            return text;
        }

        /// <summary>
        /// 极小米字：全大写拉丁 / 中文标签，带字距。这套排版里「小」的一极 ——
        /// 和 <see cref="Readout"/> 的超大数字同屏，尺度对比就是全部的设计。
        /// </summary>
        public static UiText MicroLabel(Transform parent, string name, string value, Color color,
            float x, float y, float w, float h)
        {
            var text = Label(parent, name, value, BattleUiTheme.Type.Micro, color, TextAnchor.MiddleLeft, x, y, w, h);
            text.SetCharacterSpacing(1.6f);
            return text;
        }

        // ================= 纹样 =================
        // 这一节现在是空的 —— 这套视觉里已经没有任何纹样。
        // 斜切随 Card 预设一起删掉了（改回直角），角标零件更早之前就删了
        // （唯一的使用者是暂停菜单，而那儿没有任何需要「取景」的东西）。
        //
        // 下面这个封印环暂时**没有调用点**：结算屏评级字母后面那圈 250px 的环已经删了。
        // 它是整套视觉里最后一个还没决定去留的零件，留在这儿等一个决定 ——
        // 要么找到一件它真正编码了状态的事，要么把它和 SpriteFactory.SealRing 一起删掉。

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
                // 主行动靠**它自己在发光**被认出来，加上全屏唯一的那一大块绯色实心 ——
                // 不靠顶部那条白色受光带。
                //
                // 那条带子（原 Rim）是「假装按钮被上方光源照到」。屏幕里没有这个光源，
                // 而它让按钮的上半截比下半截亮出一大截，读起来像一块廉价的塑料键帽。
                // 发光是**状态**（可点 / 已就绪），受光是**布景**（假装有盏灯）——
                // 这里只留前者。
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
            // 默认的 16pt 内缩是给整行大按钮留的。倍速档那种 36×26 的小方块用它，
            // 标签框会被压成 4px 宽 —— 字全被挤没。小按钮必须自己给内缩。
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
        /// 全白且零时长的颜色过渡。状态反馈一律交给 <see cref="UiMotion"/> 和面板本体，
        /// 不走 ColorBlock —— ColorBlock 是**乘算**，会把烘进顶点的渐变和受光带一起压平，
        /// 结果状态越多的按钮越糊。
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
