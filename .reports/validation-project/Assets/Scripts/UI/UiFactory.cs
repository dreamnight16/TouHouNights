using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI 构建工厂 v3：运行时纯代码创建 Canvas / EventSystem / Image / Text / Button / 进度条。
    /// 几何采用圆角九宫格（方舟/Fluent 式卡片），材质采用 Fluent 亚克力（半透明+噪点+顶部高光+底部内阴影）。
    /// 字体使用系统自带中文字体（Windows 下 Microsoft YaHei），避免外部字体资源；不使用伪粗体。
    /// </summary>
    public static class UiFactory
    {
        /// <summary>
        /// 是否使用 TextMeshPro（SDF 矢量字体）。当前**默认关闭**：
        /// TMP 需要把字形逐字烘焙进自有图集，从动态 OS 字体取中文时 96pt@1024² 图集只够约 80 个汉字，
        /// 界面中文一多就溢出变成方框。为「先保证能显示中文」，暂用 legacy Text（走系统 DirectWrite，
        /// 任何中文都能排出来，代价是稍糊）。评级字母的居中不依赖此开关（legacy/TMP 皆有测量逻辑）。
        /// 待导入正式中文字体资源（.ttf → TMP 字体资产）后再置 true 即可获得 SDF 锐利中文。
        /// </summary>
        public static bool UseTextMeshPro = true;  // 优先 TMP；是否真的可用由 UiFont.TmpReady 决定（中文字形实测通过才算可用）

        private static Font _font;
        private static TMP_FontAsset _tmpFont;
        private static TMP_FontAsset _serifTmpFont;
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
                    // 交给 UiFont：逐个候选实测 HasCharacter(中)，拿不到真中文字体就降级。
                    _font = UiFont.Cjk;
                }
                return _font;
            }
        }

        private static Font TryLoadFont(string name)
        {
            try
            {
                // 用较大基准字号创建动态字体，避免小字号下字形被拉伸导致文字发糊。
                return Font.CreateDynamicFontFromOSFont(name, 64);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 衬线拉丁字体资产：本想走 Georgia / Times New Roman 衬线体营造史诗感，
        /// 但 Font.CreateDynamicFontFromOSFont 在 Windows 下返回的 Font 对象 TMP 无法烘焙其 face
        /// （控制台刷 "Unable to load font face" 且字形缺失）。
        /// 项目内亦无衬线 .ttf 资源，故直接返回 null；TMP 会 fallback 到默认字体资产，无警告。
        /// </summary>
        public static TMP_FontAsset SerifTmpFont
        {
            get
            {
                // 衬线体暂不可用；调用方应判断 null 后跳过 SetFont。
                return null;
            }
        }

        /// <summary>
        /// TextMeshPro 动态字体资产（SDF 渲染：矢量抗锯齿，中文字形按需写入 atlas）。
        /// 失败时返回 null，UiText 自动回退 legacy Text。
        /// </summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null)
                {
                    _tmpFont = UiFont.TmpCjk;   // 2048 图集 + 动态填充 + 多图集，中文按需入集
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
            // 锁定宽度（match width = 0）：画布宽度永远 = 1280，整体随屏幕宽度等比缩放。
            // 这样为 1280 宽设计的顶/底栏布局永远成立——bar 内容不会被摊开或被推到画布外，
            // 顶栏永远在画布上缘、底栏永远在下缘。宽屏下只是中间战场区变矮，绝不裁切、不飘移。
            // （之前用 Expand 在自由比例下会把画布拉得过大，bar 内容被摊开，显得「乱」。）
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
        /// 创建矢量圆角面板（RoundedRectImage：顶点级圆角+描边，任意缩放物理级锐利，永不糊）。
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

            // 矢量按钮：小圆角+细描边+投影，与卡片圆角系统统一（解决「直角太硬」的粗糙感）
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
                highlightedColor = Color.Lerp(bgColor, new Color(0.78f, 0.92f, 1f), 0.28f), // 微蓝高亮，反馈更明显
                pressedColor = Color.Lerp(bgColor, new Color(0.03f, 0.06f, 0.10f), 0.38f),  // 深压，手感更实
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
