using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Util
{
    /// <summary>
    /// 运行时生成纯色/圆形/方形贴图，让工程不依赖任何外部美术资源即可运行。
    /// 生成结果按「形状 + 尺寸 + 颜色」缓存，避免每次刷怪/开火都新建纹理造成内存抖动。
    /// </summary>
    public static class SpriteFactory
    {
        private const int Resolution = 128;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>生成一个填满纹理的正方形 Sprite，边长 = size 世界单位。</summary>
        public static Sprite Square(float size, Color color)
        {
            return GetOrCreate("sq", size, color, (dx, dy) => 1f);
        }

        /// <summary>生成一个圆形 Sprite，直径 = 2 * radius 世界单位（带软边抗锯齿）。</summary>
        public static Sprite Circle(float radius, Color color)
        {
            return GetOrCreate("circle", radius * 2f, color, (dx, dy) =>
            {
                float nx = (dx - 0.5f) * 2f;
                float ny = (dy - 0.5f) * 2f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float soft = 2f / Resolution;
                if (d <= 1f) return 1f;
                if (d <= 1f + soft) return (1f + soft - d) / soft;
                return 0f;
            });
        }

        private static Sprite GetOrCreate(string kind, float worldSize, Color color, Func<float, float, float> coverage)
        {
            var c = (Color32)color;
            string key = $"{kind}|{worldSize}|{c.r},{c.g},{c.b},{c.a}";

            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var sprite = Create(color, coverage, worldSize);
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Create(Color color, Func<float, float, float> coverage, float worldSize)
        {
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float dx = (x + 0.5f) / Resolution;
                    float dy = (y + 0.5f) / Resolution;
                    float a = coverage(dx, dy);
                    pixels[y * Resolution + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            float ppu = Resolution / Mathf.Max(0.001f, worldSize);
            return Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
