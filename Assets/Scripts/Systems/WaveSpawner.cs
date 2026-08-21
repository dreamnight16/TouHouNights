using System.Collections;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 波次刷怪器：一个波次含多个刷怪组（SpawnGroup），可同时从多个红门出怪。
    /// 用「剩余刷怪数 + 活跃协程数」双计数判定波次是否刷完。
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        private int _remainingSpawns;
        private int _activeSpawners;

        public bool IsWaveComplete => _remainingSpawns <= 0 && _activeSpawners == 0;

        public void StartWave(int waveIndex)
        {
            Reset();

            var wave = GameConfig.Waves[waveIndex];
            float healthScale = 1f + waveIndex * 0.18f;

            foreach (var group in wave.Groups)
            {
                _remainingSpawns += group.Count;
                _activeSpawners++;
                StartCoroutine(SpawnGroup(group, healthScale));
            }
        }

        public void Reset()
        {
            StopAllCoroutines();
            _remainingSpawns = 0;
            _activeSpawners = 0;
        }

        private IEnumerator SpawnGroup(SpawnGroup group, float healthScale)
        {
            var definition = GameConfig.Enemies[group.EnemyType];
            var route = GameManager.Instance.Map.GetRouteWorld(group.RouteIndex);
            var spawnPos = GameManager.Instance.Map.GetSpawnPosition(group.RouteIndex);

            for (int i = 0; i < group.Count; i++)
            {
                Enemy.Spawn(definition, healthScale, route, spawnPos);
                _remainingSpawns--;
                yield return new WaitForSeconds(group.SpawnInterval);
            }

            _activeSpawners--;
        }
    }
}
