using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Systems;

public static class DifficultyVerification
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Tools/Tower Defense/Verify Difficulty and Grades")]
    public static void Run()
    {
        var game = GameManager.Instance;
        Check(Application.isPlaying && game != null, "Run difficulty verification in Play Mode after bootstrap");
        bool hadBest = PlayerPrefs.HasKey("td_best_score");
        int oldBest = game.BestScore;
        try
        {
            game.BeginRun();
            for (int stage = 0; stage < 4; stage++) ClearStage(game, stage);
            game.NotifyEnemySpawned(null);
            game.NotifyEnemyRemoved(null);
            Check((int)Field(game, "_spawnedEnemies").GetValue(game) == 1, "Spawn counter did not advance");
            game.NotifyEnemyReachedBase(GameConfig.StartingLives);
            Check(game.State == GameState.GameOver && game.CompletedStages == 4, "Game over lost cleared stages");
            Check(game.WaveSpawner.CurrentRoundIndex == -1, "Game over did not reset spawner");
            Check((int)Field(game, "_spawnedEnemies").GetValue(game) == 1, "Game over lost spawn count");
            game.Restart();
            Check(game.CompletedStages == 0 && (int)Field(game, "_spawnedEnemies").GetValue(game) == 0,
                "Restart retained previous progress");
            ClearStage(game, 0);
            Set(game, "_spawnedEnemies", 10);
            game.BeginRun();
            Check(game.CompletedStages == 0 && (int)Field(game, "_spawnedEnemies").GetValue(game) == 0,
                "BeginRun retained previous progress");

            string[] grades = { "C", "B", "A", "S", "V", "V" };
            for (int cleared = 0; cleared <= 5; cleared++)
            {
                for (int quality = 0; quality <= 1; quality++)
                {
                    var result = Sample(game, cleared, GameState.GameOver, quality * GameConfig.TotalEnemies,
                        0, quality * GameConfig.StartingLives, quality * GameConfig.TotalEnemies);
                    Check(result.CompletedStages == cleared && !result.IsPractice && !result.Victory,
                        "Result snapshot lost campaign progress");
                    Check(result.Grade == grades[cleared], "Failed run grade disagrees with cleared stages: " + cleared);
                    Check(cleared != 4 || (result.Rating >= 90 && result.Rating <= 94), "Four-stage score escaped 90..94");
                    Check(cleared != 5 || (result.Rating >= 95 && result.Rating <= 99), "Five-stage score escaped 95..99");
                }
            }

            var perfect = Sample(game, 5, GameState.Victory, GameConfig.TotalEnemies, 0, GameConfig.StartingLives, 0);
            Check(perfect.Grade == "Φ" && perfect.Rating == 100, "Full campaign without leaks must receive Phi");
            var leaked = Sample(game, 5, GameState.Victory, GameConfig.TotalEnemies - 1, 1, GameConfig.StartingLives - 1, 0);
            Check(leaked.Grade == "V" && leaked.Rating >= 95 && leaked.Rating <= 99, "Leaked full campaign must remain V");
            var incomplete = Sample(game, 5, GameState.Victory, GameConfig.TotalEnemies - 1, 0, GameConfig.StartingLives, 0);
            Check(incomplete.Grade != "Φ" && incomplete.Rating < 100, "Missing enemy kill incorrectly received Phi");

            game.BeginRun(4, true);
            ClearStage(game, 4);
            Check(game.CompletedStages == 1, "Practice stage index was mistaken for five cleared stages");
            PlayerPrefs.SetInt("td_best_score", 1234);
            Set(game, "_score", 1000000);
            var practice = Sample(game, 1, GameState.Victory, GameConfig.RoundEnemyCount(4), 0, GameConfig.StartingLives, 0);
            Check(practice.IsPractice && practice.CompletedStages == 1 && practice.Grade != "Φ" && practice.Rating < 100,
                "Practice incorrectly received campaign Phi");
            Check(!practice.NewRecord && game.BestScore == 1234, "Practice contaminated campaign record");
        }
        finally
        {
            if (hadBest) PlayerPrefs.SetInt("td_best_score", oldBest);
            else PlayerPrefs.DeleteKey("td_best_score");
            PlayerPrefs.Save();
            game.ReturnToMenu();
        }
        Debug.Log("DIFFICULTY_VERIFY PASS: cleared-stage grades, score bands, Phi, practice isolation and lifecycle resets");
    }

    private static GameResult Sample(GameManager game, int cleared, GameState state, int kills, int leaks, int lives, int combo)
    {
        typeof(GameManager).GetProperty("CompletedStages").SetValue(game, cleared);
        typeof(GameManager).GetProperty("State").SetValue(game, state);
        Set(game, "_spawnedEnemies", game.IsPractice ? GameConfig.RoundEnemyCount(game.StartStage) : GameConfig.TotalEnemies);
        Set(game, "_totalKills", kills);
        Set(game, "_leakedEnemies", leaks);
        Set(game, "_lives", lives);
        Set(game, "_bestCombo", combo);
        return game.GetResult();
    }

    private static void ClearStage(GameManager game, int stage)
    {
        // 触发真实事件订阅，验证 CreateWorld 的计数接线，而非直接调用计数方法。
        var cleared = (Action<int>)typeof(WaveSpawner).GetField("OnRoundClear", PrivateInstance).GetValue(game.WaveSpawner);
        Check(cleared != null, "Stage clear event has no subscribers");
        cleared(stage);
    }

    private static FieldInfo Field(GameManager game, string name) => game.GetType().GetField(name, PrivateInstance);
    private static void Set(GameManager game, string name, object value) => Field(game, name).SetValue(game, value);
    private static void Check(bool passed, string message)
    {
        if (!passed) throw new Exception("DIFFICULTY_VERIFY FAIL: " + message);
    }
}
