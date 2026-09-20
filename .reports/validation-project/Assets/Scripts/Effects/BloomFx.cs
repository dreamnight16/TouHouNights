using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// Bloom 已合并进 ScreenBlurFx（Unity 同一相机仅一个 OnRenderImage 生效）。
    /// 保留此文件仅为说明；不要挂载本组件。
    /// </summary>
    [System.Obsolete("Bloom 逻辑已并入 ScreenBlurFx，请勿挂载本组件。")]
    public sealed class LegacyBloomNote : MonoBehaviour
    {
    }
}
