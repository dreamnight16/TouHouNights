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
            public UiPanel Panel;
            public UiText Cost;
            public UiText State;
            public RectTransform Rect;
            /// <summary>选中时亮起的辉光。「选中」这件事由光表达，不再叠一条更粗的边框。</summary>
            public UiHalo Halo;
            public Vector2 RestPosition;
            /// <summary>可以开始落位的时间（unscaled）。错峰用，见 <c>AnimateHand</c>。</summary>
            public float EnterAt;
            public int VisualState = -1;
        }

        /// <summary>符卡静止高度 / 选中时向上展开的增量（方舟干员卡：选中即长高，而不是变色）。</summary>
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
        /// <summary>当前倍速档下方的短横线。档位本身继续只用文字明度区分，线只负责「是这一档」。</summary>
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

            // 提示条是「事件」而不是「控件」：整块用事件色染色，只有标题保留满强度。
            // 底色压到接近墨黑，让那一抹 Accent 在满屏暗色里跳出来 —— 一屏最多同时存在一处。
            //
            // 平涂而非渐变。原来的两点渐变（.86 → .95）几乎看不出差别，却让这块面
            // 读起来像「上半截被照到」，而它只是一个存在 1.5 秒的通知。
            // 事件色交给描边和辉光说，它们本来就是这块面板上唯一可变的部分。
            _alertPanel.Flat(BattleUiTheme.Deepen(accent, .90f))
                       .Border(BattleUiTheme.WithAlpha(accent, .55f), BattleUiTheme.Border)
                       .Glow(BattleUiTheme.WithAlpha(accent, .26f), 22f);

            // 警戒条纹只在 Boss 这种非日常事件亮出：一条 45° 斜纹带压在面板左缘，
            // 颜色随事件色走。常规波次不配条纹 —— 条纹一旦随处可见，它就只是装饰。
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
            // 播报是「事件」——它得自己走进来。原来这里只有一句 SetActive(true)，
            // 面板是「啪」一下出现在屏幕上的，读起来像界面掉了一帧。
            //
            // 方向用**从下往上**，和出阵卡、以及整套 HUD 的入场是同一个方向：
            // 这块界面里「出现」这个词只有一个含义，就是「升上来」。
            UiEntrance.Play((RectTransform)_alert.transform, Vector2.down * 12f, 0f, BattleUiTheme.Motion.Normal);
            _alertRoutine = StartCoroutine(HideAlertAfter(duration));
        }

        private IEnumerator HideAlertAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            // 出场对称地再走一遍。一条无声无息消失的播报，和它无声无息地出现一样糟 ——
            // 玩家会以为是自己看漏了，而不是「它已经播完了」。
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
            // 备战期没有幕次可报。原来的占位是 40pt 的「—」，渲染出来像一条凭空出现的横线；
            // 「00」读起来才像一个还没开始的编号，配色也退到灰阶里去。
            _stageNumber.content = roundIndex < 0 ? "00" : (roundIndex + 1).ToString("00");
            _stageNumber.color = roundIndex < 0 ? BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .55f) : BattleUiTheme.Bone;
            _missionSub.content = (_game.IsPractice ? "练习 / " : "") + (_spawner != null ? _spawner.PhaseText : "准备");
            // 大数字都写成「A / B」形式：同一段宽度里字号才能开到最大，
            // 把「场上还有几只」这类次要信息挤到下面的小米字去。
            // 击破数变化时数字做一次 0.18s 的放大脉冲（方舟 DP 跳动的感觉）——
            // 战斗里数字偶尔在变，动一下才像「真的在计数」。
            int kills = _game.TotalKills;
            if (kills > _lastKills && _lastKills >= 0) Pulse(_kills.rectTransform, ref _killsPulse);
            _lastKills = kills;
            _kills.content = kills.ToString();
            _enemies.content = roundTotal <= 0 ? "0 / 0" : roundSpawned + " / " + roundTotal;
            _enemiesCaption.content = roundTotal <= 0 ? "出现 / 总数" : "出现 / 总数 · 场上 " + _game.EnemyCount;
            _barrier.content = _game.Lives + " / " + GameConfig.StartingLives;
            _barrier.color = _game.Lives <= 1 ? BattleUiTheme.Scarlet : BattleUiTheme.Bone;
            _speed.content = _game.SpeedScale.ToString("0.#") + "×";

            // 档位只用颜色区分（不重建网格，纯改 text color）：当前档 = 骨白，其余 = 灰。
            // 档下再加一条 2px 的骨白短线，不换颜色不换字号 —— 玩家扫一眼就知道自己在哪一档。
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

        /// <summary>
        /// 弹幕按钮只有两个状态，而且区别极大：就绪 = 整块绯色实心（全屏唯一的大面积 Accent），
        /// 充能中 = 沉成暗板 + 灰字。用「亮起来」而不是「变灰」表达就绪 ——
        /// 玩家在等的是它亮，不是等它可用。
        /// </summary>
        private void RefreshBarrage()
        {
            int state = _game.CanBarrage ? 1 : 0;
            // 灯是无状态的开关，每帧设一次无所谓；放在状态比较之外，就绪那一刻会自然闪起来。
            if (_barrageHalo != null) _barrageHalo.Set(state == 1);

            if (_barrageState != state)
            {
                _barrageState = state;
                if (state == 1)
                {
                    // 就绪 = 绯色实心 + 辉光。没有白色受光带 —— 和主按钮同一条规则：
                    // 实心的面积和那圈光已经足够把「现在可以了」喊出来，不需要再假装有盏灯。
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
            // 灵力增加时大数字脉冲一次；减少（买塔/撤退）保持安静 —— 花钱不需要庆祝。
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
                // 卡片上唯一的大字是消耗，所以它的颜色就是「买得起 / 买不起」的唯一信号：
                // 买得起 = 骨白（普通数字），买不起 = 灰（退到暗处）。不标红。
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
                // 还没轮到自己入场就待在起点别动，否则 AnimateHand 会在开场那一帧
                // 把卡片从起点直接拽到落点，错峰就白做了。
                if (Time.unscaledTime < card.EnterAt) continue;

                bool selected = _game.TowerPlacer.SelectedTower == null && _game.TowerPlacer.SelectedType == card.Type;
                // 选中的卡向上**长高** 18px（方舟干员卡的做法）：卡片底部钉死，
                // 只让顶边升起来，内容随顶边一起上移 —— 与其给整张卡换一圈描边，
                // 不如让它的体积本身说「我起来了」。位置只负责入场归位，不再叠加位移。
                card.Rect.anchoredPosition = Vector2.Lerp(card.Rect.anchoredPosition, card.RestPosition,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                var size = card.Rect.sizeDelta;
                size.y = Mathf.Lerp(size.y, CardHeight + (selected ? CardExpand : 0f),
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                card.Rect.sizeDelta = size;
            }
        }

        /// <summary>0.18s 的单次放大脉冲：1 → 1.12 → 1。数字「跳了一下」，但没有位移、没有颜色变化。</summary>
        private System.Collections.IEnumerator PulseRoutine(RectTransform rect)
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
        /// 卡片只有三种状态，而且全部靠「本体亮度」区分，不靠色相：
        /// 待选 = 石墨、选中 = 板岩 + 绯色描边与辉光、不可出 = 退到炭。
        ///
        /// 三级台阶现在是**平涂**的，一档一档干净地亮上去 —— 这正是卡片唯一的深度语言：
        /// 亮一档就是浮高一层。选中态在此之上再加两样：绯色描边（颜色）与 UiHalo（光），
        /// 外加 AnimateHand 把它推高 9px（动）。三样都真的对应一个状态。
        ///
        /// 不可出**绝不标红** —— 绯红是危险与就绪专用的，被「钱不够」这种日常状态占掉就失效了。
        /// </summary>
        private static void ApplyCardVisualState(DeployCard card, bool selected, bool allowed)
        {
            // 辉光是无状态的开关，每帧设一次都无所谓（内部只记一个目标值），
            // 所以放在提前返回之前 —— 否则「选中但视觉档位没变」时灯光会漏掉。
            card.Halo.Set(selected);

            int state = !allowed ? 0 : (selected ? 2 : 1);
            if (card.VisualState == state) return;
            card.VisualState = state;

            switch (state)
            {
                case 2:
                    // 只有一圈很紧的贴边热芯；外圈那层柔光是 UiHalo 在管，它可以呼吸、可以淡出，
                    // 而烘进网格的 Glow 改一次就要重建网格，做不了动画。
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

            // 发动键只有「就绪」时才配得上绯色实心。充能中的它必须沉下去 ——
            // 否则一个按不动的亮红键比没有键更糟：它一直在喊「现在可以了」。
            // 撤退键是 Ghost、永远安静，两个键的权重差由此保持真实。
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
                // 音乐键不能是永远一模一样的「开 / 关」——那等于让玩家试错。
                // 状态写在按钮上，按完下一个刷新周期就会改过来。
                if (_musicLabel != null)
                    _musicLabel.content = TowerDefense.Effects.BattleMusic.IsMuted ? "音乐　关" : "音乐　开";
            }
        }

        /// <summary>
        /// 结算屏是「这局已经结束」的宣告：HUD 从屏幕上撤下，只留结算本身。
        /// 原来那种 90% 的半透明遮罩下顶栏和卡组仍在透光，结果字排和 HUD 文字叠在一起读。
        /// </summary>
        private void SetHudHidden(bool hidden)
        {
            if (_hudHidden == hidden) return;
            _hudHidden = hidden;
            foreach (var name in new[] { "TopBand", "BottomBand", "MissionTag", "BattleReadout", "TimeControls", "DeploymentHand", "BarrageCommand" })
                if (transform.Find(name) != null) transform.Find(name).gameObject.SetActive(!hidden);
        }
        private void ShowResult(GameResult result)
        {
            _resultTitle.content = (_game.IsPractice ? "练习 / " : "") + (result.Victory ? "异变解决" : "满身疮痍");
            _resultTitle.color = result.Victory ? BattleUiTheme.Bone : BattleUiTheme.Scarlet;

            // 结果色带同题：胜利骨白、败北绯。两个字说的是同一件事。
            if (_resultBand != null) _resultBand.color = result.Victory ? BattleUiTheme.Bone : BattleUiTheme.Scarlet;

            _resultGrade.content = result.Grade;
            // 评级是全场最大的字形，所以它的颜色就是结果的温度：
            // Φ / V / S 这些「打得好」的评级用骨白，A 以下退成灰 —— 尺度已经足够大，颜色只做分层。
            _resultGrade.color = (result.Grade == "Φ" || result.Grade == "V" || result.Grade == "S")
                ? BattleUiTheme.Bone : BattleUiTheme.Ash;

            _resultStats.content =
                "得分    " + result.Score + "\n" +
                "击破    " + result.TotalKills + "\n" +
                "漏过    " + result.LeakedEnemies + "\n" +
                "残机    " + result.LivesRemaining + "\n" +
                "连击    " + result.BestCombo;
            // 新纪录只在该出现的那一局出现：一小行绯字，不抢评级字母的视线。
            if (_resultRecord != null) _resultRecord.gameObject.SetActive(result.NewRecord && result.Victory);
            _result.SetActive(true);
        }

        private static string TargetingName(TargetingPriority priority) { switch (priority) { case TargetingPriority.LowestHealth: return "残血"; case TargetingPriority.ClosestToEnd: return "突进"; default: return "最近"; } }
        private static string Role(TowerDefinition d) { return d.HealPerSecond > 0 ? "修复" : d.Damage <= 0 ? "减速" : d.SplashRadius > 0 ? "范围" : "攻击"; }
        private static string Stats(TowerDefinition d) { if (d.HealPerSecond > 0) return "治疗 " + d.HealPerSecond.ToString("0") + "/秒\n范围 " + d.RangeCells + " 格"; if (d.Damage <= 0) return "减速 " + Mathf.RoundToInt((1f-d.SlowFactor)*100f) + "% / " + d.SlowDuration.ToString("0.#") + " 秒\n范围 " + d.RangeCells + " 格"; return "伤害 " + d.Damage.ToString("0") + "  ·  频率 " + d.FireRate.ToString("0.#") + "/秒\n范围 " + d.RangeCells + " 格"; }

    }
}


