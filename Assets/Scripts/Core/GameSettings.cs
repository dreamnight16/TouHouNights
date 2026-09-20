using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>音频和画面偏好即时生效，并跨启动保存。</summary>
    public static class GameSettings
    {
        public static float MusicVolume { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat("td_music_volume", .5f));
        public static float SfxVolume { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat("td_sfx_volume", .65f));
        public static bool CameraMotion { get; private set; } = PlayerPrefs.GetInt("td_camera_motion", 1) != 0;
        public static void SetMusicVolume(float value) { MusicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat("td_music_volume", MusicVolume); }
        public static void SetSfxVolume(float value) { SfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat("td_sfx_volume", SfxVolume); }
        public static void SetCameraMotion(bool value) { CameraMotion = value; PlayerPrefs.SetInt("td_camera_motion", value ? 1 : 0); }
        public static void Save() { PlayerPrefs.Save(); }
    }
}
