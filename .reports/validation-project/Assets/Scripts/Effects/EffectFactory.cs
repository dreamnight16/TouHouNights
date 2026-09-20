using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Effects
{
    /// <summary>特效创建入口。</summary>
    public static class EffectFactory
    {
        public static void SpawnBurst(Vector2 position, Color color, float radius, float duration)
        {
            var go = new GameObject("Burst");
            go.transform.position = position;

            var worldRoot = GameManager.Instance != null ? GameManager.Instance.WorldRoot : null;
            if (worldRoot != null)
            {
                go.transform.SetParent(worldRoot, true); // 挂到 WorldRoot，重开时一并销毁，不留孤儿
            }

            var burst = go.AddComponent<BurstEffect>();
            burst.Init(color, radius, duration);
        }
    }
}
