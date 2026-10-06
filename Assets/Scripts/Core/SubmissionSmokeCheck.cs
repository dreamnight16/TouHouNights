#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Data;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    // 仅由发布包的命令行自检参数启用，不参与正常游戏。
    public sealed class SubmissionSmokeCheck : MonoBehaviour
    {
        private int _phase;
        private float _nextCheck;
        private float _deadline;
        private bool _finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--submission-smoke-test") >= 0)
                new GameObject("SubmissionSmokeCheck").AddComponent<SubmissionSmokeCheck>();
        }

        private void Awake()
        {
            _nextCheck = Time.realtimeSinceStartup + 3f;
            _deadline = Time.realtimeSinceStartup + 30f;
            Application.logMessageReceived += OnLog;
        }

        private void Update()
        {
            if (_finished || Time.realtimeSinceStartup < _nextCheck) return;
            try
            {
                Check(Time.realtimeSinceStartup < _deadline, "Player verification timed out");
                var game = GameManager.Instance;
                Check(game != null, "Game did not bootstrap");
                var ui = FindAnyObjectByType<BattleUiRoot>(FindObjectsInactive.Include);
                switch (_phase)
                {
                    case 0:
                        Check(game.State == GameState.Menu, "Missing title state");
                        Check(FindAnyObjectByType<FrontEndUi>().CurrentPage == "Home", "Missing title page");
                        Check(UiFont.TmpReady, "Bundled Chinese font did not initialize");
                        for (int stage = 1; stage <= 5; stage++)
                        {
                            var clip = Resources.Load<AudioClip>("Audio/stage" + stage);
                            Check(clip != null && clip.length > 30f, "Invalid music asset " + stage);
                        }
                        game.BeginRun();
                        break;
                    case 1:
                        Check(ui != null, "Missing battle interface");
                        ui.transform.Find("DeploymentHand/Card_Sniper").GetComponent<Button>().onClick.Invoke();
                        Check(game.TowerPlacer.SelectedType == TowerType.Sniper, "Deployment selection failed");
                        Check(game.TowerPlacer.TryDeploy(new Vector2Int(-6, -2), TowerType.Sniper), "Deployment failed");
                        game.TogglePause();
                        break;
                    case 2:
                        Check(game.IsPaused && ui.transform.Find("PauseScreen").gameObject.activeSelf, "Pause failed");
                        Check(!ui.transform.Find("DeploymentHand/Card_Sniper").GetComponent<Button>().interactable,
                            "Deployment remained enabled while paused");
                        game.Restart();
                        break;
                    case 3:
                        Check(!game.IsPaused && game.TowerCount == 0 && game.CompletedStages == 0, "Restart retained state");
                        game.ReturnToMenu();
                        break;
                    default:
                        Check(game.State == GameState.Menu && !game.WorldRoot.gameObject.activeSelf,
                            "Returning to title left battle active");
                        Finish(0);
                        return;
                }
                _phase++;
                _nextCheck = Time.realtimeSinceStartup + .5f;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (!_finished && (type == LogType.Error || type == LogType.Exception)) Finish(1);
        }

        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }

        private void Finish(int exitCode)
        {
            if (_finished) return;
            _finished = true;
            Application.logMessageReceived -= OnLog;
            if (exitCode == 0) Debug.Log("SUBMISSION_PLAYER_SMOKE PASS: title, Chinese font, music, deploy, pause, restart, return");
            else Debug.LogError("SUBMISSION_PLAYER_SMOKE FAIL");
            Application.Quit(exitCode);
        }
    }
}
#endif
