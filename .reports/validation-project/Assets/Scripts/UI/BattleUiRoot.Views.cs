using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    public sealed partial class BattleUiRoot
    {
        private void Build()
        {
            BuildChrome();
            BuildMissionTag();
            BuildReadout();
            BuildTimeControls();
            BuildBarrage();
            BuildEnemyIntel();
            BuildHand();
            BuildDossier();
            BuildAlert();
            BuildPause();
            BuildResult();
        }

        private void BuildChrome()
        {
            var top = Image(transform, "TopBand", BattleUiTheme.SurfaceDeep);
            top.rectTransform.anchorMin = new Vector2(0, 1);
            top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.pivot = new Vector2(.5f, 1);
            top.rectTransform.sizeDelta = new Vector2(0, 116);
            top.raycastTarget = false;
            var bottom = Image(transform, "BottomBand", BattleUiTheme.Ink);
            bottom.rectTransform.anchorMin = Vector2.zero;
            bottom.rectTransform.anchorMax = new Vector2(1, 0);
            bottom.rectTransform.pivot = new Vector2(.5f, 0);
            bottom.rectTransform.sizeDelta = new Vector2(0, 206);
            bottom.raycastTarget = false;
            var leftRail = Image(transform, "LeftSafetyRail", BattleUiTheme.Grid);
            leftRail.raycastTarget = false;
            leftRail.rectTransform.anchorMin = new Vector2(0f, 0.12f);
            leftRail.rectTransform.anchorMax = new Vector2(0f, 0.88f);
            leftRail.rectTransform.pivot = new Vector2(0f, 0.5f);
            leftRail.rectTransform.anchoredPosition = new Vector2(14f, 0f);
            leftRail.rectTransform.sizeDelta = new Vector2(BattleUiTheme.Hairline, 0f);

            var bottomRail = Image(transform, "BottomSafetyRail", BattleUiTheme.Grid);
            bottomRail.raycastTarget = false;
            bottomRail.rectTransform.anchorMin = new Vector2(0.08f, 0f);
            bottomRail.rectTransform.anchorMax = new Vector2(0.92f, 0f);
            bottomRail.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomRail.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            bottomRail.rectTransform.sizeDelta = new Vector2(0f, BattleUiTheme.Hairline);

            Text(transform, "DreamNight", "梦夜十六", 11,
                BattleUiTheme.WithAlpha(BattleUiTheme.Text, .42f), TextAnchor.MiddleLeft,
                BattleUiTheme.Safe, 124f, 280f, 16f).SetCharacterSpacing(1.5f);
        }

        private void BuildMissionTag()
        {
            var tag = Panel("MissionTag", BattleUiTheme.SurfaceDeep);
            PinTopLeft(tag.rectTransform, BattleUiTheme.Safe, BattleUiTheme.Safe, 240f, 84f);
            var stripe = Image(tag.transform, "No", BattleUiTheme.Ink);
            PinTopLeft(stripe.rectTransform, 0f, 0f, 42f, 84f);
            _stageNumber = Text(tag.transform, "NoText", "—", 22, BattleUiTheme.Paper, TextAnchor.MiddleCenter, 0f, 13f, 42f, 42f);
            Text(tag.transform, "Sector", "STAGE", 10, BattleUiTheme.Action, TextAnchor.MiddleCenter, 0f, 56f, 42f, 16f);
            _mission = Text(tag.transform, "Mission", "东方阵符录", 24, BattleUiTheme.Text, TextAnchor.MiddleLeft, 55f, 9f, 174f, 44f);
            _mission.FitInBox(19, true);
            _missionSub = Text(tag.transform, "Sub", "准备", 12, BattleUiTheme.Text, TextAnchor.MiddleLeft, 56f, 53f, 174f, 18f);
            _missionSub.SetCharacterSpacing(1f);
            var underline = Image(tag.transform, "MissionUnderline", BattleUiTheme.Action);
            PinBottomLeft(underline.rectTransform, 55f, 8f, 52f, 2f);
            underline.raycastTarget = false;
        }

        private void BuildReadout()
        {
            var group = new GameObject("BattleReadout", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinTopCenter(group, 0f, BattleUiTheme.Safe, 448f, 74f);
            _wave = Chip(group, "Wave", "幕次", 0f, 142f);
            _enemies = Chip(group, "Enemies", "出现 / 场上", 153f, 142f);
            _barrier = Chip(group, "Barrier", "残机", 306f, 142f);
            var rail = Image(group, "ProgressRail", BattleUiTheme.Grid);
            PinBottomLeft(rail.rectTransform, 0f, -7f, 448f, 3f);
            rail.raycastTarget = false;
        }

        private UiText Chip(Transform parent, string name, string label, float x, float width)
        {
            var chip = Panel(parent, name, BattleUiTheme.SurfaceDeep);
            PinTopLeft(chip.rectTransform, x, 0f, width, 84f);
            Text(chip.transform, "Label", label, 14, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 12f, 12f, width - 24f, 17f);
            return Text(chip.transform, "Value", "—", 21, BattleUiTheme.Text, TextAnchor.MiddleLeft, 12f, 35f, width - 24f, 27f);
        }

        private void BuildTimeControls()
        {
            var group = Panel("TimeControls", BattleUiTheme.SurfaceDeep);
            PinTopRight(group.rectTransform, BattleUiTheme.Safe, BattleUiTheme.Safe, 312f, 84f);
            _speed = Text(group.transform, "Speed", "1×", 24, BattleUiTheme.Text, TextAnchor.MiddleCenter, 14f, 12f, 48f, 56f);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var b = Button(group.transform, "Speed" + i, new[] { "0.5", "1", "2", "4" }[i], () => _game.SetSpeedIndex(index));
                _speedLabels[i] = b.GetComponentInChildren<UiText>();
                PinTopLeft(b.GetComponent<RectTransform>(), 70f + 40f * i, 20f, 34f, 40f);
            }
            var pause = Button(group.transform, "Pause", "Ⅱ", () => _game.TogglePause());
            PinTopRight(pause.GetComponent<RectTransform>(), 12f, 16f, 48f, 48f);
        }

        private void BuildBarrage()
        {
            var panel = Panel("BarrageCommand", BattleUiTheme.Surface);
            PinBottomLeft(panel.rectTransform, BattleUiTheme.Safe, 24f, 224f, 182f);
            Text(panel.transform, "Title", "弹幕威力", 16, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 14f, 12f, 154f, 18f);
            _power = Text(panel.transform, "Power", "P 0.00", 30, BattleUiTheme.Text, TextAnchor.MiddleLeft, 14f, 32f, 196f, 44f);
            _powerBonus = Text(panel.transform, "Bonus", "增伤 +0%", 14, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 14f, 80f, 196f, 22f);
            var rail = Image(panel.transform, "Rail", BattleUiTheme.WithAlpha(BattleUiTheme.Text, .16f));
            PinTopLeft(rail.rectTransform, 14f, 108f, 196f, 4f);
            _powerFill = Image(rail.transform, "Fill", BattleUiTheme.Action);
            _powerFill.rectTransform.anchorMin = new Vector2(0f, 0f); _powerFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _powerFill.rectTransform.pivot = new Vector2(0f, .5f); _powerFill.rectTransform.sizeDelta = Vector2.zero;
            _barrage = Button(panel.transform, "Fire", "E", () => _game.TriggerBarrage());
            PinBottomLeft(_barrage.GetComponent<RectTransform>(), 14f, 16f, 196f, 44f);
            _barrageLabel = _barrage.GetComponentInChildren<UiText>();
        }

        private void BuildHand()
        {
            var hand = new GameObject("DeploymentHand", typeof(RectTransform)).GetComponent<RectTransform>();
            hand.SetParent(transform, false);
            PinBottomRight(hand, BattleUiTheme.Safe, 24f, 744f, 182f);
            var header = Panel(hand, "DeploymentHeader", BattleUiTheme.Ink);
            PinTopLeft(header.rectTransform, 0f, 0f, 744f, 42f);
            _gold = Text(header.transform, "Gold", "灵力 0", 26, BattleUiTheme.Ready, TextAnchor.MiddleLeft, 14f, 5f, 172f, 32f);
            Text(header.transform, "Caption", "符阵选择", 12, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 198f, 10f, 190f, 22f);
            _slots = Text(header.transform, "Slots", "出阵 0 / 8", 16, BattleUiTheme.Text, TextAnchor.MiddleRight, 388f, 9f, 140f, 24f);
            _targeting = Button(header.transform, "Targeting", "目标 / 最近", () => _game.CycleTargetingPriority());
            PinTopRight(_targeting.GetComponent<RectTransform>(), 8f, 6f, 180f, 30f);
            _targetingLabel = _targeting.GetComponentInChildren<UiText>();
            for (int i = 0; i < _order.Length; i++) _cards[i] = BuildCard(hand, _order[i], i);
        }
        private void BuildEnemyIntel()
        {
            _enemyIntel = Panel("EnemyIntel", BattleUiTheme.SurfaceDeep).gameObject;
            var rect = _enemyIntel.GetComponent<RectTransform>();
            PinTopRight(rect, BattleUiTheme.Safe, 132f, 236f, 126f);
            var spine = Image(_enemyIntel.transform, "ThreatSpine", BattleUiTheme.Danger);
            PinTopLeft(spine.rectTransform, 0f, 0f, 6f, 126f);
            Text(_enemyIntel.transform, "Label", "敌方情报", 12, BattleUiTheme.Danger,
                TextAnchor.MiddleLeft, 18f, 10f, 202f, 17f).SetCharacterSpacing(1f);
            _enemyIntelTitle = Text(_enemyIntel.transform, "Title", "", 22, BattleUiTheme.Text,
                TextAnchor.MiddleLeft, 18f, 30f, 202f, 28f);
            _enemyIntelHealth = Text(_enemyIntel.transform, "Health", "", 16, BattleUiTheme.Text,
                TextAnchor.MiddleLeft, 18f, 62f, 202f, 20f);
            _enemyIntelStats = Text(_enemyIntel.transform, "Stats", "", 14, BattleUiTheme.Muted,
                TextAnchor.UpperLeft, 18f, 87f, 202f, 34f);
            _enemyIntel.SetActive(false);
        }

        private void BuildAlert()
        {
            _alertPanel = Panel("AlertDirector", BattleUiTheme.SurfaceDeep);
            _alert = _alertPanel.gameObject;
            PinTopCenter(_alertPanel.rectTransform, 0f, 126f, 480f, 68f);
            _alertMarker = Image(_alert.transform, "Marker", BattleUiTheme.Ready);
            PinTopLeft(_alertMarker.rectTransform, 0f, 0f, 10f, 68f);
            _alertTitle = Text(_alert.transform, "Title", "", 24, BattleUiTheme.Text,
                TextAnchor.MiddleLeft, 28f, 10f, 420f, 28f);
            _alertSub = Text(_alert.transform, "Sub", "", 12, BattleUiTheme.Muted,
                TextAnchor.MiddleLeft, 29f, 42f, 420f, 17f);
            _alert.SetActive(false);
        }

        private DeployCard BuildCard(Transform hand, TowerType type, int index)
        {
            var def = GameConfig.Towers[type];
            var card = new DeployCard { Type = type };
            var panel = Panel(hand, "Card_" + type, Color.white);
            PinBottomLeft(panel.rectTransform, index * 150f, 0f, 144f, 128f);
            card.Panel = panel;
            card.Rect = panel.rectTransform;
            var accent = Color.Lerp(def.Color, BattleUiTheme.Paper, .18f);
            var plate = Image(panel.transform, "IllustrationPlate", Color.Lerp(BattleUiTheme.Ink, accent, .13f));
            PinTopLeft(plate.rectTransform, 0f, 0f, 144f, 76f);
            plate.raycastTarget = false;
            var diagonal = Image(plate.transform, "TechnicalSlash", BattleUiTheme.WithAlpha(accent, .13f));
            PinTopLeft(diagonal.rectTransform, 79f, 3f, 12f, 67f);
            diagonal.rectTransform.localRotation = Quaternion.Euler(0, 0, -24f);
            diagonal.raycastTarget = false;
            var icon = UiIcon.CreateTowerIcon(plate.transform, type, 49f, accent);
            PinTopLeft(icon, 78f, 15f, 49f, 49f);
            Text(plate.transform, "Class", Role(def), 11, accent, TextAnchor.MiddleLeft, 10f, 52f, 60f, 18f);
            var costPlate = Image(plate.transform, "CostPlate", BattleUiTheme.Surface);
            PinTopLeft(costPlate.rectTransform, 0f, 0f, 55f, 44f);
            costPlate.raycastTarget = false;
            card.Cost = Text(costPlate.transform, "Cost", def.Cost.ToString(), 29, BattleUiTheme.Text, TextAnchor.MiddleCenter, 0f, 0f, 55f, 40f);
            Text(panel.transform, "Name", def.DisplayName, 18, BattleUiTheme.Text, TextAnchor.MiddleLeft, 10f, 79f, 124f, 26f);
            Text(panel.transform, "Key", (index + 1).ToString("00"), 12, BattleUiTheme.Muted, TextAnchor.MiddleRight, 116f, 106f, 20f, 18f);
            card.State = Text(panel.transform, "State", "选择", 11, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 10f, 106f, 126f, 18f);
            card.StatusPip = Image(panel.transform, "StatusPip", BattleUiTheme.Action);
            PinTopLeft(card.StatusPip.rectTransform, 4f, 112f, 3f, 7f);
            card.StatusPip.raycastTarget = false;
            card.SelectionRail = Image(panel.transform, "SelectionRail", BattleUiTheme.Ready);
            PinBottomLeft(card.SelectionRail.rectTransform, 0f, 0f, 144f, 3f);
            card.SelectionRail.raycastTarget = false;
            card.SelectionRail.gameObject.SetActive(false);
            card.Button = panel.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = panel;
            card.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            UiMotion.Attach(card.Button);
            card.Button.onClick.AddListener(() =>
            {
                if (_game.State != GameState.Running || _game.IsPaused || _game.TowerPlacer == null) return;
                _game.TowerPlacer.ClearSelection();
                _game.TowerPlacer.SelectTower(type);
            });
            card.RestPosition = panel.rectTransform.anchoredPosition;
            return card;
        }
        private void BuildDossier()
        {
            _dossier = Panel("UnitDossier", BattleUiTheme.Surface).gameObject;
            var rect = _dossier.GetComponent<RectTransform>(); PinBottomLeft(rect, BattleUiTheme.Safe, 222f, 304f, 280f);
            _dossierTitle = Text(_dossier.transform, "Title", "", 26, BattleUiTheme.Text, TextAnchor.MiddleLeft, 20f, 20f, 265f, 30f);
            _dossierHealth = Text(_dossier.transform, "Health", "", 16, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 20f, 61f, 265f, 21f);
            _dossierStats = Text(_dossier.transform, "Stats", "", 17, BattleUiTheme.Text, TextAnchor.UpperLeft, 20f, 96f, 265f, 62f);
            _dossierBoom = Text(_dossier.transform, "BoomStatus", "", 16, BattleUiTheme.Ready, TextAnchor.MiddleLeft, 20f, 153f, 265f, 22f);
            _boom = Button(_dossier.transform, "Boom", "符卡发动", () => { var tower = _game.TowerPlacer.SelectedTower; if (tower != null) tower.TriggerBoom(); });
            PinBottomLeft(_boom.GetComponent<RectTransform>(), 20f, 32f, 116f, 40f);
            _retreat = Button(_dossier.transform, "Retreat", "撤退", () => _game.TowerPlacer.RetreatSelected());
            PinBottomLeft(_retreat.GetComponent<RectTransform>(), 156f, 32f, 128f, 40f);
            _retreatLabel = _retreat.GetComponentInChildren<UiText>();
            _dossier.SetActive(false);
        }

        private void BuildPause()
        {
            _pause = new GameObject("PauseScreen", typeof(RectTransform)).gameObject; _pause.transform.SetParent(transform, false); UiFactory.Stretch((RectTransform)_pause.transform);
            var shade = Image(_pause.transform, "Shade", BattleUiTheme.WithAlpha(BattleUiTheme.Ink, .92f)); UiFactory.Stretch(shade.rectTransform);
            var menu = Panel(_pause.transform, "Menu", BattleUiTheme.SurfaceDeep); PinLeft(menu.rectTransform, 150f, 0f, 330f, 392f);
            Text(_pause.transform, "PauseWatermark", "PAUSED", 78, BattleUiTheme.WithAlpha(BattleUiTheme.Paper, .08f), TextAnchor.MiddleRight, 0f, 0f, 1f, 1f);
            var watermark = _pause.transform.Find("PauseWatermark").GetComponent<RectTransform>();
            PinRight(watermark, 120f, 170f, 430f, 110f);
            Text(menu.transform, "Title", "暂停", 37, BattleUiTheme.Text, TextAnchor.MiddleLeft, 30f, 36f, 270f, 48f);
            Text(menu.transform, "Hint", "SPACE 继续  ·  X 倍速", 15, BattleUiTheme.Text, TextAnchor.MiddleLeft, 30f, 91f, 270f, 22f);
            var music = Button(menu.transform, "Music", "音乐 开 / 关", () => TowerDefense.Effects.BattleMusic.Toggle()); PinTopLeft(music.GetComponent<RectTransform>(), 30f, 252f, 270f, 46f);
            var resume = Button(menu.transform, "Resume", "继续", () => _game.TogglePause()); PinTopLeft(resume.GetComponent<RectTransform>(), 30f, 138f, 270f, 46f);
            var retry = Button(menu.transform, "Retry", "重新开始", () => _game.Restart()); PinTopLeft(retry.GetComponent<RectTransform>(), 30f, 195f, 270f, 46f);
            var home = Button(menu.transform, "Home", "返回标题", () => _game.ReturnToMenu()); PinTopLeft(home.GetComponent<RectTransform>(), 30f, 309f, 270f, 46f);
            _pauseBrief = Text(_pause.transform, "Brief", "", 19, BattleUiTheme.Text, TextAnchor.UpperLeft, 0f, 0f, 1f, 1f);
            PinRight(_pauseBrief.rectTransform, 150f, 0f, 330f, 240f);
            _pause.SetActive(false);
        }

        private void BuildResult()
        {
            _result = new GameObject("ResultScreen", typeof(RectTransform)).gameObject; _result.transform.SetParent(transform, false); UiFactory.Stretch((RectTransform)_result.transform);
            var bg = Image(_result.transform, "Background", BattleUiTheme.Ink); UiFactory.Stretch(bg.rectTransform);
            var band = Image(_result.transform, "ResultBand", BattleUiTheme.Action); PinLeft(band.rectTransform, 0f, 0f, 12f, 1080f);
            var reportLabel = Text(_result.transform, "ReportLabel", "RESULT / 结果", 15, BattleUiTheme.Action, TextAnchor.MiddleLeft, 120f, 68f, 700f, 24f);
            reportLabel.SetCharacterSpacing(2f);
            var divider = Image(_result.transform, "ReportDivider", BattleUiTheme.Grid);
            divider.raycastTarget = false;
            PinTopLeft(divider.rectTransform, 120f, 110f, 900f, 1f);
            _resultTitle = Text(_result.transform, "Title", "异变解决", 42, BattleUiTheme.Text, TextAnchor.MiddleLeft, 0f, 0f, 1f, 1f); PinLeft(_resultTitle.rectTransform, 120f, 160f, 500f, 54f);
            _resultGrade = Text(_result.transform, "Grade", "S", 184, BattleUiTheme.Paper, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f); PinLeft(_resultGrade.rectTransform, 145f, -20f, 300f, 220f);
            _resultStats = Text(_result.transform, "Stats", "", 25, BattleUiTheme.Text, TextAnchor.UpperLeft, 0f, 0f, 1f, 1f); PinRight(_resultStats.rectTransform, 180f, 0f, 400f, 260f);
            var retry = Button(_result.transform, "Retry", "再来一次 [R]", () => _game.Restart()); PinBottomLeft(retry.GetComponent<RectTransform>(), 120f, 95f, 270f, 54f);
            var home = Button(_result.transform, "Home", "返回标题", () => _game.ReturnToMenu()); PinBottomLeft(home.GetComponent<RectTransform>(), 410f, 95f, 270f, 54f);
            _result.SetActive(false);
        }

    }
}

