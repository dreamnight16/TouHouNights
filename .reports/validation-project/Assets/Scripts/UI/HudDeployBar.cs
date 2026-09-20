using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// 快速操作层（底部）—— 重构版（参照明日方舟视觉语言）。
    /// 炭灰玻璃底 + 顶部青蓝光轨；符卡为「职业色顶带 + 角标 + 图标发光」的单元，
    /// 选中/悬停上浮 + 青蓝描边 + 外发光；右侧 P 点环用径向进度 + 呼吸光。
    /// </summary>
    public sealed class HudDeployBar : MonoBehaviour
    {
        private sealed class Card
        {
            public TowerType Type;
            public Button Button;
            public RoundedRectImage Panel;
            public Image TopBand;
            public Image Glow;
            public RectTransform Lift;
            public RectTransform Icon;
            public UiText Name;
            public UiText Cost;
            public UiText Role;
        }

        private readonly Card[] _cards = new Card[TdKit.SpellOrder.Length];
        private RectTransform _band;
        private RectTransform _cardGroup;
        private float _cardGroupW;

        private UiText _countLabel;
        private Button _targetingButton;
        private UiText _targetingLabel;

        private RadialProgressImage _powerRing;
        private Image _powerGlow;
        private Button _barrageButton;
        private UiText _barrageGlyph;
        private RectTransform _barrageRect;

        private void Awake()
        {
            var bar = TdKit.Glass(transform, "DeployBar", 0f, TdTheme.Surface1, default, 0f);
            TdLayout.Band(bar.rectTransform, false, TdTheme.BottomBarH);
            _band = bar.rectTransform;

            // 顶部青蓝光轨（全局强调色具象化）
            TdKit.AccentLine(bar.transform, "AccentLine", false, TdTheme.Accent);
            TdKit.Corners(bar.transform, "Frame", new Color(TdTheme.Accent.r, TdTheme.Accent.g, TdTheme.Accent.b, 0.45f), 14f, 1.5f);

            BuildLeftCluster(bar.transform);
            BuildCards(bar.transform);
            BuildBarrage(bar.transform);
        }

        private void BuildLeftCluster(Transform parent)
        {
            var cluster = TdLayout.NewChild(parent, "LeftCluster");
            TdLayout.InLeft(cluster, TdTheme.Space.Lg, 150f, 64f);

            _countLabel = TdKit.Text(cluster, "Count", "符卡 0/0", 12, TdTheme.InkDim, TextAnchor.MiddleLeft);
            TdLayout.At(_countLabel.rectTransform, 0f, 40f, 150f, 14f);
            _countLabel.SetCharacterSpacing(1f);

            _targetingButton = TdKit.Button(cluster, "Targeting", "索敌·最近", 0f, TdTheme.Surface2,
                () => GameManager.Instance?.CycleTargetingPriority(), 12);
            TdLayout.AtBottom(_targetingButton.GetComponent<RectTransform>(), 0f, 0f, 150f, 30f);
            _targetingLabel = _targetingButton.GetComponentInChildren<UiText>();
            if (_targetingLabel != null) _targetingLabel.color = TdTheme.Ink;
        }

        private void BuildCards(Transform parent)
        {
            int n = TdKit.SpellOrder.Length;
            float total = n * TdTheme.CardW + (n - 1) * TdTheme.CardGap;
            float groupH = TdTheme.CardH + TdTheme.Space.Md;

            var group = TdLayout.NewChild(parent, "CardGroup");
            TdLayout.InCenter(group, 0f, total, groupH);
            _cardGroup = group;
            _cardGroupW = total;

            for (int i = 0; i < n; i++)
            {
                _cards[i] = BuildCard(group, TdKit.SpellOrder[i], i, i * (TdTheme.CardW + TdTheme.CardGap));
            }
        }

        private Card BuildCard(Transform group, TowerType type, int index, float x)
        {
            var card = new Card { Type = type };
            var def = GameConfig.Towers[type];

            card.Lift = TdLayout.NewChild(group, "Lift_" + type);
            TdLayout.Anchor(card.Lift,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(x, 0f), new Vector2(TdTheme.CardW, TdTheme.CardH));

            // 外发光（选中 / 悬停亮起更强；常驻时也给一缕极淡的职业色光晕，避免卡片贴在底栏上像「死纸」）
            card.Glow = TdKit.Glow(card.Lift, "Glow", new Color(def.Color.r, def.Color.g, def.Color.b, 0.1f));
            TdLayout.AtCenter(card.Glow.rectTransform, TdTheme.CardW * 0.5f, TdTheme.CardH * 0.5f, TdTheme.CardW + 22f, TdTheme.CardH + 20f);

            card.Panel = TdKit.Glass(card.Lift, "Card", 0f, TdTheme.Surface3, TdTheme.HairlineStrong, 1.2f);
            TdLayout.At(card.Panel.rectTransform, 0f, TdTheme.CardH * 0.5f, TdTheme.CardW, TdTheme.CardH);
            TdKit.Sheen(card.Panel.transform, "Sheen", 0f, TdTheme.Sheen);
            // 四角角标（职业色），呼应顶栏
            TdKit.Corners(card.Panel.transform, "Frame", new Color(def.Color.r, def.Color.g, def.Color.b, 0.85f), 12f, 2f);

            card.Button = card.Panel.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = card.Panel;
            card.Button.transition = Selectable.Transition.None;
            card.Button.onClick.AddListener(() => GameManager.Instance?.TowerPlacer?.SelectTower(type));

            // 职业色顶带（卡片唯一彩色条）
            card.TopBand = UiFactory.CreateImage(card.Panel.transform, "Band", def.Color);
            card.TopBand.raycastTarget = false;
            TdLayout.At(card.TopBand.rectTransform, 0f, 2f, TdTheme.CardW, 4f);

            // 职业图标（第一识别符号，职业色）
            card.Icon = UiIcon.CreateTowerIcon(card.Panel.transform, type, 30f, def.Color);
            TdLayout.AtCenter(card.Icon, TdTheme.CardW * 0.5f, 32f, 30f, 30f);

            card.Name = TdKit.Text(card.Panel.transform, "Name", def.DisplayName, TdTheme.Small, TdTheme.Ink, TextAnchor.MiddleCenter);
            TdLayout.At(card.Name.rectTransform, 0f, 55f, TdTheme.CardW, 16f);

            var coin = UiIcon.Create(card.Panel.transform, UiIconKind.Coin, 13f, TdTheme.Spirit);
            TdLayout.At(coin, 30f, 73f, 13f, 13f);
            card.Cost = TdKit.Text(card.Panel.transform, "Cost", def.Cost.ToString(), TdTheme.Small, TdTheme.Spirit, TextAnchor.MiddleLeft);
            TdLayout.At(card.Cost.rectTransform, 46f, 73f, 56f, 16f);

            card.Role = TdKit.Text(card.Panel.transform, "Role", TdKit.RoleOf(def), 12, TdTheme.InkDim, TextAnchor.MiddleRight);
            TdLayout.At(card.Role.rectTransform, 65f, 10f, 48f, 12f);

            // 快捷键徽章（青蓝，2px 圆角 + 白色描边 → 在暗卡面上清晰可读）
            var idxBadge = TdKit.Glass(card.Panel.transform, "IdxBadge", 2f, TdTheme.Accent, TdTheme.HairlineStrong, 1f);
            TdLayout.At(idxBadge.rectTransform, 5f, 13f, 22f, 18f);
            var idx = TdKit.Text(card.Panel.transform, "Idx", (index + 1).ToString(), 12, TdTheme.Surface0, TextAnchor.MiddleCenter);
            TdLayout.At(idx.rectTransform, 5f, 13f, 22f, 18f);

            return card;
        }

        private void BuildBarrage(Transform parent)
        {
            var cluster = TdLayout.NewChild(parent, "BarrageCluster");
            TdLayout.InRight(cluster, TdTheme.Space.Lg, 86f, 86f);

            _powerGlow = TdKit.Glow(cluster, "Glow", new Color(TdTheme.Spirit.r, TdTheme.Spirit.g, TdTheme.Spirit.b, 0f));
            TdLayout.AtCenter(_powerGlow.rectTransform, 43f, 43f, 96f, 96f);

            var shell = TdKit.Glass(cluster, "Shell", 0f, TdTheme.Surface2, TdTheme.Hairline, 1f);
            TdLayout.AtCenter(shell.rectTransform, 43f, 43f, 70f, 70f);
            TdKit.Corners(shell.transform, "Frame", new Color(TdTheme.Spirit.r, TdTheme.Spirit.g, TdTheme.Spirit.b, 0.5f), 12f, 1.5f);
            _barrageRect = shell.rectTransform;

            _barrageButton = shell.gameObject.AddComponent<Button>();
            _barrageButton.targetGraphic = shell;
            _barrageButton.transition = Selectable.Transition.None;
            _barrageButton.onClick.AddListener(() => GameManager.Instance?.TriggerBarrage());

            _powerRing = TdKit.Ring(shell.transform, "Ring", 30f, 3.5f, TdTheme.Accent, TdTheme.Hairline);
            UiFactory.Stretch(_powerRing.rectTransform);

            _barrageGlyph = TdKit.Text(shell.transform, "Glyph", "P", TdTheme.Huge, TdTheme.Accent, TextAnchor.MiddleCenter);
            UiFactory.Stretch(_barrageGlyph.rectTransform);

            var hint = TdKit.Text(cluster, "Hint", "弹幕 BARrage", 9, TdTheme.InkFaint, TextAnchor.MiddleCenter);
            TdLayout.AtBottom(hint.rectTransform, 0f, 2f, 86f, 12f);
            hint.SetCharacterSpacing(2f);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            FitCardGroup();
            RefreshCards(gm);
            RefreshCluster(gm);
            RefreshBarrage(gm);
        }

        private void FitCardGroup()
        {
            if (_cardGroup == null || _band == null || _cardGroupW <= 0f) return;
            float w = _band.rect.width;
            if (w <= 1f) return;
            float reserved = 2f * (TdTheme.Space.Lg + 150f);
            float avail = w - reserved;
            float target = avail >= _cardGroupW ? 1f : Mathf.Clamp(avail / _cardGroupW, 0.72f, 1f);
            _cardGroup.localScale = new Vector3(target, target, 1f);
        }

        private void RefreshCards(GameManager gm)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                var def = GameConfig.Towers[card.Type];
                bool affordable = gm.Gold >= def.Cost;
                bool selected = gm.TowerPlacer != null && gm.TowerPlacer.SelectedType == card.Type;
                bool hovered = RectTransformUtility.RectangleContainsScreenPoint(card.Panel.rectTransform, Input.mousePosition);

                card.Button.interactable = affordable && !gm.IsPaused && gm.State == GameState.Running;

                card.Panel.color = !affordable ? TdTheme.Surface0 : (selected ? new Color(0.090f, 0.215f, 0.310f, 0.98f) : TdTheme.Surface3);
                card.Panel.borderColor = selected ? TdTheme.Accent : (hovered ? TdTheme.HairlineStrong : TdTheme.Hairline);
                card.Panel.borderWidth = selected ? 3.2f : (hovered ? 2.2f : 1f);

                float bandAlpha = !affordable ? 0.14f : (selected ? 1f : 0.7f);
                card.TopBand.color = new Color(def.Color.r, def.Color.g, def.Color.b, bandAlpha);

                card.Icon.localScale = Vector3.one * (affordable ? 1f : 0.84f);
                card.Cost.color = affordable ? TdTheme.Spirit : TdTheme.InkFaint;
                card.Name.color = affordable ? TdTheme.Ink : TdTheme.InkFaint;

                float targetLift = selected ? 6f : (hovered ? 3f : 0f);
                var pos = card.Lift.anchoredPosition;
                pos.y = Mathf.Lerp(pos.y, targetLift, 0.25f);
                card.Lift.anchoredPosition = pos;

                float glowAlpha = selected ? 0.55f : (hovered ? 0.28f : 0.1f);
                var gc = card.Glow.color;
                gc.a = Mathf.Lerp(gc.a, glowAlpha, 0.22f);
                card.Glow.color = gc;
            }
        }

        private void RefreshCluster(GameManager gm)
        {
            _countLabel.text = "符卡 " + gm.TowerCount + " / " + GameConfig.MaxTowers;
            if (_targetingLabel != null) _targetingLabel.text = "索敌·" + TargetingText(gm.TargetingPriority);
            if (_targetingButton != null) _targetingButton.interactable = gm.State == GameState.Running;
        }

        private void RefreshBarrage(GameManager gm)
        {
            bool full = gm.Power >= GameConfig.MaxPower;
            _powerRing.Progress = GameConfig.MaxPower > 0f ? gm.Power / GameConfig.MaxPower : 0f;
            _powerRing.fillColor = full ? TdTheme.Spirit : TdTheme.Accent;

            _barrageGlyph.text = full ? "弹" : "P";
            _barrageGlyph.color = full ? TdTheme.Spirit : TdTheme.Accent;

            bool canBarrage = gm.CanBarrage && gm.State == GameState.Running && !gm.IsPaused;
            float pulse = canBarrage ? (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.5f)) : 0f;
            _powerGlow.color = new Color(TdTheme.Spirit.r, TdTheme.Spirit.g, TdTheme.Spirit.b,
                canBarrage ? 0.14f + 0.22f * pulse : 0f);
            _barrageButton.interactable = canBarrage;
            _barrageRect.localScale = Vector3.Lerp(_barrageRect.localScale,
                Vector3.one * (canBarrage ? 1f + 0.045f * pulse : 1f), 0.25f);
        }

        private static string TargetingText(TargetingPriority priority)
        {
            switch (priority)
            {
                case TargetingPriority.Nearest: return "最近";
                case TargetingPriority.LowestHealth: return "残血";
                case TargetingPriority.ClosestToEnd: return "突进";
                default: return priority.ToString();
            }
        }
    }
}
