using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>主菜单及其子页。保持单实例，不创建第二套战斗或音乐播放器。</summary>
    public sealed class FrontEndUi : MonoBehaviour
    {
        private GameManager _game;
        private BattleMusic _music;
        private GameObject _home, _practice, _musicBox, _settings;
        private UiText _trackTitle, _trackTime, _playLabel;
        private Slider _seek;
        private readonly Button[] _tracks = new Button[5];
        private GameObject _current;
        private const float ContentTop = 182f;
        private Vector2 _layoutSize;
        private float _pageAge;
        public string CurrentPage => _current != null ? _current.name : "";

        public void Init(GameManager game)
        {
            _game = game;
            _music = game.GetComponent<BattleMusic>();
            var scaler = GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = UiFactory.CreateImage(transform, "Backdrop", BattleUiTheme.Ink);
            UiFactory.Stretch(background.rectTransform);
            BuildHome();
            BuildPractice();
            BuildMusicBox();
            BuildSettings();
            ShowHome();
        }

        private GameObject Page(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(1280, 720);
            return root;
        }
        private void BuildHome()
        {
            _home = Page("Home");
            var p = _home.transform;
            var moon = Box(p, "Moon", BattleUiTheme.Surface, 115, 92, 390, 390);
            moon.sprite = SpriteFactory.Circle(.5f, Color.white);
            moon.raycastTarget = false;
            var shadow = Box(p, "MoonShadow", BattleUiTheme.Ink, 181, 73, 350, 350);
            shadow.sprite = moon.sprite;
            shadow.raycastTarget = false;
            var orbit = new GameObject("MoonOrbit", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiMoon));
            orbit.transform.SetParent(p, false);
            Place((RectTransform)orbit.transform, 46, 38, 520, 520);
            orbit.GetComponent<UiMoon>().raycastTarget = false;
            Box(p, "Horizon", BattleUiTheme.WithAlpha(BattleUiTheme.Paper, .22f), 64, 578, 580, 1);
            // 朱色鸟居剪影：独立于棋盘的标题页场景母题。
            var red = Color.Lerp(BattleUiTheme.Ink, BattleUiTheme.Action, .48f);
            Box(p, "GateLeft", red, 196, 404, 16, 174);
            Box(p, "GateRight", red, 423, 404, 16, 174);
            Box(p, "GateBeam", red, 165, 388, 303, 14);
            Box(p, "GateSecond", red, 185, 424, 263, 8);
            Label(p, "Title", "东方阵符录", 64, BattleUiTheme.Text, 72, 210, 580, 92);
            Label(p, "EnglishTitle", "～ Tactical Spell Card", 22, BattleUiTheme.Muted, 78, 308, 540, 36);
            Action(p, "Start", "01    开始游戏", () => _game.BeginRun(), 770, 190, 410, 62);
            Action(p, "Practice", "02    练习模式", () => Show(_practice), 770, 264, 410, 62);
            Action(p, "MusicBox", "03    音乐盒", () => Show(_musicBox), 770, 338, 410, 62);
            Action(p, "Settings", "04    设置", () => Show(_settings), 770, 412, 410, 62);
            Action(p, "Quit", "05    退出游戏", Quit, 770, 486, 410, 62);
        }
        private void Header(GameObject page, string title)
        {
            Box(page.transform, "TitleMark", BattleUiTheme.Action, 80, 76, 5, 48);
            Label(page.transform, "Title", title, 40, BattleUiTheme.Text, 100, 72, 840, 60);
            Box(page.transform, "Divider", BattleUiTheme.Grid, 80, 150, 1120, 1);
            Action(page.transform, "Back", "返回  /  ESC", ShowHome, 990, 74, 210, 46);
        }
        private void BuildPractice()
        {
            _practice = Page("Practice");
            Header(_practice, "练习模式");
            for (int i = 0; i < GameConfig.Rounds.Length; i++)
            {
                int stage = i;
                var button = Action(_practice.transform, "Stage" + (i + 1),
                    $"0{i + 1}    {GameConfig.Rounds[i].Title}", () => _game.BeginRun(stage, true), 80, ContentTop + i * 76, 1120, 62);
                Place(button.GetComponentInChildren<UiText>().rectTransform, 22, 8, 560, 46);
                Label(button.transform, "Detail", $"灵力 {GameConfig.PracticeStartingGold(i)}   /   敌影 {GameConfig.RoundEnemyCount(i)}",
                    16, BattleUiTheme.Muted, 610, 17, 485, 28);
            }
        }
        private void BuildMusicBox()
        {
            _musicBox = Page("MusicBox");
            Header(_musicBox, "音乐盒");
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                _tracks[i] = Action(_musicBox.transform, "Track" + (i + 1),
                    $"0{i + 1}    {BattleMusic.Titles[i]}", () => _music.Preview(index), 80, ContentTop + i * 70, 710, 56);
            }
            var player = Box(_musicBox.transform, "PlayerPlate", BattleUiTheme.SurfaceDeep, 832, ContentTop, 368, 386);
            Box(player.transform, "PlayerMark", BattleUiTheme.Action, 0, 0, 4, 386).raycastTarget = false;
            Label(player.transform, "NowPlaying", "NOW PLAYING", 12, BattleUiTheme.Paper, 26, 26, 315, 25);
            _trackTitle = Label(player.transform, "TrackTitle", "", 22, BattleUiTheme.Text, 26, 68, 315, 94);
            _trackTitle.FitInBox(18, true);
            _trackTime = Label(player.transform, "TrackTime", "", 16, BattleUiTheme.Muted, 26, 178, 315, 28);
            _seek = Slider(player.transform, "Seek", 0, v => _music.Seek(v), 26, 219, 314);
            var play = Action(player.transform, "Playback", "暂停", () => _music.TogglePlayback(), 26, 289, 315, 48);
            _playLabel = play.GetComponentInChildren<UiText>();
        }
        private void BuildSettings()
        {
            _settings = Page("Settings");
            Header(_settings, "设置");
            VolumeRow("MusicVolume", "音乐音量", GameSettings.MusicVolume, GameSettings.SetMusicVolume, 200);
            VolumeRow("SfxVolume", "音效音量", GameSettings.SfxVolume, GameSettings.SetSfxVolume, 296);
            Button motion = null;
            motion = Action(_settings.transform, "CameraMotion", "", () =>
            {
                GameSettings.SetCameraMotion(!GameSettings.CameraMotion);
                motion.GetComponentInChildren<UiText>().content = "镜头漂移与震动    " + (GameSettings.CameraMotion ? "开" : "关");
            }, 80, 402, 530, 58);
            motion.GetComponentInChildren<UiText>().content = "镜头漂移与震动    " + (GameSettings.CameraMotion ? "开" : "关");
            Button fullscreen = null;
            fullscreen = Action(_settings.transform, "Fullscreen", "切换全屏 / 窗口", () =>
            {
                bool next = !Screen.fullScreen;
                Screen.fullScreen = next;
                PlayerPrefs.SetInt("td_fullscreen", next ? 1 : 0);
            }, 650, 402, 550, 58);
            Label(_settings.transform, "Controls", "1–5 符阵  ·  Space 暂停  ·  X 倍速  ·  E 弹幕  ·  R 重来", 16, BattleUiTheme.Muted, 82, 520, 1100, 30);
        }
        private void VolumeRow(string name, string title, float value, Action<float> onChange, float y)
        {
            Label(_settings.transform, name + "Label", title, 22, BattleUiTheme.Text, 80, y, 280, 40);
            var valueLabel = Label(_settings.transform, name + "Value", Mathf.RoundToInt(value * 100) + "%", 20, BattleUiTheme.Paper, 1090, y, 100, 40);
            Slider(_settings.transform, name, value, v => { onChange(v); valueLabel.content = Mathf.RoundToInt(v * 100) + "%"; }, 390, y + 4, 650);
        }
        public void ShowHome()
        {
            GameSettings.Save();
            Show(_home);
            _music.MenuMusic();
        }
        private void Show(GameObject page)
        {
            foreach (var p in new[] { _home, _practice, _musicBox, _settings }) p.SetActive(p == page);
            _current = page;
            _pageAge = 0;
            if (page.GetComponent<CanvasGroup>() == null) page.AddComponent<CanvasGroup>();
            var first = page.GetComponentInChildren<Button>();
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        private void Update()
        {
            RefreshLayout();
            _pageAge += Time.unscaledDeltaTime;
            if (_current != null) _current.GetComponent<CanvasGroup>().alpha = Mathf.Clamp01(_pageAge / .22f);
            if (Input.GetKeyDown(KeyCode.Escape) && _current != _home) ShowHome();
            if (_current != _musicBox) return;
            int track = _music.TrackIndex;
            _trackTitle.content = track >= 0 ? BattleMusic.Titles[track] : "选择曲目";
            _trackTime.content = _music.Error ?? (_music.IsLoading ? "载入中…" : Clock(_music.Position) + " / " + Clock(_music.Duration));
            _playLabel.content = _music.IsPlaying ? "暂停" : "播放";
            _seek.interactable = !_music.IsLoading && _music.Duration > 0;
            _seek.SetValueWithoutNotify(_music.Duration > 0 ? _music.Position / _music.Duration : 0);
            for (int i = 0; i < _tracks.Length; i++)
                _tracks[i].GetComponentInChildren<UiText>().color = i == track ? BattleUiTheme.Ready : BattleUiTheme.Text;
        }

        public void RefreshLayout()
        {
            var size = ((RectTransform)transform).rect.size;
            if (size == _layoutSize || size.x < 1) return;
            _layoutSize = size;
            bool narrow = size.x / size.y < 1.25f;
            Vector2 pageSize = narrow ? new Vector2(720, 1120) : new Vector2(1280, 720);
            float scale = Mathf.Min(size.x / pageSize.x, size.y / pageSize.y);
            foreach (var page in new[] { _home, _practice, _musicBox, _settings })
            {
                var rect = (RectTransform)page.transform;
                rect.sizeDelta = pageSize;
                rect.localScale = Vector3.one * scale;
                if (page == _home) continue;
                Move(page, "Title", 100, 72, narrow ? 360 : 840, 60);
                Move(page, "Back", narrow ? 490 : 990, 74, 190, 46);
                Move(page, "Divider", 40, 150, pageSize.x - 80, 1);
            }
            Move(_home, "Title", 72, narrow ? 130 : 210, 580, 92);
            Move(_home, "EnglishTitle", 78, narrow ? 226 : 308, 540, 36);
            string[] buttons = { "Start", "Practice", "MusicBox", "Settings", "Quit" };
            for (int i = 0; i < buttons.Length; i++) Move(_home, buttons[i], narrow ? 80 : 770, (narrow ? 520 : 190) + i * 74, narrow ? 560 : 410, 62);
            for (int i = 0; i < GameConfig.Rounds.Length; i++)
            {
                string path = "Stage" + (i + 1);
                Move(_practice, path, narrow ? 40 : 80, ContentTop + i * (narrow ? 104 : 76), narrow ? 640 : 1120, narrow ? 92 : 62);
                Move(_practice, path + "/Label", 22, 8, narrow ? 590 : 560, 40);
                Move(_practice, path + "/Detail", narrow ? 22 : 610, narrow ? 50 : 17, narrow ? 590 : 485, 28);
            }
            for (int i = 0; i < _tracks.Length; i++) Move(_musicBox, "Track" + (i + 1), narrow ? 40 : 80, ContentTop + i * 70, narrow ? 640 : 710, 56);
            Move(_musicBox, "PlayerPlate", narrow ? 176 : 832, narrow ? 574 : ContentTop, 368, 386);
            foreach (var name in new[] { "MusicVolume", "SfxVolume" })
            {
                float y = name == "MusicVolume" ? 200 : 330;
                Move(_settings, name + "Label", 80, y, 280, 40);
                Move(_settings, name + "Value", narrow ? 550 : 1090, y, 100, 40);
                Move(_settings, name, narrow ? 80 : 390, narrow ? y + 58 : y + 4, 650, 36);
                _settings.transform.Find(name).localScale = new Vector3(narrow ? .86f : 1, 1, 1);
            }
            Move(_settings, "CameraMotion", 80, narrow ? 490 : 442, 530, 58);
            Move(_settings, "Fullscreen", narrow ? 80 : 650, narrow ? 570 : 442, narrow ? 530 : 550, 58);
            Move(_settings, "Controls", 80, narrow ? 680 : 540, narrow ? 560 : 1100, narrow ? 100 : 30);
            _settings.transform.Find("Controls").GetComponent<UiText>().FitInBox(14, narrow);
        }
        private static void Move(GameObject page, string path, float x, float y, float w, float h)
        {
            Place((RectTransform)page.transform.Find(path), x, y, w, h);
        }
        private static string Clock(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
        private static void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }
        private static Image Box(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var image = UiFactory.CreateImage(parent, name, color);
            Place(image.rectTransform, x, y, w, h);
            return image;
        }
        private static UiText Label(Transform parent, string name, string text, int size, Color color, float x, float y, float w, float h)
        {
            var label = UiFactory.CreateText(parent, name, text, size, color, TextAnchor.MiddleLeft);
            Place(label.rectTransform, x, y, w, h);
            label.FitInBox(Mathf.Max(11, size - 3));
            return label;
        }
        private static Button Action(Transform parent, string name, string text, UnityEngine.Events.UnityAction action, float x, float y, float w, float h)
        {
            var plate = Box(parent, name, Color.white, x, y, w, h);
            var button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            var colors = button.colors;
            colors.normalColor = BattleUiTheme.SurfaceDeep;
            colors.highlightedColor = BattleUiTheme.Surface;
            colors.selectedColor = BattleUiTheme.Surface;
            colors.pressedColor = Color.Lerp(BattleUiTheme.Surface, BattleUiTheme.Paper, .2f);
            colors.fadeDuration = .12f;
            button.colors = colors;
            button.onClick.AddListener(() => { Sfx.Click(); action(); });
            Label(plate.transform, "Label", text, 20, BattleUiTheme.Text, 22, 8, w - 42, h - 16);
            var accent = Box(plate.transform, "Accent", BattleUiTheme.Action, 0, 0, 3, h);
            accent.raycastTarget = false;
            UiMotion.Attach(button);
            return button;
        }
        private static Slider Slider(Transform parent, string name, float value, Action<float> action, float x, float y, float w)
        {
            var area = Box(parent, name, Color.clear, x, y, w, 36);
            var rail = Box(area.transform, "Rail", BattleUiTheme.Surface, 0, 15, w, 6);
            var fill = Box(rail.transform, "Fill", BattleUiTheme.Ready, 0, 0, w, 6);
            var handle = Box(area.transform, "Handle", BattleUiTheme.Paper, 0, 6, 12, 24);
            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0; slider.maxValue = 1;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => action(v));
            return slider;
        }
    }
}
