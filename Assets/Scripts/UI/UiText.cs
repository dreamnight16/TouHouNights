using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefense.UI
{
    /// <summary>
    /// 文本统一封装：优先 TextMeshPro（SDF 场渲染——矢量级抗锯齿，彻底解决 legacy Text 的像素感），
    /// 中文字形走动态字体资产；若 TMP 初始化失败则自动回退 legacy Text，保证任何环境可用。
    /// 对外暴露与旧 Text 等价的最小接口（content/color/fontSize），调用方无感知。
    /// </summary>
    public sealed class UiText : MonoBehaviour
    {
        private TextMeshProUGUI _tmp;
        private Text _legacy;

        /// <summary>当前由 TMP 承载（false = legacy 回退）。</summary>
        public bool IsTmp => _tmp != null;

        /// <summary>矩形变换（与 Graphic.rectTransform 同构，方便既有调用）。</summary>
        public RectTransform rectTransform => (RectTransform)transform;

        public string content
        {
            get => _tmp != null ? _tmp.text : _legacy.text;
            set
            {
                if (_tmp != null) _tmp.text = value;
                else _legacy.text = value;
            }
        }

        /// <summary>与 legacy Text.text 同名的别名，方便既有调用点零改动迁移。</summary>
        public string text
        {
            get => content;
            set => content = value;
        }

        public Color color
        {
            get => _tmp != null ? _tmp.color : _legacy.color;
            set
            {
                if (_tmp != null) _tmp.color = value;
                else _legacy.color = value;
            }
        }

        public int fontSize
        {
            get => _tmp != null ? Mathf.RoundToInt(_tmp.fontSize) : _legacy.fontSize;
            set
            {
                if (_tmp != null) _tmp.fontSize = value;
                else _legacy.fontSize = value;
            }
        }

        /// <summary>legacy Text 分支（默认路径：无需任何资源、必然显示）。</summary>
        internal static UiText CreateLegacy(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var legacy = go.AddComponent<Text>();
            legacy.font = UiFactory.Font;
            legacy.fontStyle = FontStyle.Normal; // 禁用伪粗体（合成粗体是中文发糊元凶）
            legacy.alignment = TextAnchor.MiddleCenter;
            legacy.raycastTarget = false;
            legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
            legacy.verticalOverflow = VerticalWrapMode.Overflow;

            var ui = go.AddComponent<UiText>();
            ui._legacy = legacy;
            return ui;
        }

        /// <summary>TextMeshPro 分支（需项目已导入 TMP Essential Resources，否则渲染空白）。</summary>
        internal static UiText CreateTmp(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            try
            {
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.font = UiFactory.TmpFont;
                if (tmp.font == null) throw new System.Exception("TMP font asset unavailable");
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.richText = false;
                tmp.raycastTarget = false;
                tmp.fontSize = 16;

                var ui = go.AddComponent<UiText>();
                ui._tmp = tmp;
                return ui;
            }
            catch
            {
                // TMP 不可用：回退 legacy Text
                return CreateLegacy(parent, name);
            }
        }

        /// <summary>纵向顶点渐变（TMP 特性）：顶色→底色，标题/数值的精致质感。</summary>
        internal void SetVerticalGradient(Color top, Color bottom)
        {
            if (_tmp != null)
            {
                _tmp.enableVertexGradient = true;
                _tmp.colorGradient = new VertexGradient(top, top, bottom, bottom);
            }
        }

        /// <summary>切换字体资产（TMP 特性；legacy 回退时忽略）。</summary>
        internal void SetFont(TMP_FontAsset asset)
        {
            if (_tmp != null && asset != null)
            {
                _tmp.font = asset;
            }
        }

        /// <summary>字间距（TMP 特性；legacy 回退时忽略，单位 1/100 em）。</summary>
        internal void SetCharacterSpacing(float spacing)
        {
            if (_tmp != null)
            {
                _tmp.characterSpacing = spacing;
            }
        }

        internal void SetAlignment(TextAnchor anchor)
        {
            var tmp = _tmp;
            if (tmp == null)
            {
                _legacy.alignment = anchor;
                return;
            }

            switch (anchor)
            {
                case TextAnchor.UpperLeft: tmp.alignment = TextAlignmentOptions.TopLeft; break;
                case TextAnchor.UpperCenter: tmp.alignment = TextAlignmentOptions.Top; break;
                case TextAnchor.UpperRight: tmp.alignment = TextAlignmentOptions.TopRight; break;
                case TextAnchor.MiddleLeft: tmp.alignment = TextAlignmentOptions.Left; break;
                case TextAnchor.MiddleRight: tmp.alignment = TextAlignmentOptions.Right; break;
                case TextAnchor.LowerLeft: tmp.alignment = TextAlignmentOptions.BottomLeft; break;
                case TextAnchor.LowerCenter: tmp.alignment = TextAlignmentOptions.Bottom; break;
                case TextAnchor.LowerRight: tmp.alignment = TextAlignmentOptions.BottomRight; break;
                default: tmp.alignment = TextAlignmentOptions.Center; break;
            }
        }

        /// <summary>
        /// 让文字自动缩到框内。<paramref name="wrap"/> 为 true 时才允许多行并截断。
        ///
        /// 溢出模式必须跟着 <paramref name="wrap"/> 走：单行标签的矩形是**定位工具**而不是裁剪框，
        /// 而 TMP 的 Ellipsis 是拿「行高 > 框高」当溢出判定的 —— 框比行高矮几个像素，
        /// 整行就被直接丢掉，文字凭空消失，调用点看上去却毫无问题（这正是幕次编号、
        /// 灵力、弹幕威力三个大数字一起消失的原因：它们的框高都比行高矮了 4~8 像素）。
        /// 需要多行的场合才用 Ellipsis 截断；单行宁可溢出，也绝不静默丢字。
        /// </summary>
        internal void FitInBox(int minimumSize, bool wrap = false)
        {
            if (_tmp != null)
            {
                _tmp.fontSizeMax = _tmp.fontSize;
                _tmp.fontSizeMin = minimumSize;
                _tmp.enableAutoSizing = true;
                _tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                _tmp.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            }
            else
            {
                _legacy.resizeTextMaxSize = _legacy.fontSize;
                _legacy.resizeTextMinSize = minimumSize;
                _legacy.resizeTextForBestFit = true;
                _legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
                _legacy.verticalOverflow = wrap ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            }
        }

        internal void AddLegacyShadow(Vector2 offset, Color color)
        {
            if (IsTmp || _legacy == null) return; // TMP 由 SDF 渲染自带锐利边缘，无需位图阴影
            var shadow = _legacy.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = offset;
        }
    }
}
