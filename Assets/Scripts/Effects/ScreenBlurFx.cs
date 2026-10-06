using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 相机后处理：生成 UI 模糊纹理，再提取、模糊并合成 Bloom。
    /// </summary>
    public sealed class ScreenBlurFx : MonoBehaviour
    {
        public static RenderTexture Blurred { get; private set; }

        private Material _blurMaterial;
        private Material _bloomMaterial;
        private RenderTexture _raw;
        private RenderTexture _bloomA;
        private RenderTexture _bloomB;

        private void Awake()
        {
            _blurMaterial = CreateMaterial("TowerDefense/FX/Blur");
            _bloomMaterial = CreateMaterial("TowerDefense/FX/Bloom");
        }

        private static Material CreateMaterial(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            return shader != null ? new Material(shader) : null;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            DoBlur(source);
            DoBloom(source, destination);
        }

        private void DoBlur(RenderTexture source)
        {
            int w = Mathf.Max(160, source.width / 4);
            int h = Mathf.Max(90, source.height / 4);

            if (_raw == null || _raw.width != w || _raw.height != h)
            {
                if (_raw != null) Destroy(_raw);
                _raw = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "ScreenBlurRaw" };
            }
            if (Blurred == null || Blurred.width != w || Blurred.height != h)
            {
                if (Blurred != null) DestroyImmediate(Blurred);
                Blurred = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "ScreenBlurred" };
            }

            var prev = RenderTexture.active;
            Graphics.Blit(source, _raw);
            if (_blurMaterial != null)
            {
                // 在低分辨率纹理上迭代模糊：3 次 pass 0，再执行 2 次 pass 1。
                _blurMaterial.SetTexture("_MainTex", _raw);
                _blurMaterial.SetFloat("_Distance", 1.0f);
                Graphics.Blit(_raw, Blurred, _blurMaterial, 0);
                _blurMaterial.SetTexture("_MainTex", Blurred);
                Graphics.Blit(Blurred, _raw, _blurMaterial, 0);
                _blurMaterial.SetTexture("_MainTex", _raw);
                Graphics.Blit(_raw, Blurred, _blurMaterial, 0);

                _blurMaterial.SetTexture("_MainTex", Blurred);
                _blurMaterial.SetFloat("_Distance", 1.0f);
                Graphics.Blit(Blurred, _raw, _blurMaterial, 1);
                _blurMaterial.SetTexture("_MainTex", _raw);
                Graphics.Blit(_raw, Blurred, _blurMaterial, 1);
            }
            else
            {
                Graphics.Blit(_raw, Blurred);
            }
            RenderTexture.active = prev;
        }

        private void DoBloom(RenderTexture source, RenderTexture destination)
        {
            if (_bloomMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int w = Mathf.Max(160, source.width / 3);
            int h = Mathf.Max(90, source.height / 3);

            if (_bloomA == null || _bloomA.width != w || _bloomA.height != h)
            {
                if (_bloomA != null) Destroy(_bloomA);
                if (_bloomB != null) Destroy(_bloomB);
                _bloomA = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "BloomA" };
                _bloomB = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "BloomB" };
            }

            _bloomMaterial.SetFloat("_Threshold", 0.72f);
            _bloomMaterial.SetFloat("_Intensity", 1.15f);

            Graphics.Blit(source, _bloomA, _bloomMaterial, 0);
            Graphics.Blit(_bloomA, _bloomB, _bloomMaterial, 1);
            Graphics.Blit(_bloomB, _bloomA, _bloomMaterial, 2);
            Graphics.Blit(_bloomA, _bloomB, _bloomMaterial, 1);
            Graphics.Blit(_bloomB, _bloomA, _bloomMaterial, 2);

            _bloomMaterial.SetTexture("_BlurTex", _bloomA);
            Graphics.Blit(source, destination, _bloomMaterial, 3);
        }

        private void OnDestroy()
        {
            if (_raw != null) Destroy(_raw);
            if (_bloomA != null) Destroy(_bloomA);
            if (_bloomB != null) Destroy(_bloomB);
            if (Blurred != null) Destroy(Blurred);
            Blurred = null;
        }
    }
}
