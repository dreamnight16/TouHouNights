using System.Collections;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 波次刷怪器：按波次定义在路径起点生成敌人，血量随波次线性成长。
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        private Coroutine _waveCoroutine;
        private int _remainingSpawns;

        public bool IsWaveComplete => _remainingSpawns <= 0 && _waveCoroutine == null;

        public void StartWave(int waveIndex)
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
            }

            var wave = GameConfig.Waves[waveIndex];
            _remainingSpawns = wave.Count;
            float healthScale = 1f + waveIndex * 0.18f;
            _waveCoroutine = StartCoroutine(SpawnRoutine(wave, healthScale));
        }

        public void Reset()
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
                _waveCoroutine = null;
            }
            _remainingSpawns = 0;
        }

        private IEnumerator SpawnRoutine(WaveDefinition wave, float healthScale)
        {
            var definition = GameConfig.Enemies[wave.EnemyType];
            var trajectory = GameManager.Instance.Map.GetTrajectory(wave.EnemyType);
            var spawnPos = GameManager.Instance.Map.StartPosition;

            for (int i = 0; i < wave.Count; i++)
            {
                Enemy.Spawn(definition, healthScale, trajectory, spawnPos);
                _remainingSpawns--;
                yield return new WaitForSeconds(wave.SpawnInterval);
            }

            _waveCoroutine = null;
        }
    }
}
