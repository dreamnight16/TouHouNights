using System;
using UnityEditor;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.UI;

/// <summary>
/// 使用第五关练习模式快速进入结算屏，截图后退出编辑器。
/// </summary>
[InitializeOnLoad]
public static class ResultScreenVerification
{
    /// <summary>与 MenuFlowVerification 一致的参考阵容。</summary>
    private static readonly Vector2Int[] Cells =
    {
        new Vector2Int(-5, 2), new Vector2Int(-4, -2), new Vector2Int(-1, 2), new Vector2Int(2, -2),
        new Vector2Int(2, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(5, 2)
    };
    private static readonly TowerType[] Types =
    {
        TowerType.Sniper, TowerType.Gun, TowerType.Sniper, TowerType.Sniper,
        TowerType.Slow, TowerType.Missile, TowerType.Heal, TowerType.Sniper
    };

    private const double BootDelay = 3.0;
    private const double SettleDelay = 0.9;
    private const double StageBudget = 300.0;

    private static double _started;
    private static double _deadline;
    private static int _phase;

    static ResultScreenVerification()
    {
        if (SessionState.GetBool("ResultScreenVerify", false))
        {
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        SessionState.SetBool("ResultScreenVerify", true);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup - _started < BootDelay) return;
        try
        {
            var game = GameManager.Instance;
            if (game == null) throw new Exception("Game did not bootstrap");

            if (_phase == 0)
            {
                if (game.State != GameState.Menu) return;
                game.BeginRun(4, true);
                _deadline = EditorApplication.timeSinceStartup + StageBudget;
                _phase = 1;
                return;
            }

            var ui = UnityEngine.Object.FindAnyObjectByType<BattleUiRoot>();
            if (ui == null) throw new Exception("Missing battle UI");

            if (_phase == 1)
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new Exception("Stage did not resolve");
                if (game.State == GameState.Running || game.State == GameState.Menu)
                {
                    // 使用与玩家操作相同的公共部署入口。
                    game.SetSpeedIndex(3);
                    for (int i = 0; i < Cells.Length; i++) game.TowerPlacer.TryDeploy(Cells[i], Types[i]);
                    return;
                }
                // 结算时 timeScale 为 0，截图前按编辑器时间等待布局稳定。
                _deadline = EditorApplication.timeSinceStartup + SettleDelay;
                _phase = 2;
                return;
            }

            if (EditorApplication.timeSinceStartup < _deadline) return;

            ui.RefreshLayout();
            BattleUiPlayVerification.Capture(ui, "result.png");
            BattleUiPlayVerification.Capture(ui, "result-portrait.png", 720, 1280);
            Debug.Log($"RESULT_SCREEN PASS state={game.State} grade={game.GetResult().Grade} " +
                      $"kills={game.TotalKills} lives={game.Lives}");
            Finish(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Finish(1);
        }
    }

    private static void Finish(int code)
    {
        SessionState.SetBool("ResultScreenVerify", false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
