using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 使用低频噪声微调相机旋转；位置由 ScreenShake 控制，避免相互覆盖。
    /// </summary>
    public sealed class CameraDrift : MonoBehaviour
    {
        private Quaternion _baseRotation;

        private void Awake()
        {
            _baseRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (!TowerDefense.Core.GameSettings.CameraMotion) { transform.rotation = _baseRotation; return; }
            float t = Time.unscaledTime;
            float roll = (Mathf.PerlinNoise(t * 0.16f, 0.53f) - 0.5f) * 2f * 0.45f;   // Z 轴滚转
            float pitch = (Mathf.PerlinNoise(0.31f, t * 0.16f) - 0.5f) * 2f * 0.34f;  // X 轴俯仰
            float yaw = (Mathf.PerlinNoise(0.67f, t * 0.11f) - 0.5f) * 2f * 0.30f;    // Y 轴偏航

            transform.rotation = _baseRotation
                * Quaternion.Euler(pitch, yaw, roll);
        }
    }
}
