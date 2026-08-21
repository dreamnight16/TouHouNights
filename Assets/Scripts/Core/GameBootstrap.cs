using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// 工程自启动入口：无需在场景里手动放置任何物体，
    /// 打开任意（空）场景后按下 Play 即会自动创建 GameManager 并搭建整个游戏。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }
    }
}
