using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.UI;
using TowerDefense.Effects;

[InitializeOnLoad]
public static class MenuFlowVerification
{
    static MenuFlowVerification() { EditorApplication.update += Tick; }
    public static void Run()
    {
        SessionState.SetBool("MenuFlowVerify", true);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        if (!SessionState.GetBool("MenuFlowVerify", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || GameManager.Instance == null) return;
        SessionState.SetBool("MenuFlowVerify", false);
        new GameObject("MenuVerificationRunner").AddComponent<MenuVerificationRunner>();
    }
}

public sealed class MenuVerificationRunner : MonoBehaviour
{
    private float _deadline;
    private bool _finishing;
    private int _oldBest;
    private void Awake()
    {
        _deadline = Time.realtimeSinceStartup + 400;
        _oldBest = PlayerPrefs.GetInt("td_best_score", 0);
        Application.logMessageReceived += Log;
    }
    private void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) Finish(1);
    }
    private void Update()
    {
        if (Time.realtimeSinceStartup > _deadline) { Debug.LogError("MENU_VERIFY timeout"); Finish(1); }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Click(Transform root, string path) { root.Find(path).GetComponent<Button>().onClick.Invoke(); }
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3);
        var game = GameManager.Instance;
        var menu = FindFirstObjectByType<FrontEndUi>();
        var music = game.GetComponent<BattleMusic>();
        Check(game.State == GameState.Menu && game.EnemyCount == 0 && Time.timeScale == 0, "Title must not spawn enemies");
        Check(menu.CurrentPage == "Home", "Missing title page");
        BattleUiPlayVerification.Capture(menu, "title.png");
        Click(menu.transform, "Home/MusicBox");
        Click(menu.transform, "MusicBox/Track5");
        yield return new WaitForSecondsRealtime(4);
        Check(music.TrackIndex == 4 && music.IsPlaying && music.Duration > 60, "Music box selection failed");
        Click(menu.transform, "MusicBox/PlayerPlate/Playback");
        Check(!music.IsPlaying, "Music pause failed");
        menu.transform.Find("MusicBox/PlayerPlate/Seek").GetComponent<Slider>().value = .5f;
        Check(Mathf.Abs(music.Position - music.Duration * .5f) < 1, "Music seek failed");
        Click(menu.transform, "MusicBox/PlayerPlate/Playback");
        Check(music.IsPlaying, "Music resume failed");
        BattleUiPlayVerification.Capture(menu, "music-box.png");
        Click(menu.transform, "MusicBox/Back");
        Click(menu.transform, "Home/Settings");
        float oldVolume = GameSettings.MusicVolume;
        var volume = menu.transform.Find("Settings/MusicVolume").GetComponent<Slider>();
        volume.value = .23f;
        Check(Mathf.Abs(GameSettings.MusicVolume - .23f) < .01f, "Music setting not applied");
        bool motion = GameSettings.CameraMotion;
        Click(menu.transform, "Settings/CameraMotion");
        Check(GameSettings.CameraMotion != motion, "Camera setting not applied");
        BattleUiPlayVerification.Capture(menu, "settings.png");
        volume.value = oldVolume;
        Click(menu.transform, "Settings/CameraMotion");
        Click(menu.transform, "Settings/Back");
        Click(menu.transform, "Home/Practice");
        BattleUiPlayVerification.Capture(menu, "practice.png");
        Click(menu.transform, "Practice/Stage5");
        Check(game.IsPractice && game.StartStage == 4 && game.Spirit == 870, "Practice loadout failed");
        yield return new WaitForSecondsRealtime(3);
        Check(music.TrackIndex == 4, "Practice must use selected stage music during preparation");
        var ui = FindFirstObjectByType<BattleUiRoot>();
        game.TogglePause();
        yield return new WaitForSecondsRealtime(.2f);
        Check(ui.transform.Find("PauseScreen").gameObject.activeSelf, "Pause screen missing");
        Check(!game.TowerPlacer.TryDeploy(new Vector2Int(-5, 2), TowerType.Sniper), "Deployment allowed while paused");
        Click(ui.transform, "PauseScreen/Menu/Retry");
        Check(game.IsPractice && game.StartStage == 4 && game.TowerCount == 0 && !game.IsPaused, "Practice restart lost mode");
        yield return null;
        game.TogglePause();
        yield return new WaitForSecondsRealtime(.2f);
        Click(ui.transform, "PauseScreen/Menu/Home");
        Check(game.State == GameState.Menu && !game.WorldRoot.gameObject.activeSelf, "Return to menu leaves battle active");
        Click(menu.transform, "Home/Start");
        Check(!game.IsPractice && game.StartStage == 0 && game.Spirit == 150, "Campaign retained practice state");
        yield return new WaitForSecondsRealtime(1);
        BattleUiPlayVerification.Capture(ui, "menu-battle.png");
        Debug.Log("MENU_FLOW PASS: title, settings, music select/pause/seek, practice, restart, return, campaign reset");
        // Same public deployment checks as mouse input. No bonus gold or damage cheats.
        yield return RunBalance(game, false);
        Check(game.State == GameState.Victory, "Campaign reference formation could not win");
        Debug.Log($"CAMPAIGN_BALANCE PASS kills={game.TotalKills} leaks={game.LeakedEnemies} lives={game.Lives} towers={game.TowerCount} spirit={game.Spirit}");
        // 结算屏此前是唯一没有截图覆盖的一屏，而它刚被改动过（删掉了评级字母后面那圈封印环）。
        // 没有截图，「删掉环之后会不会显得空」就只能靠想象 —— 所以借这一局胜利截一张。
        yield return new WaitForSecondsRealtime(.6f);
        BattleUiPlayVerification.Capture(ui, "result.png");
        game.ReturnToMenu();
        game.BeginRun(4, true);
        yield return RunBalance(game, true);
        Check(game.State == GameState.Victory, "Final practice reference formation could not win");
        Debug.Log($"FINAL_BALANCE PASS kills={game.TotalKills} leaks={game.LeakedEnemies} lives={game.Lives} towers={game.TowerCount} spirit={game.Spirit}");
        int record = game.BestScore;
        game.GetResult();
        Check(record == game.BestScore, "Practice contaminated campaign record");
        Check(game.WaveSpawner.CurrentRoundIndex == 4, "Practice advanced beyond selected stage");
        Debug.Log("MENU_VERIFY ALL PASS");
        Finish(0);
    }
    private IEnumerator RunBalance(GameManager game, bool practice)
    {
        var cells = new[] { new Vector2Int(-5,2), new Vector2Int(-4,-2), new Vector2Int(-1,2), new Vector2Int(2,-2),
            new Vector2Int(2,0), new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(5,2) };
        var types = new[] { TowerType.Sniper, TowerType.Gun, TowerType.Sniper, TowerType.Sniper,
            TowerType.Slow, TowerType.Missile, TowerType.Heal, TowerType.Sniper };
        game.SetSpeedIndex(3);
        int stage = -1;
        float elapsed = 0;
        while (game.State == GameState.Running)
        {
            elapsed += Time.deltaTime;
            Check(elapsed < 1000, "Operation did not finish");
            for (int i = 0; i < cells.Length; i++)
                game.TowerPlacer.TryDeploy(cells[i], types[i]);
            int current = game.WaveSpawner.CurrentRoundIndex;
            if (current != stage)
            {
                stage = current;
                Debug.Log($"BALANCE stage={stage + 1} practice={practice} lives={game.Lives} spirit={game.Spirit} towers={game.TowerCount}");
            }
            yield return null;
        }
    }
    private void Finish(int code)
    {
        if (_finishing) return;
        _finishing = true;
        Application.logMessageReceived -= Log;
        PlayerPrefs.SetInt("td_best_score", _oldBest);
        GameSettings.Save();
        EditorApplication.Exit(code);
    }
}
