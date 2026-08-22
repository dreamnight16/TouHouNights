using System;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// UGUI HUD（方舟式锐角工业风）：顶栏（生命/金币/敌人/波次/得分/P点进度条/状态 + 速度/索敌/弹幕/重开），
    /// 底栏（4 张塔卡片，1px 描边 + 色块 + 费用 + 射程/伤害），以及结算面板。
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
        private Text _enemyText;
        private Text _killText;
        private Text _towersText;
        private Image _powerFill;

        private Text _speedLabel;
        private Text _pauseLabel;
        private Text _targetingLabel;
        private Text _barrageLabel;
        private Button _barrageButton;

        private sealed class TowerCard
        {
            public TowerType Type;
            public Button Button;
            public Image Bg;
            public Image Border;
            public Text Name;
            public Text Sub;
        }

        private readonly TowerCard[] _cards = new TowerCard[4];
        private ResultPanel _resultPanel;
        private bool _resultShown;

        private GameObject _towerInfoRoot;
        private Text _towerInfoTitle;
        private Text _towerInfoStats;
        private Text _towerInfoRefund;
        private Button _towerInfoRetreat;

        private void Awake()
        {
            BuildTopBar();
            BuildBottomBar();
            BuildTowerInfoPanel();
            BuildResultPanel();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            RefreshTopBar(gm);
            RefreshCards(gm);
            RefreshTowerInfo(gm);
            RefreshResult(gm);
        }

        // ---- 顶栏 ----

        private void BuildTopBar()
        {
            var top = UiFactory.CreatePanel(transform, "TopBar");
            UiFactory.SetStretch(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -UiTheme.TopBarHeight), Vector2.zero);

            float cy = -UiTheme.TopBarHeight * 0.5f;

            _livesText = CreateStatChip(top.transform, "Lives", UiTheme.EnemyRed, 16f, 40f);
            _goldText = CreateStatChip(top.transform, "Gold", UiTheme.Gold, 90f, 48f);
            _enemyText = CreateStatChip(top.transform, "Enemy", UiTheme.EnemyRed, 172f, 44f);
            _killText = CreateStatChip(top.transform, "Kills", UiTheme.Accent, 252f, 82f);
            _towersText = CreateStatChip(top.transform, "Towers", UiTheme.GradeS, 368f, 60f);

            var powerLabel = UiFactory.CreateText(top.transform, "PowerLabel", "P", UiTheme.FontSizeSmall, UiTheme.InkDim, TextAnchor.MiddleLeft);
            SetLeftAnchor(powerLabel.rectTransform, 452f, 30f);

            var powerBar = UiFactory.CreateProgressBar(top.transform, "PowerBar", UiTheme.PowerBar, UiTheme.PowerBarBg);
            SetCenterAnchor(powerBar.background.rectTransform, new Vector2(0f, 1f), new Vector2(521f, cy), new Vector2(70f, 14f));
            _powerFill = powerBar.fill;

            _speedLabel = CreateTopButton(top.transform, "Speed", "速度 1x", 378f, 80f, CycleSpeed, out _);
            _pauseLabel = CreateTopButton(top.transform, "Pause", "暂停", 466f, 80f, TogglePause, out _);
            _targetingLabel = CreateTopButton(top.transform, "Targeting", "索敌", 220f, 150f, CycleTargeting, out _);
            _barrageLabel = CreateTopButton(top.transform, "Barrage", "弹幕", 112f, 100f, TriggerBarrage, out _barrageButton);
            CreateTopButton(top.transform, "Restart", "重新开始", 16f, 88f, RestartGame, out _);
        }

        private Text CreateStatChip(Transform parent, string name, Color iconColor, float x, float valueWidth)
        {
            var icon = UiFactory.CreateImage(parent, name + "_Icon", iconColor);
            icon.raycastTarget = false;
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(x, -UiTheme.TopBarHeight * 0.5f);
            irt.sizeDelta = new Vector2(12f, 12f);

            var value = UiFactory.CreateText(parent, name, string.Empty, UiTheme.FontSize, UiTheme.Ink, TextAnchor.MiddleLeft);
            SetLeftAnchor(value.rectTransform, x + 18f, valueWidth);
            return value;
        }

        private Text CreateTopButton(Transform parent, string name, string label, float rightMargin, float width, Action onClick, out Button button)
        {
            var b = UiFactory.CreateButton(parent, name, label, UiTheme.ButtonBg, UiTheme.FontSizeSmall, onClick);
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-rightMargin, -UiTheme.TopBarHeight * 0.5f);
            rt.sizeDelta = new Vector2(width, 32f);
            button = b;
            return b.GetComponentInChildren<Text>();
        }

        // ---- 底栏 ----

        private void BuildBottomBar()
        {
            var bottom = UiFactory.CreatePanel(transform, "BottomBar");
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

            // 外层 1px 描边
            var border = UiFactory.CreateRoundedImage(parent, def.DisplayName + "_Border", UiTheme.PanelLine);
            border.raycastTarget = false;
            var borderRect = border.rectTransform;
            borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.anchoredPosition = anchoredPosition;
            borderRect.sizeDelta = new Vector2(UiTheme.TowerCardWidth, UiTheme.TowerCardHeight);

            // 内层按钮（填充，内缩 1px）
            var button = UiFactory.CreateButton(border.transform, def.DisplayName, string.Empty, UiTheme.CardBg, UiTheme.FontSizeCardSub, () => GameManager.Instance?.TowerPlacer.SelectTower(type));
            button.transition = Selectable.Transition.None;
            UiFactory.Inset(button.GetComponent<RectTransform>(), 1f);
            card.Button = button;
            card.Bg = button.GetComponent<Image>();
            card.Border = border;

            // 色块图标（左上）
            var icon = UiFactory.CreateImage(button.transform, "Icon", def.Color);
            icon.raycastTarget = false;
            SetAnchor(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(14f, -14f), new Vector2(18f, 18f));

            // 名称（左上）
            card.Name = UiFactory.CreateText(button.transform, "Name", def.DisplayName, UiTheme.FontSizeCardTitle, UiTheme.Ink, TextAnchor.MiddleLeft);
            SetAnchor(card.Name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(40f, -12f), new Vector2(90f, 22f));

            // 费用（右上，金色）
            var cost = UiFactory.CreateText(button.transform, "Cost", $"${def.Cost}", UiTheme.FontSizeCardTitle, UiTheme.Gold, TextAnchor.MiddleRight);
            SetAnchor(cost.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-10f, -12f), new Vector2(60f, 22f));

            // 射程/伤害（底部，次级）
            card.Sub = UiFactory.CreateText(button.transform, "Sub", SubText(def), UiTheme.FontSizeCardSub, UiTheme.InkDim, TextAnchor.MiddleLeft);
            SetAnchor(card.Sub.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(10f, 14f), new Vector2(148f, 18f));

            return card;
        }

        // ---- 结算 ----

        private void BuildResultPanel()
        {
            _resultPanel = gameObject.AddComponent<ResultPanel>();
            _resultPanel.Build(transform);
        }

        // ---- 塔信息面板（点击塔检视，含撤退）----

        private void BuildTowerInfoPanel()
        {
            _towerInfoRoot = new GameObject("TowerInfoPanel", typeof(RectTransform));
            _towerInfoRoot.transform.SetParent(transform, false);
            UiFactory.Stretch(_towerInfoRoot.GetComponent<RectTransform>()); // 根节点铺满全屏，子元素才能正确锚定到屏幕边缘

            var panel = UiFactory.CreatePanel(_towerInfoRoot.transform, "Panel");
            UiFactory.SetRect(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(250f, 220f));

            _towerInfoTitle = UiFactory.CreateText(panel.transform, "Title", string.Empty, UiTheme.FontSize, UiTheme.Accent);
            UiFactory.SetRect(_towerInfoTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 76f), new Vector2(230f, 30f));

            _towerInfoStats = UiFactory.CreateText(panel.transform, "Stats", string.Empty, UiTheme.FontSizeSmall, UiTheme.Ink, TextAnchor.UpperLeft);
            UiFactory.SetRect(_towerInfoStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(220f, 90f));

            _towerInfoRefund = UiFactory.CreateText(panel.transform, "Refund", string.Empty, UiTheme.FontSizeCardSub, UiTheme.Gold);
            UiFactory.SetRect(_towerInfoRefund.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(220f, 18f));

            var retreat = UiFactory.CreateButton(panel.transform, "Retreat", "撤退", UiTheme.EnemyRed, UiTheme.FontSize, RetreatSelected);
            UiFactory.SetRect(retreat.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), new Vector2(180f, 40f));
            _towerInfoRetreat = retreat;

            _towerInfoRoot.SetActive(false);
        }

        private void RefreshTowerInfo(GameManager gm)
        {
            var tower = gm.TowerPlacer != null ? gm.TowerPlacer.SelectedTower : null;

            if (tower == null)
            {
                if (_towerInfoRoot.activeSelf) _towerInfoRoot.SetActive(false);
                return;
            }

            _towerInfoRoot.SetActive(true);
            _towerInfoTitle.text = tower.Definition.DisplayName;
            _towerInfoStats.text = BuildTowerStats(tower);
            _towerInfoRefund.text = BuildRefundText(tower);
            _towerInfoRetreat.interactable = !gm.IsPaused;
        }

        private static string BuildTowerStats(Tower tower)
        {
            var def = tower.Definition;
            string hp = $"生命  {Mathf.CeilToInt(tower.Health)}/{Mathf.CeilToInt(tower.MaxHealth)}";

            if (def.Damage <= 0f)
            {
                return $"{hp}\n效果  减速{Mathf.RoundToInt((1f - def.SlowFactor) * 100f)}%\n射程  {def.RangeCells} 格";
            }

            return $"{hp}\n伤害  {Mathf.RoundToInt(def.Damage)}\n攻速  {def.FireRate:0.#} 次/秒\n射程  {def.RangeCells} 格";
        }

        private static string BuildRefundText(Tower tower)
        {
            var def = tower.Definition;
            int retreat = Mathf.RoundToInt(def.Cost * GameConfig.RetreatRefundRatio);
            int defeat = Mathf.RoundToInt(def.Cost * GameConfig.DefeatRefundRatio);
            return $"撤退返还 {retreat} 金币 · 击毁返还 {defeat} 金币";
        }

        // ---- 刷新 ----

        private void RefreshTopBar(GameManager gm)
        {
            _livesText.text = $"{gm.Lives}";
            _goldText.text = $"{gm.Gold}";
            _enemyText.text = $"{gm.EnemyCount}";
            _killText.text = $"{gm.TotalKills}/{gm.TotalEnemies}";
            _towersText.text = $"{gm.TowerCount}/{GameConfig.MaxTowers}";
            _powerFill.fillAmount = gm.Power;

            _speedLabel.text = $"速度 {gm.SpeedScale:0.#}x";
            _pauseLabel.text = gm.IsPaused ? "继续" : "暂停";
            _targetingLabel.text = $"索敌：{TargetingText(gm.TargetingPriority)}";

            bool canBarrage = gm.CanBarrage && gm.State == GameState.Running && !gm.IsPaused;
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

                card.Button.interactable = affordable && !gm.IsPaused;
                card.Border.color = selected ? UiTheme.Accent : UiTheme.PanelLine;
                card.Bg.color = !affordable ? UiTheme.CardDisabled : selected ? UiTheme.CardSelected : UiTheme.CardBg;
                card.Name.color = !affordable ? UiTheme.InkDim : selected ? UiTheme.Accent : UiTheme.Ink;
                card.Sub.color = affordable ? UiTheme.InkDim : new Color(0.42f, 0.45f, 0.52f, 1f);
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
            rect.anchoredPosition = new Vector2(x, -UiTheme.TopBarHeight * 0.5f);
            rect.sizeDelta = new Vector2(width, 26f);
        }

        private static void SetCenterAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void CycleSpeed() => GameManager.Instance?.CycleSpeed();
        private static void TogglePause() => GameManager.Instance?.TogglePause();
        private static void CycleTargeting() => GameManager.Instance?.CycleTargetingPriority();
        private static void TriggerBarrage() => GameManager.Instance?.TriggerBarrage();
        private static void RetreatSelected() => GameManager.Instance?.TowerPlacer?.RetreatSelected();
        private static void RestartGame() => GameManager.Instance?.Restart();

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
                return $"射程{def.RangeCells} 减速{Mathf.RoundToInt((1f - def.SlowFactor) * 100f)}%";
            }
            return $"射程{def.RangeCells} 伤害{Mathf.RoundToInt(def.Damage)}";
        }
    }
}
