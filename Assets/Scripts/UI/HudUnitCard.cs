using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// 单位信息卡：玩家点谁 → 信息跟着谁。
    ///
    /// 三条硬规则：
    /// 1) 面板贴着被选中单位（世界锚定 + 引导线），不是弹一个居中面板 —— 信息属于谁一眼可辨；
    /// 2) 只在「有选中单位」时存在，默认不占任何屏幕空间（Progressive Disclosure）；
    /// 3) 摆放交给 <see cref="TdLayout.BesideUnit"/>：右侧放不下就翻左侧，并夹在上下安全区之间，
    ///    因此永远不会压在顶栏 / 底栏上，也不会跑出画布。
    ///
    /// 卡面层次：左侧职业色脊线（身份）→ 名称 + 职业标签 → 耐久条 → 一行说清干什么 → 返还 → 操作。
    /// 技能（爆）用环形充能 + 换色表达就绪，而不是写「冷却 3.2 秒」。
    /// </summary>
    public sealed class HudUnitCard : MonoBehaviour
    {
        private const float PanelW = 300f;
        private const float PanelH = 156f;

        private RectTransform _canvasRect;
        private GameObject _root;
        private RectTransform _panel;
        private Image _leader;
        private Image _spine;

        private UiText _title;
        private UiText _roleTag;
        private RoundedRectImage _hpFill;
        private UiText _hpText;
        private UiText _stats;
        private UiText _refund;

        private Button _boomButton;
        private RadialProgressImage _boomRing;
        private UiText _boomLabel;
        private Button _recallButton;

        private bool _visible;
        private float _entrance;

        private void Awake()
        {
            _canvasRect = (RectTransform)transform;

            _root = new GameObject("UnitCard", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            TdLayout.Fill(_root.GetComponent<RectTransform>());

            // 引导线：从面板靠单位的一侧连回单位
            _leader = UiFactory.CreateImage(_root.transform, "Leader", TdTheme.Star);
            _leader.raycastTarget = false;
            TdLayout.PinToCanvas(_leader.rectTransform, new Vector2(0f, 0.5f));
            _leader.rectTransform.sizeDelta = new Vector2(1f, 1f);

            var panel = TdKit.Glass(_root.transform, "Panel", TdTheme.Radius.Panel, TdTheme.Glass, TdTheme.Edge, 1f);
            _panel = panel.rectTransform;
            TdLayout.PinToCanvas(_panel, new Vector2(0.5f, 0.5f));
            _panel.sizeDelta = new Vector2(PanelW, PanelH);
            TdKit.Corners(_panel, "Frame", new Color(TdTheme.Star.r, TdTheme.Star.g, TdTheme.Star.b, 0.5f), 12f, 2f);

            // 左侧职业色脊线：卡面唯一的彩色元素，回答「这是什么角色」
            _spine = UiFactory.CreateImage(_panel, "Spine", TdTheme.Star);
            _spine.raycastTarget = false;
            TdLayout.At(_spine.rectTransform, 0f, PanelH * 0.5f, 3f, PanelH);

            // 头部：名称 + 职业标签
            _title = TdKit.Text(_panel, "Title", string.Empty, TdTheme.Body, TdTheme.Ink,
                TextAnchor.MiddleLeft, true);
            TdLayout.At(_title.rectTransform, 16f, 26f, 168f, 22f);

            var tagBg = TdKit.Glass(_panel, "RoleTag", TdTheme.Radius.Sm, TdTheme.BtnActive, TdTheme.SlotEdge, 1f);
            tagBg.raycastTarget = false;
            TdLayout.AtRight(tagBg.rectTransform, 14f, 26f, 48f, 20f);
            _roleTag = TdKit.Text(tagBg.transform, "RoleText", "单体", TdTheme.Tiny, TdTheme.Star,
                TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(_roleTag.rectTransform);

            // 耐久：条 + 数字，数字右对齐成一组
            var hp = UiFactory.CreateProgressBar(_panel, "Hp", TdTheme.Sem.Success, TdTheme.Rail);
            TdLayout.At(hp.background.rectTransform, 16f, 52f, 196f, 8f);
            _hpFill = hp.fill;

            _hpText = TdKit.Text(_panel, "HpText", string.Empty, TdTheme.Tiny, TdTheme.InkDim,
                TextAnchor.MiddleRight, true);
            TdLayout.AtRight(_hpText.rectTransform, 16f, 52f, 68f, 14f);

            // 一行说清这张符卡干什么
            _stats = TdKit.Text(_panel, "Stats", string.Empty, TdTheme.Tiny, TdTheme.InkDim,
                TextAnchor.MiddleLeft);
            TdLayout.At(_stats.rectTransform, 16f, 76f, PanelW - 32f, 16f);

            _refund = TdKit.Text(_panel, "Refund", string.Empty, TdTheme.Tiny, TdTheme.InkFaint,
                TextAnchor.MiddleLeft);
            TdLayout.At(_refund.rectTransform, 16f, 96f, PanelW - 32f, 14f);

            // 底部分隔线，把「读数」和「操作」分开
            var rule = TdKit.Line(_panel, "Rule", 0f, 1f, TdTheme.Divider);
            TdLayout.At(rule.rectTransform, 16f, 114f, PanelW - 32f, 1f);

            BuildBoomButton();
            BuildRecallButton();

            _root.SetActive(false);
        }

        private void BuildBoomButton()
        {
            var shell = TdKit.Glass(_panel, "Boom", TdTheme.Radius.Panel, TdTheme.BtnBg, TdTheme.SlotEdge, 1f);
            TdLayout.At(shell.rectTransform, 16f, 136f, 36f, 36f);

            _boomButton = shell.gameObject.AddComponent<Button>();
            _boomButton.targetGraphic = shell;
            _boomButton.transition = Selectable.Transition.None;
            _boomButton.onClick.AddListener(() => GameManager.Instance?.TowerPlacer?.SelectedTower?.TriggerBoom());

            _boomRing = TdKit.Ring(shell.transform, "Ring", 16f, 3f, TdTheme.Star, TdTheme.Rail);
            UiFactory.Stretch(_boomRing.rectTransform);

            _boomLabel = TdKit.Text(shell.transform, "Label", "爆", TdTheme.Small, TdTheme.InkFaint,
                TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(_boomLabel.rectTransform);
        }

        private void BuildRecallButton()
        {
            _recallButton = TdKit.Button(_panel, "Recall", "撤退", TdTheme.Radius.Sm, TdTheme.BtnBg,
                () => GameManager.Instance?.TowerPlacer?.RetreatSelected(), TdTheme.Small);
            TdLayout.AtRight(_recallButton.GetComponent<RectTransform>(), 16f, 136f, 92f, 32f);
            var label = _recallButton.GetComponentInChildren<UiText>();
            if (label != null) label.color = TdTheme.Barrier;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            var tower = gm != null && gm.TowerPlacer != null ? gm.TowerPlacer.SelectedTower : null;

            if (tower == null)
            {
                if (_visible)
                {
                    _visible = false;
                    _root.SetActive(false);
                }
                return;
            }

            if (!_visible)
            {
                _visible = true;
                _entrance = 0f;
                _root.SetActive(true);
            }
            _entrance = Mathf.Clamp01(_entrance + Time.unscaledDeltaTime / TdTheme.Motion.Base);

            Position(tower);
            RefreshContent(gm, tower);
        }

        private void Position(Tower tower)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var unit = TdLayout.WorldToCanvasLocal(_canvasRect, tower.transform.position, cam);
            float halfW = PanelW * 0.5f;
            float halfH = PanelH * 0.5f;

            var pos = TdLayout.BesideUnit(_canvasRect, unit, halfW, halfH, TdTheme.Space.Xl, out bool flip);

            float eased = UiEasings.OutCubic(_entrance);
            float scale = Mathf.Lerp(0.96f, 1f, eased);
            _panel.localScale = new Vector3(scale, scale, 1f);
            _panel.anchoredPosition = TdLayout.ToAnchored(_canvasRect, pos);

            float edgeX = flip ? pos.x + halfW * scale : pos.x - halfW * scale;
            var from = new Vector2(edgeX, pos.y);
            var delta = unit - from;
            _leader.rectTransform.anchoredPosition = TdLayout.ToAnchored(_canvasRect, from);
            _leader.rectTransform.sizeDelta = new Vector2(delta.magnitude, 1f);
            _leader.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _leader.color = new Color(_spine.color.r, _spine.color.g, _spine.color.b, 0.55f * eased);
        }

        private void RefreshContent(GameManager gm, Tower tower)
        {
            var def = tower.Definition;
            int idx = TdKit.SpellIndex(def.Type);

            _title.text = def.DisplayName;
            _roleTag.text = TdKit.RoleOf(def);
            _roleTag.color = def.Color;
            _spine.color = new Color(def.Color.r, def.Color.g, def.Color.b, 0.95f);

            float ratio = tower.HealthRatio;
            _hpFill.Progress = ratio;
            _hpFill.color = Color.Lerp(TdTheme.Barrier, TdTheme.Sem.Success, ratio);
            _hpText.text = Mathf.CeilToInt(tower.Health) + " / " + Mathf.CeilToInt(tower.MaxHealth);

            _stats.text = Describe(def);
            _refund.text = "撤退返还 " + Mathf.RoundToInt(def.Cost * GameConfig.RetreatRefundRatio)
                + " · 击毁返还 " + Mathf.RoundToInt(def.Cost * GameConfig.DefeatRefundRatio);

            bool canBoom = tower.CanBoom;
            _boomButton.gameObject.SetActive(canBoom);
            if (canBoom)
            {
                bool ready = tower.BoomReady;
                _boomRing.Progress = tower.BoomReadyRatio;
                _boomRing.fillColor = ready ? TdTheme.Spirit : TdTheme.Star;
                _boomLabel.color = ready ? TdTheme.Spirit : TdTheme.InkFaint;
                _boomButton.interactable = ready && gm.State == GameState.Running && !gm.IsPaused;
            }

            _recallButton.interactable = gm.State == GameState.Running && !gm.IsPaused;
        }

        /// <summary>一行说清这张符卡干什么：状态 / 角色优先，数值跟在后面。</summary>
        private static string Describe(TowerDefinition def)
        {
            if (def.HealPerSecond > 0f)
            {
                return "治疗 " + Mathf.RoundToInt(def.HealPerSecond) + "/秒 · 射程 " + def.RangeCells + " 格";
            }
            if (def.Damage <= 0f)
            {
                return "减速 " + Mathf.RoundToInt((1f - def.SlowFactor) * 100f) + "% · 持续 "
                    + def.SlowDuration.ToString("0.#") + " 秒 · 射程 " + def.RangeCells + " 格";
            }
            string splash = def.SplashRadius > 0f ? " · 溅射 " + def.SplashRadius.ToString("0.#") + " 格" : string.Empty;
            return "伤害 " + Mathf.RoundToInt(def.Damage) + " · " + def.FireRate.ToString("0.#")
                + "/秒 · DPS " + (def.Damage * def.FireRate).ToString("0")
                + " · 射程 " + def.RangeCells + " 格" + splash;
        }
    }
}
