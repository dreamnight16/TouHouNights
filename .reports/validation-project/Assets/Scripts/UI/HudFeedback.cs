using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Effects;
using TowerDefense.Systems;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 反馈层：横幅 / 边框警示 / 全屏闪 / 暗角 / 连击 / 静止覆盖 / 快捷键提示 / 里程碑与低血警报。
    ///
    /// 这一层只回答一个问题：「现在发生了什么」。它不承载任何决策信息，
    /// 也不接收点击（blocksRaycasts = false），纯粹是世界的回音。
    /// 动画只用于表达状态变化 —— 危险时才脉动、Boss 出现才闪屏，不做无意义装饰动画。
    ///
    /// 布局约定（和上下两条带一样）：居中元素一律挂在一个「锚点为画布中心」的容器里，
    /// 绝不使用 ReferenceResolution.x * 0.5f 这类绝对坐标 —— 画布宽度一旦偏离 1280
    /// 那些坐标就会把横幅推出屏幕。
    /// </summary>
    public sealed class HudFeedback : MonoBehaviour
    {
        // ---- 横幅 ----
        private CanvasGroup _announceGroup;
        private UiText _announceText;
        private UiText _announceSub;
        private Coroutine _announceRoutine;

        // ---- 警示 / 闪屏 ----
        private CanvasGroup _edgeGroup;
        private float _edgeAlpha;
        private CanvasGroup _fireFlash;
        private float _fireFlashAlpha;

        // ---- 连击 ----
        private UiText _comboLabel;
        private float _comboPulse;
        private int _lastComboShown;

        // ---- 静止覆盖 ----
        private CanvasGroup _suspendOverlay;
        private RectTransform _suspendGroup;

        // ---- 里程碑 / 低血 ----
        private int _lastLives;
        private int _lastMilestone;
        private float _heartbeatTimer;
        private bool _prevCanBarrage;

        private WaveSpawner _subscribedSpawner;

        private void Awake()
        {
            BuildAtmosphere();
            BuildAnnounce();
            BuildEdgeFlash();
            BuildFireFlash();
            BuildComboTag();
            BuildSuspendOverlay();
            BuildKeyHints();

            Enemy.OnBossSpawned += ShowBossWarning;
            Enemy.OnBossKilled += ShowBossKilled;
            StartCoroutine(Opener());
        }

        private void OnDestroy()
        {
            Enemy.OnBossSpawned -= ShowBossWarning;
            Enemy.OnBossKilled -= ShowBossKilled;
            UnhookSpawner();
        }

        // ================= 构建 =================

        /// <summary>新建一个「锚在画布中心」的容器：子元素相对它排布，任何画布宽度下都居中。</summary>
        private static RectTransform CenteredGroup(Transform parent, string name, float w, float h, float y)
        {
            var g = TdLayout.NewChild(parent, name);
            g.anchorMin = g.anchorMax = new Vector2(0.5f, 0.5f);
            g.pivot = new Vector2(0.5f, 0.5f);
            g.anchoredPosition = new Vector2(0f, y);
            g.sizeDelta = new Vector2(w, h);
            return g;
        }

        /// <summary>四角暗角 + 顶部柔光：把视线收进战场中心，不参与交互。</summary>
        private void BuildAtmosphere()
        {
            var root = TdLayout.NewChild(transform, "Atmosphere");
            TdLayout.Fill(root);

            var vig = UiFactory.CreateImage(root, "Vignette", new Color(0f, 0f, 0f, 0.32f));
            vig.raycastTarget = false;
            vig.sprite = SpriteFactory.Vignette(Color.white);
            TdLayout.Fill(vig.rectTransform);

            var beam = UiFactory.CreateImage(root, "TopBeam", new Color(0.55f, 0.78f, 0.96f, 0.05f));
            beam.raycastTarget = false;
            beam.sprite = SpriteFactory.GradientTop(Color.white);
            UiFactory.SetStretch(beam.rectTransform, new Vector2(0f, 0.62f), Vector2.one,
                Vector2.zero, Vector2.zero);
        }

        private void BuildAnnounce()
        {
            var root = TdLayout.NewChild(transform, "Announce");
            TdLayout.Fill(root);
            _announceGroup = root.gameObject.AddComponent<CanvasGroup>();
            _announceGroup.alpha = 0f;
            _announceGroup.blocksRaycasts = false;

            // 横幅挂在顶栏下方的一条居中带里，y 用「距上缘」表达
            var band = CenteredGroup(root, "Band", 560f, 76f, 0f);
            band.anchorMin = band.anchorMax = new Vector2(0.5f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.anchoredPosition = new Vector2(0f, -TdTheme.SafeTop);

            var bg = TdKit.Glass(band, "Bg", TdTheme.Radius.Sm, TdTheme.GlassDeep, TdTheme.Edge, 1f);
            TdLayout.At(bg.rectTransform, 0f, 26f, 560f, 52f);

            _announceText = TdKit.Text(band, "Text", string.Empty, TdTheme.Huge, TdTheme.Star,
                TextAnchor.MiddleCenter, true);
            TdLayout.At(_announceText.rectTransform, 0f, 22f, 560f, 32f);

            _announceSub = TdKit.Text(band, "Sub", string.Empty, TdTheme.Small, TdTheme.InkDim,
                TextAnchor.MiddleCenter, true);
            TdLayout.At(_announceSub.rectTransform, 0f, 46f, 560f, 18f);
        }

        private void BuildEdgeFlash()
        {
            var root = TdLayout.NewChild(transform, "EdgeFlash");
            TdLayout.Fill(root);
            _edgeGroup = root.gameObject.AddComponent<CanvasGroup>();
            _edgeGroup.alpha = 0f;
            _edgeGroup.blocksRaycasts = false;

            CreateEdgeStrip(root, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), 0f, new Vector2(0f, -100f), Vector2.zero);
            CreateEdgeStrip(root, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), 180f, Vector2.zero, new Vector2(0f, 100f));
            CreateEdgeStrip(root, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), 0f, Vector2.zero, new Vector2(100f, 0f));
            CreateEdgeStrip(root, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), 0f, new Vector2(-100f, 0f), Vector2.zero);
        }

        private void CreateEdgeStrip(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            float rotZ, Vector2 offsetMin, Vector2 offsetMax)
        {
            var img = UiFactory.CreateImage(parent, name, TdTheme.Barrier);
            img.raycastTarget = false;
            img.sprite = SpriteFactory.GradientTop(Color.white);
            UiFactory.SetStretch(img.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            if (Mathf.Abs(rotZ) > 0.01f) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        private void BuildFireFlash()
        {
            var root = TdLayout.NewChild(transform, "FireFlash");
            TdLayout.Fill(root);
            _fireFlash = root.gameObject.AddComponent<CanvasGroup>();
            _fireFlash.alpha = 0f;
            _fireFlash.blocksRaycasts = false;

            var img = UiFactory.CreateImage(root, "Flash", new Color(0.40f, 0.70f, 0.92f, 1f));
            img.raycastTarget = false;
            TdLayout.Fill(img.rectTransform);
        }

        private void BuildComboTag()
        {
            var band = CenteredGroup(transform, "ComboBand", 240f, 22f, 0f);
            band.anchorMin = band.anchorMax = new Vector2(0.5f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.anchoredPosition = new Vector2(0f, -TdTheme.SafeTop);

            _comboLabel = TdKit.Text(band, "ComboTag", string.Empty, TdTheme.Small, TdTheme.Star,
                TextAnchor.MiddleCenter, true);
            TdLayout.At(_comboLabel.rectTransform, 0f, 11f, 240f, 22f);
            _comboLabel.color = new Color(TdTheme.Star.r, TdTheme.Star.g, TdTheme.Star.b, 0f);
        }

        /// <summary>静止覆盖层：把「继续 / 再来一战」下沉到这里，战斗界面上就不必常驻系统按钮。</summary>
        private void BuildSuspendOverlay()
        {
            var root = TdLayout.NewChild(transform, "SuspendOverlay");
            TdLayout.Fill(root);
            _suspendOverlay = root.gameObject.AddComponent<CanvasGroup>();
            _suspendOverlay.alpha = 0f;
            _suspendOverlay.blocksRaycasts = false;

            var veil = UiFactory.CreateImage(root, "Veil",
                new Color(TdTheme.Void.r, TdTheme.Void.g, TdTheme.Void.b, 0.72f));
            veil.raycastTarget = false;
            TdLayout.Fill(veil.rectTransform);

            // 内容组锚在画布正中，两个按钮相对组心左右对称
            _suspendGroup = CenteredGroup(root, "Group", 300f, 130f, 0f);

            var title = TdKit.Text(_suspendGroup, "Title", "静止世界", TdTheme.Huge, TdTheme.Ink,
                TextAnchor.MiddleCenter, true);
            TdLayout.At(title.rectTransform, 0f, 28f, 300f, 40f);

            var sub = TdKit.Text(_suspendGroup, "Sub", "THE WORLD IS HELD", TdTheme.Tiny, TdTheme.InkFaint,
                TextAnchor.MiddleCenter);
            TdLayout.At(sub.rectTransform, 0f, 56f, 300f, 14f);

            var resume = TdKit.Button(_suspendGroup, "Resume", "继续", TdTheme.Radius.Sm, TdTheme.BtnBg,
                () => GameManager.Instance?.TogglePause(), TdTheme.Small);
            TdLayout.AtCenter(resume.GetComponent<RectTransform>(), 90f, 92f, 108f, 36f);
            var resumeLabel = resume.GetComponentInChildren<UiText>();
            if (resumeLabel != null) resumeLabel.color = TdTheme.Star;

            var retry = TdKit.Button(_suspendGroup, "Retry", "再来一战", TdTheme.Radius.Sm, TdTheme.BtnBg,
                () => GameManager.Instance?.Restart(), TdTheme.Small);
            TdLayout.AtCenter(retry.GetComponent<RectTransform>(), 210f, 92f, 108f, 36f);
            var retryLabel = retry.GetComponentInChildren<UiText>();
            if (retryLabel != null) retryLabel.color = TdTheme.Ink;
        }

        private void BuildKeyHints()
        {
            var root = TdLayout.NewChild(transform, "KeyHints");
            TdLayout.Fill(root);

            var panel = TdKit.Glass(root, "Panel", TdTheme.Radius.Sm,
                new Color(TdTheme.Void.r, TdTheme.Void.g, TdTheme.Void.b, 0.55f), TdTheme.Divider, 0.6f);
            TdLayout.AtBottom(panel.rectTransform, TdTheme.Space.Md, TdTheme.BottomBarH + TdTheme.Space.Sm,
                252f, 22f);

            var hints = TdKit.Text(panel.transform, "Text",
                "[1-5] 布防  [Space] 静止  [X] 流速  [E] 弹幕  [R] 重开  [Esc] 取消",
                TdTheme.Tiny, TdTheme.InkFaint, TextAnchor.MiddleLeft);
            TdLayout.At(hints.rectTransform, TdTheme.Space.Sm, 11f, 236f, 14f);
        }

        // ================= 对外：横幅 =================

        public void ShowAnnounce(string text, Color color)
        {
            ShowAnnounce(text, string.Empty, color);
        }

        public void ShowAnnounce(string text, string sub, Color color)
        {
            if (_announceRoutine != null) StopCoroutine(_announceRoutine);
            _announceText.text = text;
            _announceText.color = color;
            _announceSub.text = sub;
            _announceSub.color = TdTheme.InkDim;
            _announceRoutine = StartCoroutine(AnnounceRoutine(TdTheme.Motion.Hold));
        }

        private IEnumerator AnnounceRoutine(float hold)
        {
            const float fadeIn = 0.20f, fadeOut = 0.35f;
            float elapsed = 0f;

            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                _announceGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }
            _announceGroup.alpha = 1f;

            elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                _announceGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }
            _announceGroup.alpha = 0f;
            _announceRoutine = null;
        }

        private IEnumerator Opener()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            ShowAnnounce("结界展开", "BARRIER ONLINE", TdTheme.Star);
        }

        // ================= 事件 =================

        private void ShowBossWarning(Enemy boss)
        {
            ShowAnnounce("结界破坏者 袭来", "BOSS APPROACHING", TdTheme.Barrier);
            _edgeAlpha = 0.90f;
            _fireFlashAlpha = 0.18f;
        }

        private void ShowBossKilled(Enemy boss)
        {
            ShowAnnounce("结界破坏者 · 击破", "BOSS DOWN", TdTheme.Spirit);
            _fireFlashAlpha = 0.30f;
        }

        private void ShowStageBanner(int index, string title, string eng)
        {
            int total = GameConfig.Rounds.Length;
            ShowAnnounce(title, eng + " · " + (index + 1) + "/" + total, StageColor(index));
            _edgeAlpha = Mathf.Max(_edgeAlpha, 0.30f);

            var gm = GameManager.Instance;
            if (gm != null && gm.Map != null) gm.Map.PulseDoors();
        }

        private void ShowWaveClear(int index)
        {
            if (index >= GameConfig.Rounds.Length - 1) return; // 最后一波直接进结算
            ShowAnnounce("波次清除", "STAGE CLEAR", TdTheme.Sem.Success);
        }

        private static Color StageColor(int index)
        {
            switch (index % 4)
            {
                case 0: return TdTheme.Star;
                case 1: return TdTheme.Spirit;
                case 2: return TdTheme.Glass2;
                default: return TdTheme.Barrier;
            }
        }

        private void HookSpawner(WaveSpawner spawner)
        {
            if (_subscribedSpawner == spawner) return;
            UnhookSpawner();
            _subscribedSpawner = spawner;
            _subscribedSpawner.OnRoundStart += ShowStageBanner;
            _subscribedSpawner.OnRoundClear += ShowWaveClear;
        }

        private void UnhookSpawner()
        {
            if (_subscribedSpawner == null) return;
            _subscribedSpawner.OnRoundStart -= ShowStageBanner;
            _subscribedSpawner.OnRoundClear -= ShowWaveClear;
            _subscribedSpawner = null;
        }

        // ================= 每帧 =================

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Restart 会重建 WorldRoot → 新的 WaveSpawner，需要跟随重挂
            if (gm.WaveSpawner != null) HookSpawner(gm.WaveSpawner);

            // 新一局开始（击破数归零）时把「上一局的记忆」清掉，
            // 否则第二局的低血警示与里程碑横幅永远不会再触发。
            if (gm.TotalKills == 0)
            {
                _lastMilestone = 0;
                _prevCanBarrage = false;
            }

            RefreshLifeLoss(gm);
            RefreshBarrageMoment(gm);
            RefreshMilestone(gm);
            RefreshCombo(gm);
            RefreshDanger(gm);
            RefreshDecay(gm);
        }

        private void RefreshLifeLoss(GameManager gm)
        {
            if (gm.Lives < _lastLives)
            {
                _edgeAlpha = 0.85f;
                if (gm.Map != null)
                {
                    var pos = (Vector2)gm.Map.CellToWorld(GameConfig.BaseCell) + new Vector2(0f, 1.1f);
                    FloatingTextView.SpawnWorld(pos, "-" + (_lastLives - gm.Lives) + " 结界", TdTheme.Barrier, 1.1f, 15);
                }
            }
            _lastLives = gm.Lives;
        }

        private void RefreshBarrageMoment(GameManager gm)
        {
            bool canBarrage = gm.CanBarrage && gm.State == GameState.Running && !gm.IsPaused;
            if (gm.State == GameState.Running && !gm.IsPaused)
            {
                if (canBarrage && !_prevCanBarrage)
                {
                    ShowAnnounce("P 点蓄满 · 弹幕就绪", "BARRAGE READY", TdTheme.Spirit);
                    _fireFlashAlpha = 0.10f;
                }
                else if (!canBarrage && _prevCanBarrage)
                {
                    ShowAnnounce("弹幕展开", "BARRAGE FIRED", TdTheme.Star);
                    _fireFlashAlpha = 0.24f;
                }
            }
            _prevCanBarrage = canBarrage;
        }

        private void RefreshMilestone(GameManager gm)
        {
            int milestone = gm.TotalKills / GameConfig.AnnihilationMilestoneInterval;
            if (gm.TotalKills > 0 && milestone > _lastMilestone)
            {
                _lastMilestone = milestone;
                ShowAnnounce("击破里程碑 ×" + (milestone * GameConfig.AnnihilationMilestoneInterval)
                    + " · 灵力 +" + GameConfig.AnnihilationMilestoneGold, TdTheme.Spirit);

                if (gm.Map != null)
                {
                    var pos = (Vector2)gm.Map.CellToWorld(GameConfig.BaseCell) + new Vector2(0f, 1.5f);
                    FloatingTextView.SpawnWorld(pos, "+" + GameConfig.AnnihilationMilestoneGold,
                        TdTheme.Spirit, 1.2f, 18);
                }
            }
        }

        private void RefreshCombo(GameManager gm)
        {
            if (gm.Combo >= 2)
            {
                if (gm.Combo != _lastComboShown)
                {
                    _lastComboShown = gm.Combo;
                    _comboPulse = 1f;
                    if (gm.Combo % 10 == 0)
                    {
                        Sfx.Combo();
                        if (gm.Map != null)
                        {
                            var pos = (Vector2)gm.Map.CellToWorld(GameConfig.BaseCell) + new Vector2(0f, 1.8f);
                            FloatingTextView.SpawnWorld(pos, "连击 ×" + gm.Combo + " · 灵力 +2",
                                TdTheme.Spirit, 1.0f, 15);
                        }
                    }
                }

                _comboLabel.text = "连击 ×" + gm.Combo;
                var cc = _comboLabel.color;
                cc.a = Mathf.Lerp(cc.a, 1f, 12f * Time.unscaledDeltaTime);
                bool hot = gm.Combo >= 10;
                var tint = hot ? TdTheme.Spirit : TdTheme.Star;
                cc.r = tint.r; cc.g = tint.g; cc.b = tint.b;
                _comboLabel.color = cc;

                _comboPulse = Mathf.Max(0f, _comboPulse - Time.unscaledDeltaTime * 3.2f);
                _comboLabel.rectTransform.localScale = Vector3.one * (1f + 0.30f * _comboPulse);
            }
            else
            {
                _lastComboShown = 0;
                var cc = _comboLabel.color;
                cc.a = Mathf.Lerp(cc.a, 0f, 6f * Time.unscaledDeltaTime);
                _comboLabel.color = cc;
            }
        }

        private void RefreshDanger(GameManager gm)
        {
            bool danger = gm.State == GameState.Running && !gm.IsPaused && gm.Lives <= 3;
            if (danger)
            {
                _edgeAlpha = Mathf.Max(_edgeAlpha, 0.22f + 0.10f * Mathf.Sin(Time.unscaledTime * 4f));
                _heartbeatTimer -= Time.unscaledDeltaTime;
                if (_heartbeatTimer <= 0f)
                {
                    Sfx.Heartbeat();
                    _heartbeatTimer = 1.6f;
                }
            }
        }

        private void RefreshDecay(GameManager gm)
        {
            _edgeAlpha = Mathf.Max(0f, _edgeAlpha - Time.unscaledDeltaTime * 1.6f);
            _edgeGroup.alpha = _edgeAlpha;

            _fireFlashAlpha = Mathf.Max(0f, _fireFlashAlpha - Time.unscaledDeltaTime * 1.3f);
            _fireFlash.alpha = _fireFlashAlpha;

            bool paused = gm != null && gm.IsPaused;
            _suspendOverlay.alpha = Mathf.Lerp(_suspendOverlay.alpha, paused ? 1f : 0f, 9f * Time.unscaledDeltaTime);

            // 只有真正显示出来时才吃掉点击：否则「继续 / 再来一战」会挡住整块战场。
            bool interactive = _suspendOverlay.alpha > 0.5f;
            _suspendOverlay.blocksRaycasts = interactive;
            _suspendOverlay.interactable = interactive;

            if (_suspendGroup != null)
            {
                float s = Mathf.Lerp(0.96f, 1f, _suspendOverlay.alpha);
                _suspendGroup.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
