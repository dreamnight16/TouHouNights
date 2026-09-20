using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 方舟式的「手持呼吸感」相机漂移：只微调相机的旋转（±0.4° 级别），
    /// 位置完全留给 ScreenShake 独占，因此两者互不抢占、也不会在震屏结束时产生位置跳变。
    /// 用平滑噪声合成极缓的手持晃动，让整幅战场像被柔光提着，而不是钉死在屏幕上。
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
            // l + r 的叠加噪声：低频主摆 + 微量次级抖动，肉眼几乎察觉不到顶点，却让画面「活」起来。
            float t = Time.unscaledTime;
            float roll = (Mathf.PerlinNoise(t * 0.16f, 0.53f) - 0.5f) * 2f * 0.45f;   // Z 轴滚转
            float pitch = (Mathf.PerlinNoise(0.31f, t * 0.16f) - 0.5f) * 2f * 0.34f;  // X 轴俯仰
            float yaw = (Mathf.PerlinNoise(0.67f, t * 0.11f) - 0.5f) * 2f * 0.30f;    // Y 轴偏航

            transform.rotation = _baseRotation
                * Quaternion.Euler(pitch, yaw, roll);
        }
    }
}
