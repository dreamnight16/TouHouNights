using System;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI HUD：顶栏（生命/金币/波次/得分/P点进度条/状态 + 速度/索敌/弹幕/重开），
    /// 底栏（4 张塔卡片，选中高亮、金币不足置灰），以及结算面板。
    /// 运行时纯代码构建，不使用 prefab。
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private static readonly TowerType[] TowerOrder =
        {
            TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow
        };

        private Text _livesText;
        private Text _goldText;
        private Text _waveText;
        private Text _scoreText;
        private Text _stateText;
        private Image _powerFill;

        private Text _speedLabel;
        private Text _targetingLabel;
        private Text _barrageLabel;
        private Button _barrageButton;

        private sealed class TowerCard
        {
            public TowerType Type;
            public Button Button;
            public Image Bg;
            public Text Name;
            public Text Sub;
        }

        private readonly TowerCard[] _cards = new TowerCard[4];
        private ResultPanel _resultPanel;
        private bool _resultShown;

        private void Awake()
        {
            BuildTopBar();
            BuildBottomBar();
            BuildResultPanel();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            RefreshTopBar(gm);
            RefreshCards(gm);
            RefreshResult(gm);
        }

        // ---- 顶栏 ----

        private void BuildTopBar()
        {
            var top = UiFactory.CreateImage(transform, "TopBar", UiTheme.PanelBg);
            UiFactory.SetStretch(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -UiTheme.TopBarHeight), Vector2.zero);

            _livesText = CreateStat(top.transform, "Lives", 16f, 72f);
            _goldText = CreateStat(top.transform, "Gold", 90f, 84f);
            _waveText = CreateStat(top.transform, "Wave", 176f, 110f);
            _scoreText = CreateStat(top.transform, "Score", 290f, 100f);

            var powerLabel = UiFactory.CreateText(top.transform, "PowerLabel", "P点", UiTheme.FontSize, UiTheme.TextDim, TextAnchor.MiddleLeft);
            SetLeftAnchor(powerLabel.rectTransform, 392f, 44f);

            var powerBar = UiFactory.CreateProgressBar(top.transform, "PowerBar", UiTheme.PowerBar, UiTheme.PowerBarBg);
            SetCenterAnchor(powerBar.background.rectTransform, new Vector2(0f, 1f), new Vector2(500f, -28f), new Vector2(110f, 14f));
            _powerFill = powerBar.fill;

            _stateText = CreateStat(top.transform, "State", 568f, 120f);

            _speedLabel = CreateTopButton(top.transform, "Speed", "速度 1x", 432f, 96f, ToggleSpeed, out _);
            _targetingLabel = CreateTopButton(top.transform, "Targeting", "索敌", 238f, 186f, CycleTargeting, out _);
            _barrageLabel = CreateTopButton(top.transform, "Barrage", "弹幕", 120f, 110f, TriggerBarrage, out _barrageButton);
            CreateTopButton(top.transform, "Restart", "重新开始", 16f, 96f, RestartGame, out _);
        }

        private Text CreateStat(Transform parent, string name, float x, float width)
        {
            var text = UiFactory.CreateText(parent, name, string.Empty, UiTheme.FontSize, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            SetLeftAnchor(text.rectTransform, x, width);
            return text;
        }

        private Text CreateTopButton(Transform parent, string name, string label, float rightMargin, float width, Action onClick, out Button button)
        {
            var b = UiFactory.CreateButton(parent, name, label, UiTheme.ButtonBg, UiTheme.FontSize, onClick);
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-rightMargin, -28f);
            rt.sizeDelta = new Vector2(width, 32f);
            button = b;
            return b.GetComponentInChildren<Text>();
        }

        // ---- 底栏 ----

        private void BuildBottomBar()
        {
            var bottom = UiFactory.CreateImage(transform, "BottomBar", UiTheme.PanelBg);
            UiFactory.SetStretch(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, UiTheme.BottomBarHeight));

            float total = TowerOrder.Length * UiTheme.TowerCardWidth + (TowerOrder.Length - 1) * UiTheme.TowerCardGap;
            float x0 = (UiTheme.ReferenceResolution.x - total) * 0.5f;

            for (int i = 0; i < TowerOrder.Length; i++)
            {
                var type = TowerOrder[i];
                var def = GameConfig.Towers[type];
                float centerX = x0 + UiTheme.TowerCardWidth * 0.5f + i * (UiTheme.TowerCardWidth + UiTheme.TowerCardGap) - UiTheme.ReferenceResolution.x * 0.5f;
                _cards[i] = BuildTowerCard(bottom.transform, type, def, new Vector2(centerX, UiTheme.BottomBarHeight * 0.5f));
            }
        }

        private TowerCard BuildTowerCard(Transform parent, TowerType type, TowerDefinition def, Vector2 anchoredPosition)
        {
            var card = new TowerCard { Type = type };

            var button = UiFactory.CreateButton(parent, def.DisplayName, string.Empty, UiTheme.CardBg, UiTheme.FontSizeCardSub, () => GameManager.Instance?.TowerPlacer.SelectTower(type));
            button.transition = Selectable.Transition.None; // 颜色由 RefreshCards 手动管理，避免与 ColorTint 打架

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(UiTheme.TowerCardWidth, UiTheme.TowerCardHeight);

            card.Button = button;
            card.Bg = button.GetComponent<Image>();

            var icon = UiFactory.CreateImage(button.transform, "Icon", def.Color);
            icon.raycastTarget = false; // 不拦截卡片按钮点击
            SetCenterAnchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(20f, 20f));

            card.Name = UiFactory.CreateText(button.transform, "Name", def.DisplayName, UiTheme.FontSizeCardTitle, UiTheme.TextPrimary);
            SetCenterAnchor(card.Name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(150f, 20f));

            card.Sub = UiFactory.CreateText(button.transform, "Sub", SubText(def), UiTheme.FontSizeCardSub, UiTheme.TextDim);
            SetCenterAnchor(card.Sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(150f, 16f));

            return card;
        }

        // ---- 结算 ----

        private void BuildResultPanel()
        {
            _resultPanel = gameObject.AddComponent<ResultPanel>();
            _resultPanel.Build(transform);
        }

        // ---- 刷新 ----

        private void RefreshTopBar(GameManager gm)
        {
            _livesText.text = $"生命 {gm.Lives}";
            _goldText.text = $"金币 {gm.Gold}";
            _waveText.text = $"波次 {gm.CurrentWave}/{gm.TotalWaves}";
            _scoreText.text = $"得分 {gm.Score}";
            _stateText.text = StateText(gm.State);
            _powerFill.fillAmount = gm.Power;

            _speedLabel.text = gm.IsDoubleSpeed ? "速度 2x" : "速度 1x";
            _targetingLabel.text = $"索敌：{TargetingText(gm.TargetingPriority)}";

            bool canBarrage = gm.CanBarrage && gm.State == GameState.Running;
            _barrageButton.interactable = canBarrage;
            _barrageLabel.text = gm.CanBarrage ? "弹幕射击" : $"弹幕 P {gm.Power:0.0}";
        }

        private void RefreshCards(GameManager gm)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                var def = GameConfig.Towers[card.Type];

                bool affordable = gm.Gold >= def.Cost;
                bool selected = gm.TowerPlacer.SelectedType == card.Type;

                card.Button.interactable = affordable;
                card.Bg.color = !affordable ? UiTheme.CardDisabled : selected ? UiTheme.CardSelected : UiTheme.CardBg;
                card.Name.color = affordable ? UiTheme.TextPrimary : UiTheme.TextDim;
                card.Sub.color = affordable ? UiTheme.TextDim : new Color(0.45f, 0.48f, 0.55f, 1f);
            }
        }

        private void RefreshResult(GameManager gm)
        {
            if (gm.State == GameState.Running)
            {
                if (_resultShown)
                {
                    _resultShown = false;
                    _resultPanel.Hide();
                }
                return;
            }

            if (!_resultShown && (gm.State == GameState.GameOver || gm.State == GameState.Victory))
            {
                _resultShown = true;
                _resultPanel.Show(gm.GetResult());
            }
        }

        // ---- 布局小工具 ----

        private static void SetLeftAnchor(RectTransform rect, float x, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -28f);
            rect.sizeDelta = new Vector2(width, 26f);
        }

        private static void SetCenterAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ToggleSpeed() => GameManager.Instance?.ToggleSpeed();
        private static void CycleTargeting() => GameManager.Instance?.CycleTargetingPriority();
        private static void TriggerBarrage() => GameManager.Instance?.TriggerBarrage();
        private static void RestartGame() => GameManager.Instance?.Restart();

        private static string StateText(GameState state)
        {
            switch (state)
            {
                case GameState.Running: return "进行中";
                case GameState.GameOver: return "游戏失败";
                case GameState.Victory: return "胜利";
                default: return state.ToString();
            }
        }

        private static string TargetingText(TargetingPriority priority)
        {
            switch (priority)
            {
                case TargetingPriority.Nearest: return "距离最近";
                case TargetingPriority.LowestHealth: return "血量最低";
                case TargetingPriority.ClosestToEnd: return "距终点最近";
                default: return priority.ToString();
            }
        }

        private static string SubText(TowerDefinition def)
        {
            if (def.Damage <= 0f)
            {
                return $"费用{def.Cost} 射程{def.RangeCells} 减速{Mathf.RoundToInt((1f - def.SlowFactor) * 100f)}%";
            }
            return $"费用{def.Cost} 射程{def.RangeCells} 伤害{Mathf.RoundToInt(def.Damage)}";
        }
    }
}
