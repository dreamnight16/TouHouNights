using UnityEngine;
using TMPro;

namespace TowerDefense.UI
{
    /// <summary>优先加载随项目发布的 Noto 中文字体；系统字体仅用于资源缺失时回退。
    /// TMP 动态图集允许扩展，字体资源由 Resources 与本类缓存持有。
    /// </summary>
    public static class UiFont
    {
        // 中文候选（按优先级）。覆盖 Windows / macOS / Linux 常见中文字体。
        private static readonly string[] CjkCandidates =
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "DengXian", "SimHei", "SimSun",
            "Noto Sans CJK SC", "Noto Sans SC", "Source Han Sans SC", "WenQuanYi Micro Hei",
            "PingFang SC", "Hiragino Sans GB", "Arial Unicode MS"
        };

        private const char Probe = '中';   // 用于检查中文覆盖。

        private static Font _cjk;
        private static TMP_FontAsset _tmpCjk;
        private static bool _tmpTried;

        /// <summary>优先使用项目中文字体，缺失时查找系统字体，最后回退到 Unity 内置字体。</summary>
        public static Font Cjk
        {
            get
            {
                if (_cjk == null)
                {
                    _cjk = ResolveCjk() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _cjk;
            }
        }

        /// <summary>最近一次字体查找是否通过中文探针检查。</summary>
        public static bool HasCjk { get; private set; }

        /// <summary>中文 TMP 字体资产；创建失败为 null。</summary>
        public static TMP_FontAsset TmpCjk
        {
            get
            {
                if (!_tmpTried)
                {
                    _tmpTried = true;
                    _tmpCjk = CreateTmp();
                }
                return _tmpCjk;
            }
        }

        public static bool TmpReady => TmpCjk != null;

        private static Font ResolveCjk()
        {
            var bundled = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (bundled != null && bundled.HasCharacter(Probe))
            {
                HasCjk = true;
                return bundled;
            }
            foreach (var name in CjkCandidates)
            {
                var font = TryCreate(name);
                if (font == null) continue;

                if (font.HasCharacter(Probe))
                {
                    HasCjk = true;
                    return font;
                }
            }

            HasCjk = false;
            return null;
        }

        private static Font TryCreate(string name)
        {
            try
            {
                // 同时传进拉丁回退名，Unity 在缺字时可按顺序兜底。
                var font = Font.CreateDynamicFontFromOSFont(new[] { name, "Arial" }, 48);
                return font != null && font.name != null ? font : null;
            }
            catch
            {
                return null;
            }
        }

        private static TMP_FontAsset CreateTmp()
        {
            try
            {
                var source = ResolveCjk();
                if (source == null) return null;

                var asset = TMP_FontAsset.CreateFontAsset(source);
                if (asset == null) return null;
                asset.isMultiAtlasTexturesEnabled = true;
                asset.name = "UiFontCjk";
                return asset;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>把一批字符提前烘进图集，避免第一次出现时闪一下方框。</summary>
        public static void Preload(string text)
        {
            var asset = TmpCjk;
            if (asset == null || string.IsNullOrEmpty(text)) return;

            try
            {
                asset.TryAddCharacters(text);
            }
            catch
            {
                // 预载失败不影响运行：动态填充会在需要时补上。
            }
        }
    }
}

