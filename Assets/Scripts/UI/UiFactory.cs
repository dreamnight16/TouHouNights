using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI 构建工厂：运行时纯代码创建 Canvas / EventSystem / Image / Text / Button / 进度条。
    /// 字体使用系统自带中文字体（Windows 下 Microsoft YaHei），避免外部字体资源。
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = TryLoadFont("Microsoft YaHei")
                         ?? TryLoadFont("SimHei")
                         ?? TryLoadFont("Microsoft JhengHei")
                         ?? TryLoadFont("Arial")
                         ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        private static Font TryLoadFont(string name)
        {
            try
            {
                return Font.CreateDynamicFontFromOSFont(name, 16);
            }
            catch
            {
                return null;
            }
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>创建圆角（Fluent 式）Image：白色圆角精灵 + Sliced 拉伸 + 颜色染色。</summary>
        public static Image CreateRoundedImage(Transform parent, string name, Color color)
        {
            var image = CreateImage(parent, name, color);
            image.sprite = SpriteFactory.RoundedRect();
            image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>
        /// 方舟式「1px 冷描边面板」：外层是描边色，内层面板底自动内缩 1px。
        /// 返回外层（描边）便于定位。
        /// </summary>
        public static Image CreatePanel(Transform parent, string name)
        {
            var border = CreateRoundedImage(parent, name + "_Border", UiTheme.PanelLine);
            var fill = CreateRoundedImage(border.transform, name, UiTheme.PanelBg);
            Inset(fill.rectTransform, 1f);
            return border;
        }

        /// <summary>把子矩形内缩 amount 像素（相对父），用于描边面板的内层。</summary>
        public static void Inset(RectTransform rect, float amount)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(amount, amount);
            rect.offsetMax = new Vector2(-amount, -amount);
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color bgColor, int fontSize, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = SpriteFactory.RoundedRect();
            image.type = Image.Type.Sliced;
            image.color = bgColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = bgColor,
                highlightedColor = Color.Lerp(bgColor, Color.white, 0.18f),
                pressedColor = Color.Lerp(bgColor, Color.black, 0.18f),
                selectedColor = bgColor,
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.15f,
            };

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            var labelRect = CreateText(go.transform, "Label", label, fontSize, UiTheme.Ink).rectTransform;
            Stretch(labelRect);
            return button;
        }

        /// <summary>进度条：返回背景与填充（填充挂在背景下，用 Filled 实现）。</summary>
        public static (Image background, Image fill) CreateProgressBar(Transform parent, string name, Color fillColor, Color bgColor)
        {
            var background = CreateImage(parent, name, bgColor);

            var fill = CreateImage(background.transform, "Fill", fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            Stretch(fill.rectTransform);

            return (background, fill);
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        public static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
