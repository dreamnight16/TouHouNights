using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.UI;

[InitializeOnLoad]
public static class BattleUiPlayVerification
{
    private static double started;
    private static int phase;
    static BattleUiPlayVerification()
    {
        if (SessionState.GetBool("BattleUiVerify", false))
        {
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
    }
    public static void Run()
    {
        BattleUiVerification.Run();
        SessionState.SetBool("BattleUiVerify", true);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup - started < 3) return;
        try
        {
            var game = GameManager.Instance;
            if (game == null) throw new Exception("Game did not bootstrap");
            if (phase == 0 && game.State == GameState.Menu)
            {
                var menu = UnityEngine.Object.FindFirstObjectByType<FrontEndUi>();
                Capture(menu, "title-current.png");
                Capture(menu, "title-portrait.png", 720, 1280);
                game.BeginRun();
                started = EditorApplication.timeSinceStartup;
                return;
            }
            var ui = UnityEngine.Object.FindFirstObjectByType<BattleUiRoot>();
            if (ui == null) throw new Exception("Missing battle UI");
            if (phase == 0)
            {
                if (game.State == GameState.Menu) { game.BeginRun(); started = EditorApplication.timeSinceStartup; return; }
                if (!UiFont.TmpReady) throw new Exception("Bundled TMP font failed to load");
                var card = ui.transform.Find("DeploymentHand/Card_Sniper").GetComponent<UnityEngine.UI.Button>();
                card.onClick.Invoke();
                if (game.TowerPlacer.SelectedType != TowerDefense.Data.TowerType.Sniper) throw new Exception("Deployment card selection failed");
                Capture(ui, "battle-ui.png");
                Capture(ui, "battle-4x3.png", 1024, 768);
                Capture(ui, "battle-ultrawide.png", 1920, 810);
                Capture(ui, "battle-portrait.png", 720, 1280);
                game.TogglePause();
                phase = 1;
                started = EditorApplication.timeSinceStartup;
                return;
            }
            if (phase == 1)
            {
                if (!game.IsPaused || !ui.transform.Find("PauseScreen").gameObject.activeSelf) throw new Exception("Pause overlay failed");
                if (ui.transform.Find("DeploymentHand/Card_Sniper").GetComponent<UnityEngine.UI.Button>().interactable) throw new Exception("Deployment remains enabled while paused");
                Capture(ui, "battle-ui-pause.png");
                game.Restart();
                phase = 2;
                started = EditorApplication.timeSinceStartup;
                return;
            }
            if (game.IsPaused || ui.transform.Find("PauseScreen").gameObject.activeSelf) throw new Exception("Pause retained after restart");
            if (phase == 2)
            {
                if (game.TowerCount != 0) throw new Exception("Deployments retained after restart");
                // 部署一座塔再选中它：左下角的固定档案面板（UnitDossier）只在选中时出现，
                // 它的排版只有真实截图能验收。走 TryDeploy —— 和玩家点击同一条 Public 路径。
                if (!game.TowerPlacer.TryDeploy(new Vector2Int(-6, -2), TowerDefense.Data.TowerType.Sniper))
                    throw new Exception("Deploy for dossier failed");
                started = EditorApplication.timeSinceStartup;
                phase = 3;
                return;
            }
            if (phase == 3)
            {
                if (EditorApplication.timeSinceStartup - started < 0.4) return;
                var tower = UnityEngine.Object.FindFirstObjectByType<TowerDefense.Actors.Tower>();
                if (tower == null) throw new Exception("Deployed tower missing");
                var placer = game.TowerPlacer;
                typeof(TowerDefense.Systems.TowerPlacer).GetProperty("SelectedTower").SetValue(placer, tower, null);
                started = EditorApplication.timeSinceStartup;
                phase = 4;
                return;
            }
            if (EditorApplication.timeSinceStartup - started < 0.35) return;
            if (ui.transform.Find("UnitDossier") == null || !ui.transform.Find("UnitDossier").gameObject.activeSelf)
                throw new Exception("Dossier did not open for selected tower");
            Capture(ui, "battle-dossier.png");
            var music = game.GetComponent<TowerDefense.Effects.BattleMusic>();
            if (music == null) throw new Exception("Missing music controller");
            var source = music.GetComponent<AudioSource>();
            if (source == null || source.clip == null || !source.isPlaying || !source.loop)
                throw new Exception("Stage music is not looping");
            bool muted = source.mute;
            TowerDefense.Effects.BattleMusic.Toggle();
            if (source.mute == muted) throw new Exception("Music mute failed");
            TowerDefense.Effects.BattleMusic.Toggle();
            Debug.Log("TOUHOU_AUDIO_PLAY PASS: looping playback and mute toggle");
            Debug.Log("BATTLE_UI_PLAY PASS: bootstrap, pause overlay, restart, dossier; screenshots captured");
            Finish(0);
        }
        catch (Exception ex) { Debug.LogException(ex); Finish(1); }
    }
    public static void Capture(Component ui, string name, int width = 1280, int height = 720)
    {
        var camera = Camera.main;
        var canvas = ui.GetComponent<Canvas>();
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var oldTarget = camera.targetTexture;
        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        var oldScale = canvas.scaleFactor;
        RenderTexture worldTarget = null;
        Rect worldViewport = camera.rect;
        var uiCameraObject = new GameObject("UI Capture Camera");
        var uiCamera = uiCameraObject.AddComponent<Camera>();
        var children = ui.GetComponentsInChildren<Transform>(true);
        var layers = new int[children.Length];
        try
        {
            for (int i = 0; i < children.Length; i++)
            {
                layers[i] = children[i].gameObject.layer;
                children[i].gameObject.layer = 5;
            }
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << 5;
            uiCamera.orthographic = true;
            uiCamera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            var reference = canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution;
            canvas.scaleFactor = Mathf.Min(width / reference.x, height / reference.y);
            Canvas.ForceUpdateCanvases();
            if (ui is BattleUiRoot battle) battle.RefreshLayout();
            if (ui is FrontEndUi menu) menu.RefreshLayout();
            worldViewport = camera.rect;
            worldTarget = new RenderTexture(width, Mathf.Max(1, Mathf.RoundToInt(height * worldViewport.height)), 24);
            camera.targetTexture = worldTarget;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.Render();
            camera.rect = worldViewport;
            RenderTexture.active = target;
            GL.Clear(true, true, BattleUiTheme.Ink);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, width, height, 0);
            Graphics.DrawTexture(new Rect(0, (1 - worldViewport.yMax) * height, width, worldViewport.height * height), worldTarget);
            GL.PopMatrix();
            uiCamera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(".reports", name), pixels.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.scaleFactor = oldScale;
            for (int i = 0; i < children.Length; i++) children[i].gameObject.layer = layers[i];
            camera.targetTexture = oldTarget;
            camera.rect = worldViewport;
            if (worldTarget != null) { worldTarget.Release(); UnityEngine.Object.DestroyImmediate(worldTarget); }
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(pixels);
            target.Release();
            uiCamera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(uiCameraObject);
        }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("BattleUiVerify", false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
