using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Effects;

namespace TowerDefense.UI
{
    /// <summary>
    /// 运行时创建 Canvas、事件系统、文本和基础控件。字体由 UiFont 提供。
    /// </summary>
    public static class UiFactory
    {
        /// <summary>
        /// 优先使用 TextMeshPro；字体不可用时回退到 legacy Text。
        /// </summary>
        public static bool UseTextMeshPro = true;

        private static Font _font;
        private static TMP_FontAsset _tmpFont;
        private static Material _acrylicMaterial;

        /// <summary>程序化亚克力材质（顶光/底影/磨砂噪声/边缘发光），所有面板与按钮共享同一实例。</summary>
        public static Material AcrylicMaterial
        {
            get
            {
                if (_acrylicMaterial == null)
                {
                    var shader = Resources.Load<Shader>("UI_Acrylic");
                    if (shader != null)
                    {
                        _acrylicMaterial = new Material(shader);
                    }
                }
                return _acrylicMaterial;
            }
        }

        /// <summary>给 UI 图元应用亚克力材质（找不到 Shader 时安全跳过）。</summary>
        public static void ApplyAcrylicMaterial(Image image)
        {
            if (image == null) return;
            if (AcrylicMaterial == null) return;
            image.material = AcrylicMaterial;
        }

        /// <summary>每帧把相机模糊后的屏幕绑定到亚克力材质（由 HUD Update 驱动）。</summary>
        public static void RefreshBlurTexture()
        {
            if (_acrylicMaterial == null) return;
            var rt = ScreenBlurFx.Blurred;
            _acrylicMaterial.SetTexture("_BlurTex", rt != null ? rt : Texture2D.whiteTexture);
        }

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    // UiFont 优先使用项目字体，再检查系统中文字体。
                    _font = UiFont.Cjk;
                }
                return _font;
            }
        }

        /// <summary>
        /// 衬线字体尚未提供，返回 null。UiText.SetFont 会保留现有字体。
        /// </summary>
        public static TMP_FontAsset SerifTmpFont
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// TextMeshPro 动态字体资产，中文字形按需写入图集。
        /// 失败时返回 null，UiText 自动回退 legacy Text。
        /// </summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null)
                {
                    _tmpFont = UiFont.TmpCjk;
                }
                return _tmpFont;
            }
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = TdTheme.ReferenceResolution;
            // 默认按宽度缩放；具体界面可覆盖参考分辨率和匹配模式。
            scaler.matchWidthOrHeight = 0f;

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

        /// <summary>
        /// 创建带描边的圆角面板，几何由 RoundedRectImage 生成。
        /// 自动应用亚克力材质（毛玻璃/顶光/底影/磨砂由 shader 负责）。
        /// </summary>
        public static RoundedRectImage CreateVectorPanel(Transform parent, string name, float radius, Color bg, Color border, float borderWidth)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var panel = go.AddComponent<RoundedRectImage>();
            panel.cornerRadius = radius;
            panel.color = bg;
            panel.borderWidth = borderWidth;
            panel.borderColor = border;
            ApplyAcrylicMaterial(panel);

            // 投影：面板浮于背景之上，形成层次
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = TdTheme.ShadowColor;
            shadow.effectDistance = TdTheme.ShadowDistance;

            return panel;
        }

        /// <summary>
        /// 创建文本：TextMeshPro（SDF，矢量抗锯齿）优先，TMP 不可用时自动回退 legacy Text。
        /// 返回 UiText 统一封装，调用方通过 content/color/fontSize 访问。
        /// </summary>
        public static UiText CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool shadow = false)
        {
            var text = (UseTextMeshPro && UiFont.TmpReady)
                ? UiText.CreateTmp(parent, name)
                : UiText.CreateLegacy(parent, name);
            text.content = content;
            text.fontSize = fontSize;
            text.color = color;
            text.SetAlignment(alignment);

            if (shadow)
            {
                text.AddLegacyShadow(new Vector2(1.5f, -1.5f), new Color(0f, 0f, 0f, 0.55f));
            }
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color bgColor, int fontSize, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            // 与旧版圆角卡片使用相同几何和材质。
            var image = go.AddComponent<RoundedRectImage>();
            image.color = bgColor;
            image.cornerRadius = TdTheme.Radius.Sm;
            image.borderWidth = 1.2f;
            image.borderColor = new Color(1f, 1f, 1f, 0.40f);
            ApplyAcrylicMaterial(image);

            // 投影：增加深度层次，让按钮从背景中浮起
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = TdTheme.ShadowColor;
            shadow.effectDistance = TdTheme.ShadowDistance;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = bgColor,
                highlightedColor = Color.Lerp(bgColor, new Color(0.78f, 0.92f, 1f), 0.28f),
                pressedColor = Color.Lerp(bgColor, new Color(0.03f, 0.06f, 0.10f), 0.38f),
                selectedColor = bgColor,
                disabledColor = new Color(0.22f, 0.26f, 0.30f, 0.40f),
                colorMultiplier = 1f,
                fadeDuration = 0.10f,
            };

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            if (!string.IsNullOrEmpty(label))
            {
                var labelRect = CreateText(go.transform, "Label", label, fontSize, TdTheme.Ink, TextAnchor.MiddleCenter, true).rectTransform;
                Stretch(labelRect);
            }

            // 点击回弹动效（Fluent 弹簧）
            UiButtonFx.Attach(button);
            return button;
        }

        /// <summary>进度条（矢量）：背景带细描边，填充为矢量 Progress 裁剪（无 bit-map 填充）。</summary>
        public static (Image background, RoundedRectImage fill) CreateProgressBar(Transform parent, string name, Color fillColor, Color bgColor)
        {
            var background = CreateVectorPanel(parent, name, 0f, bgColor, new Color(1f, 1f, 1f, 0.20f), 1f);

            var fill = CreateVectorPanel(background.transform, "Fill", 0f, fillColor, fillColor, 0f);
            fill.Progress = 0f;
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
