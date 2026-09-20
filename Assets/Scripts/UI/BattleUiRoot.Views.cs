using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    public sealed partial class BattleUiRoot
    {
        /// <summary>
        /// 界面装配。原则只有一条：**每条信息只有一个主角**。
        /// 每块区域里最大的那个数字决定玩家先看到什么，其余全部退成极小米字。
        /// 上一版的问题是每样东西都装在小盒子里、字号只差两级，看过去全是同权重的方框。
        /// </summary>
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

        /// <summary>
        /// 入场编排。战斗开始时 HUD 的各块**依次落位**，而不是一次性全部就位 ——
        /// 层级在这里是靠时间讲出来的：先出现的先被读到。
        /// 顺序照着玩家的视线走：关卡 → 战况 → 倍速 → 出阵 → 弹幕。
        /// </summary>
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
            // 找不到就跳过。入场是装饰，绝不能因为它没找到而让 HUD 缺一块。
            if (transform.Find(name) is RectTransform rect) UiEntrance.Play(rect, from, delay);
        }

        /// <summary>
        /// 顶栏与底栏。它们**不是**两条留出来的黑带，而是压在战场上的两片渐变暗角 ——
        /// 相机现在铺满全屏（见 Layout.cs），所以这里的每一级 alpha 都是直接叠在天空和星野上的。
        /// 唯一的作用是让贴边的文字读得清；出了这块区域 alpha 归零，战场一像素都没被挡住。
        ///
        /// 这两处渐变是全项目**仅存**的渐变，而它们和别处被删掉的渐变不是一回事：
        /// 面板上的 Stone→Char 是**模仿材质**（假装这块面被光照到），而这里的 Char→透明
        /// 本身就是**内容** —— 它没有模仿任何东西，它就是「从有到无」这条过渡本身。
        /// 判据是：把这条渐变换成平涂，它就不再是它了（会变成一条有硬边的黑带）；
        /// 而把面板渐变换成平涂，画面只是变干净了。前者留下，后者删掉。
        /// </summary>
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

            // 作者署名：做成印刷品上的制作者暗记 —— 极小、贴边、不参与布局博弈。
            // 具体位置由 Layout.cs 决定（它要跟着底栏高度走）。
            Micro(transform, "DreamNight", "梦夜十六", BattleUiTheme.WithAlpha(BattleUiTheme.Ash, .50f),
                0f, 0f, 1f, 1f).Also(t => PinBottomLeft(t.rectTransform, 24f, 4f, 200f, 14f))
                             .SetCharacterSpacing(2.2f);
        }

        /// <summary>
        /// 关卡编号。这里曾经是一块 240×84 的描边面板，里面装着：封印环、编号、一条竖分隔线，
        /// 还有一行**游戏自己的名字**。三样东西都该走：
        ///
        ///   · 游戏名出现在战斗 HUD 上是纯粹的自我指涉 —— 玩家已经在玩了，不必再被告知在玩什么。
        ///   · 封印环只是给编号当画框，编号本身已经足够大。
        ///   · 整块面板占掉左上角一块实地。而它承载的信息只有「第几关」三个字符。
        ///
        /// 现在只剩两组文字，直接压在顶栏的暗角上：左边「STAGE / 编号」，
        /// 右边「当前幕次 / 阶段」。分隔靠间距，不靠线。
        /// </summary>
        private void BuildMissionTag()
        {
            var tag = new GameObject("MissionTag", typeof(RectTransform)).GetComponent<RectTransform>();
            tag.SetParent(transform, false);
            PinTopLeft(tag, BattleUiTheme.Safe, BattleUiTheme.Safe, 300f, 84f);

            // 东方 HUD 的顺序：小标签在上、大数值在下。
            // 40pt 的字形连行距大约要 58px，框给矮了 TMP 会判定「放不下」而丢掉整行。
            Micro(tag, "Sector", "STAGE", BattleUiTheme.Scarlet, 0f, 2f, 110f, 16f);
            _stageNumber = Text(tag, "No", "—", 40, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 18f, 110f, 58f);
            _stageNumber.SetCharacterSpacing(-1.5f);

            _mission = Text(tag, "Mission", "作战准备", 19, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 116f, 22f, 184f, 28f);
            _missionSub = Text(tag, "Sub", "准备", 12, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 117f, 50f, 184f, 18f);
            _missionSub.SetCharacterSpacing(.6f);
        }

        /// <summary>
        /// 战况读数：三个超大数字横排，中间只用发丝线分隔。
        /// 上一版给每个读数套了一个小盒子 —— 盒子立刻把界面拉回「控制台仪表盘」，
        /// 而数字本身才是玩家真正在看的东西。去掉盒子，把省下的空间全部给字号。
        /// </summary>
        private void BuildReadout()
        {
            var group = new GameObject("BattleReadout", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinTopCenter(group, 0f, BattleUiTheme.Safe, 448f, 76f);

            // 原来第一列是「STAGE —/5」—— 和左上角的关卡编号是同一个数字，说了两遍。
            // 空出来的位置给击破数：一个玩家真正会想去瞄、而且别处看不到的量。
            _kills = Readout(group, "Kills", "击破 KILLS", 0f, out _);
            _enemies = Readout(group, "Enemies", "出现 / 总数", 152f, out _enemiesCaption);
            _barrier = Readout(group, "Barrier", "结界耐久", 304f, out _);

            UiKit.RuleV(group, "SepA", BattleUiTheme.Grid, 142f, 14f, 48f);
            UiKit.RuleV(group, "SepB", BattleUiTheme.Grid, 294f, 14f, 48f);
            // 三个读数下面原本压着一条横贯 448px 的装饰线。它不表示进度、不表示分组边界，
            // 只是给这排数字「垫个底」—— 东方原作的 HUD 上没有一个像素是为垫底而存在的。
        }

        /// <summary>
        /// 一个读数 = 一行极小米字 + 一个超大数字，小米字压在数字**上方**。
        /// 这条顺序是从东方原作的 HUD 直接搬过来的：原作里 Score / HiScore / Player / Power
        /// 全是「小标签在上、数字在下」，玩家先读到「这是什么」，再读到「值是多少」。
        /// 标签放在下面时，眼睛会先撞上一个大数字、再回头去找它是什么 —— 顺序是反的。
        /// </summary>
        private UiText Readout(Transform parent, string name, string caption, float x, out UiText captionText)
        {
            captionText = Micro(parent, name + "Caption", caption, BattleUiTheme.Ash, x, 0f, 138f, 16f);
            var value = Text(parent, name, "—", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, x, 16f, 138f, 48f);
            value.SetCharacterSpacing(-.5f);
            return value;
        }

        /// <summary>
        /// 倍速与暂停。当前倍速是主角（超大数字），四个档位退成它下面的小米字。
        /// 面板底也已撤掉 —— 这块和左上角的关卡编号一样，内容全是「值 + 几个可选档位」，
        /// 给它一个 340×84 的框只是把两条本可以直接浮在天空上的信息装进盒子。
        /// </summary>
        private void BuildTimeControls()
        {
            var group = new GameObject("TimeControls", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinTopRight(group, BattleUiTheme.Safe, BattleUiTheme.Safe, 340f, 84f);

            _speed = Text(group, "Speed", "1×", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 4f, 84f, 50f);
            _speed.SetCharacterSpacing(-.5f);
            Micro(group, "SpeedLabel", "倍速 SPEED", BattleUiTheme.Ash, 19f, 56f, 84f, 16f);

            // 倍速数字和档位之间原来有一条竖发丝线。它不是分隔，只是「两个东西之间放点什么」——
            // 82px 的空白本身就是分隔。
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                // 4 个小方块挤在 156px 里，必须自己给字号和内缩 —— 默认的 16pt 内缩会把标签框压到 4px 宽。
                var chip = UiKit.Toggle(group, "Speed" + i, new[] { "0.5", "1", "2", "4" }[i],
                    () => _game.SetSpeedIndex(index), BattleUiTheme.Type.Small, 4f);
                PinTopLeft(chip.GetComponent<RectTransform>(), 112f + 38f * i, 46f, 36f, 26f);
                _speedLabels[i] = chip.GetComponentInChildren<UiText>();

                // 当前档的短横线：贴着小方块的下沿，骨白 85% —— 它是状态标记，不是装饰，
                // 所以不用绯（绯留给危险与就绪）。默认关掉，RefreshReadout 里按当前档位点亮。
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

        /// <summary>
        /// 弹幕威力。这里原本是一块 224×182 的描边面板，孤零零挂在左下角，
        /// 而它对面 461px 宽的底栏是空的 —— 一条栏被拆成「一个盒子 + 一片空地」，正是
        /// 「UI 独立于战场之外」最典型的样子。
        ///
        /// 现在它是一条横躺在底栏左半边的读数：左边「标签 / 大数字 / 增伤」，右边发动键，
        /// 底下压一条贯穿全宽的充能轨。底栏因此变成一行连贯的信息带：
        /// 弹幕 → 灵力 · 出阵 → 符卡。
        /// </summary>
        private void BuildBarrage()
        {
            var group = new GameObject("BarrageCommand", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(transform, false);
            PinBottomLeft(group, BattleUiTheme.Safe, BattleUiTheme.Gap.Lg, 600f, 132f);

            // 和出阵区同一条排版律：小米字在上、大数值在下。
            Micro(group, "Title", "弹幕威力 POWER", BattleUiTheme.Ash, 0f, 0f, 200f, 16f);
            _power = Text(group, "Power", "0.00", 44, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 14f, 152f, 64f);
            _power.SetCharacterSpacing(-1.5f);
            // 增伤贴着大数字的垂直中线，字号抬高到 15 —— 它原来孤零零悬在半空、小得像误入的灰尘。
            // 就绪时它会变绯，成为左下角唯一会说话的颜色。
            _powerBonus = Text(group, "Bonus", "增伤 +0%", 15, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 154f, 42f, 146f, 22f);

            // 发动键紧挨着读数（左缘 340），而不是甩到 600 宽读数条的最右端 ——
            // 原来按钮和数值之间隔着 200px 的空地，读起来像两个不相干的东西。
            // 挪过来之后：数字 → 增伤 → 发动键，一整块视觉单元；右侧留白由底栏呼吸承担。
            _barrage = UiKit.Button(group, "Fire", "E  发动", UiButtonKind.Primary, () => _game.TriggerBarrage());
            PinBottomLeft(_barrage.GetComponent<RectTransform>(), 340f, 24f, 240f, 56f);
            _barragePanel = _barrage.GetComponent<UiPanel>();
            // 就绪不是「换成一个红按钮」，而是**按钮开始发光**。呼吸让「可以按了」这件事
            // 在余光里也能被察觉 —— 玩家不必盯着灵力条去数什么时候满。
            _barrageHalo = UiHalo.Attach(_barragePanel, BattleUiTheme.GlowScarlet, 26f).Breathe(2.2f, .45f);
            _barrageLabel = _barrage.GetComponentInChildren<UiText>();

            // 充能轨铺满整条读数，是这块区域里唯一一条横向元素 —— 它同时兼任
            // 「这块到哪儿为止」的下边界，所以不需要再给面板画框。
            // 空轨用 .16 而不是 .10：这一块在灵力为 0 时几乎全是深色，
            // 轨道太淡会让整条读数看起来是一块什么都没画的空地。
            var rail = Image(group, "Rail", BattleUiTheme.WithAlpha(BattleUiTheme.Bone, .16f));
            PinBottomLeft(rail.rectTransform, 0f, 0f, 600f, 4f);
            _powerFill = Image(rail.transform, "Fill", BattleUiTheme.Scarlet);
            _powerFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _powerFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _powerFill.rectTransform.pivot = new Vector2(0f, .5f);
            _powerFill.rectTransform.sizeDelta = Vector2.zero;
        }

        /// <summary>出阵区：一条极简信息头 + 五张符卡。卡匣留出 214 的高度 —— 选中的卡会向上展开 18px，必须给它留出伸进来的余地。</summary>
        private void BuildHand()
        {
            var hand = new GameObject("DeploymentHand", typeof(RectTransform)).GetComponent<RectTransform>();
            hand.SetParent(transform, false);
            PinBottomRight(hand, BattleUiTheme.Safe, 24f, 744f, 214f);

            var header = new GameObject("DeploymentHeader", typeof(RectTransform)).GetComponent<RectTransform>();
            header.SetParent(hand, false);
            PinTopLeft(header, 0f, 0f, 744f, 60f);

            // 灵力：全场唯一的灰金。它是货币，用「钱」的颜色，但面积控制在一个数字之内。
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

        /// <summary>
        /// 一张符卡。信息优先级：灵力消耗（最大）→ 塔名 → 职能 + 快捷键（最小）。
        /// 不可出阵时**整张卡退到暗处**，而不是变红 —— 绯红是危险与就绪专用的，
        /// 「钱不够」是日常状态，不该占用全场唯一的高热色。
        /// </summary>
        private DeployCard BuildCard(Transform hand, TowerType type, int index)
        {
            var def = GameConfig.Towers[type];
            var card = new DeployCard { Type = type };

            var panel = Surface(hand, "Card_" + type, UiSurfaceKind.Card);
            // 左上 / 右下 12px 斜切：方舟干员卡的签名切口。两张斜角让一排五张卡
            // 不再是五个一模一样的方块，而且切口永远只咬住没有内容的那个角。
            panel.Corners(UiCorner.Diagonal, 12f);
            PinBottomLeft(panel.rectTransform, index * 150f, 0f, 144f, CardHeight);
            card.Panel = panel;
            card.Rect = panel.rectTransform;

            // 武器图形：用塔的自身色，但压暗到「近单色 + 一点色相」的程度，
            // 免得五张卡变成五块彩色贴纸，把绯红挤掉。比例 0.52：五个色相还在，
            // 但饱和度退到只能近看才分辨的程度，五卡一排时读起来是灰阶里的一点色偏。
            var iconTint = Color.Lerp(def.Color, BattleUiTheme.Bone, .52f);
            var icon = UiIcon.CreateTowerIcon(panel.transform, type, 42f, iconTint);
            PinTopRight(icon, 12f, 12f, 42f, 42f);

            card.Cost = Text(panel.transform, "Cost", def.Cost.ToString(), 38, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 14f, 4f, 72f, 56f);
            card.Cost.SetCharacterSpacing(-1f);
            // 这张标签原来是灰金。可它五张卡各有一个 —— 一屏六处金，
            // 「金 = 灵力」这条唯一的语义立刻被稀释成背景色。
            // 现在金只剩出阵区右上角那一个「灵力 SPIRIT」，这里退成和其它小米字一样的灰。
            Micro(panel.transform, "CostLabel", "灵力", BattleUiTheme.Ash, 15f, 60f, 60f, 16f);

            var name = Text(panel.transform, "Name", def.DisplayName, 19, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 14f, 82f, 118f, 30f);
            name.FitInBox(15, true);

            card.State = Text(panel.transform, "State", "选择", 11, BattleUiTheme.Ash, TextAnchor.MiddleLeft, 14f, 114f, 96f, 16f);
            // 快捷键右移回最贴边会被右下斜切咬掉一角，收进 10px 保平安。
            Micro(panel.transform, "Key", (index + 1).ToString("00"), BattleUiTheme.Ash, 110f, 114f, 14f, 16f)
                .Also(t => t.SetAlignment(TextAnchor.MiddleRight));

            // 这里原本还有两个绯色零件：卡顶一条 42×2 的「状态点」，卡底一条 132×3 的「选中轨」。
            // 两者都已删除。理由是同一条：「出得起 / 选中」这两个状态，卡面本身已经说了三遍 ——
            // 消耗数字的明度、卡面的明度档位、以及底下那行「选择 / 灵力差 30 / 待出阵」。
            // 再叠横条，玩家读到的是四个不同步的信号，而不是一个更清楚的信号。
            // 东方原作的做法相反：状态只写在文字里，颜色永远只占极小面积。

            // 辉光层：这张卡是**平的**，它凭什么被选中的唯一表达就是这圈光。
            // 呼吸速度压到 1.6 —— 五张卡同时呼吸会像在报警，慢到接近静止才像「有生命」。
            card.Halo = UiHalo.Attach(panel, BattleUiTheme.GlowScarlet, 22f).Breathe(1.6f, .40f);

            card.Button = panel.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = panel;
            card.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            card.Button.colors = AllWhiteColors();
            UiMotion.Attach(card.Button, BattleUiTheme.Scarlet);
            card.Button.onClick.AddListener(() =>
            {
                if (_game.State != GameState.Running || _game.IsPaused || _game.TowerPlacer == null) return;
                _game.TowerPlacer.ClearSelection();
                _game.TowerPlacer.SelectTower(type);
            });

            card.RestPosition = panel.rectTransform.anchoredPosition;
            // 卡片从下方滑上来落位。这里只给一个起点和一个开始时间 ——
            // 真正的插值交给 AnimateHand 那条本来就在跑的缓动，节奏天然和「选中上浮」一致，
            // 不必再挂一个组件，两套动画也不会互相抢 anchoredPosition。
            panel.rectTransform.anchoredPosition = card.RestPosition + Vector2.down * 30f;
            card.EnterAt = Time.unscaledTime + .18f + index * .055f;
            return card;
        }

        private static ColorBlock AllWhiteColors()
        {
            // 卡片的状态变化由 ApplyCardVisualState 直接改面板本体，不走 ColorBlock 染色 ——
            // 染色是乘算，会把烘进顶点的渐变一起压平，状态区分反而更糊。
            return new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = Color.white,
                pressedColor = Color.white,
                selectedColor = Color.white,
                disabledColor = Color.white,
                colorMultiplier = 1f,
                fadeDuration = 0f,
            };
        }

        /// <summary>敌方情报。冷青是「数据」的颜色，与绯红的「危险」分工明确。</summary>
        private void BuildEnemyIntel()
        {
            var intel = Surface(transform, "EnemyIntel", UiSurfaceKind.Panel);
            _enemyIntel = intel.gameObject;
            PinTopRight(intel.rectTransform, BattleUiTheme.Safe, 132f, 248f, 132f);
            // 情报板必须让点：它悬浮在战场上方，如果吃掉射线，PickEnemy 会在
            // 「悬停 → 显示情报 → 情报挡住指针 → 判定无敌人 → 隐藏情报」之间每秒闪好几次。
            intel.raycastTarget = false;

            // 面板左缘那条 3×132 的青色竖条已删。情报板是「悬停才出现」的浮层，
            // 它出现在哪本身就是信号；再补一根色条，等于把「这是青色系统」说了两遍。
            Micro(intel.transform, "Label", "敌方情报 CONTACT", BattleUiTheme.Cyan, 18f, 12f, 214f, 14f);
            _enemyIntelTitle = Text(intel.transform, "Title", "", 21, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 30f, 214f, 32f);
            _enemyIntelHealth = Text(intel.transform, "Health", "", 15, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 18f, 62f, 214f, 22f);
            _enemyIntelStats = Text(intel.transform, "Stats", "", 13, BattleUiTheme.Ash, TextAnchor.UpperLeft, 18f, 88f, 214f, 36f);
            _enemyIntel.SetActive(false);
        }

        /// <summary>中央播报。整块用事件色染色：绯=危险、绯=就绪、骨白=中性。</summary>
        private void BuildAlert()
        {
            _alertPanel = Surface(transform, "AlertDirector", UiSurfaceKind.Panel);
            _alert = _alertPanel.gameObject;
            PinTopCenter(_alertPanel.rectTransform, 0f, 126f, 480f, 68f);
            _alertPanel.raycastTarget = false; // 纯播报，一秒半后自己消失，不该吃掉任何点击

            // 左缘警戒条：45° 斜纹贴图（方舟 Boss 预警带的语汇）。只在 Boss 这种
            // 「非日常」事件亮出 —— 常规波次不配条纹，否则它就退化成一道普通装饰边。
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
            // 左上 / 右下 14px 斜切：方舟暂停菜单的不对称切口。和右下角的 PAUSED 水印
            // 一起把这块面板从「居中弹窗」的惯性里拽出来。
            menu.Corners(UiCorner.Diagonal, 14f);
            PinLeft(menu.rectTransform, 150f, 0f, 330f, 392f);
            // 面板四角的 L 形取景标已删。它们是「取景框」的语汇，出现在一个暂停菜单上
            // 没有任何要取景的东西可指 —— 纯粹的图形噪音。

            // 水印排在菜单上方，所以先建它，保证「PAUSED」永远压在面板下层。
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

        /// <summary>
        /// 结算。评级字母是全场最大的一个字形 —— 这正是「超大数字」这条原则的终点：
        /// 一局打完，玩家只想先看到那个字母。
        /// </summary>
        private void BuildResult()
        {
            _result = new GameObject("ResultScreen", typeof(RectTransform)).gameObject;
            _result.transform.SetParent(transform, false);
            UiFactory.Stretch((RectTransform)_result.transform);

            var background = Surface(_result.transform, "Background", UiSurfaceKind.Veil);
            UiFactory.Stretch(background.rectTransform);
            // 结算屏底下已经没有任何要「透出来」的东西 —— HUD 会整组撤下（见 RefreshScreens），
            // 战场也打完了。遮罩提到 96%，只留一线暗影表示底下还有个世界。
            background.Flat(BattleUiTheme.WithAlpha(BattleUiTheme.Ink, .96f))
                      .Border(Color.clear, 0f);

            var reportLabel = Micro(_result.transform, "ReportLabel", "RESULT / 作战结果", BattleUiTheme.Scarlet, 120f, 68f, 700f, 16f);
            reportLabel.SetCharacterSpacing(2.4f);

            var divider = Image(_result.transform, "ReportDivider", BattleUiTheme.Grid);
            divider.raycastTarget = false;
            PinTopLeft(divider.rectTransform, 120f, 110f, 900f, 1f);

            _resultTitle = Text(_result.transform, "Title", "异变解决", 34, BattleUiTheme.Bone, TextAnchor.MiddleLeft, 0f, 0f, 1f, 1f);
            PinLeft(_resultTitle.rectTransform, 120f, 160f, 500f, 48f);

            // 结果色带：一根 4×220 的竖线立在评级字母左侧。颜色随结果切换 ——
            // 胜利 = 骨白（档案），败北 = 绯（满身疮痍）。它是这块屏幕上唯一的「色带」，
            // 平时完全不出现，出现时就是结果的温度。
            _resultBand = Image(_result.transform, "ResultBand", BattleUiTheme.Bone);
            PinLeft(_resultBand.rectTransform, 146f, -20f, 4f, 220f);
            _resultBand.raycastTarget = false;

            // 新纪录：一小行绯字挂在标题下面。它只在真的破纪录那局出现，平时完全不占版面。
            _resultRecord = Micro(_result.transform, "Record", "新纪录  NEW RECORD", BattleUiTheme.Scarlet, 0f, 0f, 320f, 18f);
            PinLeft(_resultRecord.rectTransform, 122f, 214f, 320f, 18f);
            _resultRecord.gameObject.SetActive(false);

            _resultGrade = Text(_result.transform, "Grade", "S", BattleUiTheme.Type.Display, BattleUiTheme.Bone, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
            PinLeft(_resultGrade.rectTransform, 160f, -20f, 250f, 250f);

            // 评级字母后面原来挂着一圈 250px 的绯色封印环（4 段弧，断口落在对角线上）。
            // 删掉了。它不编码任何状态 —— 结算只有一个字母，字母本身已经把结果说完了 ——
            // 它存在的理由只是「圆环看起来神秘」。而它正套在全场最大的那个字形外面，
            // 和它抢同一条视线。玩家看到的是「一个被圈起来的字母」，不是「一个字母」。
            //
            // 96pt 的字号本身就是这块屏幕上最强的信号，它不需要画框。

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
