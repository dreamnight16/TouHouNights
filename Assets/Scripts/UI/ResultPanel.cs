using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// 结算面板（Phigros 式评级 + 东方 stage 战绩）：巨型彩色评级字母（带回声）、
    /// 同色细线、双列战绩、成就徽章与「再来一局」。
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private CanvasGroup _canvasGroup;
        private Text _echoText;
        private Text _gradeText;
        private Image _rule;
        private Text _titleText;
        private Text _ratingText;
        private Text _badgeText;
        private Text _labelText;
        private Text _valueText;

        public void Build(Transform canvasRoot)
        {
            _root = new GameObject("ResultPanel", typeof(RectTransform));
            _root.transform.SetParent(canvasRoot, false);
            UiFactory.Stretch(_root.GetComponent<RectTransform>());
            _canvasGroup = _root.AddComponent<CanvasGroup>();

            // 全屏遮罩，阻挡点击。
            var overlay = UiFactory.CreateImage(_root.transform, "Overlay", UiTheme.Overlay);
            UiFactory.SetStretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 左右分栏面板（左评级 / 右战报）
            var box = UiFactory.CreatePanel(_root.transform, "Box");
            UiFactory.SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 420f));
            var fill = box.transform;

            // 左：评级
            _echoText = UiFactory.CreateText(fill, "GradeEcho", "S", 170, new Color(1f, 1f, 1f, 0.10f));
            UiFactory.SetRect(_echoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, 55f), new Vector2(280f, 170f));

            _gradeText = UiFactory.CreateText(fill, "Grade", "S", UiTheme.FontSizeGrade, UiTheme.Accent);
            UiFactory.SetRect(_gradeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, 55f), new Vector2(280f, 170f));

            _rule = UiFactory.CreateImage(fill, "Rule", UiTheme.Accent);
            _rule.raycastTarget = false;
            UiFactory.SetRect(_rule.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, -35f), new Vector2(240f, 2f));

            _titleText = UiFactory.CreateText(fill, "Title", "胜利！", UiTheme.FontSizeTitle, UiTheme.Ink);
            UiFactory.SetRect(_titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, -75f), new Vector2(280f, 36f));

            _ratingText = UiFactory.CreateText(fill, "Rating", "评分 0", UiTheme.FontSize, UiTheme.Accent);
            UiFactory.SetRect(_ratingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, -110f), new Vector2(280f, 28f));

            _badgeText = UiFactory.CreateText(fill, "Badge", string.Empty, UiTheme.FontSizeSmall, UiTheme.Gold);
            UiFactory.SetRect(_badgeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, -142f), new Vector2(280f, 24f));

            // 中：分隔线
            var divider = UiFactory.CreateImage(fill, "Divider", UiTheme.PanelLine);
            divider.raycastTarget = false;
            UiFactory.SetRect(divider.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 300f));

            // 右：战报
            var header = UiFactory.CreateText(fill, "Header", "战报", UiTheme.FontSizeTitle, UiTheme.InkDim);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(160f, 120f), new Vector2(240f, 30f));

            _labelText = UiFactory.CreateText(fill, "Labels", string.Empty, UiTheme.FontSize, UiTheme.InkDim, TextAnchor.MiddleLeft);
            UiFactory.SetRect(_labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(95f, 15f), new Vector2(100f, 160f));

            _valueText = UiFactory.CreateText(fill, "Values", string.Empty, UiTheme.FontSize, UiTheme.Ink, TextAnchor.MiddleRight);
            UiFactory.SetRect(_valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220f, 15f), new Vector2(140f, 160f));

            var restart = UiFactory.CreateButton(fill, "Restart", "再来一局", UiTheme.Accent, UiTheme.FontSize, RestartGame);
            var label = restart.GetComponentInChildren<Text>();
            if (label != null) label.color = new Color(0.02f, 0.04f, 0.06f, 1f); // 强调色按钮上用深色文字
            UiFactory.SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -178f), new Vector2(240f, 52f));

            _root.SetActive(false);
        }

        public void Show(GameResult result)
        {
            if (_root == null) return;

            _root.SetActive(true);
            _canvasGroup.alpha = 0f;

            var color = GradeColor(result.Grade);

            _echoText.text = result.Grade;
            _echoText.color = new Color(color.r, color.g, color.b, 0.10f);

            _gradeText.text = result.Grade;
            _gradeText.color = color;

            _rule.color = color;

            _titleText.text = result.Victory ? "胜利！" : "游戏失败";

            _ratingText.text = $"评分 {result.Rating}";

            string badges = string.Empty;
            if (result.LeakedEnemies == 0) badges = "零漏怪";
            if (result.LivesRemaining >= GameConfig.StartingLives) badges = AppendBadge(badges, "满血通关");
            _badgeText.text = badges;

            _labelText.text = "得分\n击杀\n漏怪\n剩余生命";
            _valueText.text =
                $"{result.Score}\n" +
                $"{result.TotalKills}\n" +
                $"{result.LeakedEnemies}\n" +
                $"{result.LivesRemaining}";

            StopAllCoroutines();
            StartCoroutine(FadeIn());
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
            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 结算时 timeScale=0，用 unscaledDeltaTime 才能播放动画。
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private static string AppendBadge(string current, string badge)
        {
            return current.Length > 0 ? current + "  ·  " + badge : badge;
        }

        private static void RestartGame()
        {
            GameManager.Instance?.Restart();
        }

        private static Color GradeColor(string grade)
        {
            switch (grade)
            {
                case "Φ": return UiTheme.GradePhi;
                case "V": return UiTheme.GradeV;
                case "S": return UiTheme.GradeS;
                case "A": return UiTheme.GradeA;
                case "B": return UiTheme.GradeB;
                default: return UiTheme.GradeC;
            }
        }
    }
}
