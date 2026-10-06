using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    public sealed partial class BattleUiRoot
    {
        /// <summary>装配战斗界面并播放入场动画。</summary>
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
            PlayEntrance();
        }

        /// <summary>依次显示关卡、战况、倍速、出阵和弹幕区域。</summary>
        private void PlayEntrance()
        {
            Enter("MissionTag", new Vector2(-22f, 0f), 0f);
            Enter("BattleReadout", new Vector2(0f, 14f), .06f);
            Enter("TimeControls", new Vector2(22f, 0f), .12f);
            Enter("DeploymentHand", new Vector2(0f, -22f), .18f);
            Enter("BarrageCommand", new Vector2(-22f, 0f), .24f);
        }

        private void Enter(string name, Vector2 from, float delay)
        {
            if (transform.Find(name) is RectTransform rect) UiEntrance.Play(rect, from, delay);
        }

        /// <summary>顶底渐变遮罩叠在全屏战场上，提高贴边文字的可读性。</summary>
        private void BuildChrome()
        {
            var top = Surface(transform, "TopBand", UiSurfaceKind.Veil);
            top.Body(BattleUiTheme.WithAlpha(BattleUiTheme.Char, .95f), BattleUiTheme.WithAlpha(BattleUiTheme.Ink, 0f))
               .Border(Color.clear, 0f);
            var topRect = top.rectTransform;
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = Vector2.one;
            topRect.pivot = new Vector2(.5f, 1);
            topRect.sizeDelta = new Vector2(0, 116);
            top.raycastTarget = false;

            var bottom = Surface(transform, "BottomBand", UiSurfaceKind.Veil);
            bottom.Body(BattleUiTheme.WithAlpha(BattleUiTheme.Ink, 0f), BattleUiTheme.WithAlpha(BattleUiTheme.Ink, .96f))
                  .Border(Color.clear, 0f);
            var bottomRect = bottom.rectTransform;
            bottomRect.anchorMin = Vector2.zero;
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(.5f, 0);
            bottomRect.sizeDelta = new Vector2(0, 206);
            bottom.raycastTarget = false;

            Micro(transform, "DreamNight", "梦夜十六", BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .50f),
                0f, 0f, 1f, 1f).Also(t => PinBottomLeft(t.rectTransform, 24f, 4f, 200f, 14f))
                             .SetCharacterSpacing(2.2f);
        }

        /// <summary>关卡编号及当前幕次、阶段。</summary>
        private void BuildMissionTag()
        {
            var tag = new GameObject("MissionTag", typeof(RectTransform)).GetComponent<RectTransform>();
            tag.SetParent(transform, false);
            PinTopLeft(tag, BattleUiTheme.Safe, BattleUiTheme.Safe, 300f, 84f);

            Micro(tag, "Sector", "STAGE", BattleUiTheme.Scarlet, 0f, 2f, 110f, 16f);
            _stageNumber = Text(tag, "No", "—", 40, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 18f, 110f, 58f);
            _stageNumber.SetCharacterSpacing(-1.5f);

            _mission = Text(tag, "Mission", "作战准备", 19, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 116f, 22f, 184f, 28f);
            _missionSub = Text(tag, "Sub", "准备", 12, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 117f, 50f, 184f, 18f);
            _missionSub.SetCharacterSpacing(.6f);
        }

        /// <summary>横排显示击破数、敌人数量和结界耐久。</summary>
        private void BuildReadout()
        {
            var group = new GameObject("BattleReadout", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinTopCenter(group, 0f, BattleUiTheme.Safe, 448f, 76f);

            _kills = Readout(group, "Kills", "击破 KILLS", 0f, out _);
            _enemies = Readout(group, "Enemies", "出现 / 总数", 152f, out _enemiesCaption);
            _barrier = Readout(group, "Barrier", "结界耐久", 304f, out _);

            UiKit.RuleV(group, "SepA", BattleUiTheme.Grid, 142f, 14f, 48f);
            UiKit.RuleV(group, "SepB", BattleUiTheme.Grid, 294f, 14f, 48f);
        }

        /// <summary>上方标签与下方大数值组成一个读数单元。</summary>
        private UiText Readout(Transform parent, string name, string caption, float x, out UiText captionText)
        {
            captionText = Micro(parent, name + "Caption", caption, BattleUiTheme.Ash, x, 0f, 138f, 16f);
            var value = Text(parent, name, "—", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, x, 16f, 138f, 48f);
            value.SetCharacterSpacing(-.5f);
            return value;
        }

        /// <summary>当前倍速、四个速度档位及暂停按钮。</summary>
        private void BuildTimeControls()
        {
            var group = new GameObject("TimeControls", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinTopRight(group, BattleUiTheme.Safe, BattleUiTheme.Safe, 340f, 84f);

            _speed = Text(group, "Speed", "1×", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 4f, 84f, 50f);
            _speed.SetCharacterSpacing(-.5f);
            Micro(group, "SpeedLabel", "倍速 SPEED", BattleUiTheme.Ash, 19f, 56f, 84f, 16f);

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                // 小按钮需要减小标签内缩，避免挤压文字。
                var chip = UiKit.Toggle(group, "Speed" + i, new[] { "0.5", "1", "2", "4" }[i],
                    () => _game.SetSpeedIndex(index), BattleUiTheme.Type.Small, 4f);
                PinTopLeft(chip.GetComponent<RectTransform>(), 112f + 38f * i, 46f, 36f, 26f);
                _speedLabels[i] = chip.GetComponentInChildren<UiText>();

                var bar = Image(chip.transform, "Bar", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .85f));
                var barRect = bar.rectTransform;
                barRect.anchorMin = new Vector2(0f, 0f);
                barRect.anchorMax = new Vector2(1f, 0f);
                barRect.pivot = new Vector2(.5f, 0f);
                barRect.anchoredPosition = new Vector2(0f, -2f);
                barRect.sizeDelta = new Vector2(0f, 2f);
                bar.raycastTarget = false;
                bar.enabled = false;
                _speedBars[i] = bar;
            }

            var pause = UiKit.Button(group, "Pause", "Ⅱ", UiButtonKind.Ghost, () => _game.TogglePause());
            PinTopRight(pause.GetComponent<RectTransform>(), 14f, 18f, 56f, 48f);
        }

        /// <summary>底栏左侧显示弹幕威力、增伤、发动按钮和充能轨道。</summary>
        private void BuildBarrage()
        {
            var group = new GameObject("BarrageCommand", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinBottomLeft(group, BattleUiTheme.Safe, BattleUiTheme.Gap.Lg, 600f, 132f);

            Micro(group, "Title", "弹幕威力 POWER", BattleUiTheme.Ash, 0f, 0f, 200f, 16f);
            _power = Text(group, "Power", "0.00", 44, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 14f, 152f, 64f);
            _power.SetCharacterSpacing(-1.5f);
            _powerBonus = Text(group, "Bonus", "增伤 +0%", 15, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 154f, 42f, 146f, 22f);

            _barrage = UiKit.Button(group, "Fire", "E  发动", UiButtonKind.Primary, () => _game.TriggerBarrage());
            PinBottomLeft(_barrage.GetComponent<RectTransform>(), 340f, 24f, 240f, 56f);
            _barragePanel = _barrage.GetComponent<UiPanel>();
            _barrageHalo = UiHalo.Attach(_barragePanel, BattleUiTheme.GlowScarlet, 26f).Breathe(2.2f, .45f);
            _barrageLabel = _barrage.GetComponentInChildren<UiText>();

            var rail = Image(group, "Rail", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .16f));
            PinBottomLeft(rail.rectTransform, 0f, 0f, 600f, 4f);
            _powerFill = Image(rail.transform, "Fill", BattleUiTheme.Scarlet);
            _powerFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _powerFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _powerFill.rectTransform.pivot = new Vector2(0f, .5f);
            _powerFill.rectTransform.sizeDelta = Vector2.zero;
        }

        /// <summary>出阵区：灵力、部署数量、索敌模式和五张符卡。高度包含选中卡片展开的空间。</summary>
        private void BuildHand()
        {
            var hand = new GameObject("DeploymentHand", typeof(RectTransform)).GetComponent<RectTransform>();
            hand.SetParent(transform, false);
            PinBottomRight(hand, BattleUiTheme.Safe, 24f, 744f, 214f);

            var header = new GameObject("DeploymentHeader", typeof(RectTransform)).GetComponent<RectTransform>();
            header.SetParent(hand, false);
            PinTopLeft(header, 0f, 0f, 744f, 60f);

            Micro(header, "GoldLabel", "灵力 SPIRIT", BattleUiTheme.Gold, 0f, 2f, 120f, 14f);
            _gold = Text(header, "Gold", "0", 30, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 16f, 120f, 42f);
            _gold.SetCharacterSpacing(-.5f);

            UiKit.RuleV(header, "GoldRule", BattleUiTheme.Grid, 134f, 8f, 42f);

            _slots = Text(header, "Slots", "出阵 0 / 8", 15, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 150f, 16f, 160f, 26f);

            _targeting = UiKit.Button(header, "Targeting", "目标 / 最近", UiButtonKind.Ghost, () => _game.CycleTargetingPriority());
            PinTopRight(_targeting.GetComponent<RectTransform>(), 184f, 12f, 168f, 34f);
            _targetingLabel = _targeting.GetComponentInChildren<UiText>();

            for (int i = 0; i < _order.Length; i++) _cards[i] = BuildCard(hand, _order[i], i);
        }

        /// <summary>符卡显示消耗、塔名、职能、状态和快捷键。</summary>
        private DeployCard BuildCard(Transform hand, TowerType type, int index)
        {
            var def = GameConfig.Towers[type];
            var card = new DeployCard { Type = type };

            var panel = Surface(hand, "Card_" + type, UiSurfaceKind.Card);
            panel.Corners(UiCorner.Diagonal, 12f);
            PinBottomLeft(panel.rectTransform, index * 150f, 0f, 144f, CardHeight);
            card.Panel = panel;
            card.Rect = panel.rectTransform;

            var iconTint = Color.Lerp(def.Color, BattleUiTheme.Bone, .52f);
            var icon = UiIcon.CreateTowerIcon(panel.transform, type, 42f, iconTint);
            PinTopRight(icon, 12f, 12f, 42f, 42f);

            card.Cost = Text(panel.transform, "Cost", def.Cost.ToString(), 38, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 14f, 4f, 72f, 56f);
            card.Cost.SetCharacterSpacing(-1f);
            Micro(panel.transform, "CostLabel", "灵力", BattleUiTheme.Ash, 15f, 60f, 60f, 16f);

            var name = Text(panel.transform, "Name", def.DisplayName, 19, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 14f, 82f, 118f, 30f);
            name.FitInBox(15, true);

            card.State = Text(panel.transform, "State", "选择", 11, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 14f, 114f, 96f, 16f);
            // 快捷键向内收，避开右下斜切。
            Micro(panel.transform, "Key", (index + 1).ToString("00"), BattleUiTheme.Ash, 110f, 114f, 14f, 16f)
                .Also(t => t.SetAlignment(TextAnchor.MiddleRight));


            card.Halo = UiHalo.Attach(panel, BattleUiTheme.GlowScarlet, 22f).Breathe(1.6f, .40f);

            card.Button = panel.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = panel;
            card.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            card.Button.colors = UiKit.FlatColors();
            UiMotion.Attach(card.Button, BattleUiTheme.Scarlet);
            card.Button.onClick.AddListener(() =>
            {
                if (_game.State != GameState.Running || _game.IsPaused || _game.TowerPlacer == null) return;
                _game.TowerPlacer.ClearSelection();
                _game.TowerPlacer.SelectTower(type);
            });

            card.RestPosition = panel.rectTransform.anchoredPosition;
            // 入场位移由 AnimateHand 统一处理，避免多个动画同时修改 anchoredPosition。
            panel.rectTransform.anchoredPosition = card.RestPosition + Vector2.down * 30f;
            card.EnterAt = Time.unscaledTime + .18f + index * .055f;
            return card;
        }

        /// <summary>悬停敌人时显示名称、耐久和属性。</summary>
        private void BuildEnemyIntel()
        {
            var intel = Surface(transform, "EnemyIntel", UiSurfaceKind.Panel);
            _enemyIntel = intel.gameObject;
            PinTopRight(intel.rectTransform, BattleUiTheme.Safe, 132f, 248f, 132f);
            // 不拦截指针，避免 PickEnemy 因情报板遮挡而反复显示、隐藏。
            intel.raycastTarget = false;

            Micro(intel.transform, "Label", "敌方情报 CONTACT", BattleUiTheme.Cyan, 18f, 12f, 214f, 14f);
            _enemyIntelTitle = Text(intel.transform, "Title", "", 21, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 30f, 214f, 32f);
            _enemyIntelHealth = Text(intel.transform, "Health", "", 15, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 62f, 214f, 22f);
            _enemyIntelStats = Text(intel.transform, "Stats", "", 13, BattleUiTheme.Ash, TextAnchor.UpperLeft, 18f, 88f, 214f, 36f);
            _enemyIntel.SetActive(false);
        }

        /// <summary>波次与强敌事件播报。</summary>
        private void BuildAlert()
        {
            _alertPanel = Surface(transform, "AlertDirector", UiSurfaceKind.Panel);
            _alert = _alertPanel.gameObject;
            PinTopCenter(_alertPanel.rectTransform, 0f, 126f, 480f, 68f);
            _alertPanel.raycastTarget = false;

            var stripes = Image(_alert.transform, "Stripes", Color.white);
            stripes.sprite = SpriteFactory.Hazard(10f, 68f, 14f, Color.white);
            PinTopLeft(stripes.rectTransform, 0f, 0f, 10f, 68f);
            stripes.raycastTarget = false;
            _alertStripes = stripes;
            _alertStripes.gameObject.SetActive(false);

            _alertTitle = Text(_alert.transform, "Title", "", 26, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 26f, 8f, 424f, 38f);
            _alertSub = Micro(_alert.transform, "Sub", "", BattleUiTheme.Ash, 27f, 46f, 424f, 16f);
            _alert.SetActive(false);
        }

        /// <summary>塔详情。可撤退 / 可发动符卡的行动区放在面板底部，和标题隔开一整行留白。</summary>
        private void BuildDossier()
        {
            var dossier = Surface(transform, "UnitDossier", UiSurfaceKind.Panel);
            _dossier = dossier.gameObject;
            PinBottomLeft(dossier.rectTransform, BattleUiTheme.Safe, 222f, 304f, 280f);

            Micro(dossier.transform, "DossierLabel", "符阵详情 UNIT", BattleUiTheme.Ash, 20f, 16f, 264f, 14f);
            _dossierTitle = Text(dossier.transform, "Title", "", 24, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 20f, 34f, 264f, 36f);
            _dossierHealth = Text(dossier.transform, "Health", "", 15, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 20f, 70f, 264f, 22f);
            UiKit.RuleH(dossier.transform, "DossierRule", BattleUiTheme.Grid, 20f, 98f, 264f);
            _dossierStats = Text(dossier.transform, "Stats", "", 17, BattleUiTheme.Bone, TextAnchor.UpperLeft, 20f, 110f, 264f, 62f);
            _dossierBoom = Text(dossier.transform, "BoomStatus", "", 15, BattleUiTheme.Scarlet, TextAnchor.MiddleLeft, 20f, 176f, 264f, 22f);

            _boom = UiKit.Button(dossier.transform, "Boom", "符卡发动", UiButtonKind.Primary,
                () => { var tower = _game.TowerPlacer.SelectedTower; if (tower != null) tower.TriggerBoom(); });
            PinBottomLeft(_boom.GetComponent<RectTransform>(), 20f, 22f, 130f, 44f);
            _boomPanel = _boom.GetComponent<UiPanel>();
            _boomText = _boom.GetComponentInChildren<UiText>();

            _retreat = UiKit.Button(dossier.transform, "Retreat", "撤退", UiButtonKind.Ghost,
                () => _game.TowerPlacer.RetreatSelected());
            PinBottomLeft(_retreat.GetComponent<RectTransform>(), 160f, 22f, 124f, 44f);
            _retreatLabel = _retreat.GetComponentInChildren<UiText>();
            _dossier.SetActive(false);
        }

        private void BuildPause()
        {
            _pause = new GameObject("PauseScreen", typeof(RectTransform)).gameObject;
            _pause.transform.SetParent(transform, false);
            UiFactory.Stretch((RectTransform)_pause.transform);

            var shade = Surface(_pause.transform, "Shade", UiSurfaceKind.Veil);
            UiFactory.Stretch(shade.rectTransform);

            var menu = Surface(_pause.transform, "Menu", UiSurfaceKind.Panel);
            menu.Corners(UiCorner.Diagonal, 14f);
            PinLeft(menu.rectTransform, 150f, 0f, 330f, 392f);

            // 水印单独定位，不参与菜单按钮布局。
            var watermark = Text(_pause.transform, "PauseWatermark", "PAUSED", 74,
                BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .06f), TextAnchor.MiddleRight, 0f, 0f, 1f, 1f);
            PinRight(watermark.rectTransform, 120f, 170f, 430f, 110f);

            Micro(menu.transform, "MenuLabel", "暂停 PAUSED", BattleUiTheme.Scarlet, 30f, 28f, 270f, 14f);
            Text(menu.transform, "Hint", "SPACE 继续  ·  X 倍速", 14, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 30f, 50f, 270f, 22f);

            var resume = UiKit.Button(menu.transform, "Resume", "继续", UiButtonKind.Primary, () => _game.TogglePause());
            PinTopLeft(resume.GetComponent<RectTransform>(), 30f, 96f, 270f, 48f);
            var retry = UiKit.Button(menu.transform, "Retry", "重新开始", UiButtonKind.Ghost, () => _game.Restart());
            PinTopLeft(retry.GetComponent<RectTransform>(), 30f, 152f, 270f, 48f);
            var music = UiKit.Button(menu.transform, "Music", "音乐　开", UiButtonKind.Ghost,
                () => TowerDefense.Effects.BattleMusic.Toggle());
            PinTopLeft(music.GetComponent<RectTransform>(), 30f, 208f, 270f, 48f);
            _musicLabel = music.GetComponentInChildren<UiText>();
            var home = UiKit.Button(menu.transform, "Home", "返回标题", UiButtonKind.Quiet, () => _game.ReturnToMenu());
            PinTopLeft(home.GetComponent<RectTransform>(), 30f, 268f, 270f, 48f);

            _pauseBrief = Text(_pause.transform, "Brief", "", 18, BattleUiTheme.Ash, TextAnchor.UpperLeft, 0f, 0f, 1f, 1f);
            PinRight(_pauseBrief.rectTransform, 150f, 0f, 330f, 240f);
            _pause.SetActive(false);
        }

        /// <summary>结算评级、统计、纪录及返回入口。</summary>
        private void BuildResult()
        {
            _result = new GameObject("ResultScreen", typeof(RectTransform)).gameObject;
            _result.transform.SetParent(transform, false);
            UiFactory.Stretch((RectTransform)_result.transform);

            var background = Surface(_result.transform, "Background", UiSurfaceKind.Veil);
            UiFactory.Stretch(background.rectTransform);
            background.Flat(BattleUiTheme.WithAlpha(BattleUiTheme.Ink, .96f))
                      .Border(Color.clear, 0f);

            var reportLabel = Micro(_result.transform, "ReportLabel", "RESULT / 作战结果", BattleUiTheme.Scarlet, 120f, 68f, 700f, 16f);
            reportLabel.SetCharacterSpacing(2.4f);

            var divider = Image(_result.transform, "ReportDivider", BattleUiTheme.Grid);
            divider.raycastTarget = false;
            PinTopLeft(divider.rectTransform, 120f, 110f, 900f, 1f);

            _resultTitle = Text(_result.transform, "Title", "异变解决", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 0f, 1f, 1f);
            PinLeft(_resultTitle.rectTransform, 120f, 160f, 500f, 48f);

            _resultBand = Image(_result.transform, "ResultBand", BattleUiTheme.Bone);
            PinLeft(_resultBand.rectTransform, 146f, -20f, 4f, 220f);
            _resultBand.raycastTarget = false;

            _resultRecord = Micro(_result.transform, "Record", "新纪录  NEW RECORD", BattleUiTheme.Scarlet, 0f, 0f, 320f, 18f);
            PinLeft(_resultRecord.rectTransform, 122f, 214f, 320f, 18f);
            _resultRecord.gameObject.SetActive(false);

            _resultGrade = Text(_result.transform, "Grade", "S", BattleUiTheme.Type.Display, BattleUiTheme.Bone, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
            PinLeft(_resultGrade.rectTransform, 160f, -20f, 250f, 250f);


            _resultStats = Text(_result.transform, "Stats", "", 22, BattleUiTheme.Bone, TextAnchor.UpperLeft, 0f, 0f, 1f, 1f);
            PinRight(_resultStats.rectTransform, 180f, 0f, 400f, 260f);

            var retry = UiKit.Button(_result.transform, "Retry", "再来一次  [ R ]", UiButtonKind.Primary, () => _game.Restart());
            PinBottomLeft(retry.GetComponent<RectTransform>(), 120f, 95f, 270f, 54f);
            var home = UiKit.Button(_result.transform, "Home", "返回标题", UiButtonKind.Ghost, () => _game.ReturnToMenu());
            PinBottomLeft(home.GetComponent<RectTransform>(), 410f, 95f, 270f, 54f);

            _result.SetActive(false);
        }
    }
}
