using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefense.UI
{
    /// <summary>
    /// 统一封装 TextMeshPro 和 legacy Text，提供内容、颜色和字号接口。
    /// TMP 创建失败时回退到 legacy Text。
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

        /// <summary>使用 UiFont 提供的字体创建 legacy Text。</summary>
        internal static UiText CreateLegacy(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var legacy = go.AddComponent<Text>();
            legacy.font = UiFactory.Font;
            legacy.fontStyle = FontStyle.Normal;
            legacy.alignment = TextAnchor.MiddleCenter;
            legacy.raycastTarget = false;
            legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
            legacy.verticalOverflow = VerticalWrapMode.Overflow;

            var ui = go.AddComponent<UiText>();
            ui._legacy = legacy;
            return ui;
        }

        /// <summary>创建 TextMeshPro 文本，字体由 UiFactory.TmpFont 提供。</summary>
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

        /// <summary>设置纵向顶点渐变；legacy Text 分支忽略此设置。</summary>
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
        /// 自动缩放字号。wrap 启用多行截断；TMP 单行使用 Overflow，
        /// 避免行高略超出定位框时，Ellipsis 隐藏整行文字。
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
            if (IsTmp || _legacy == null) return;
            var shadow = _legacy.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = offset;
        }
    }
}
