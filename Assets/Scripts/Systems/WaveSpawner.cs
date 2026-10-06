using System;
using System.Collections;
using UnityEngine;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 按 GameConfig.Rounds 推进幕次。
    /// 每幕刷完且场上敌人清空后发放补给，休整后开始下一幕。
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        /// <summary>幕次横幅开始时触发，参数为轮次索引、中文标题和英文卷标。</summary>
        public event Action<int, string, string> OnRoundStart;

        /// <summary>清场完成时触发，参数为轮次索引。</summary>
        public event Action<int> OnRoundClear;

        private int _remainingSpawns;
        private int _currentRoundTotal;
        private int _currentRoundIndex = -1;
        private bool _finished;
        private float _startedAt;
        private bool _cleared;
        public string PhaseText
        {
            get
            {
                if (_currentRoundIndex < 0) return "准备";
                if (_cleared) return "幕间休息";
                float elapsed = Time.time - _startedAt;
                if (elapsed < 0) return "即将开始";
                float next = float.PositiveInfinity;
                foreach (var entry in GameConfig.Rounds[_currentRoundIndex].Entries)
                    if (entry.StartTime > elapsed) next = Mathf.Min(next, entry.StartTime - elapsed);
                if (GameManager.Instance.EnemyCount == 0 && !float.IsPositiveInfinity(next))
                    return "下一波 " + Mathf.CeilToInt(next) + "秒";
                return elapsed < 50 ? "道中" : "后半";
            }
        }

        /// <summary>是否已完成所有轮次（刷完 + 清场）。</summary>
        public bool IsOperationComplete => _finished;

        /// <summary>当前波次序号（0 起始；-1 = 尚未开始）。</summary>
        public int CurrentRoundIndex => _currentRoundIndex;

        /// <summary>当前波次已刷出敌人数量。</summary>
        public int CurrentRoundSpawned => _currentRoundTotal - _remainingSpawns;

        /// <summary>当前波次敌人总数。</summary>
        public int CurrentRoundTotal => _currentRoundTotal;

        public void StartOperation(int firstStage = 0, bool practice = false)
        {
            Reset();
            StartCoroutine(RunRounds(firstStage, practice));
        }

        public void Reset()
        {
            StopAllCoroutines();
            _remainingSpawns = 0;
            _currentRoundTotal = 0;
            _currentRoundIndex = -1;
            _finished = false;
            _cleared = false;
        }

        private IEnumerator RunRounds(int firstStage, bool practice)
        {
            var rounds = GameConfig.Rounds;
            for (int i = firstStage; i < (practice ? firstStage + 1 : rounds.Length); i++)
            {
                var round = rounds[i];
                _remainingSpawns = 0;
                _currentRoundTotal = 0;
                _currentRoundIndex = i;
                _cleared = false;
                _startedAt = Time.time + GameConfig.RoundIntroDelay;
                foreach (var entry in round.Entries)
                {
                    _remainingSpawns += entry.Count;
                    _currentRoundTotal += entry.Count;
                }

                OnRoundStart?.Invoke(i, round.Title, round.Eng);
                yield return new WaitForSeconds(GameConfig.RoundIntroDelay); // 横幅停留，期间仍可放塔

                foreach (var entry in round.Entries)
                {
                    StartCoroutine(SpawnEntryRoutine(entry, i));
                }

                yield return new WaitUntil(() => _remainingSpawns <= 0 && GameManager.Instance.EnemyCount == 0);
                _cleared = true;
                GameManager.Instance.GrantRoundSupply();
                OnRoundClear?.Invoke(i);
                yield return new WaitForSeconds(GameConfig.RoundGap); // 清场休整
            }

            _finished = true;
        }

        private IEnumerator SpawnEntryRoutine(SpawnEntry entry, int roundIndex)
        {
            // 刷怪时间相对本轮横幅结束。
            yield return new WaitForSeconds(entry.StartTime);

            var definition = GameConfig.Enemies[entry.EnemyType];
            var route = GameManager.Instance.Map.GetRouteWorld(entry.RouteIndex);
            var spawnPos = GameManager.Instance.Map.GetSpawnPosition(entry.RouteIndex);
            float healthScale = GameConfig.StageHealthScale(roundIndex);
            float speedScale = GameConfig.StageSpeedScale(roundIndex);
            float damageScale = GameConfig.StageDamageScale(roundIndex);

            for (int i = 0; i < entry.Count; i++)
            {
                Enemy.Spawn(definition, healthScale, route, spawnPos, speedScale, damageScale);
                _remainingSpawns--;
                yield return new WaitForSeconds(entry.SpawnInterval);
            }
        }
    }
}
