using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>特效创建入口。</summary>
    public static class EffectFactory
    {
        public static void SpawnBurst(Vector2 position, Color color, float radius, float duration)
        {
            var go = new GameObject("Burst");
            go.transform.position = position;
            var burst = go.AddComponent<BurstEffect>();
            burst.Init(color, radius, duration);
        }
    }
}
