using System.Text;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>
    /// 结界终端 HUD —— 装配器（Composition Root）。
    ///
    /// 本类只做三件事：预热字形、把组件装到画布上、转发全局输入。每个组件各司其职，互不干涉：
    ///   HudTopBar          战局状态层：这一局现在怎么样（波次 / 灵力 / 结界 / 流速）
    ///   HudDeployBar       快速操作层：我现在能部署什么
    ///   HudUnitCard        按需信息：我选中的那张符卡是谁
    ///   HudEnemyInspector  按需信息：我盯着的那个敌影是谁（默认只有血条）
    ///   HudFeedback        纯反馈层：现在发生了什么（横幅 / 警示 / 连击 / 静止）
    ///   HudRangeIndicator  战场空间层：攻击范围 = 干净的几何边界
    /// 信息不再平铺在一屏：默认只给决策必需项，其余随玩家意图按需出现。
    /// </summary>
    public sealed class TerminalHud : MonoBehaviour
    {
        /// <summary>
        /// 界面用得到的全部汉字。开局一次性灌进 TMP 图集：
        /// 动态图集虽然会按需补字，但补字的那一帧会重建纹理 —— 战斗中突然卡顿很难受，
        /// 更重要的是万一某条路径漏了补字，玩家看到的就是方框。预载一次，两件事都解决。
        /// </summary>
        private const string UiVocabulary =
            "结界展开结界灵力敌影符卡布防静止流速弹幕重开取消索敌最近残血突进单体群攻医疗辅助" +
            "速鸦羽闪贯星爆红莲冰霜华曦甘霖撤退返还击毁治疗秒射程格减速持续伤害溅射耐久" +
            "首领疾行重甲常驻速度赏金破界余程无状态正常霜缓中移下降者袭来击波次清除守成碎幻" +
            "想之壁裂零漏怪无伤全员再接厉得分剩余最佳连战报新纪录" +
            "SPELLRAVENPIERCETHEFROSTJADE";

        private TerminalResult _resultScreen;
        private bool _resultShown;

        private void Awake()
        {
            PreloadGlyphs();
            BuildBackdrop();

            // 兄弟节点顺序 = 绘制顺序：反馈层在 HUD 之上（警示要盖住读数），结算屏在最上。
            gameObject.AddComponent<HudTopBar>();
            gameObject.AddComponent<HudDeployBar>();
            gameObject.AddComponent<HudUnitCard>();
            gameObject.AddComponent<HudEnemyInspector>();
            gameObject.AddComponent<HudFeedback>();

            FloatingTextView.Init(transform);

            _resultScreen = gameObject.AddComponent<TerminalResult>();
            _resultScreen.Build(transform);
        }

        private static void PreloadGlyphs()
        {
            var sb = new StringBuilder(UiVocabulary);

            sb.Append("BARRIERONLINETHEWORLDISHELDSTAGECLEARAPPROACHINGDOWNREADYFIREDREPORT");


            foreach (var pair in GameConfig.Towers) sb.Append(pair.Value.DisplayName);
            foreach (var pair in GameConfig.Enemies) sb.Append(pair.Value.DisplayName);
            foreach (var round in GameConfig.Rounds) sb.Append(round.Title).Append(round.Eng);

            UiFont.Preload(sb.ToString());
        }

        /// <summary>全屏背景景深：四角暗角 + 顶部冷光压暗，让 HUD 浮在战场之上（告别「贴在平面上的教学图」）。</summary>
        private void BuildBackdrop()
        {
            // 四角暗化：画面四角沉下去，注意力收到中央战场与上下两条 HUD 带。
            var vig = UiFactory.CreateImage(transform, "BackdropVignette", Color.black);
            vig.sprite = SpriteFactory.Vignette(TdTheme.Void);
            vig.raycastTarget = false;
            vig.color = new Color(1f, 1f, 1f, 0.5f);
            UiFactory.Stretch(vig.rectTransform);

            // 顶部冷光渐变：上缘压一层深蓝，顶栏从画面里浮出来。
            var top = UiFactory.CreateImage(transform, "BackdropTop", TdTheme.Void);
            top.sprite = SpriteFactory.GradientTop(TdTheme.Void);
            top.raycastTarget = false;
            top.color = new Color(1f, 1f, 1f, 0.65f);
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.anchoredPosition = Vector2.zero;
            top.rectTransform.sizeDelta = new Vector2(0f, 150f);
        }

        private void Update()
        {
            UiFactory.RefreshBlurTexture();

            var gm = GameManager.Instance;
            if (gm == null) return;

            HandleHotkeys(gm);
            RefreshResult(gm);
        }

        /// <summary>全局快捷键：只做转发，不碰任何游戏状态判断（那是 GameManager 的职责）。</summary>
        private static void HandleHotkeys(GameManager gm)
        {
            if (gm.State != GameState.Running) return;

            for (int i = 0; i < TdKit.SpellOrder.Length; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    gm.TowerPlacer?.SelectTower(TdKit.SpellOrder[i]);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space)) gm.TogglePause();
            if (Input.GetKeyDown(KeyCode.X)) gm.CycleSpeed();
            if (Input.GetKeyDown(KeyCode.E)) gm.TriggerBarrage();
            if (Input.GetKeyDown(KeyCode.R)) gm.Restart();
            if (Input.GetKeyDown(KeyCode.Escape)) gm.TowerPlacer?.ClearSelection();
        }

        private void RefreshResult(GameManager gm)
        {
            if (gm.State == GameState.Running)
            {
                if (_resultShown)
                {
                    _resultShown = false;
                    _resultScreen.Hide();
                }
                return;
            }

            if (!_resultShown && (gm.State == GameState.GameOver || gm.State == GameState.Victory))
            {
                _resultShown = true;
                _resultScreen.Show(gm.GetResult());
            }
        }
    }
}
