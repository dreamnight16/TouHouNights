$p = 'Assets/Scripts/UI/BattleUiRoot.Views.cs'
$s = Get-Content $p -Raw
$hand = @"
        private void BuildHand()
        {
            var hand = new GameObject("DeploymentHand", typeof(RectTransform)).GetComponent<RectTransform>();
            hand.SetParent(transform, false);
            PinBottomRight(hand, BattleUiTheme.Safe, 24f, 744f, 182f);
            var header = Panel(hand, "DeploymentHeader", BattleUiTheme.Ink);
            PinTopLeft(header.rectTransform, 0f, 0f, 744f, 42f);
            _gold = Text(header.transform, "Gold", "灵力 0", 26, BattleUiTheme.Ready, TextAnchor.MiddleLeft, 14f, 5f, 172f, 32f);
            Text(header.transform, "Caption", "DEPLOY / 部署序列", 12, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 198f, 10f, 190f, 22f);
            _slots = Text(header.transform, "Slots", "部署 0 / 8", 16, BattleUiTheme.Text, TextAnchor.MiddleRight, 388f, 9f, 140f, 24f);
            _targeting = Button(header.transform, "Targeting", "索敌 / 最近", () => _game.CycleTargetingPriority());
            PinTopRight(_targeting.GetComponent<RectTransform>(), 8f, 6f, 180f, 30f);
            _targetingLabel = _targeting.GetComponentInChildren<UiText>();
            for (int i = 0; i < _order.Length; i++) _cards[i] = BuildCard(hand, _order[i], i);
        }

"@
$s = [regex]::Replace($s, '(?s)        private void BuildHand\(\).*?(?=        private void BuildEnemyIntel)', $hand)
$card = @"
        private DeployCard BuildCard(Transform hand, TowerType type, int index)
        {
            var def = GameConfig.Towers[type];
            var card = new DeployCard { Type = type };
            var panel = Panel(hand, "Card_" + type, Color.white);
            PinBottomLeft(panel.rectTransform, index * 150f, 0f, 144f, 128f);
            card.Panel = panel;
            card.Rect = panel.rectTransform;
            var accent = Color.Lerp(def.Color, BattleUiTheme.Paper, .35f);
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
            var costPlate = Image(plate.transform, "CostPlate", BattleUiTheme.Paper);
            PinTopLeft(costPlate.rectTransform, 0f, 0f, 55f, 44f);
            costPlate.raycastTarget = false;
            card.Cost = Text(costPlate.transform, "Cost", def.Cost.ToString(), 29, BattleUiTheme.Ink, TextAnchor.MiddleCenter, 0f, 0f, 55f, 40f);
            Text(panel.transform, "Name", def.DisplayName, 19, BattleUiTheme.Text, TextAnchor.MiddleLeft, 10f, 79f, 104f, 26f);
            Text(panel.transform, "Key", (index + 1).ToString("00"), 12, BattleUiTheme.Muted, TextAnchor.MiddleRight, 116f, 83f, 20f, 20f);
            card.State = Text(panel.transform, "State", "点击部署", 11, BattleUiTheme.Muted, TextAnchor.MiddleLeft, 10f, 106f, 126f, 18f);
            card.SelectionRail = Image(panel.transform, "SelectionRail", BattleUiTheme.Ready);
            PinBottomLeft(card.SelectionRail.rectTransform, 0f, 0f, 144f, 3f);
            card.SelectionRail.raycastTarget = false;
            card.SelectionRail.gameObject.SetActive(false);
            card.Button = panel.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = panel;
            card.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            card.Button.onClick.AddListener(() =>
            {
                if (_game.State != GameState.Running || _game.IsPaused || _game.TowerPlacer == null) return;
                _game.TowerPlacer.ClearSelection();
                _game.TowerPlacer.SelectTower(type);
            });
            card.RestPosition = panel.rectTransform.anchoredPosition;
            return card;
        }

"@
$s = [regex]::Replace($s, '(?s)        private DeployCard BuildCard\(.*?(?=        private void BuildDossier)', $card)
$s = $s.Replace('PinTopRight(panel.rectTransform, BattleUiTheme.Safe, 150f, 184f, 104f)', 'PinBottomLeft(panel.rectTransform, BattleUiTheme.Safe, 24f, 184f, 104f)')
$s = $s.Replace('PinBottomLeft(rect, BattleUiTheme.Safe, BattleUiTheme.Safe, 304f, 280f)', 'PinBottomLeft(rect, BattleUiTheme.Safe, 146f, 304f, 280f)')
$s = $s.Replace('PinTopRight(rect, 236f, 150f, 236f, 126f)', 'PinTopRight(rect, BattleUiTheme.Safe, 132f, 236f, 126f)')
Set-Content $p $s
