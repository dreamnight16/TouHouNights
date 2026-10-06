using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>运行时自动创建 GameManager，无需预先在场景中放置。</summary>
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
