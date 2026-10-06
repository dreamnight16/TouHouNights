using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Effects;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>主菜单、练习、音乐盒和设置页面，共用 BattleUiTheme 与 UiKit。</summary>
    public sealed class FrontEndUi : MonoBehaviour
    {
        private GameManager _game;
        private BattleMusic _music;
        private GameObject _home, _practice, _musicBox, _settings;
        private UiText _trackTitle, _trackTime, _playLabel;
        private Slider _seek;
        private readonly Button[] _tracks = new Button[5];
        private GameObject _current;
        private const float ContentTop = 176f;
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
            background.raycastTarget = false;
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

            // 用背景色圆盘覆盖月盘一部分，形成月牙。
            var halo = Deco(p, "MoonHalo", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .07f), 880, -8, 340, 340);
            halo.sprite = SpriteFactory.Glow(1f, Color.white);
            var moon = Deco(p, "Moon", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .26f), 952, 62, 196, 196);
            moon.sprite = SpriteFactory.Circle(.5f, Color.white);
            var carve = Deco(p, "MoonCarve", BattleUiTheme.Ink, 1004, 34, 196, 196);
            carve.sprite = SpriteFactory.Circle(.5f, Color.white);

            Micro(p, "Kicker", "结界防卫  /  SPELL CARD DEFENSE", BattleUiTheme.Scarlet, 96, 124, 560, 16);
            Label(p, "Title", "东方阵符录", 72, BattleUiTheme.Text, 96, 150, 620, 112);
            Deco(p, "TitleRule", BattleUiTheme.Edge, 96, 266, 460, BattleUiTheme.Hairline);
            Deco(p, "TitleRuleAccent", BattleUiTheme.Scarlet, 96, 264, 76, 3);
            Label(p, "EnglishTitle", "～ Tactical Spell Card", 20, BattleUiTheme.Muted, 98, 284, 480, 32);


            Micro(p, "HomeMeta", "1–5 符阵　·　SPACE 暂停　·　X 倍速　·　E 弹幕", BattleUiTheme.Muted, 96, 620, 620, 16);
            Deco(p, "ColumnRule", BattleUiTheme.Grid, 712, 140, BattleUiTheme.Hairline, 440);

            // 菜单末行与左栏提示共用底线 636，列表起点为 636 - 358 = 278。
            MenuRow(p, "Start", "01", "开始游戏", () => _game.BeginRun(), 768, 278, 416, 62);
            MenuRow(p, "Practice", "02", "练习模式", () => Show(_practice), 768, 352, 416, 62);
            MenuRow(p, "MusicBox", "03", "音乐盒", () => Show(_musicBox), 768, 426, 416, 62);
            MenuRow(p, "Settings", "04", "设置", () => Show(_settings), 768, 500, 416, 62);
            MenuRow(p, "Quit", "05", "退出游戏", Quit, 768, 574, 416, 62);
        }

        /// <summary>子页标题、返回按钮和分隔线。</summary>
        private void Header(GameObject page, string title, string kicker)
        {
            var p = page.transform;
            Deco(p, "TitleMark", BattleUiTheme.Scarlet, 80, 76, 3, 44);
            Micro(p, "Kicker", kicker, BattleUiTheme.Muted, 96, 70, 620, 16);
            Label(p, "Title", title, 40, BattleUiTheme.Text, 96, 86, 700, 62);
            Deco(p, "Divider", BattleUiTheme.Grid, 80, 150, 1120, BattleUiTheme.Hairline);
            var back = UiKit.Button(p, "Back", "返回  /  ESC", UiButtonKind.Ghost, ShowHome);
            Place((RectTransform)back.transform, 990, 74, 210, 46);
        }
        private void BuildPractice()
        {
            _practice = Page("Practice");
            Header(_practice, "练习模式", "PRACTICE");
            for (int i = 0; i < GameConfig.Rounds.Length; i++)
            {
                int stage = i;
                var button = MenuRow(_practice.transform, "Stage" + (i + 1), "0" + (i + 1),
                    GameConfig.Rounds[i].Title, () => _game.BeginRun(stage, true), 80, ContentTop + i * 88, 1120, 70);
                var detail = Label(button.transform, "Detail",
                    "灵力 " + GameConfig.PracticeStartingSpirit(i) + "　/　敌影 " + GameConfig.RoundEnemyCount(i),
                    15, BattleUiTheme.Muted, 0, 0, 360, 70);
                AlignDetail(detail, false);
            }
        }
        private void BuildMusicBox()
        {
            _musicBox = Page("MusicBox");
            Header(_musicBox, "音乐盒", "MUSIC ROOM");
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                _tracks[i] = MenuRow(_musicBox.transform, "Track" + (i + 1), "0" + (i + 1),
                    BattleMusic.Titles[i], () => _music.Preview(index), 80, ContentTop + i * 78, 710, 62);
            }

            var player = UiKit.Surface(_musicBox.transform, "PlayerPlate", UiSurfaceKind.Panel);
            Place(player.rectTransform, 832, ContentTop, 368, 386);

            Deco(player.transform, "PlayerMark", BattleUiTheme.Scarlet, 0, 0, 3, 386);
            Micro(player.transform, "NowPlaying", "NOW PLAYING", BattleUiTheme.Paper, 26, 26, 315, 16);

            _trackTitle = Label(player.transform, "TrackTitle", "", 22, BattleUiTheme.Text, 26, 62, 315, 94);
            _trackTitle.FitInBox(18, true);
            _trackTime = Label(player.transform, "TrackTime", "", 15, BattleUiTheme.Muted, 26, 174, 315, 24);

            _seek = Slider(player.transform, "Seek", 0, v => _music.Seek(v), 26, 214, 314);
            var play = UiKit.Button(player.transform, "Playback", "暂停", UiButtonKind.Primary, () => _music.TogglePlayback());
            Place((RectTransform)play.transform, 26, 288, 315, 48);
            _playLabel = play.GetComponentInChildren<UiText>();
        }
        private void BuildSettings()
        {
            _settings = Page("Settings");
            Header(_settings, "设置", "SETTINGS");
            VolumeRow("MusicVolume", "音乐音量", GameSettings.MusicVolume, GameSettings.SetMusicVolume, 200);
            VolumeRow("SfxVolume", "音效音量", GameSettings.SfxVolume, GameSettings.SetSfxVolume, 306);

            Button motion = null;
            motion = Action(_settings.transform, "CameraMotion", "", () =>
            {
                GameSettings.SetCameraMotion(!GameSettings.CameraMotion);
                motion.GetComponentInChildren<UiText>().content = "镜头漂移与震动　" + (GameSettings.CameraMotion ? "开" : "关");
            }, 80, 412, 530, 58);
            motion.GetComponentInChildren<UiText>().content = "镜头漂移与震动　" + (GameSettings.CameraMotion ? "开" : "关");

            Action(_settings.transform, "Fullscreen", "切换全屏  /  窗口", () =>
            {
                bool next = !Screen.fullScreen;
                Screen.fullScreen = next;
                PlayerPrefs.SetInt("td_fullscreen", next ? 1 : 0);
            }, 650, 412, 550, 58);

            Micro(_settings.transform, "Controls",
                "1–5 符阵　·　SPACE 暂停　·　X 倍速　·　E 弹幕　·　R 重来",
                BattleUiTheme.Muted, 82, 520, 1100, 20);
        }
        private void VolumeRow(string name, string title, float value, Action<float> onChange, float y)
        {
            Label(_settings.transform, name + "Label", title, 20, BattleUiTheme.Text, 80, y, 280, 40);
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
            // 按名字获取 Label；MenuRow 的第一个 UiText 是序号，不能用 GetComponentInChildren。
            for (int i = 0; i < _tracks.Length; i++)
            {
                var title = _tracks[i].transform.Find("Label")?.GetComponent<UiText>();
                if (title != null) title.color = i == track ? BattleUiTheme.Scarlet : BattleUiTheme.Bone;
            }
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
                Move(page, "TitleMark", 80, 76, 3, 44);
                Move(page, "Kicker", 96, 70, narrow ? 560 : 620, 16);
                Move(page, "Title", 96, 86, narrow ? 560 : 840, 54);
                Move(page, "Back", narrow ? 480 : 990, 74, 200, 46);
                Move(page, "Divider", 80, 150, pageSize.x - 160, BattleUiTheme.Hairline);
            }

            // ---- 标题页 ----
            Move(_home, "MoonHalo", narrow ? 200 : 880, -8, narrow ? 320 : 340, narrow ? 320 : 340);
            Move(_home, "Moon", narrow ? 262 : 952, narrow ? 90 : 62, 196, 196);
            // 遮罩圆相对月盘向右上偏移，保持月牙方向。
            Move(_home, "MoonCarve", narrow ? 314 : 1004, narrow ? 62 : 34, 196, 196);
            float gutter = narrow ? 48f : 96f;
            Move(_home, "Kicker", gutter, narrow ? 96 : 124, narrow ? 600 : 560, 16);
            Move(_home, "Title", gutter, narrow ? 122 : 150, narrow ? 624 : 620, narrow ? 92 : 96);
            Move(_home, "TitleRule", gutter, narrow ? 230 : 266, narrow ? 600 : 460, BattleUiTheme.Hairline);
            Move(_home, "TitleRuleAccent", gutter, narrow ? 228 : 264, 76, 3);
            Move(_home, "EnglishTitle", gutter + 2f, narrow ? 248 : 284, narrow ? 560 : 480, 30);
            Move(_home, "ColumnRule", narrow ? 48 : 712, narrow ? 520 : 140,
                narrow ? 624 : BattleUiTheme.Hairline, narrow ? BattleUiTheme.Hairline : 440);
            Move(_home, "HomeMeta", gutter, narrow ? 1050 : 620, narrow ? 624 : 620, 16);
            string[] buttons = { "Start", "Practice", "MusicBox", "Settings", "Quit" };
            for (int i = 0; i < buttons.Length; i++)
                // 宽屏末行与左栏对齐；窄屏按竖排版式定位。
                Move(_home, buttons[i], narrow ? 48 : 768, (narrow ? 560 : 278) + i * (narrow ? 78 : 74), narrow ? 624 : 416, narrow ? 66 : 62);

            // ---- 练习模式 ----
            for (int i = 0; i < GameConfig.Rounds.Length; i++)
            {
                string path = "Stage" + (i + 1);
                Move(_practice, path, narrow ? 40 : 80, ContentTop + i * (narrow ? 104 : 88), narrow ? 640 : 1120, narrow ? 84 : 70);
                var detail = _practice.transform.Find(path + "/Detail");
                if (detail != null) AlignDetail(detail.GetComponent<UiText>(), narrow);
            }

            // ---- 音乐盒 ----
            for (int i = 0; i < _tracks.Length; i++)
                Move(_musicBox, "Track" + (i + 1), narrow ? 40 : 80, ContentTop + i * (narrow ? 92 : 78), narrow ? 640 : 710, narrow ? 76 : 62);
            Move(_musicBox, "PlayerPlate", narrow ? 176 : 832, narrow ? 656 : ContentTop, 368, 386);
            Move(_musicBox, "PlayerPlate/PlayerMark", 0, 0, 3, 386);

            // ---- 设置 ----
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
            Move(_settings, "Controls", 80, narrow ? 680 : 540, narrow ? 560 : 1100, narrow ? 60 : 20);
            _settings.transform.Find("Controls").GetComponent<UiText>().FitInBox(13, narrow);
        }
        private static void Move(GameObject page, string path, float x, float y, float w, float h)
        {
            Place((RectTransform)page.transform.Find(path), x, y, w, h);
        }

        /// <summary>
        /// 练习行副信息在宽屏右对齐，窄屏移到主标题下方。
        /// 拉伸锚点下 sizeDelta.y 表示高度增量，两种布局需使用不同锚点。
        /// </summary>
        private static void AlignDetail(UiText detail, bool narrow)
        {
            var r = detail.rectTransform;
            if (narrow)
            {
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
                r.anchoredPosition = new Vector2(-24f, 12f);
                r.sizeDelta = new Vector2(592f, 24f);
                detail.SetAlignment(TextAnchor.MiddleLeft);
            }
            else
            {
                r.anchorMin = new Vector2(1f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, .5f);
                r.anchoredPosition = new Vector2(-24f, 0f);
                r.sizeDelta = new Vector2(360f, 0f);
                detail.SetAlignment(TextAnchor.MiddleRight);
            }
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

        /// <summary>不拦截指针的装饰块。</summary>
        private static Image Deco(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var image = Box(parent, name, color, x, y, w, h);
            image.raycastTarget = false;
            return image;
        }
        private static UiText Label(Transform parent, string name, string text, int size, Color color, float x, float y, float w, float h)
        {
            return UiKit.Label(parent, name, text, size, color, TextAnchor.MiddleLeft, x, y, w, h);
        }
        private static UiText Micro(Transform parent, string name, string text, Color color, float x, float y, float w, float h)
        {
            return UiKit.MicroLabel(parent, name, text, color, x, y, w, h);
        }

        /// <summary>带编号、标题和底部分隔线的菜单行，悬停反馈由 UiMotion 处理。</summary>
        private static Button MenuRow(Transform parent, string name, string index, string text,
            UnityEngine.Events.UnityAction action, float x, float y, float w, float h)
        {
            var panel = UiKit.Surface(parent, name, UiSurfaceKind.Veil);
            panel.Flat(Color.clear).Border(Color.clear, 0f);
            Place(panel.rectTransform, x, y, w, h);

            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = UiKit.FlatColors();
            button.onClick.AddListener(() => { Sfx.Click(); action(); });

            var rule = UiFactory.CreateImage(panel.transform, "Rule", BattleUiTheme.Edge);
            var rr = rule.rectTransform;
            rr.anchorMin = new Vector2(0f, 0f);
            rr.anchorMax = new Vector2(1f, 0f);
            rr.pivot = new Vector2(.5f, 0f);
            rr.anchoredPosition = Vector2.zero;
            rr.sizeDelta = new Vector2(0f, BattleUiTheme.Hairline);
            rule.raycastTarget = false;

            var number = UiFactory.CreateText(panel.transform, "Index", index, BattleUiTheme.Type.Small,
                BattleUiTheme.Muted, TextAnchor.MiddleLeft);
            var nr = number.rectTransform;
            nr.anchorMin = new Vector2(0f, 0f);
            nr.anchorMax = new Vector2(0f, 1f);
            nr.pivot = new Vector2(0f, .5f);
            nr.anchoredPosition = new Vector2(20f, 0f);
            nr.sizeDelta = new Vector2(40f, 0f);
            number.SetCharacterSpacing(2f);

            var label = UiFactory.CreateText(panel.transform, "Label", text, 23, BattleUiTheme.Bone, TextAnchor.MiddleLeft);
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0f, .5f);
            lr.offsetMin = new Vector2(66f, 0f);
            lr.offsetMax = new Vector2(-24f, 0f);
            label.FitInBox(16);

            UiMotion.Attach(button);
            return button;
        }

        /// <summary>使用 Ghost 样式的次级行动按钮。</summary>
        private static Button Action(Transform parent, string name, string text,
            UnityEngine.Events.UnityAction action, float x, float y, float w, float h)
        {
            var button = UiKit.Button(parent, name, text, UiButtonKind.Ghost, () => action());
            Place((RectTransform)button.transform, x, y, w, h);
            return button;
        }
        private static Slider Slider(Transform parent, string name, float value, Action<float> action, float x, float y, float w)
        {
            const float RailHeight = 4f;
            const float HandleAreaHeight = 36f;
            var area = Box(parent, name, Color.clear, x, y, w, HandleAreaHeight);

            var rail = Deco(area.transform, "Rail", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .12f), 0, 16, w, RailHeight);
            // Slider 会改写 fillRect 和 handleRect 的锚点，尺寸由其父容器控制。
            var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(rail.transform, false);
            UiFactory.Stretch(fillArea);
            var fill = UiFactory.CreateImage(fillArea, "Fill", BattleUiTheme.Scarlet);
            fill.raycastTarget = false;
            var fr = fill.rectTransform;
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 1f);
            fr.pivot = new Vector2(0f, .5f);
            fr.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(area.transform, false);
            UiFactory.Stretch(handleArea);
            var handle = UiFactory.CreateImage(handleArea, "Handle", BattleUiTheme.Bone);
            handle.raycastTarget = false;
            var hr = handle.rectTransform;
            hr.anchorMin = new Vector2(0f, 0f);
            hr.anchorMax = new Vector2(0f, 1f);
            hr.pivot = new Vector2(.5f, .5f);
            // HandleArea 高度为 36；拉伸锚点下要得到 8 点高度，sizeDelta.y 应为 8 - 36。
            hr.sizeDelta = new Vector2(3f, 8f - HandleAreaHeight);

            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fr;
            slider.handleRect = hr;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => action(v));
            return slider;
        }
    }
}
