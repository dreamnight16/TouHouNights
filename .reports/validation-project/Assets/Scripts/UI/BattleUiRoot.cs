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
    /// “结界作战档案”战斗界面。这个类是唯一的战斗 UI 装配入口；它通过 Init 注入
    /// GameManager。状态刷新、界面装配和视觉基础分别放在独立 partial 文件。
    /// </summary>
    public sealed partial class BattleUiRoot : MonoBehaviour
    {
        private sealed class DeployCard
        {
            public TowerType Type;
            public Button Button;
            public Image Panel;
            public UiText Cost;
            public UiText State;
            public RectTransform Rect;
            public Image SelectionRail;
            public Image StatusPip;
            public Vector2 RestPosition;
            public int VisualState = -1;
        }

        private readonly TowerType[] _order = { TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow, TowerType.Heal };
        private readonly DeployCard[] _cards = new DeployCard[5];

        private GameManager _game;
        private UiText _mission;
        private UiText _stageNumber;
        private UiText _missionSub;
        private UiText _wave;
        private UiText _enemies;
        private UiText _barrier;
        private UiText _gold;
        private UiText _slots;
        private UiText _speed;
        private readonly UiText[] _speedLabels = new UiText[4];
        private UiText _power;
        private UiText _powerBonus;
        private Image _powerFill;
        private Button _barrage;
        private UiText _barrageLabel;
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
        private Image _alertPanel;
        private Image _alertMarker;
        private UiText _alertTitle;
        private UiText _alertSub;
        private Coroutine _alertRoutine;
        private GameObject _dossier;
        private UiText _dossierTitle;
        private UiText _dossierStats;
        private UiText _dossierHealth;
        private UiText _dossierBoom;
        private Button _boom;
        private UiText _retreatLabel;
        private Button _retreat;
        private GameObject _pause;
        private UiText _pauseBrief;
        private GameObject _result;
        private UiText _resultTitle;
        private UiText _resultGrade;
        private UiText _resultStats;
        private bool _resultShown;
        private WaveSpawner _spawner;
        private string _roundTitle = "少女作战中…";
        private float _nextRefreshTime;

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
            ShowAlert("强敌来袭！", "CAUTION!", BattleUiTheme.Danger, 1.8f);
        }

        private void OnBossKilled(Enemy boss)
        {
            ShowAlert("退治完毕！", "CLEAR!", BattleUiTheme.Ready, 1.5f);
        }

        private void ShowAlert(string title, string sub, Color accent, float duration)
        {
            if (_alert == null) return;
            if (_alertRoutine != null) StopCoroutine(_alertRoutine);
            _alertPanel.color = Color.Lerp(BattleUiTheme.SurfaceDeep, accent, .12f);
            _alertMarker.color = accent;
            _alertTitle.content = title;
            _alertTitle.color = accent;
            _alertSub.content = sub;
            _alert.SetActive(true);
            _alertRoutine = StartCoroutine(HideAlertAfter(duration));
        }

        private IEnumerator HideAlertAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
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
            _enemyIntelStats.content = "速度 " + def.Speed.ToString("0.##") + "  ·  灵力 " + def.GoldReward
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
            _mission.content = roundIndex < 0 ? "东方阵符录" : _roundTitle;
            _stageNumber.content = roundIndex < 0 ? "—" : (roundIndex + 1).ToString("00");
            _missionSub.content = (_game.IsPractice ? "练习 / " : "") + (_spawner != null ? _spawner.PhaseText : "准备");
            _wave.content = roundIndex < 0 ? "— / " + GameConfig.Rounds.Length : (roundIndex + 1) + " / " + GameConfig.Rounds.Length;
            _enemies.content = roundTotal <= 0 ? "0 / 0" : roundSpawned + "/" + roundTotal + " · " + _game.EnemyCount;
            _barrier.content = _game.Lives + " / " + GameConfig.StartingLives;
            _speed.content = _game.SpeedScale.ToString("0.#") + "×";
            for (int i = 0; i < _speedLabels.Length; i++)
            {
                bool active = Mathf.Approximately(_game.SpeedScale, i == 0 ? .5f : i == 1 ? 1f : i == 2 ? 2f : 4f);
                _speedLabels[i].color = active ? BattleUiTheme.Ready : BattleUiTheme.Muted;
            }
            float power = _game.Power; _power.content = "P " + power.ToString("0.00");
            _powerBonus.content = "增伤 +" + Mathf.RoundToInt(power * GameConfig.PowerDamageBonus * 100f) + "%";
            _powerFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(power / GameConfig.MaxPower), 1f);
            _powerFill.color = _game.CanBarrage ? BattleUiTheme.Ready : BattleUiTheme.Action;
            _barrage.interactable = _game.CanBarrage && _game.State == GameState.Running && !_game.IsPaused;
            _barrageLabel.content = _game.CanBarrage ? "E!" : "E";
            _barrageLabel.color = _game.CanBarrage ? BattleUiTheme.Ready : BattleUiTheme.Text;
        }
        private void RefreshHand()
        {
            _gold.content = "灵力 " + _game.Gold;
            _slots.content = "出阵 " + _game.TowerCount + " / " + GameConfig.MaxTowers;
            _slots.color = _game.CanPlaceTower ? BattleUiTheme.Muted : BattleUiTheme.Danger;
            _targetingLabel.content = "目标 / " + TargetingName(_game.TargetingPriority);
            _targeting.interactable = _game.State == GameState.Running && !_game.IsPaused;
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i]; var def = GameConfig.Towers[card.Type]; int lack = Mathf.Max(0, def.Cost - _game.Gold);
                bool allowed = lack == 0 && _game.CanPlaceTower && _game.State == GameState.Running && !_game.IsPaused;
                bool selected = _game.TowerPlacer != null && _game.TowerPlacer.SelectedTower == null && _game.TowerPlacer.SelectedType == card.Type;
                card.Button.interactable = allowed;
                card.State.content = _game.IsPaused ? "暂停中" : lack > 0 ? "灵力差 " + lack : (!_game.CanPlaceTower ? "出阵已满" : (selected ? "待出阵" : "选择"));
                card.State.color = lack > 0 || !_game.CanPlaceTower ? BattleUiTheme.Danger : (selected ? BattleUiTheme.Ready : BattleUiTheme.Muted);
                card.StatusPip.color = lack > 0 || !_game.CanPlaceTower ? BattleUiTheme.Danger : (selected ? BattleUiTheme.Ready : BattleUiTheme.Action);
                ApplyCardVisualState(card, selected, allowed);
                card.SelectionRail.gameObject.SetActive(selected);
            }
        }

        private void AnimateHand()
        {
            if (_game.TowerPlacer == null) return;
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                bool selected = _game.TowerPlacer.SelectedTower == null && _game.TowerPlacer.SelectedType == card.Type;
                Vector2 target = card.RestPosition + (selected ? Vector2.up * 9f : Vector2.zero);
                card.Rect.anchoredPosition = Vector2.Lerp(card.Rect.anchoredPosition, target,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                card.SelectionRail.color = BattleUiTheme.WithAlpha(BattleUiTheme.Ready,
                    .8f + .2f * Mathf.Sin(Time.unscaledTime * 2f));
            }
        }

        private static void ApplyCardVisualState(DeployCard card, bool selected, bool allowed)
        {
            int state = !allowed ? 0 : (selected ? 2 : 1);
            if (card.VisualState == state) return;
            card.VisualState = state;

            Color normal = allowed
                ? (selected ? BattleUiTheme.Surface : BattleUiTheme.SurfaceDeep)
                : BattleUiTheme.WithAlpha(BattleUiTheme.SurfaceDeep, .55f);
            var colors = card.Button.colors;
            colors.normalColor = normal;
            colors.selectedColor = normal;
            colors.highlightedColor = Color.Lerp(normal, BattleUiTheme.Paper, .16f);
            colors.pressedColor = Color.Lerp(normal, BattleUiTheme.Ready, .28f);
            colors.disabledColor = normal;
            colors.fadeDuration = .08f;
            card.Button.colors = colors;
            card.Panel.color = Color.white;
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
            if (!tower.CanBoom) _dossierBoom.content = "";
            if (tower.CanBoom) { _dossierBoom.content = tower.BoomReady ? "符卡就绪" : "符卡充能 " + Mathf.RoundToInt(tower.BoomReadyRatio * 100f) + "%"; _boom.interactable = tower.BoomReady && !_game.IsPaused; }
        }
        private void RefreshScreens()
        {
            bool finished = _game.State == GameState.GameOver || _game.State == GameState.Victory;
            if (finished && !_resultShown) { ShowResult(_game.GetResult()); _resultShown = true; }
            if (!finished) { _resultShown = false; _result.SetActive(false); }
            _pause.SetActive(_game.State == GameState.Running && _game.IsPaused);
            if (_pause.activeSelf) _pauseBrief.content = "战况\n\n击破 " + _game.TotalKills + "\n残机 " + _game.Lives + "/" + GameConfig.StartingLives + "\n得分 " + _game.Score;
        }
        private void ShowResult(GameResult result)
        {
            _resultTitle.content = (_game.IsPractice ? "练习 / " : "") + (result.Victory ? "异变解决" : "满身疮痍"); _resultGrade.content = result.Grade;
            _resultStats.content = "结果\n\n得分  " + result.Score + "\n击破  " + result.TotalKills + "\n漏过  " + result.LeakedEnemies + "\n残机  " + result.LivesRemaining + "\n连击  " + result.BestCombo;
            _result.SetActive(true);
        }

        private static string TargetingName(TargetingPriority priority) { switch (priority) { case TargetingPriority.LowestHealth: return "残血"; case TargetingPriority.ClosestToEnd: return "突进"; default: return "最近"; } }
        private static string Role(TowerDefinition d) { return d.HealPerSecond > 0 ? "修复" : d.Damage <= 0 ? "减速" : d.SplashRadius > 0 ? "范围" : "攻击"; }
        private static string Stats(TowerDefinition d) { if (d.HealPerSecond > 0) return "治疗 " + d.HealPerSecond.ToString("0") + "/秒\n范围 " + d.RangeCells + " 格"; if (d.Damage <= 0) return "减速 " + Mathf.RoundToInt((1f-d.SlowFactor)*100f) + "% / " + d.SlowDuration.ToString("0.#") + " 秒\n范围 " + d.RangeCells + " 格"; return "伤害 " + d.Damage.ToString("0") + "  ·  频率 " + d.FireRate.ToString("0.#") + "/秒\n范围 " + d.RangeCells + " 格"; }

    }
}


