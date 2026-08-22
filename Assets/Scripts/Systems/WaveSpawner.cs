using System.Collections;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 剿灭式刷怪器：按一条连续时间轴（AnnihilationSchedule）从多个红门持续刷怪，
    /// 混合兵种、随时间变难。用「剩余刷怪数 + 活跃协程数」双计数判定是否刷完。
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        private int _remainingSpawns;
        private int _activeSpawners;

        public bool IsOperationComplete => _remainingSpawns <= 0 && _activeSpawners == 0;

        public void StartOperation()
        {
            Reset();

            foreach (var entry in GameConfig.AnnihilationSchedule)
            {
                _remainingSpawns += entry.Count;
                _activeSpawners++;
                StartCoroutine(SpawnEntryRoutine(entry));
            }
        }

        public void Reset()
        {
            StopAllCoroutines();
            _remainingSpawns = 0;
            _activeSpawners = 0;
        }

        private IEnumerator SpawnEntryRoutine(SpawnEntry entry)
        {
            // 先等待条目自身设定的开始时间。
            yield return new WaitForSeconds(entry.StartTime);

            var definition = GameConfig.Enemies[entry.EnemyType];
            var route = GameManager.Instance.Map.GetRouteWorld(entry.RouteIndex);
            var spawnPos = GameManager.Instance.Map.GetSpawnPosition(entry.RouteIndex);
            float healthScale = 1f + entry.StartTime * GameConfig.AnnihilationHealthRamp;

            for (int i = 0; i < entry.Count; i++)
            {
                Enemy.Spawn(definition, healthScale, route, spawnPos);
                _remainingSpawns--;
                yield return new WaitForSeconds(entry.SpawnInterval);
            }

            _activeSpawners--;
        }
    }
}
