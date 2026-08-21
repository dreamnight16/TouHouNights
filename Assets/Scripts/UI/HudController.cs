using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD（IMGUI）：显示金币/生命/波次/状态，提供选塔、开波、重开与索敌策略切换。
    /// 采用 IMGUI 而非 UGUI，避免 EventSystem/Canvas/Text 的工程配置依赖，保证开箱即跑。
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private static readonly TowerType[] TowerOrder =
        {
            TowerType.Gun, TowerType.Sniper, TowerType.Missile, TowerType.Slow
        };

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _titleStyle;

        private void OnGUI()
        {
            EnsureStyles();

            var gm = GameManager.Instance;
            if (gm == null) return;

            DrawStatsPanel(gm);
            DrawTowerButtons(gm);
            DrawFlowButtons(gm);
            DrawOverlay(gm);
        }

        private void DrawStatsPanel(GameManager gm)
        {
            GUI.Box(HudLayout.Panel, string.Empty, _boxStyle);

            float x = HudLayout.Panel.x + 14f;
            float y = HudLayout.Panel.y + 10f;
            float line = 24f;

            GUI.Label(new Rect(x, y, 300f, 28f), "自动塔防 · 追踪导弹", _titleStyle);
            y += line + 4f;
            GUI.Label(new Rect(x, y, 300f, 22f), $"金币：{gm.Gold}", _labelStyle);
            y += line;
            GUI.Label(new Rect(x, y, 300f, 22f), $"生命：{gm.Lives}", _labelStyle);
            y += line;
            GUI.Label(new Rect(x, y, 300f, 22f), $"波次：{Mathf.Min(gm.WaveIndex + 1, gm.TotalWaves)} / {gm.TotalWaves}", _labelStyle);
            y += line;
            GUI.Label(new Rect(x, y, 300f, 22f), $"状态：{StateText(gm.State)}", _labelStyle);
        }

        private void DrawTowerButtons(GameManager gm)
        {
            for (int i = 0; i < TowerOrder.Length; i++)
            {
                var type = TowerOrder[i];
                var def = GameConfig.Towers[type];
                var rect = HudLayout.TowerButton(i);
                bool selected = gm.TowerPlacer.SelectedType == type;

                var style = selected ? _selectedButtonStyle : _buttonStyle;
                if (GUI.Button(rect, $"{def.DisplayName}  ${def.Cost}", style))
                {
                    gm.TowerPlacer.SelectTower(type);
                }
            }
        }

        private void DrawFlowButtons(GameManager gm)
        {
            bool canStart = gm.State == GameState.Building && gm.WaveIndex < gm.TotalWaves;

            GUI.enabled = canStart;
            if (GUI.Button(HudLayout.StartButton, "开始下一波", _buttonStyle))
            {
                gm.StartNextWave();
            }
            GUI.enabled = true;

            if (GUI.Button(HudLayout.RestartButton, "重新开始", _buttonStyle))
            {
                gm.Restart();
            }

            if (GUI.Button(HudLayout.TargetingButton, $"索敌：{TargetingText(gm.TargetingPriority)}（点击切换）", _buttonStyle))
            {
                gm.CycleTargetingPriority();
            }

            GUI.Label(new Rect(12f, 330f, 400f, 24f), "左键点击空地放置所选塔", _labelStyle);
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
                normal = { background = Texture(new Color(0.10f, 0.11f, 0.15f, 0.92f)) },
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
                normal = { textColor = new Color(0.55f, 0.85f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { background = Texture(new Color(0.22f, 0.28f, 0.38f)), textColor = Color.white },
                hover = { background = Texture(new Color(0.28f, 0.36f, 0.48f)), textColor = Color.white },
                active = { background = Texture(new Color(0.16f, 0.20f, 0.28f)), textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _selectedButtonStyle = new GUIStyle(_buttonStyle)
            {
                normal = { background = Texture(new Color(0.20f, 0.62f, 0.35f)), textColor = Color.white }
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
                case GameState.Building: return "布防阶段";
                case GameState.WaveActive: return "战斗进行中";
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
