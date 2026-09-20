using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Util
{
    /// <summary>
    /// 运行时生成纯色/圆形/方形/圆角/光晕/环形/三角形/心形/渐变贴图，让工程不依赖任何外部美术资源即可运行。
    /// 生成结果按「形状参数 + 尺寸 + 颜色」缓存，避免每次刷怪/开火都新建纹理造成内存抖动。
    /// 另外提供带九宫格边框（border）的圆角 UI 切片贴图（方舟/Fluent 式卡片），
    /// 用于卡片/按钮/面板，保证任意缩放时圆角近似不变形。
    /// </summary>
    public static class SpriteFactory
    {
        // 512：世界贴图实体按更高分辨率烘焙，边缘抗锯齿带按 1/半径 比例更细腻，消除「精致感不足」的颗粒感。
        private const int Resolution = 512;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>UI 圆角九宫格贴图种类：纹理尺寸与圆角按实际控件的典型尺寸烘焙，拉伸时圆角近似不变。</summary>
        public enum UiKind
        {
            Card = 0,      // 塔卡片 160x64 r12
            Button,        // 小按钮 104x40 r8
            Panel,         // 信息面板 560x128 r10
            ResultBox,     // 结算面板 700x440 r14
            Chip,          // 小胶囊 56x22 r6
            Bar,           // 进度条 112x16 r4
            Notice         // 顶部横幅 640x44 r8
        }

        // ---- 世界空间贴图 ----

        /// <summary>生成一个填满纹理的正方形 Sprite，边长 = size 世界单位。</summary>
        public static Sprite Square(float size, Color color)
        {
            return GetOrCreate("sq", size, color, (px, py) => 1f);
        }

        /// <summary>生成一个圆形 Sprite，直径 = 2 * radius 世界单位（带软边抗锯齿）。</summary>
        public static Sprite Circle(float radius, Color color)
        {
            return GetOrCreate($"circle|{radius}", radius * 2f, color, (px, py) =>
            {
                float half = Resolution * 0.5f;
                float d = Mathf.Sqrt(px * px + py * py) / half;
                float soft = 2.0f / half;
                return Mathf.Clamp01((1f + soft - d) / soft);
            });
        }

        /// <summary>
        /// 生成受光球体 Sprite（左上受光、右下渐暗，明暗在贴图层逐像素烘焙），
        /// 让圆形单位呈现 3D 体积感，而非 Circle 那种平涂圆块。适合做塔身/敌人主体。
        /// </summary>
        public static Sprite Sphere(float radius, Color color)
        {
            string key = $"sphere|{radius:0.0}|{ColorKey(color)}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            float half = Resolution * 0.5f;
            var tex = CreateTextureTinted(Resolution, Resolution, (px, py) =>
            {
                float nx = px / half;
                float ny = py / half;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float edge = Mathf.Clamp01((1f - d) * half / 2f);   // 圆软边（约 2px 抗锯齿）
                // 光从左上 45° 来：正面偏亮、右下压到约 0.38 倍亮度 → 像滚动的珠子/玻璃球
                float light = Mathf.Clamp01(0.40f + 0.60f * (0.5f - 0.5f * (nx * 0.71f - ny * 0.71f)));
                Color c = Color.Lerp(color * 0.38f, color * 1.30f, light);
                c.a = edge;
                return c;
            });

            float ppu = Resolution / Mathf.Max(0.001f, radius * 2f);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), ppu);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>生成圆角正方形 Sprite，边长 size、圆角 cornerRadius（均世界单位），带软边。</summary>
        public static Sprite RoundedSquare(float size, float cornerRadius, Color color)
        {
            float half = Resolution * 0.5f;
            float r = cornerRadius * (Resolution / Mathf.Max(0.0001f, size)); // 圆形角换算到像素

            return GetOrCreate($"rrect|{cornerRadius}|{size}", size, color, (px, py) =>
            {
                float rx = Mathf.Abs(px) - (half - r);
                float ry = Mathf.Abs(py) - (half - r);
                float ox = Mathf.Max(rx, 0f);
                float oy = Mathf.Max(ry, 0f);
                float dist = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(rx, ry), 0f) - r;
                return Mathf.Clamp01((1.5f - dist) / 1.5f);
            });
        }

        /// <summary>生成环形（圆环描边）Sprite：外径 = 2 * outerRadius，环带厚 thickness（世界单位）。</summary>
        public static Sprite Shell(float outerRadius, float thickness, Color color)
        {
            float half = Resolution * 0.5f;
            float soft = 1.5f / half;
            float inner = Mathf.Max(0f, (outerRadius - thickness) / Mathf.Max(0.0001f, outerRadius));

            return GetOrCreate($"shell|{outerRadius}|{thickness}", outerRadius * 2f, color, (px, py) =>
            {
                float d = Mathf.Sqrt(px * px + py * py) / half;
                float outer = Mathf.Clamp01((1f + soft - d) / soft);
                float ringInner = Mathf.Clamp01((d - inner + soft) / soft);
                return outer * ringInner;
            });
        }

        /// <summary>生成径向光晕 Sprite：中心不透明、向边缘平方衰减，适合做发光/氛围。</summary>
        public static Sprite Glow(float radius, Color color)
        {
            float half = Resolution * 0.5f;
            return GetOrCreate($"glow|{radius}", radius * 2f, color, (px, py) =>
            {
                float d = Mathf.Sqrt(px * px + py * py) / half;
                float a = Mathf.Clamp01(1f - d);
                return a * a;
            });
        }

        /// <summary>生成朝上三角形 Sprite（宽 width、高 height，世界单位），底边平直、顶点居中。</summary>
        public static Sprite Triangle(float width, float height, Color color)
        {
            float half = Resolution * 0.5f;
            float aspect = width / Mathf.Max(0.0001f, height);

            return GetOrCreate($"tri|{width}|{height}", height, color, (px, py) =>
            {
                float t = Mathf.Clamp01((py + half) / (half * 2f));            // 0=底边 1=顶点
                float halfW = aspect * half * (1f - t);                        // 每行允许的半宽（底边 = 满宽）
                float edge = Mathf.Min(halfW - Mathf.Abs(px), t * half + 1.5f); // 斜边与顶点侧软边
                return Mathf.Clamp01(edge / 1.5f) * Mathf.Clamp01((py + half + 1.5f) / 3f); // 底边软边
            });
        }

        /// <summary>
        /// 正六边形（尖顶：上下各一个顶点），外径 = radius。
        /// 策略棋盘的通用「地块/机体」轮廓，比圆形更有工业感、比方形更柔和。
        /// </summary>
        public static Sprite Hex(float radius, Color color)
        {
            float half = Resolution * 0.5f;
            // 交换 x/y 输入 → 标准（平顶）六边形旋转 90°，得到尖顶版本。
            return GetOrCreate($"hex|{radius}", radius * 2f, color,
                (px, py) => Mathf.Clamp01((1.5f - HexDistance(py, px, half)) / 1.5f));
        }

        /// <summary>标准六边形 SDF（平顶），返回像素单位的带符号距离。</summary>
        private static float HexDistance(float px, float py, float r)
        {
            const float k1 = -0.866025404f;
            const float k2 = 0.5f;
            const float k3 = 0.577350269f;

            float x = Mathf.Abs(px);
            float y = Mathf.Abs(py);

            float dot = k1 * x + k2 * y;
            if (dot < 0f)
            {
                x -= 2f * dot * k1;
                y -= 2f * dot * k2;
            }

            x -= Mathf.Clamp(x, -k3 * r, k3 * r);
            y -= r;

            float d = Mathf.Sqrt(x * x + y * y);
            return y >= 0f ? d : -d;
        }

        /// <summary>菱形 Sprite：宽 width、高 height（世界单位）。用于针状/晶体类剪影。</summary>
        public static Sprite Diamond(float width, float height, Color color)
        {
            float half = Resolution * 0.5f;
            float size = Mathf.Max(width, height);
            float s = size / Resolution;              // 每像素对应的世界尺寸
            float a = Mathf.Max(0.0001f, width * 0.5f);
            float b = Mathf.Max(0.0001f, height * 0.5f);
            float grad = Mathf.Sqrt(1f / (a * a) + 1f / (b * b));

            return GetOrCreate($"dia|{width}|{height}", size, color, (px, py) =>
            {
                float f = Mathf.Abs(px * s) / a + Mathf.Abs(py * s) / b - 1f;
                return Mathf.Clamp01((1.5f - (f / grad) / s) / 1.5f);
            });
        }

        /// <summary>
        /// 朝上的箭形 Sprite（窄柄 + 带肩箭头）：宽 width、高 height（世界单位）。
        /// 用作敌人/弹丸的「指向性」剪影 —— 有明确前后，一眼能看出朝向。
        /// </summary>
        public static Sprite Arrow(float width, float height, Color color)
        {
            float half = Resolution * 0.5f;
            float size = Mathf.Max(width, height);
            float s = size / Resolution;
            float hw = Mathf.Max(0.0001f, width * 0.5f);
            const float shaft = 0.42f;   // 柄半宽（相对半宽的比例）

            return GetOrCreate($"arrow|{width}|{height}", size, color, (px, py) =>
            {
                float nx = Mathf.Abs(px * s) / hw;                  // 0 = 中轴，1 = 边缘
                float u = Mathf.Clamp01((py + half) / (half * 2f)); // 0 = 底，1 = 顶

                float limit;
                if (u <= 0.55f) limit = shaft;                                       // 柄
                else if (u <= 0.72f) limit = shaft + (1f - shaft) * ((u - 0.55f) / 0.17f); // 箭头外张
                else limit = 1f - (u - 0.72f) / 0.28f;                               // 收成尖

                float d = (nx - limit) * hw / s;                    // 到侧轮廓的像素距离
                return Mathf.Clamp01((1.5f - d) / 1.5f) * Mathf.Clamp01((py + half) / 1.5f);
            });
        }

        /// <summary>十字 Sprite（臂厚 thickness），用于治疗/增益标识。</summary>
        public static Sprite Cross(float size, float thickness, Color color)
        {
            float half = Resolution * 0.5f;
            float t = Mathf.Clamp01(thickness / Mathf.Max(0.0001f, size));

            return GetOrCreate($"cross|{size}|{thickness}", size, color, (px, py) =>
            {
                float nx = px / half;
                float ny = py / half;
                float dh = Mathf.Max(Mathf.Abs(nx) - 1f, Mathf.Abs(ny) - t);
                float dv = Mathf.Max(Mathf.Abs(nx) - t, Mathf.Abs(ny) - 1f);
                return Mathf.Clamp01((1.5f - Mathf.Min(dh, dv) * half) / 1.5f);
            });
        }

        /// <summary>左下角的 L 形角标 Sprite（臂厚 thickness）。旋转 4 次即得四角准星框。</summary>
        public static Sprite Corner(float size, float thickness, Color color)
        {
            float half = Resolution * 0.5f;
            float t = Mathf.Clamp01(thickness / Mathf.Max(0.0001f, size));

            return GetOrCreate($"corner|{size}|{thickness}", size, color, (px, py) =>
            {
                float a = px / half + 1f;   // 0 = 左缘，2 = 右缘
                float b = py / half + 1f;   // 0 = 下缘，2 = 上缘
                // 臂要贴着外缘铺满 thickness：所以区间是 [0, 2t]，中心在 t。
                float dV = Mathf.Max(Mathf.Abs(a - t) - t, Mathf.Abs(b - 1f) - 1f);
                float dH = Mathf.Max(Mathf.Abs(b - t) - t, Mathf.Abs(a - 1f) - 1f);
                return Mathf.Clamp01((1.5f - Mathf.Min(dV, dH) * half) / 1.5f);
            });
        }

        /// <summary>生成心形 Sprite（SDF 隐式函数），用于生命值图标。</summary>
        public static Sprite Heart(float size, Color color)
        {
            float half = Resolution * 0.5f;
            return GetOrCreate("heart", size, color, (px, py) =>
            {
                float x = (px / half) * 1.30f;
                float y = (py / half) * 1.30f;
                float x2 = x * x;
                float y2 = y * y;
                float f = (x2 + y2 - 1f);
                f = f * f * f - x2 * y2 * y;
                return Mathf.Clamp01(0.5f - f * 5f);
            });
        }

        /// <summary>
        /// 生成灰度噪点 Sprite（0~1 随机明度），用于 Fluent 亚克力磨砂质感：
        /// 以低 alpha 平铺覆盖在面板上，模拟玻璃/亚克力内部噪点。
        /// </summary>
        public static Sprite Noise(Color color)
        {
            string key = "noise";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            int w = 128;
            var tex = new Texture2D(w, w, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[w * w];
            var rng = new System.Random(20240601);
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)rng.Next(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, w), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>生成垂直渐变 Sprite：顶部 alpha=1、底部 alpha=0（颜色由 color 决定）。</summary>
        public static Sprite GradientTop(Color color)
        {
            float half = Resolution * 0.5f;
            return GetOrCreate("gradtop|" + ColorKey(color), 2f, color, (px, py) =>
                Mathf.Clamp01((py + half) / (half * 2f)));
        }

        /// <summary>生成四角暗化的暗角 Sprite（拉伸到全屏使用）。</summary>
        public static Sprite Vignette(Color color)
        {
            float half = Resolution * 0.5f;
            return GetOrCreate("vignette|" + ColorKey(color), 2f, color, (px, py) =>
            {
                float d = Mathf.Sqrt(px * px + py * py) / half;
                float a = Mathf.Clamp01(d / 1.15f);
                a = Mathf.Pow(a, 2.6f);
                return a;
            });
        }

        // ---- UI 九宫格（Sliced）----

        /// <summary>
        /// 生成纯白圆角 UI 贴图（带 border 九宫格），Image.type = Sliced 时任意尺寸圆角不变形。
        /// 贴图为白色、仅含 alpha，运行时用 Image.color 染色，才能支持按钮 hover/press 状态变色。
        /// 方舟/类 Fluent 视觉：卡片圆角 + 细描边 + 亚克力材质（亚克力由 UI/UI_Acrylic shader 提供）。
        /// </summary>
        public static Sprite UiRounded(UiKind kind)
        {
            // 按 2x 分辨率烘焙（UHD）：显示端的 1px = 贴图 2px，圆角边缘抗锯齿加倍细腻，
            // 9-slice border 同比例放大，Image 缩放显示时圆角尺寸不变而边缘锐利。
            int w, h, r;
            switch (kind)
            {
                case UiKind.Card:      w = 320;  h = 128;  r = 24; break;
                case UiKind.Button:    w = 208;  h = 80;   r = 16; break;
                case UiKind.Panel:     w = 1120; h = 256;  r = 20; break;
                case UiKind.ResultBox: w = 1400; h = 880;  r = 28; break;
                case UiKind.Chip:      w = 112;  h = 44;   r = 12; break;
                case UiKind.Bar:       w = 224;  h = 32;   r = 8;  break;
                default:               w = 1280; h = 88;   r = 16; break; // Notice
            }

            string key = $"ui_rnd|{(int)kind}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var tex = CreateTexture(w, h, (px, py) =>
            {
                float halfW = w * 0.5f;
                float halfH = h * 0.5f;
                float rx = Mathf.Abs(px) - (halfW - r);
                float ry = Mathf.Abs(py) - (halfH - r);
                float ox = Mathf.Max(rx, 0f);
                float oy = Mathf.Max(ry, 0f);
                float dist = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(rx, ry), 0f) - r;
                return Mathf.Clamp01((2.4f - dist) / 2.4f); // 柔和圆边（2x 纹理 ≈ 显示 1.2px 抗锯齿）
            }, Color.white);

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            Cache[key] = sprite;
            return sprite;
        }

        // ---- 内部实现 ----

        private static Sprite GetOrCreate(string kindKey, float size, Color color, Func<float, float, float> coverage)
        {
            // 尺寸按 0.1 舍入：避免浮点噪声/连续缩放撑爆 512² 贴图的缓存（每次缓存 1MB，无节制会堆积）。
            string key = $"{kindKey}|{size:0.0}|{ColorKey(color)}";
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var tex = CreateTexture(Resolution, Resolution, coverage, color);
            float ppu = Resolution / Mathf.Max(0.001f, size);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), ppu);
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D CreateTexture(int w, int h, Func<float, float, float> coverage, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f - w * 0.5f;
                    float py = y + 0.5f - h * 0.5f;
                    float a = Mathf.Clamp01(coverage(px, py));
                    pixels[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>直接按像素回调生成纹理：允许逐像素调制 RGB（用于 Sphere 烘焙明暗），alpha 由此回调返回。</summary>
        private static Texture2D CreateTextureTinted(int w, int h, Func<float, float, Color> pixel)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f - w * 0.5f;
                    float py = y + 0.5f - h * 0.5f;
                    pixels[y * w + x] = pixel(px, py);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static string ColorKey(Color c)
        {
            var c32 = (Color32)c;
            return $"{c32.r},{c32.g},{c32.b},{c32.a}";
        }
    }
}
