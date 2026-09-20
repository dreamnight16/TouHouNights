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

        /// <summary>
        /// 生成 45° 警戒条纹 Sprite（方舟式警戒带的「斜纹」本体），矩形按 width × height 世界单位烘焙。
        /// 条纹周期沿世界坐标计量：phase = (x + y) mod period，前半周期亮、后半周期透明，
        /// 边缘各留约 2 世界单位的软边。宽高比烘进纹理，UI 侧任意拉伸都保持屏幕上 45° 的走向。
        /// </summary>
        public static Sprite Hazard(float width, float height, float period, Color color)
        {
            string key = $"hazard|{width:0.#}|{height:0.#}|{period:0.#}|{ColorKey(color)}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int texWidth = 128;
            int texHeight = Mathf.Clamp(Mathf.RoundToInt(texWidth * height / Mathf.Max(0.001f, width)), 8, 1024);
            float wx = width / texWidth;
            float wy = height / texHeight;
            float soft = 2f * (wx + wy) * .5f;

            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[texWidth * texHeight];
            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    float wxp = (x + .5f) * wx - width * .5f;
                    float wyp = (y + .5f) * wy - height * .5f;
                    float phase = Mathf.Repeat(wxp + wyp, period);
                    float alpha = Mathf.Min(Mathf.Clamp01(phase / soft), Mathf.Clamp01((period - phase) / soft));
                    pixels[y * texWidth + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(.5f, .5f), texWidth / Mathf.Max(0.001f, width));
            Cache[key] = sprite;
            return sprite;
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

        // ---- 氛围与纹样（纯程序化，仍然零外部资源）----

        /// <summary>
        /// 樱瓣：一枚尖椭圆，顶点朝 +y，中段饱满、两端收细，峰值 alpha 略小于 1（花瓣是半透的）。
        /// 用于战场上飘落的花瓣 —— 单个花瓣尺寸小，所以形状只需要轮廓正确，不需要叶脉细节。
        /// </summary>
        public static Sprite Petal(float size, Color color)
        {
            float half = Resolution * 0.5f;
            float soft = 3f / half;   // 3px 抗锯齿，花瓣边缘不能有硬边
            return GetOrCreate("petal", size, color, (px, py) =>
            {
                float nx = px / half;
                float ny = py / half * 1.06f;   // 纵向略微拉长，尖端更尖
                float taper = Mathf.Pow(Mathf.Max(0f, 1f - ny * ny), 0.58f);
                float d = Mathf.Abs(nx) - 0.40f * taper;
                return Mathf.Clamp01((soft - d) / soft) * 0.92f;
            });
        }

        /// <summary>
        /// 封印环：一圈断成 <paramref name="arcs"/> 段的细环，缺口落在对角线上。
        /// 这是整套视觉里唯一的「纹样」——用在关卡编号、结算评级、战场门扉上，
        /// 表达「封印 / 结界」而不是装饰。
        /// </summary>
        public static Sprite SealRing(float size, float thickness, Color color, int arcs = 4, float gap = 0.22f)
        {
            string key = $"sealring|{size:0.0}|{thickness:0.000}|{arcs}|{gap:0.00}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            float outerRadius = size * 0.5f;
            float unitsPerPixel = size / Resolution;
            float aa = unitsPerPixel * 1.6f;

            var tex = CreateTexture(Resolution, Resolution, (px, py) =>
            {
                float radius = Mathf.Sqrt(px * px + py * py) * unitsPerPixel;
                float ringDistance = Mathf.Abs(radius - (outerRadius - thickness * 0.5f)) - thickness * 0.5f;
                float radial = Mathf.Clamp01((aa - ringDistance) / aa);
                if (radial <= 0f) return 0f;

                float segment = Mathf.PI * 2f / arcs;
                // 相位偏移半段，让缺口落在 45° 对角线上；对 arcs=4 尤其明显。
                float local = Mathf.Repeat(Mathf.Atan2(py, px) + segment * 0.5f, segment);
                float trim = segment * gap * 0.5f;
                float angularAa = Mathf.Max(0.02f, aa / Mathf.Max(0.05f, outerRadius));
                float head = Mathf.Clamp01((local - trim) / angularAa);
                float tail = Mathf.Clamp01((segment - trim - local) / angularAa);
                return radial * Mathf.Min(head, tail);
            }, color);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f),
                Resolution / Mathf.Max(0.001f, size));
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>四芒星闪光：中心核 + 沿坐标轴的四道星芒。用于金尘与命中亮点。</summary>
        public static Sprite Sparkle(float size, Color color)
        {
            float half = Resolution * 0.5f;
            return GetOrCreate("sparkle", size, color, (px, py) =>
            {
                float nx = Mathf.Abs(px) / half;
                float ny = Mathf.Abs(py) / half;
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny) * 2.3f);
                float spike = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Min(nx, ny) * 6f), 3f)
                            * Mathf.Clamp01(1f - Mathf.Max(nx, ny) * 1.15f);
                return Mathf.Clamp01(core + spike * 0.85f);
            });
        }

        /// <summary>
        /// 夜空垂直渐变（不透明）。三段插值，并叠加逐行有序抖动 ——
        /// 8 位色深的暗部渐变必然起色带，抖动是唯一不用额外贴图就能打散它的办法。
        /// 贴图只有 16 px 宽：横向拉伸时每一行颜色相同，不会产生棋盘格。
        /// </summary>
        public static Sprite SkyGradient(Color top, Color mid, Color bottom)
        {
            string key = $"sky|{ColorKey(top)}|{ColorKey(mid)}|{ColorKey(bottom)}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int w = 16, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[w * h];
            uint hash = 0x9E3779B9u;
            for (int y = 0; y < h; y++)
            {
                // SetPixels32 的索引 0 是**贴图底行**，而贴图底行就是 sprite 底边（= 屏幕底）。
                // 这里曾经写成 1 - (y+0.5)/h，于是整片天幕上下颠倒：地平线的暖光跑到画面顶端，
                // 天顶的近黑沉到了棋盘底下 —— 看起来就像「天空挂在头顶、地面在发光」。
                float t = (y + 0.5f) / h;   // 0 = 屏幕底，1 = 屏幕顶
                Color c = t < 0.55f
                    ? Color.Lerp(bottom, mid, t / 0.55f)
                    : Color.Lerp(mid, top, (t - 0.55f) / 0.45f);

                // 每行一个 ±1/255 的抖动值：足以打散色带，又低于可见阈值。
                hash = hash * 1664525u + 1013904223u;
                float dither = ((hash >> 24) / 255f - 0.5f) * (2f / 255f);
                var row = new Color32(
                    (byte)(Mathf.Clamp01(c.r + dither) * 255f),
                    (byte)(Mathf.Clamp01(c.g + dither) * 255f),
                    (byte)(Mathf.Clamp01(c.b + dither) * 255f),
                    255);

                for (int x = 0; x < w; x++) pixels[y * w + x] = row;
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 星野：四方连续的星点贴图。星星按环绕寻址绘制，所以平铺时接缝处不会断星。
        ///
        /// 必须配 <c>SpriteRenderer.drawMode = Tiled</c>（或 UI 的 Tiled Image）使用。
        /// 用普通拉伸模式画，256px 的贴图会被放大到几十个世界单位，每颗星随之放大十几倍，
        /// 星野就变成一片模糊的白斑 —— 这正是它看起来像「灰尘」而不是「星星」的原因。
        /// </summary>
        /// <param name="starSize">单颗星的半径（贴图像素）。贴图一个 tile 只有 2.56 世界单位，
        /// 默认的 1px 级半径投影到屏幕上是亚像素，会闪；星野要看得见就得给到 5~8。</param>
        public static Sprite StarTile(int seed, int starCount, float starSize = 1f)
        {
            string key = $"startile|{seed}|{starCount}|{starSize:0.0}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 256;
            var field = new float[size * size];
            var rng = new System.Random(seed);

            for (int i = 0; i < starCount; i++)
            {
                float cx = (float)rng.NextDouble() * size;
                float cy = (float)rng.NextDouble() * size;
                float brightness = 0.22f + (float)rng.NextDouble() * 0.78f;
                float radius = (0.6f + (float)rng.NextDouble() * 1.2f) * starSize;
                int reach = Mathf.CeilToInt(radius * 3f);

                for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float v = Mathf.Clamp01(1f - d);
                    v *= v;
                    // 环绕寻址 = 四方连续；否则贴图右/上边缘的星星会被切掉，平铺出现网格感。
                    int x = ((Mathf.RoundToInt(cx) + dx) % size + size) % size;
                    int y = ((Mathf.RoundToInt(cy) + dy) % size + size) % size;
                    int index = y * size + x;
                    field[index] = Mathf.Max(field[index], v * brightness);
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = (byte)(Mathf.Clamp01(field[i]) * 255f);
                pixels[i] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 低对比云雾噪声（四方连续）。用于漆器底板与大块石面的材质感 ——
        /// 纯色大平面看起来像没做完的图块，一层极低对比的云雾就能把它变成「材料」。
        /// </summary>
        public static Sprite Mottle(int seed, int cells, float contrast)
        {
            string key = $"mottle|{seed}|{cells}|{contrast:0.00}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // 三个倍频叠加：大块起伏 + 中等斑驳 + 细微颗粒。
                float v = TileableFbm(x / (float)size, y / (float)size, cells, seed);
                byte level = (byte)(Mathf.Clamp01(0.5f + (v - 0.5f) * contrast) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, level);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 胶片颗粒：逐像素随机 alpha 的白色噪点，四方连续。
        /// 以极低 alpha 覆盖全屏，是所有「像渲染出来的画面」而不是「像矢量图的画面」的关键一步。
        /// </summary>
        public static Sprite Grain(int seed)
        {
            string key = $"grain|{seed}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,   // 颗粒必须点采样，双线性会把它糊成一片灰
            };

            var rng = new System.Random(seed);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = (byte)rng.Next(0, 200);
                pixels[i] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>环绕值噪声的 3 倍频叠加，输出 0~1。x/y 取 0~1（贴图归一化坐标）。</summary>
        private static float TileableFbm(float x, float y, int cells, int seed)
        {
            float sum = 0f, amplitude = 0.55f, total = 0f;
            for (int octave = 0; octave < 3; octave++)
            {
                sum += TileableValueNoise(x, y, cells << octave, seed + octave * 977) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
            }
            return sum / Mathf.Max(0.0001f, total);
        }

        private static float TileableValueNoise(float x, float y, int period, int seed)
        {
            float fx = x * period, fy = y * period;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            // 平滑插值：线性插值会在格子边界留下可见的菱形棱，smoothstep 消除它。
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float v00 = Hash01(x0, y0, period, seed);
            float v10 = Hash01(x0 + 1, y0, period, seed);
            float v01 = Hash01(x0, y0 + 1, period, seed);
            float v11 = Hash01(x0 + 1, y0 + 1, period, seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
        }

        /// <summary>格点哈希：取模保证四方连续，乘大质数保证相邻格点不相关。</summary>
        private static float Hash01(int x, int y, int period, int seed)
        {
            int px = ((x % period) + period) % period;
            int py = ((y % period) + period) % period;
            uint h = (uint)(px * 73856093 ^ py * 19349663 ^ seed * 83492791);
            h ^= h >> 13;
            h *= 0x85EBCA6Bu;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
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
