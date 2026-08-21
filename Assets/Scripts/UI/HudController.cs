using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD（IMGUI，明日方舟式布局）：顶栏显示生命/金币/波次/状态与流程按钮，
    /// 底栏为居中排列的防御塔选择卡片；金币不足的塔卡片自动标灰且不可点。
    /// 采用 IMGUI 而非 UGUI，避免 EventSystem/Canvas/Text 的工程配置依赖。
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private static readonly TowerType[] TowerOrder =
        {
            TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow
        };

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardSelectedStyle;
        private GUIStyle _cardDisabledStyle;
        private GUIStyle _hintStyle;

        private void OnGUI()
        {
            EnsureStyles();

            var gm = GameManager.Instance;
            if (gm == null) return;

            DrawTopBar(gm);
            DrawBottomBar(gm);
            DrawOverlay(gm);
        }

        private void DrawTopBar(GameManager gm)
        {
            GUI.Box(HudLayout.TopBar, string.Empty, _boxStyle);

            GUI.Label(new Rect(14f, 13f, 70f, 24f), $"生命 {gm.Lives}", _labelStyle);
            GUI.Label(new Rect(86f, 13f, 80f, 24f), $"金币 {gm.Gold}", _labelStyle);
            GUI.Label(new Rect(166f, 13f, 100f, 24f), $"波次 {gm.CurrentWave}/{gm.TotalWaves}", _labelStyle);
            GUI.Label(new Rect(266f, 13f, 110f, 24f), $"得分 {gm.Score}", _labelStyle);
            GUI.Label(new Rect(376f, 13f, 120f, 24f), $"P点 {gm.Power:0.00}", _labelStyle);
            GUI.Label(new Rect(496f, 13f, 120f, 24f), StateText(gm.State), _labelStyle);

            if (GUI.Button(HudLayout.SpeedButton, gm.IsDoubleSpeed ? "速度 2x" : "速度 1x", _buttonStyle))
            {
                gm.ToggleSpeed();
            }

            if (GUI.Button(HudLayout.TargetingButton, $"索敌：{TargetingText(gm.TargetingPriority)}", _buttonStyle))
            {
                gm.CycleTargetingPriority();
            }

            bool canBarrage = gm.CanBarrage && gm.State == GameState.Running;
            GUI.enabled = canBarrage;
            string barrageLabel = gm.CanBarrage ? "弹幕射击" : $"弹幕 P {gm.Power:0.0}";
            if (GUI.Button(HudLayout.BarrageButton, barrageLabel, _buttonStyle))
            {
                gm.TriggerBarrage();
            }
            GUI.enabled = true;

            if (GUI.Button(HudLayout.RestartButton, "重新开始", _buttonStyle))
            {
                gm.Restart();
            }
        }

        private void DrawBottomBar(GameManager gm)
        {
            GUI.Box(HudLayout.BottomBar, string.Empty, _boxStyle);

            for (int i = 0; i < TowerOrder.Length; i++)
            {
                var type = TowerOrder[i];
                var def = GameConfig.Towers[type];
                var rect = HudLayout.TowerButton(i, TowerOrder.Length);

                bool affordable = gm.Gold >= def.Cost;
                bool selected = gm.TowerPlacer.SelectedType == type;

                var style = !affordable ? _cardDisabledStyle
                          : selected ? _cardSelectedStyle
                          : _cardStyle;

                string label = $"{def.DisplayName}  ${def.Cost}  射程{def.RangeCells}";
                if (GUI.Button(rect, label, style) && affordable)
                {
                    gm.TowerPlacer.SelectTower(type);
                }
            }

            GUI.Label(HudLayout.HintLabel, "左键点空地放塔 · 点已有塔撤退(返还50%)", _hintStyle);
        }

        private void DrawOverlay(GameManager gm)
        {
            if (gm.State != GameState.GameOver && gm.State != GameState.Victory) return;

            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 90f, 360f, 140f);
            GUI.Box(rect, string.Empty, _boxStyle);

            string title = gm.State == GameState.Victory ? "胜利！所有波次已被击退" : "游戏失败：生命值归零";
            GUI.Label(new Rect(rect.x + 20f, rect.y + 24f, 320f, 40f), title, _titleStyle);

            if (GUI.Button(new Rect(rect.x + 90f, rect.y + 82f, 180f, 40f), "再来一局", _buttonStyle))
            {
                gm.Restart();
            }
        }

        // ---- 样式 ----

        private void EnsureStyles()
        {
            if (_boxStyle != null) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture(new Color(0.10f, 0.11f, 0.15f, 0.94f)) },
                padding = new RectOffset(10, 10, 10, 10)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.94f, 1f) }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.85f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { background = Texture(new Color(0.22f, 0.28f, 0.38f)), textColor = Color.white },
                hover = { background = Texture(new Color(0.28f, 0.36f, 0.48f)), textColor = Color.white },
                active = { background = Texture(new Color(0.16f, 0.20f, 0.28f)), textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _cardStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { background = Texture(new Color(0.20f, 0.26f, 0.35f)), textColor = Color.white },
                hover = { background = Texture(new Color(0.26f, 0.34f, 0.45f)), textColor = Color.white },
                active = { background = Texture(new Color(0.15f, 0.19f, 0.26f)), textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _cardSelectedStyle = new GUIStyle(_cardStyle)
            {
                normal = { background = Texture(new Color(0.20f, 0.60f, 0.34f)), textColor = Color.white }
            };

            _cardDisabledStyle = new GUIStyle(_cardStyle)
            {
                normal = { background = Texture(new Color(0.13f, 0.15f, 0.18f)), textColor = new Color(0.55f, 0.55f, 0.55f) },
                hover = { background = Texture(new Color(0.13f, 0.15f, 0.18f)), textColor = new Color(0.55f, 0.55f, 0.55f) },
                active = { background = Texture(new Color(0.13f, 0.15f, 0.18f)), textColor = new Color(0.55f, 0.55f, 0.55f) }
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.65f, 0.68f, 0.75f) }
            };
        }

        private static Texture2D Texture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static string StateText(GameState state)
        {
            switch (state)
            {
                case GameState.Running: return "进行中";
                case GameState.GameOver: return "游戏失败";
                case GameState.Victory: return "胜利";
                default: return state.ToString();
            }
        }

        private static string TargetingText(TargetingPriority priority)
        {
            switch (priority)
            {
                case TargetingPriority.Nearest: return "距离最近";
                case TargetingPriority.LowestHealth: return "血量最低";
                case TargetingPriority.ClosestToEnd: return "距终点最近";
                default: return priority.ToString();
            }
        }
    }
}
