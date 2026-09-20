using System.Collections;
using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Effects
{
    /// <summary>战斗与音乐盒共用一个播放器；音乐使用非缩放时间，不随战斗倍速变调。</summary>
    public sealed class BattleMusic : MonoBehaviour
    {
        public static readonly string[] Titles = {
            "眷爱众生之神 ～ Romantic Fall", "众神眷恋的幻想乡",
            "上海红茶馆 ～ Chinese Tea", "女仆与血之怀表", "神圣庄严的古战场 ～ Suwa Foughten Field"
        };
        private AudioSource _source;
        private Coroutine _transition;
        private bool _followBattle;
        private bool _paused;
        private float _fade;
        public int TrackIndex { get; private set; } = -1;
        public bool IsPlaying => _source != null && _source.isPlaying;
        public bool IsLoading { get; private set; }
        public string Error { get; private set; }
        public float Position => _source != null && _source.clip != null ? _source.time : 0;
        public float Duration => _source != null && _source.clip != null ? _source.clip.length : 0;
        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0;
            _source.volume = 0;
            _source.mute = PlayerPrefs.GetInt("td_music_muted", 0) != 0;
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();
        }
        public static void Toggle()
        {
            var music = FindFirstObjectByType<BattleMusic>();
            if (music == null) return;
            music._source.mute = !music._source.mute;
            PlayerPrefs.SetInt("td_music_muted", music._source.mute ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>当前播放器是否静音。没有播放器时按「关」处理，UI 据此标注音乐键。</summary>
        public static bool IsMuted
        {
            get
            {
                var music = FindFirstObjectByType<BattleMusic>();
                return music == null || music._source == null || music._source.mute;
            }
        }
        public void FollowBattle(int firstStage)
        {
            _followBattle = true;
            PlayTrack(firstStage, true);
        }
        public void Preview(int index)
        {
            _followBattle = false;
            // 音乐盒里明确点击播放时恢复静音状态，避免显示播放却听不到。
            _source.mute = false;
            PlayerPrefs.SetInt("td_music_muted", 0);
            PlayTrack(index, false);
        }
        public void MenuMusic()
        {
            _followBattle = false;
            PlayTrack(0, false);
        }
        public void TogglePlayback()
        {
            if (IsLoading || _source.clip == null) return;
            _paused = !_paused;
            if (_paused) _source.Pause(); else _source.UnPause();
        }
        public void Seek(float fraction)
        {
            if (IsLoading || _source.clip == null) return;
            _source.time = Mathf.Clamp(fraction * Duration, 0, Mathf.Max(0, Duration - .1f));
        }
        private void PlayTrack(int index, bool restart)
        {
            index = Mathf.Clamp(index, 0, Titles.Length - 1);
            if (TrackIndex == index && !restart && Error == null)
            {
                if (_paused) { _paused = false; _source.UnPause(); }
                return;
            }
            TrackIndex = index;
            _paused = false;
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(SwitchTrack(index));
        }
        private void Update()
        {
            _source.volume = _fade * GameSettings.MusicVolume;
            var game = GameManager.Instance;
            if (!_followBattle || game == null || game.State != GameState.Running || game.WaveSpawner == null) return;
            int round = game.WaveSpawner.CurrentRoundIndex;
            if (round >= 0 && round != TrackIndex) PlayTrack(round, true);
        }
        private IEnumerator SwitchTrack(int round)
        {
            Error = null;
            IsLoading = true;
            var request = Resources.LoadAsync<AudioClip>("Audio/stage" + (round + 1));
            yield return request;
            var clip = request.asset as AudioClip;
            if (clip == null)
            {
                Error = "曲目加载失败";
                IsLoading = false;
                _transition = null;
                Debug.LogError("Missing stage music: Audio/stage" + (round + 1));
                yield break;
            }
            float initial = _fade;
            for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
            {
                _fade = Mathf.Lerp(initial, 0, t / .5f);
                yield return null;
            }
            _source.Stop();
            _source.clip = clip;
            _fade = 0;
            _source.Play();
            for (float t = 0; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                _fade = Mathf.Clamp01(t / 1.2f);
                yield return null;
            }
            _fade = 1;
            IsLoading = false;
            _transition = null;
        }
        private void OnApplicationQuit() { GameSettings.Save(); }
    }
}
