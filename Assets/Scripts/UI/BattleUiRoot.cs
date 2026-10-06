using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// 战斗 UI 装配入口，通过 Init 注入 GameManager。
    /// 状态刷新、界面装配和基础控件分别放在 partial 文件中。
    /// </summary>
    public sealed partial class BattleUiRoot : MonoBehaviour
    {
        private sealed class DeployCard
        {
            public TowerType Type;
            public Button Button;
            public UiPanel Panel;
            public UiText Cost;
            public UiText State;
            public RectTransform Rect;
            /// <summary>选中时亮起的辉光。</summary>
            public UiHalo Halo;
            public Vector2 RestPosition;
            /// <summary>可以开始落位的时间（unscaled）。错峰用，见 <c>AnimateHand</c>。</summary>
            public float EnterAt;
            public int VisualState = -1;
        }

        // 卡片底边固定，选中时增加高度。
        private const float CardHeight = 136f;
        private const float CardExpand = 18f;

        private readonly TowerType[] _order = { TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow, TowerType.Heal };
        private readonly DeployCard[] _cards = new DeployCard[5];

        private GameManager _game;
        private UiText _mission;
        private UiText _stageNumber;
        private UiText _missionSub;
        private UiText _kills;
        private UiText _enemies;
        private UiText _enemiesCaption;
        private UiText _barrier;
        private UiText _gold;
        private UiText _slots;
        private UiText _speed;
        private readonly UiText[] _speedLabels = new UiText[4];
        /// <summary>当前倍速档下方的状态标记。</summary>
        private readonly Image[] _speedBars = new Image[4];
        private UiText _power;
        private UiText _powerBonus;
        private Image _powerFill;
        private Button _barrage;
        private UiPanel _barragePanel;
        private UiHalo _barrageHalo;
        private UiText _barrageLabel;
        private int _barrageState = -1;
        private Button _targeting;
        private UiText _targetingLabel;
        private GameObject _enemyIntel;
        private UiText _enemyIntelTitle;
        private UiText _enemyIntelHealth;
        private UiText _enemyIntelStats;
        private Enemy _hoverCandidate;
        private Enemy _inspectedEnemy;
        private float _hoverStartedAt;
        private GameObject _alert;
        private UiPanel _alertPanel;
        private UiText _alertTitle;
        private UiText _alertSub;
        private Image _alertStripes;
        private Coroutine _alertRoutine;
        private GameObject _dossier;
        private UiText _dossierTitle;
        private UiText _dossierStats;
        private UiText _dossierHealth;
        private UiText _dossierBoom;
        private Button _boom;
        private UiPanel _boomPanel;
        private UiText _boomText;
        private int _boomState = -1;
        private UiText _retreatLabel;
        private Button _retreat;
        private GameObject _pause;
        private UiText _pauseBrief;
        private UiText _musicLabel;
        private GameObject _result;
        private UiText _resultTitle;
        private UiText _resultGrade;
        private UiText _resultStats;
        private UiText _resultRecord;
        private Image _resultBand;
        private bool _resultShown;
        private WaveSpawner _spawner;
        private string _roundTitle = "少女作战中…";
        private float _nextRefreshTime;
        private bool _hudHidden;
        private int _lastGold = -1;
        private int _lastKills = -1;
        private Coroutine _goldPulse;
        private Coroutine _killsPulse;

        private const float RefreshInterval = 0.08f;
        private const float EnemyHoverDelay = 0.12f;

        /// <summary>由 GameManager 在创建 Canvas 后立刻调用；避免 UI 自行查询全局单例。</summary>
        public void Init(GameManager game)
        {
            if (_game != null || game == null) return;
            _game = game;
            ConfigureCanvas();
            Build();
            Enemy.OnBossSpawned += OnBossSpawned;
            Enemy.OnBossKilled += OnBossKilled;
        }

        private void ConfigureCanvas()
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = new Vector2(960f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }

        private void OnDestroy()
        {
            if (_spawner != null) _spawner.OnRoundStart -= OnRoundStart;
            Enemy.OnBossSpawned -= OnBossSpawned;
            Enemy.OnBossKilled -= OnBossKilled;
        }

        private void Update()
        {
            if (_game == null) return;
            RefreshLayout();
            HandleKeys();
            HookSpawner();
            AnimateHand();
            UpdateEnemyIntel();
            if (Time.unscaledTime < _nextRefreshTime) return;
            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshReadout(); RefreshHand(); RefreshDossier(); RefreshScreens();
        }

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.R)) { _game.Restart(); return; }
            if (_game.State != GameState.Running) return;
            if (Input.GetKeyDown(KeyCode.Space)) _game.TogglePause();
            if (Input.GetKeyDown(KeyCode.X)) _game.CycleSpeed();
            if (Input.GetKeyDown(KeyCode.E)) _game.TriggerBarrage();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_game.IsPaused) _game.TogglePause();
                else _game.TowerPlacer.ClearSelection();
            }
            if (_game.IsPaused) return;
            for (int i = 0; i < _order.Length; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) _game.TowerPlacer.SelectTower(_order[i]);
        }

        private void HookSpawner()
        {
            if (_spawner == _game.WaveSpawner) return;
            if (_spawner != null) _spawner.OnRoundStart -= OnRoundStart;
            _spawner = _game.WaveSpawner;
            _hoverCandidate = null;
            _inspectedEnemy = null;
            _enemyIntel.SetActive(false);
            if (_alertRoutine != null) StopCoroutine(_alertRoutine);
            _alertRoutine = null;
            _alert.SetActive(false);
            _roundTitle = "少女作战中…";
            if (_spawner != null) _spawner.OnRoundStart += OnRoundStart;
        }
        private void OnRoundStart(int index, string title, string eng)
        {
            _roundTitle = title;
            ShowAlert(title, eng, BattleUiTheme.Action, 1.15f);
        }

        private void OnBossSpawned(Enemy boss)
        {
            ShowAlert("强敌来袭！", "CAUTION!", BattleUiTheme.Danger, 1.8f, hazard: true);
        }

        private void OnBossKilled(Enemy boss)
        {
            ShowAlert("退治完毕！", "CLEAR!", BattleUiTheme.Ready, 1.5f);
        }

        private void ShowAlert(string title, string sub, Color accent, float duration, bool hazard = false)
        {
            if (_alert == null) return;
            if (_alertRoutine != null) StopCoroutine(_alertRoutine);

            // 背景保持暗色，描边与辉光随事件色变化。
            _alertPanel.Flat(BattleUiTheme.Deepen(accent, .90f))
                       .Border(BattleUiTheme.WithAlpha(accent, .55f), BattleUiTheme.Border)
                       .Glow(BattleUiTheme.WithAlpha(accent, .26f), 22f);

            // 警戒条纹仅用于强敌预警。
            if (_alertStripes != null)
            {
                _alertStripes.color = BattleUiTheme.WithAlpha(accent, .92f);
                _alertStripes.gameObject.SetActive(hazard);
            }

            _alertTitle.content = title;
            _alertTitle.color = BattleUiTheme.Bone;
            _alertSub.content = sub;
            _alertSub.color = BattleUiTheme.WithAlpha(accent, .90f);
            _alert.SetActive(true);
            UiEntrance.Play((RectTransform)_alert.transform, Vector2.down * 12f, 0f, BattleUiTheme.Motion.Normal);
            _alertRoutine = StartCoroutine(HideAlertAfter(duration));
        }

        private IEnumerator HideAlertAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            var group = _alert.GetComponent<CanvasGroup>();
            if (group != null)
            {
                for (float t = 0f; t < BattleUiTheme.Motion.Normal; t += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - Mathf.Clamp01(t / BattleUiTheme.Motion.Normal);
                    yield return null;
                }
                group.alpha = 0f;
            }
            _alert.SetActive(false);
            _alertRoutine = null;
        }

        private void UpdateEnemyIntel()
        {
            Enemy enemy = PickEnemy();
            if (enemy != _hoverCandidate)
            {
                _hoverCandidate = enemy;
                _hoverStartedAt = Time.unscaledTime;
                _inspectedEnemy = null;
                _enemyIntel.SetActive(false);
                return;
            }
            if (enemy == null || Time.unscaledTime - _hoverStartedAt < EnemyHoverDelay)
            {
                _enemyIntel.SetActive(false);
                return;
            }

            _enemyIntel.SetActive(true);
            var def = enemy.Definition;
            if (def == null) return;
            if (_inspectedEnemy != enemy)
            {
                _inspectedEnemy = enemy;
                _enemyIntelTitle.content = def.DisplayName + (enemy.IsBoss ? " / 强敌" : "");
            }
            _enemyIntelStats.content = "速度 " + def.Speed.ToString("0.##") + "  ·  灵力 " + def.SpiritReward
                + "\n破界 " + def.DamageToBase + "  ·  " + (enemy.IsSlowed ? "低速中" : "通常");
            _enemyIntelHealth.content = "耐久 " + Mathf.CeilToInt(enemy.Health) + " / " + Mathf.CeilToInt(enemy.MaxHealth)
                + "  ·  距离 " + Mathf.Max(0, Mathf.RoundToInt(enemy.DistanceToEnd));
        }

        private Enemy PickEnemy()
        {
            var cam = Camera.main;
            if (cam == null || _game.State != GameState.Running || _game.IsPaused) return null;
            if (!cam.pixelRect.Contains(Input.mousePosition)) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, GameConfig.EnemyVisualHeight));
            if (!plane.Raycast(ray, out float hit)) return null;
            Vector2 point = ray.GetPoint(hit);
            Enemy enemy = _game.GetNearestEnemy(point, 2.2f);
            if (enemy == null || !enemy.IsAlive) return null;
            return Vector2.Distance(point, enemy.transform.position) <= enemy.Radius + .28f ? enemy : null;
        }
        private void RefreshReadout()
        {
            int roundIndex = _spawner != null ? _spawner.CurrentRoundIndex : -1;
            int roundTotal = _spawner != null ? _spawner.CurrentRoundTotal : 0;
            int roundSpawned = _spawner != null ? _spawner.CurrentRoundSpawned : 0;
            _mission.content = roundIndex < 0 ? "作战准备" : _roundTitle;
            _stageNumber.content = roundIndex < 0 ? "00" : (roundIndex + 1).ToString("00");
            _stageNumber.color = roundIndex < 0 ? BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .55f) : BattleUiTheme.Bone;
            _missionSub.content = (_game.IsPractice ? "练习 / " : "") + (_spawner != null ? _spawner.PhaseText : "准备");
            // 首次刷新不播放脉冲，仅在计数增加时提示。
            int kills = _game.TotalKills;
            if (kills > _lastKills && _lastKills >= 0) Pulse(_kills.rectTransform, ref _killsPulse);
            _lastKills = kills;
            _kills.content = kills.ToString();
            _enemies.content = roundTotal <= 0 ? "0 / 0" : roundSpawned + " / " + roundTotal;
            _enemiesCaption.content = roundTotal <= 0 ? "出现 / 总数" : "出现 / 总数 · 场上 " + _game.EnemyCount;
            _barrier.content = _game.Lives + " / " + GameConfig.StartingLives;
            _barrier.color = _game.Lives <= 1 ? BattleUiTheme.Scarlet : BattleUiTheme.Bone;
            _speed.content = _game.SpeedScale.ToString("0.#") + "×";

            for (int i = 0; i < _speedLabels.Length; i++)
            {
                if (_speedLabels[i] == null) continue;
                bool active = Mathf.Approximately(_game.SpeedScale, i == 0 ? .5f : i == 1 ? 1f : i == 2 ? 2f : 4f);
                _speedLabels[i].color = active ? BattleUiTheme.Bone : BattleUiTheme.Ash;
                if (_speedBars[i] != null) _speedBars[i].enabled = active;
            }

            float power = _game.Power;
            _power.content = power.ToString("0.00");
            _powerBonus.content = "增伤 +" + Mathf.RoundToInt(power * GameConfig.PowerDamageBonus * 100f) + "%";
            _powerFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(power / GameConfig.MaxPower), 1f);
            _powerFill.color = _game.CanBarrage ? BattleUiTheme.Scarlet : BattleUiTheme.WithAlpha(BattleUiTheme.Scarlet, .55f);
            _powerBonus.color = _game.CanBarrage ? BattleUiTheme.Scarlet : BattleUiTheme.Ash;
            _barrage.interactable = _game.CanBarrage && _game.State == GameState.Running && !_game.IsPaused;
            RefreshBarrage();
        }

        /// <summary>就绪时显示绯色按钮，充能时使用暗底与灰字。</summary>
        private void RefreshBarrage()
        {
            int state = _game.CanBarrage ? 1 : 0;
            // 持续同步辉光目标；面板网格只在状态变化时更新。
            if (_barrageHalo != null) _barrageHalo.Set(state == 1);

            if (_barrageState != state)
            {
                _barrageState = state;
                if (state == 1)
                {
                    _barragePanel.Flat(BattleUiTheme.Scarlet)
                                 .Border(BattleUiTheme.Lift(BattleUiTheme.Scarlet, .45f), BattleUiTheme.Border)
                                 .Glow(BattleUiTheme.GlowScarlet, 10f);
                }
                else
                {
                    _barragePanel.Flat(BattleUiTheme.Char)
                                 .Border(BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .09f), BattleUiTheme.Border)
                                 .Glow(Color.clear, 0f);
                }
                _barrageLabel.content = state == 1 ? "E  发动" : "充能中";
                _barrageLabel.color = state == 1 ? BattleUiTheme.Ink : BattleUiTheme.Ash;
            }
        }
        private void RefreshHand()
        {
            // 仅在灵力增加时播放脉冲。
            int spirit = _game.Spirit;
            if (spirit > _lastGold && _lastGold >= 0) Pulse(_gold.rectTransform, ref _goldPulse);
            _lastGold = spirit;
            _gold.content = spirit.ToString();
            _slots.content = "出阵 " + _game.TowerCount + " / " + GameConfig.MaxTowers;
            _slots.color = _game.CanPlaceTower ? BattleUiTheme.Ash : BattleUiTheme.Scarlet;
            _targetingLabel.content = "目标 / " + TargetingName(_game.TargetingPriority);
            _targeting.interactable = _game.State == GameState.Running && !_game.IsPaused;

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                var def = GameConfig.Towers[card.Type];
                int lack = Mathf.Max(0, def.Cost - _game.Spirit);
                bool allowed = lack == 0 && _game.CanPlaceTower && _game.State == GameState.Running && !_game.IsPaused;
                bool selected = _game.TowerPlacer != null && _game.TowerPlacer.SelectedTower == null && _game.TowerPlacer.SelectedType == card.Type;

                card.Button.interactable = allowed;
                card.Cost.color = allowed || lack == 0 ? BattleUiTheme.Bone : BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .75f);

                card.State.content = _game.IsPaused ? "暂停中"
                    : lack > 0 ? "灵力差 " + lack
                    : !_game.CanPlaceTower ? "出阵已满"
                    : selected ? "待出阵" : "选择";
                card.State.color = lack > 0 || !_game.CanPlaceTower
                    ? BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .70f)
                    : selected ? BattleUiTheme.Scarlet : BattleUiTheme.Ash;

                ApplyCardVisualState(card, selected, allowed);
            }
        }

        private void AnimateHand()
        {
            if (_game.TowerPlacer == null) return;
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                // 延迟期间保持起点，避免首次 Update 提前开始入场动画。
                if (Time.unscaledTime < card.EnterAt) continue;

                bool selected = _game.TowerPlacer.SelectedTower == null && _game.TowerPlacer.SelectedType == card.Type;
                // 位置负责入场归位，选中状态只改变高度，保持底边不动。
                card.Rect.anchoredPosition = Vector2.Lerp(card.Rect.anchoredPosition, card.RestPosition,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                var size = card.Rect.sizeDelta;
                size.y = Mathf.Lerp(size.y, CardHeight + (selected ? CardExpand : 0f),
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                card.Rect.sizeDelta = size;
            }
        }

        /// <summary>0.18 秒的单次缩放脉冲：1 → 1.12 → 1。</summary>
        private IEnumerator PulseRoutine(RectTransform rect)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / .18f;
                float scale = 1f + .12f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private void Pulse(RectTransform rect, ref Coroutine running)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(PulseRoutine(rect));
        }

        /// <summary>
        /// 按可部署、选中、不可部署三种状态更新卡面亮度与描边。
        /// 选中时的高度变化由 AnimateHand 处理。
        /// </summary>
        private static void ApplyCardVisualState(DeployCard card, bool selected, bool allowed)
        {
            // 不可部署时选中状态仍可能变化，辉光需要在提前返回前同步。
            card.Halo.Set(selected);

            int state = !allowed ? 0 : (selected ? 2 : 1);
            if (card.VisualState == state) return;
            card.VisualState = state;

            switch (state)
            {
                case 2:
                    // 动态外辉光由 UiHalo 处理，这里只设置固定的贴边辉光。
                    card.Panel.Flat(BattleUiTheme.Stone)
                              .Border(BattleUiTheme.Scarlet, BattleUiTheme.Border)
                              .Glow(BattleUiTheme.GlowScarlet, 9f);
                    break;

                case 1:
                    card.Panel.Flat(BattleUiTheme.Slate)
                              .Border(BattleUiTheme.Edge, BattleUiTheme.Border)
                              .Glow(Color.clear, 0f);
                    break;

                default:
                    card.Panel.Flat(BattleUiTheme.Char)
                              .Border(BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .07f), BattleUiTheme.Border)
                              .Glow(Color.clear, 0f);
                    break;
            }
        }
        private void RefreshDossier()
        {
            Tower tower = _game.TowerPlacer != null ? _game.TowerPlacer.SelectedTower : null;
            _dossier.SetActive(tower != null && _game.State == GameState.Running);
            if (tower == null) return;
            var def = tower.Definition; _dossierTitle.content = def.DisplayName + " / " + Role(def);
            _dossierHealth.content = "耐久 " + Mathf.CeilToInt(tower.Health) + " / " + Mathf.CeilToInt(tower.MaxHealth);
            _dossierStats.content = Stats(def);
            int refund = Mathf.RoundToInt(def.Cost * GameConfig.RetreatRefundRatio);
            _retreatLabel.content = "撤退  +" + refund;
            _retreat.interactable = !_game.IsPaused;
            _boom.gameObject.SetActive(tower.CanBoom);
            if (!tower.CanBoom) { _dossierBoom.content = ""; return; }
            _dossierBoom.content = tower.BoomReady ? "符卡就绪" : "符卡充能 " + Mathf.RoundToInt(tower.BoomReadyRatio * 100f) + "%";
            _boom.interactable = tower.BoomReady && !_game.IsPaused;

            // 区分可发动、已就绪但暂停、充能中的三种状态。
            int state = _boom.interactable ? 2 : tower.BoomReady ? 1 : 0;
            if (_boomState != state)
            {
                _boomState = state;
                if (state == 2)
                {
                    _boomPanel.Flat(BattleUiTheme.Scarlet)
                              .Border(BattleUiTheme.Lift(BattleUiTheme.Scarlet, .45f), BattleUiTheme.Border)
                              .Glow(BattleUiTheme.GlowScarlet, 14f);
                    _dossierBoom.color = BattleUiTheme.Scarlet;
                }
                else
                {
                    _boomPanel.Flat(state == 1 ? BattleUiTheme.Stone : BattleUiTheme.Char)
                              .Border(state == 1 ? BattleUiTheme.EdgeStrong : BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .09f), BattleUiTheme.Border)
                              .Glow(Color.clear, 0f);
                    _dossierBoom.color = state == 1 ? BattleUiTheme.Bone : BattleUiTheme.Ash;
                }
                _boomText.color = state == 2 ? BattleUiTheme.Ink : state == 1 ? BattleUiTheme.Bone : BattleUiTheme.Ash;
            }
        }
        private void RefreshScreens()
        {
            bool finished = _game.State == GameState.GameOver || _game.State == GameState.Victory;
            if (finished && !_resultShown) { ShowResult(_game.GetResult()); _resultShown = true; }
            if (!finished) { _resultShown = false; _result.SetActive(false); }
            SetHudHidden(finished);
            _pause.SetActive(_game.State == GameState.Running && _game.IsPaused);
            if (_pause.activeSelf)
            {
                int roundIndex = _spawner != null ? _spawner.CurrentRoundIndex : -1;
                int roundTotal = _spawner != null ? _spawner.CurrentRoundTotal : 0;
                _pauseBrief.content =
                    "STATUS\n\n" +
                    "波次    " + (roundIndex < 0 ? "—" : (roundIndex + 1) + " / " + roundTotal) + "\n" +
                    "击破    " + _game.TotalKills + "\n" +
                    "残机    " + _game.Lives + " / " + GameConfig.StartingLives + "\n" +
                    "得分    " + _game.Score;
                if (_musicLabel != null)
                    _musicLabel.content = TowerDefense.Effects.BattleMusic.IsMuted ? "音乐　关" : "音乐　开";
            }
        }

        /// <summary>结算时隐藏常规 HUD，避免文字叠在结算内容后方。</summary>
        private void SetHudHidden(bool hidden)
        {
            if (_hudHidden == hidden) return;
            _hudHidden = hidden;
            foreach (var name in new[] { "TopBand", "BottomBand", "MissionTag", "BattleReadout", "TimeControls", "DeploymentHand", "BarrageCommand" })
                if (transform.Find(name) != null) transform.Find(name).gameObject.SetActive(!hidden);
        }
        private void ShowResult(GameResult result)
        {
            _resultTitle.content = (result.IsPractice ? "练习 / " : "") + (result.Victory ? "异变解决" : "满身疮痍");
            _resultTitle.color = result.Victory ? BattleUiTheme.Bone : BattleUiTheme.Scarlet;

            if (_resultBand != null) _resultBand.color = result.Victory ? BattleUiTheme.Bone : BattleUiTheme.Scarlet;

            _resultGrade.content = result.Grade;
            _resultGrade.color = (result.Grade == "Φ" || result.Grade == "V" || result.Grade == "S")
                ? BattleUiTheme.Bone : BattleUiTheme.Ash;

            _resultStats.content =
                (result.IsPractice
                    ? "单关练习    第 " + (_game.StartStage + 1) + " 关 · " + (result.CompletedStages > 0 ? "已通过" : "未通过")
                    : "战役通关    " + result.CompletedStages + " / " + GameConfig.Rounds.Length) + "\n" +
                "综合评分    " + result.Rating + " / 100\n" +
                "得分    " + result.Score + "\n" +
                "击破    " + result.TotalKills + "\n" +
                "漏过    " + result.LeakedEnemies + "\n" +
                "残机    " + result.LivesRemaining + "\n" +
                "连击    " + result.BestCombo;
            if (_resultRecord != null) _resultRecord.gameObject.SetActive(result.NewRecord && result.Victory);
            _result.SetActive(true);
        }

        private static string TargetingName(TargetingPriority priority) { switch (priority) { case TargetingPriority.LowestHealth: return "残血"; case TargetingPriority.ClosestToEnd: return "突进"; default: return "最近"; } }
        private static string Role(TowerDefinition d) { return d.HealPerSecond > 0 ? "修复" : d.Damage <= 0 ? "减速" : d.SplashRadius > 0 ? "范围" : "攻击"; }
        private static string Stats(TowerDefinition d) { if (d.HealPerSecond > 0) return "治疗 " + d.HealPerSecond.ToString("0") + "/秒\n范围 " + d.RangeCells + " 格"; if (d.Damage <= 0) return "减速 " + Mathf.RoundToInt((1f-d.SlowFactor)*100f) + "% / " + d.SlowDuration.ToString("0.#") + " 秒\n范围 " + d.RangeCells + " 格"; return "伤害 " + d.Damage.ToString("0") + "  ·  频率 " + d.FireRate.ToString("0.#") + "/秒\n范围 " + d.RangeCells + " 格"; }

    }
}


