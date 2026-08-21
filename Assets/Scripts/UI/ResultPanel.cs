using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// 结算面板（东方 stage 结束风格 + Phigros 评级）：展示评级大字、评分与战绩，含「再来一局」。
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private Text _gradeText;
        private Text _titleText;
        private Text _ratingText;
        private Text _statsText;

        public void Build(Transform canvasRoot)
        {
            _root = new GameObject("ResultPanel", typeof(RectTransform));
            _root.transform.SetParent(canvasRoot, false);

            // 全屏遮罩，阻挡点击。
            var overlay = UiFactory.CreateImage(_root.transform, "Overlay", UiTheme.Overlay);
            UiFactory.SetStretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = UiFactory.CreateImage(_root.transform, "Box", UiTheme.PanelBg);
            UiFactory.SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 440f));

            _gradeText = UiFactory.CreateText(box.transform, "Grade", "S", UiTheme.FontSizeGrade, UiTheme.Accent);
            UiFactory.SetRect(_gradeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(400f, 120f));

            _titleText = UiFactory.CreateText(box.transform, "Title", "胜利！", UiTheme.FontSizeTitle, UiTheme.TextPrimary);
            UiFactory.SetRect(_titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(400f, 40f));

            _ratingText = UiFactory.CreateText(box.transform, "Rating", "评分 0", UiTheme.FontSize, UiTheme.Accent);
            UiFactory.SetRect(_ratingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(400f, 32f));

            _statsText = UiFactory.CreateText(box.transform, "Stats", string.Empty, UiTheme.FontSize, UiTheme.TextPrimary);
            UiFactory.SetRect(_statsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(460f, 150f));

            var restart = UiFactory.CreateButton(box.transform, "Restart", "再来一局", UiTheme.ButtonBg, UiTheme.FontSize, RestartGame);
            UiFactory.SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(220f, 52f));

            _root.SetActive(false);
        }

        public void Show(GameResult result)
        {
            if (_root == null) return;

            _root.SetActive(true);

            _gradeText.text = result.Grade;
            _gradeText.color = GradeColor(result.Grade);

            _titleText.text = result.Victory ? "胜利！" : "游戏失败";

            _ratingText.text = $"评分 {result.Rating}";

            _statsText.text =
                $"得分  {result.Score}\n" +
                $"击杀  {result.TotalKills}\n" +
                $"漏怪  {result.LeakedEnemies}\n" +
                $"剩余生命  {result.LivesRemaining}\n" +
                $"通关波次  {result.WavesCleared}/{result.TotalWaves}";
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private static void RestartGame()
        {
            GameManager.Instance?.Restart();
        }

        private static Color GradeColor(string grade)
        {
            switch (grade)
            {
                case "Φ": return new Color(1f, 0.85f, 0.30f);
                case "V": return new Color(0.55f, 0.85f, 1f);
                case "S": return new Color(0.35f, 1f, 0.55f);
                case "A": return new Color(0.45f, 0.70f, 1f);
                case "B": return new Color(1f, 0.70f, 0.40f);
                default: return new Color(0.65f, 0.68f, 0.75f);
            }
        }
    }
}
