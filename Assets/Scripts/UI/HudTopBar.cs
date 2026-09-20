using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// 战局状态层（顶栏）—— 重构版（参照明日方舟视觉语言）。
    /// 炭灰玻璃底 + 底部青蓝光轨 + 四角截取框；唯一强调色青蓝，灵力走金、结界走红。
    /// 定位沿用 TdLayout.Band + InLeft / InRight / InCenter（锁宽 1280，任意比例不裁切）。
    /// </summary>
    public sealed class HudTopBar : MonoBehaviour
    {
        private UiText _spiritValue;
        private UiText _barrierValue;
        private UiText _enemyValue;
        private UiText _roundTitle;
        private UiText _roundEng;
        private UiText _pauseLabel;
        private RoundedRectImage _roundFill;
        private RoundedRectImage _barrierChip;
        private Button[] _speedButtons;

        private const float NeededWidth = 980f;

        private RectTransform _band;
        private RectTransform _content;
        private WaveSpawner _subscribed;

        private string _title = "序章";
        private string _eng = "WAVE I";

        private void Awake()
        {
            Build();
        }

        private void OnDestroy()
        {
            UnhookSpawner();
        }

        private void HookSpawner(WaveSpawner spawner)
        {
            if (_subscribed == spawner) return;
            UnhookSpawner();
            _subscribed = spawner;
            if (_subscribed != null) _subscribed.OnRoundStart += OnRoundStart;
        }

        private void UnhookSpawner()
        {
            if (_subscribed == null) return;
            _subscribed.OnRoundStart -= OnRoundStart;
            _subscribed = null;
        }

        private void OnRoundStart(int index, string title, string eng)
        {
            _title = title;
            _eng = eng;
        }

        private void Build()
        {
            var band = TdLayout.NewChild(transform, "TopBand");
            TdLayout.Band(band, true, TdTheme.TopBarH);
            _band = band;

            // 炭灰玻璃底 + 底部青蓝光轨（全局强调色的具象化）
            var bg = TdKit.Glass(band, "Bg", 0f, TdTheme.Surface1, default, 0f);
            UiFactory.Stretch(bg.rectTransform);
            TdKit.AccentLine(band, "AccentLine", true, TdTheme.Accent);

            // 整条带四角截取框（军工终端感）
            TdKit.Corners(band, "Frame", new Color(TdTheme.Accent.r, TdTheme.Accent.g, TdTheme.Accent.b, 0.45f), 14f, 1.5f);

            var content = TdLayout.NewChild(band, "Content");
            TdLayout.Fill(content);
            _content = content;

            // ---- 左：灵力（金 · 钻石图标 + 大字号 + 衬线标签）----
            var spirit = TdLayout.NewChild(content, "Spirit");
            TdLayout.InLeft(spirit, TdTheme.Space.Lg, 168f, 48f);
            var spiritBg = TdKit.Glass(spirit, "Bg", 0f, TdTheme.Surface2, TdTheme.Hairline, 1f);
            UiFactory.Stretch(spiritBg.rectTransform);
            TdKit.Sheen(spiritBg.transform, "Sheen", 0f, TdTheme.Sheen);
            TdKit.Corners(spirit, "Frame", new Color(TdTheme.Spirit.r, TdTheme.Spirit.g, TdTheme.Spirit.b, 0.5f), 10f, 1.5f);

            var sIcon = TdKit.Glass(spirit, "Icon", 0f, TdTheme.Spirit, default, 0f);
            TdLayout.At(sIcon.rectTransform, 16f, 24f, 22f, 22f);
            sIcon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _spiritValue = TdKit.Text(spirit, "Value", "0", 26, TdTheme.Spirit, TextAnchor.MiddleLeft);
            TdLayout.At(_spiritValue.rectTransform, 46f, 14f, 100f, 24f);
            _spiritValue.SetVerticalGradient(TdTheme.Spirit, Color.Lerp(TdTheme.Spirit, Color.black, 0.28f));
            var sLabel = TdKit.Text(spirit, "Label", "灵力 ENERGY", 10, TdTheme.InkDim, TextAnchor.MiddleLeft);
            TdLayout.At(sLabel.rectTransform, 46f, 34f, 112f, 12f);
            sLabel.SetCharacterSpacing(2f);

            // ---- 中：幕次（衬线 WAVE 标题 + 中文幕名 + 进度轨）----
            var round = TdLayout.NewChild(content, "RoundModule");
            TdLayout.InCenter(round, 0f, 240f, 50f);
            var roundBg = TdKit.Glass(round, "Bg", 0f, TdTheme.Surface2, TdTheme.Hairline, 1f);
            UiFactory.Stretch(roundBg.rectTransform);
            TdKit.Sheen(roundBg.transform, "Sheen", 0f, TdTheme.Sheen);
            TdKit.Corners(round, "Frame", new Color(TdTheme.Accent.r, TdTheme.Accent.g, TdTheme.Accent.b, 0.5f), 12f, 1.5f);

            _roundEng = TdKit.Text(round, "Wave", "WAVE I", 22, TdTheme.Accent, TextAnchor.MiddleCenter);
            TdLayout.At(_roundEng.rectTransform, 0f, 34f, 240f, 22f);
            _roundEng.SetCharacterSpacing(3f);
            _roundTitle = TdKit.Text(round, "Title", "序章", 13, TdTheme.Ink, TextAnchor.MiddleCenter);
            TdLayout.At(_roundTitle.rectTransform, 0f, 14f, 240f, 16f);
            _roundTitle.SetCharacterSpacing(4f);

            var bar = TdKit.Glass(round, "Bar", 0f, new Color(1f, 1f, 1f, 0.10f), default, 0f);
            TdLayout.At(bar.rectTransform, 30f, 4f, 180f, 2f);
            _roundFill = TdKit.Glass(bar.transform, "Fill", 0f, TdTheme.Accent, default, 0f);
            UiFactory.Stretch(_roundFill.rectTransform);
            _roundFill.Progress = 0f;

            // ---- 右：敌影 / 结界 / 流速 / 静止 ----
            var enemy = Cell(content, "Enemy", 404f, TdTheme.Accent, UiIconKind.Threat, out _enemyValue);
            var barrier = Cell(content, "Barrier", 290f, TdTheme.Barrier, UiIconKind.Heart, out _barrierValue);
            _barrierChip = barrier.GetComponent<RoundedRectImage>();

            var divider = TdKit.Line(content, "Divider", 0f, 1f, TdTheme.HairlineStrong);
            TdLayout.AtRight(divider.rectTransform, 278f, 32f, 1f, 36f);

            // 流速分段
            var speedBox = TdLayout.NewChild(content, "SpeedBox");
            TdLayout.InRight(speedBox, 96f, 170f, 48f);
            _speedButtons = new Button[4];
            var labels = new[] { "0.5", "1", "2", "4" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var b = TdKit.Button(speedBox, "Speed" + i, labels[i], 0f, TdTheme.Surface2,
                    () => GameManager.Instance?.SetSpeedIndex(index), 15);
                TdLayout.At(b.GetComponent<RectTransform>(), i * 44f, 24f, 38f, 34f);
                _speedButtons[i] = b;
            }

            var pause = TdKit.Button(content, "Pause", "静止", 0f, TdTheme.Surface2,
                () => GameManager.Instance?.TogglePause(), 13);
            TdLayout.InRight(pause.GetComponent<RectTransform>(), TdTheme.Space.Lg, 64f, 48f);
            _pauseLabel = pause.GetComponentInChildren<UiText>();
        }

        /// <summary>右侧读数单元：图标 + 数值 + 小标签，外框为带角标的炭灰玻璃。</summary>
        private static RectTransform Cell(Transform parent, string name, float right, Color accent, UiIconKind kind, out UiText value)
        {
            var cell = TdKit.Glass(parent, name, 0f, TdTheme.Surface2, TdTheme.Hairline, 1f);
            TdLayout.InRight(cell.rectTransform, right, 100f, 40f);
            TdKit.Sheen(cell.transform, "Sheen", 0f, TdTheme.Sheen);
            TdKit.Corners(cell.transform, "Frame", new Color(accent.r, accent.g, accent.b, 0.45f), 9f, 1.5f);

            var icon = UiIcon.Create(cell.transform, kind, 18f, accent);
            TdLayout.At(icon, 12f, 20f, 18f, 18f);

            value = TdKit.Text(cell.transform, name + "Value", "0", 21, TdTheme.Ink, TextAnchor.MiddleLeft);
            TdLayout.At(value.rectTransform, 36f, 14f, 58f, 20f);

            var label = TdKit.Text(cell.transform, name + "Label", kind == UiIconKind.Heart ? "结界" : "敌影", 11, TdTheme.InkDim, TextAnchor.MiddleLeft);
            TdLayout.At(label.rectTransform, 36f, 30f, 58f, 12f);
            label.SetCharacterSpacing(2f);

            return cell.rectTransform;
        }

        private void FitContent()
        {
            if (_content == null || _band == null) return;
            float w = _band.rect.width;
            if (w <= 1f) return;
            float target = w >= NeededWidth ? 1f : Mathf.Clamp(w / NeededWidth, 0.72f, 1f);
            _content.localScale = new Vector3(target, target, 1f);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            HookSpawner(gm.WaveSpawner);
            FitContent();

            _spiritValue.content = gm.Spirit.ToString();
            _enemyValue.content = gm.EnemyCount + " / " + gm.TotalEnemies;
            _barrierValue.content = gm.Lives.ToString();

            var ws = gm.WaveSpawner;
            if (ws != null)
            {
                int total = ws.CurrentRoundTotal;
                _roundFill.Progress = total > 0 ? Mathf.Clamp01((float)ws.CurrentRoundSpawned / total) : 0f;
                _roundTitle.content = _title;
                _roundEng.content = _eng;
            }

            for (int i = 0; i < _speedButtons.Length; i++)
            {
                bool on = i == gm.SpeedIndex;
                var rri = _speedButtons[i].GetComponent<RoundedRectImage>();
                var lab = _speedButtons[i].GetComponentInChildren<UiText>();
                if (rri != null)
                {
                    rri.color = on ? new Color(0.090f, 0.215f, 0.310f, 0.98f) : new Color(0.10f, 0.15f, 0.20f, 0.85f);
                    rri.borderColor = on ? TdTheme.Accent : new Color(1f, 1f, 1f, 0.30f);
                    rri.borderWidth = on ? 2.2f : 1.2f;
                }
                if (lab != null) lab.color = on ? TdTheme.AccentBright : TdTheme.InkDim;
            }

            if (_pauseLabel != null) _pauseLabel.content = gm.IsPaused ? "继续" : "静止";
            var pauseBtn = _pauseLabel?.GetComponentInParent<Button>();
            if (pauseBtn != null)
            {
                var rri = pauseBtn.GetComponent<RoundedRectImage>();
                if (rri != null)
                {
                    rri.borderColor = gm.IsPaused ? TdTheme.Barrier : TdTheme.Hairline;
                    rri.borderWidth = gm.IsPaused ? 2.2f : 1f;
                }
            }

            // 结界吃紧：危险态边框呼吸（动效有目的）
            if (_barrierChip != null)
            {
                bool danger = gm.Lives <= 3;
                float pulse = danger ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
                _barrierChip.borderColor = danger
                    ? new Color(TdTheme.Barrier.r, TdTheme.Barrier.g, TdTheme.Barrier.b, pulse)
                    : TdTheme.Hairline;
            }
        }
    }
}
