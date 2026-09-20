using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 符卡评级结算屏（结界终端）：
    /// 虚空暗角 + 玻璃核 + 评级大字（衬线）环绕评分环 + 战报四行 + 再来一战。
    /// 东方世界观文案：结界守成 / 结界破碎 / 符卡阵展开。
    /// </summary>
    public sealed class TerminalResult : MonoBehaviour
    {
        private GameObject _root;
        private CanvasGroup _canvasGroup;
        private RectTransform _boxRect;

        private Image _gradeGlow;
        private UiText _echoText;
        private UiText _gradeText;
        private RadialProgressImage _ratingRing;
        private UiText _titleText;
        private UiText _subTitleText;
        private UiText _badgeText;
        private UiText _ratingText;
        private UiText[] _labelRows;
        private UiText[] _valueRows;
        private UiText _recordText;
        private Vector2 _ringCenter;   // 评级环中心（box-local），字母按字形光学中心贴齐这里

        // 战报四行：行首 标签 + 行尾 值，逐行用同一受控行高对齐（整块两种字号混排会逐行错位）。
        private static readonly string[] RowLabels = { "得分", "击破", "漏怪", "剩余结界", "最佳连击" };

        public void Build(Transform canvasRoot)
        {
            _root = new GameObject("TerminalResult", typeof(RectTransform));
            _root.transform.SetParent(canvasRoot, false);
            UiFactory.Stretch(_root.GetComponent<RectTransform>());
            _canvasGroup = _root.AddComponent<CanvasGroup>();

            var overlay = UiFactory.CreateImage(_root.transform, "Overlay", new Color(0f, 0f, 0f, 0.76f));
            UiFactory.SetStretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var vignette = UiFactory.CreateImage(_root.transform, "Vignette", new Color(0f, 0f, 0f, 0.55f));
            vignette.raycastTarget = false;
            vignette.sprite = SpriteFactory.Vignette(new Color(0f, 0f, 0f, 1f));
            UiFactory.SetStretch(vignette.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = TdKit.Glass(_root.transform, "Box", 14f, TdTheme.GlassLight, TdTheme.EdgeStrong, 1f);
            UiFactory.SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(720f, 470f));
            _boxRect = box.rectTransform;

            var fill = box.transform;
            var ringCenter = new Vector2(-175f, 10f);
            _ringCenter = ringCenter;

            _gradeGlow = TdKit.Glow(fill, "GradeGlow", new Color(1f, 1f, 1f, 0f));
            UiFactory.SetRect(_gradeGlow.rectTransform, new Vector2(0.5f, 0.5f), ringCenter, new Vector2(430f, 430f));

            _ratingRing = TdKit.Ring(fill, "RatingRing", 112f, 4f, TdTheme.Star, new Color(1f, 1f, 1f, 0.08f));
            UiFactory.SetRect(_ratingRing.rectTransform, new Vector2(0.5f, 0.5f), ringCenter, new Vector2(244f, 244f));

            // 大字评级字母与回声：legacy Text 的 MiddleCenter 以「行盒」对齐而非字形本身，
            // 大写字母无下延部，字形光学中心会偏离环心。这里先按环心放置，待 Show 得知具体评级
            // 角色后再用 CenterGradeGlyph 依据渲染出的真实字形包围盒做一次精确纵移，保证字母正对圆心。
            var letterCenter = ringCenter;

            _echoText = TdKit.Text(fill, "GradeEcho", "S", 172, new Color(1f, 1f, 1f, 0.08f), TextAnchor.MiddleCenter, false);
            _echoText.SetFont(UiFactory.SerifTmpFont);
            UiFactory.SetRect(_echoText.rectTransform, new Vector2(0.5f, 0.5f), letterCenter, new Vector2(300f, 210f));

            _gradeText = TdKit.Text(fill, "Grade", "S", TdTheme.GradeSize, TdTheme.Star, TextAnchor.MiddleCenter, true);
            _gradeText.SetFont(UiFactory.SerifTmpFont);
            UiFactory.SetRect(_gradeText.rectTransform, new Vector2(0.5f, 0.5f), letterCenter, new Vector2(300f, 180f));

            _ratingText = TdKit.Text(fill, "Rating", string.Empty, TdTheme.Small, TdTheme.InkDim, TextAnchor.MiddleCenter, true);
            UiFactory.SetRect(_ratingText.rectTransform, new Vector2(0.5f, 0.5f), ringCenter + new Vector2(0f, -110f), new Vector2(280f, 20f));

            _titleText = TdKit.Text(fill, "Title", "结界守成", TdTheme.Huge, TdTheme.Ink, TextAnchor.MiddleCenter, true);
            UiFactory.SetRect(_titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-175f, -156f), new Vector2(300f, 40f));

            _subTitleText = TdKit.Text(fill, "SubTitle", string.Empty, TdTheme.Small, TdTheme.InkDim, TextAnchor.MiddleCenter, true);
            UiFactory.SetRect(_subTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-175f, -190f), new Vector2(300f, 24f));

            // 徽章行：居中的长徽章会伸进右侧「再来一战」按钮 → 改为左对齐并收窄，使其整个落在左栏内。
            _badgeText = TdKit.Text(fill, "Badge", string.Empty, TdTheme.Small, TdTheme.Spirit, TextAnchor.MiddleLeft, true);
            UiFactory.SetRect(_badgeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-220f, -216f), new Vector2(200f, 22f));

            var divider = UiFactory.CreateImage(fill, "Divider", new Color(1f, 1f, 1f, 0.10f));
            divider.raycastTarget = false;
            UiFactory.SetRect(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-8f, -10f), new Vector2(2f, 330f));

            var header = TdKit.Text(fill, "Header", "结界战报", TdTheme.Huge, TdTheme.InkDim, TextAnchor.MiddleCenter, true);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(172f, 156f), new Vector2(260f, 34f));

            var headerEng = TdKit.Text(fill, "HeaderEng", "BARRIER REPORT", TdTheme.Tiny, TdTheme.InkFaint, TextAnchor.MiddleCenter, true);
            headerEng.SetFont(UiFactory.SerifTmpFont);
            headerEng.SetCharacterSpacing(6f);
            UiFactory.SetRect(headerEng.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(172f, 130f), new Vector2(260f, 16f));

            var headerRule = UiFactory.CreateImage(fill, "HeaderRule", TdTheme.Star);
            headerRule.raycastTarget = false;
            UiFactory.SetRect(headerRule.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(172f, 112f), new Vector2(64f, 2f));

            // 战报四行：行首「标签」+ 行尾「值」逐行对齐。值列右缘收到玻璃核内（x=246 → 右缘 316，留 44px 内缩）。
            _labelRows = new UiText[RowLabels.Length];
            _valueRows = new UiText[RowLabels.Length];
            const float rowStride = 36f;
            const float rowY0 = 26f;
            for (int i = 0; i < RowLabels.Length; i++)
            {
                float rowY = rowY0 - i * rowStride;

                var label = TdKit.Text(fill, "Label" + i, RowLabels[i], TdTheme.Body, TdTheme.InkDim, TextAnchor.MiddleLeft, true);
                UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(96f, rowY), new Vector2(120f, 34f));
                _labelRows[i] = label;

                var value = TdKit.Text(fill, "Value" + i, string.Empty, TdTheme.Big, TdTheme.Ink, TextAnchor.MiddleRight, true);
                value.SetVerticalGradient(new Color(0.97f, 0.98f, 1.00f, 1f), new Color(0.52f, 0.60f, 0.72f, 1f));
                UiFactory.SetRect(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(246f, rowY), new Vector2(140f, 34f));
                _valueRows[i] = value;
            }

            _recordText = TdKit.Text(fill, "Record", string.Empty, TdTheme.Small, TdTheme.Spirit, TextAnchor.MiddleCenter, true);
            UiFactory.SetRect(_recordText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(360f, 22f));

            var retry = TdKit.Button(fill, "Retry", "再来一战", 10f, TdTheme.Star, RetryGame, TdTheme.Body, true);
            var retryLabel = retry.GetComponentInChildren<UiText>();
            if (retryLabel != null) retryLabel.color = TdTheme.OnStar;
            UiFactory.SetRect(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -204f), new Vector2(228f, 52f));

            _root.SetActive(false);
        }

        public void Show(GameResult result)
        {
            if (_root == null) return;

            _root.SetActive(true);
            _canvasGroup.alpha = 0f;

            var color = GradeColor(result.Grade);

            _echoText.text = result.Grade;
            _echoText.color = new Color(color.r, color.g, color.b, 0.08f);

            _gradeText.text = result.Grade;
            _gradeText.color = color;
            CenterGradeGlyph(result.Grade.Length > 0 ? result.Grade[0] : 'S');

            _gradeGlow.color = new Color(color.r, color.g, color.b, 0f);
            _ratingRing.fillColor = color;
            _ratingRing.Progress = 0f;

            _titleText.text = result.Victory ? "结界守成" : "结界破碎";
            _subTitleText.text = BuildSubTitle(result, color);
            _subTitleText.color = color;

            _ratingText.text = $"符卡评级  {result.Rating} / 100";
            _badgeText.text = BuildBadges(result);

            string[] values =
            {
                result.Score.ToString(),
                result.TotalKills.ToString(),
                result.LeakedEnemies.ToString(),
                result.LivesRemaining.ToString(),
                result.BestCombo.ToString(),
            };

            int bestRecord = GameManager.Instance != null ? GameManager.Instance.BestScore : result.Score;
            _recordText.text = result.NewRecord
                ? "结界新纪录 · " + result.Score
                : (bestRecord > 0 ? "结界纪录 · " + bestRecord : string.Empty);
            _recordText.color = result.NewRecord ? TdTheme.Spirit : TdTheme.InkFaint;
            for (int i = 0; i < RowLabels.Length; i++)
            {
                _labelRows[i].text = RowLabels[i];
                _valueRows[i].text = values[i];
            }

            StopAllCoroutines();
            StartCoroutine(FadeIn());
            StartCoroutine(SlideIn());
            StartCoroutine(CountUp(result));
            StartCoroutine(GradePop(color));
        }

        public void Hide()
        {
            if (_root == null) return;
            StopAllCoroutines();
            _canvasGroup.alpha = 0f;
            _root.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            const float duration = 0.26f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator SlideIn()
        {
            const float duration = 0.36f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = UiEasings.OutCubic(t);
                _boxRect.anchoredPosition = new Vector2(0f, -4f - (1f - e) * 30f);
                yield return null;
            }
            _boxRect.anchoredPosition = new Vector2(0f, -4f);
        }

        private IEnumerator GradePop(Color color)
        {
            _gradeText.rectTransform.localScale = Vector3.one * 0.15f;
            _echoText.rectTransform.localScale = Vector3.one * 0.4f;
            _ratingRing.rectTransform.localScale = Vector3.one * 0.7f;

            const float duration = 0.6f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float back = UiEasings.OutBack(t);
                _gradeText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, back);
                // 回声用 OutBack：自然过冲后回落到 1.0，避免 1.35 末端硬切到 1.0 的单帧跳变。
                _echoText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, UiEasings.OutBack(t));
                _ratingRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, UiEasings.OutBack(t));
                _gradeGlow.color = new Color(color.r, color.g, color.b, 0.26f * t);
                yield return null;
            }
            _gradeText.rectTransform.localScale = Vector3.one;
            _echoText.rectTransform.localScale = Vector3.one;
            _ratingRing.rectTransform.localScale = Vector3.one;
            _gradeGlow.color = new Color(color.r, color.g, color.b, 0.28f);
        }

        private IEnumerator CountUp(GameResult result)
        {
            const float total = 0.8f;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / total);

                int score = Mathf.RoundToInt(result.Score * UiEasings.OutCubic(k));
                int kills = Mathf.RoundToInt(result.TotalKills * UiEasings.OutCubic(Mathf.Clamp01(k * 1.35f - 0.10f)));
                int leaked = Mathf.RoundToInt(result.LeakedEnemies * UiEasings.OutCubic(Mathf.Clamp01(k * 1.35f - 0.22f)));
                int lives = Mathf.RoundToInt(result.LivesRemaining * UiEasings.OutCubic(Mathf.Clamp01(k * 1.35f - 0.34f)));

                UpdateValueRows(new[] { score, kills, leaked, lives, result.BestCombo });
                _ratingRing.Progress = Mathf.Clamp01((k * 1.25f - 0.08f) * (result.Rating / 100f));
                yield return null;
            }

            UpdateValueRows(new[] { result.Score, result.TotalKills, result.LeakedEnemies, result.LivesRemaining, result.BestCombo });
            _ratingRing.Progress = Mathf.Clamp01(result.Rating / 100f);
        }

        private void UpdateValueRows(int[] values)
        {
            for (int i = 0; i < _valueRows.Length && i < values.Length; i++)
            {
                _valueRows[i].text = values[i].ToString();
            }
        }

        private static string BuildSubTitle(GameResult result, Color color)
        {
            if (result.Victory)
            {
                return result.LeakedEnemies == 0 ? "符卡阵 · 完全展开" : "结界余波未消";
            }
            return "幻想之壁碎裂";
        }

        private static string BuildBadges(GameResult result)
        {
            string badges = string.Empty;
            if (result.LeakedEnemies == 0) badges = AppendBadge(badges, "零漏怪");
            if (result.LivesRemaining >= GameConfig.StartingLives) badges = AppendBadge(badges, "结界无伤");
            if (result.TotalKills >= GameConfig.TotalEnemies) badges = AppendBadge(badges, "全员击破");
            return badges.Length > 0 ? badges : "再接再厉";
        }

        /// <summary>
        /// 把评级大字及回声依据各自渲染出的真实字形中心对齐到评级环圆心。
        /// legacy Text 的 MiddleCenter 以「单行行盒」居中，而行盒=上升部+下降部，大写字母仅占上升部，
        /// 因此字体不同、字符不同、字号不同，字形光学中心偏离环心的量都不同——这里逐一对齐。
        /// </summary>
        private void CenterGradeGlyph(char glyph)
        {
            if (_gradeText != null)
            {
                float g = GradeGlyphCenterLift(glyph, _gradeText);
                _gradeText.rectTransform.anchoredPosition = new Vector2(_ringCenter.x, _ringCenter.y + g);
            }
            if (_echoText != null)
            {
                float e = GradeGlyphCenterLift(glyph, _echoText);
                _echoText.rectTransform.anchoredPosition = new Vector2(_ringCenter.x, _ringCenter.y + e);
            }
        }

        /// <summary>
        /// 让字形光学中心精确落在评级环圆心上的纵向位移（rect-local 单位，正值=上移）。
        /// legacy Text / TMP 的居中都以「单行行盒」为准（行盒=上升部+下降部），大写字母无下延部、中文字体
        /// 行盒又奇高，故字形光学中心与环心总有偏差——不再用字体度量猜测，而是直接测量已生成的真实字形
        /// 顶点包围盒：坐标以枢轴（pivot=0.5,0.5 → rect 中心=环心）为原点、y 向上，因此顶点包围盒 Y 中点
        /// 就是字形中心相对环心的偏移，取负即把字形正对圆心。
        /// 度量缺失/越界一律回退 0（保持原生居中）。注意这里只做物理兜底（是否仍在 rect 内），
        /// 不做字形级的小幅截断——以前用 fontSize*0.06 会把偏下的大校正错误截成 0，导致字母始终偏下。
        /// </summary>
        private static float GradeGlyphCenterLift(char glyph, UiText ui)
        {
            if (ui == null) return 0f;

            // ---- TMP 分支（SDF 矢量字体）：测量 textInfo 真实渲染顶点 ----
            if (ui.IsTmp)
            {
                var tmp = ui.GetComponent<TextMeshProUGUI>();
                if (tmp == null) return 0f;
                tmp.ForceMeshUpdate();
                var ti = tmp.textInfo;
                if (ti == null || ti.meshInfo == null || ti.meshInfo.Length == 0) return 0f;

                float minY = float.MaxValue, maxY = float.MinValue;
                bool any = false;
                for (int m = 0; m < ti.meshInfo.Length; m++)
                {
                    var v = ti.meshInfo[m].vertices;
                    if (v == null) continue;
                    for (int i = 0; i < v.Length; i++)
                    {
                        float y = v[i].y;
                        if (float.IsNaN(y)) continue;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        any = true;
                    }
                }
                if (!any || float.IsInfinity(minY) || float.IsInfinity(maxY)) return 0f;
                float glyphCenter = (minY + maxY) * 0.5f;
                if (Mathf.Abs(glyphCenter) > ui.rectTransform.rect.height * 0.5f) return 0f;
                // 物理兜底：校正幅度封顶在自身 rect 的 45%，只拦「失控位移」使它不至于滑出圆环，
                // 不改动态测量的真实中心量（旧版 fontSize*0.06 把有效的 ~6px 校正截成了 0，这正是字母纹丝不动的根因）。
                float bound = ui.rectTransform.rect.height * 0.45f;
                return Mathf.Clamp(-glyphCenter, -bound, bound);   // TMP 顶点已位于 rect-local（枢轴原点），无需再除以 pixelsPerUnit
            }

            // ---- legacy Text 分支：测量 TextGenerator 真实字形顶点 ----
            var text = ui.GetComponent<Text>();
            if (text == null || text.font == null) return 0f;

            // 按 OnPopulateMesh 完全一致的设置重新生成一次，拿到当前文本的真实字形顶点。
            // content 取当前文本；空时回退到传入的 grade 字符（度量的是实际渲染出的字，不是入参）。
            string content = text.text;
            if (string.IsNullOrEmpty(content)) content = glyph.ToString();
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            text.cachedTextGenerator.Populate(content, settings);
            var verts = text.cachedTextGenerator.verts;
            int count = verts.Count;
            if (count < 4) return 0f;

            // 全部顶点 Y 取包围盒中点（单行多字符同基线时纵移不受影响，取 min/max 依然正确）。
            float minY2 = float.MaxValue, maxY2 = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float y = verts[i].position.y;
                if (y < minY2) minY2 = y;
                if (y > maxY2) maxY2 = y;
            }
            float glyphCenter2 = (minY2 + maxY2) * 0.5f;
            if (float.IsNaN(glyphCenter2) || float.IsInfinity(glyphCenter2)) return 0f;

            // 顶点单位为「字面像素」，OnPopulateMesh 用 /pixelsPerUnit 换算成 rect-local 单位，
            // 故此处同步除以 pixelsPerUnit（动态字体=canvas.scaleFactor），再取负得到上移/下移量。
            float ppu = text.pixelsPerUnit;
            if (ppu <= 0f) ppu = 1f;
            float lift = -glyphCenter2 / ppu;

            if (Mathf.Abs(lift) > text.rectTransform.rect.height * 0.5f) return 0f;
            float bound2 = text.rectTransform.rect.height * 0.45f;
            return Mathf.Clamp(lift, -bound2, bound2);
        }

        private static string AppendBadge(string current, string badge)
        {
            return current.Length > 0 ? current + "  ·  " + badge : badge;
        }

        private static void RetryGame()
        {
            GameManager.Instance?.Restart();
        }

        private static Color GradeColor(string grade)
        {
            switch (grade)
            {
                case "Φ": return TdTheme.GradePhi;
                case "V": return TdTheme.GradeV;
                case "S": return TdTheme.GradeS;
                case "A": return TdTheme.GradeA;
                case "B": return TdTheme.GradeB;
                default: return TdTheme.GradeC;
            }
        }
    }
}
