using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Data;
using TowerDefense.Util;

namespace TowerDefense.UI
{
    /// <summary>HUD/卡片用的小图标类型（由基础图元组合而成）。</summary>
    public enum UiIconKind
    {
        Heart,   // 生命
        Coin,    // 金币
        Threat,  // 场上敌人
        Kill,    // 击杀 / 索敌准星
        Tower,   // 塔数量
        Play,    // 播放（速度）
        Pause    // 暂停
    }

    /// <summary>
    /// 用基础图元组合 HUD 和塔图标。根节点为 RectTransform，由调用方定位；
    /// 子图元均不拦截指针。
    /// </summary>
    public static class UiIcon
    {
        public static RectTransform Create(Transform parent, UiIconKind kind, float size, Color color)
        {
            var root = CreateRoot(parent, kind.ToString(), size);

            switch (kind)
            {
                case UiIconKind.Heart:
                    AddImage(root, size, "Body", Vector2.zero, new Vector2(size, size), color,
                        SpriteFactory.Heart(size * 1.05f, color));
                    break;

                case UiIconKind.Coin:
                    AddImage(root, size, "Outer", Vector2.zero, new Vector2(size, size), color,
                        SpriteFactory.Circle(0.5f, color));
                    AddImage(root, size, "Inner", Vector2.zero, new Vector2(size * 0.58f, size * 0.58f), color,
                        SpriteFactory.Circle(0.5f, Color.Lerp(color, Color.black, 0.30f)));
                    AddImage(root, size, "Shine", new Vector2(-0.16f, 0.22f), new Vector2(size * 0.20f, size * 0.20f),
                        new Color(1f, 1f, 1f, 0.55f), SpriteFactory.Circle(0.5f, Color.white));
                    break;

                case UiIconKind.Threat:
                    AddImage(root, size, "Body", Vector2.zero, new Vector2(size, size), color,
                        SpriteFactory.Circle(0.5f, color));
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.52f, size * 0.52f),
                        new Color(0f, 0f, 0f, 0.35f), SpriteFactory.Circle(0.5f, new Color(0f, 0f, 0f, 0.35f)));
                    break;

                case UiIconKind.Kill:
                    AddImage(root, size, "TickV", Vector2.zero, new Vector2(size * 0.12f, size * 0.86f), color, null);
                    AddImage(root, size, "TickH", Vector2.zero, new Vector2(size * 0.86f, size * 0.12f), color, null);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.26f, size * 0.26f), color,
                        SpriteFactory.Circle(0.5f, color));
                    break;

                case UiIconKind.Tower:
                    AddImage(root, size, "Roof", new Vector2(0f, 0.22f), new Vector2(size * 0.72f, size * 0.40f), color,
                        SpriteFactory.Triangle(1f, 0.9f, color));
                    AddImage(root, size, "Body", new Vector2(0f, -0.12f), new Vector2(size * 0.44f, size * 0.34f),
                        Color.Lerp(color, Color.white, 0.12f), null);
                    AddImage(root, size, "Base", new Vector2(0f, -0.38f), new Vector2(size * 0.62f, size * 0.14f),
                        Color.Lerp(color, Color.black, 0.30f), null);
                    break;

                case UiIconKind.Play:
                    AddImage(root, size, "Triangle", new Vector2(0.02f, 0f), new Vector2(size * 0.62f, size * 0.78f),
                        color, SpriteFactory.Triangle(1f, 0.95f, color), -90f);
                    break;

                case UiIconKind.Pause:
                    AddImage(root, size, "BarL", new Vector2(-0.22f, 0f), new Vector2(size * 0.18f, size * 0.78f), color, null);
                    AddImage(root, size, "BarR", new Vector2(0.22f, 0f), new Vector2(size * 0.18f, size * 0.78f), color, null);
                    break;
            }

            return root;
        }

        /// <summary>
        /// 五种塔分别使用竖条、空心菱形、实心星芒、六芒细线和空心环，方便按轮廓区分。
        /// </summary>
        public static RectTransform CreateTowerIcon(Transform parent, TowerType type, float size, Color color)
        {
            var root = CreateRoot(parent, "TowerIcon_" + type, size);
            var bright = Color.Lerp(color, Color.white, 0.20f);

            switch (type)
            {
                case TowerType.Gun: // 速射：三根底边对齐、递次升高的竖条
                    Salvo(root, size, "ShotA", -0.26f, 0.52f, color);
                    Salvo(root, size, "ShotB", 0f, 0.74f, color);
                    Salvo(root, size, "ShotC", 0.26f, 0.96f, bright);
                    break;

                case TowerType.Sniper: // 狙击：空心菱形 + 正中一个实心点
                    Rhombus(root, size, 0.42f, 0.075f, color);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.20f, size * 0.20f), bright,
                        SpriteFactory.Circle(0.5f, bright));
                    break;

                case TowerType.Missile: // 爆符：四点实心星芒
                    AddImage(root, size, "Burst", Vector2.zero, new Vector2(size * 0.96f, size * 0.96f), color,
                        SpriteFactory.Sparkle(size * 0.96f, color));
                    break;

                case TowerType.Slow: // 霜华：三根细线交叉成六芒
                    AddImage(root, size, "ArmA", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null);
                    AddImage(root, size, "ArmB", Vector2.zero, new Vector2(size * 0.84f, size * 0.11f), color, null);
                    AddImage(root, size, "ArmC", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null, 60f);
                    AddImage(root, size, "ArmD", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null, -60f);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.18f, size * 0.18f), bright,
                        SpriteFactory.Circle(0.5f, bright));
                    break;

                default: // 甘霖：空心环 + 环内的实心十字
                    AddImage(root, size, "Ring", Vector2.zero, new Vector2(size * 0.94f, size * 0.94f), color,
                        SpriteFactory.Shell(0.46f, 0.05f, color));
                    AddImage(root, size, "BarV", Vector2.zero, new Vector2(size * 0.22f, size * 0.62f), bright, null);
                    AddImage(root, size, "BarH", Vector2.zero, new Vector2(size * 0.62f, size * 0.22f), bright, null);
                    break;
            }

            return root;
        }

        /// <summary>一根底边对齐的竖条，<paramref name="height"/> 是占图标边长的比例。</summary>
        private static void Salvo(RectTransform root, float size, string name, float x, float height, Color color)
        {
            AddImage(root, size, name, new Vector2(x, (height - 1f) * 0.5f),
                new Vector2(size * 0.17f, size * height), color, null);
        }

        /// <summary>
        /// 用四根 ±45° 细条组合菱形；radius 是半对角线相对图标边长的比例。
        /// </summary>
        private static void Rhombus(RectTransform root, float size, float radius, float thickness, Color color)
        {
            float edge = radius * Mathf.Sqrt(2f) * size;
            float bar = thickness * size;
            float mid = radius * 0.5f;
            AddImage(root, size, "EdgeTR", new Vector2(mid, mid), new Vector2(edge, bar), color, null, -45f);
            AddImage(root, size, "EdgeBR", new Vector2(mid, -mid), new Vector2(edge, bar), color, null, 45f);
            AddImage(root, size, "EdgeBL", new Vector2(-mid, -mid), new Vector2(edge, bar), color, null, -45f);
            AddImage(root, size, "EdgeTL", new Vector2(-mid, mid), new Vector2(edge, bar), color, null, 45f);
        }

        // ---- 内部 ----

        private static RectTransform CreateRoot(Transform parent, string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(size, size);
            return root;
        }

        private static void AddImage(RectTransform root, float size, string name, Vector2 normPos, Vector2 normSize,
            Color color, Sprite sprite, float rotZ = 0f)
        {
            var img = UiFactory.CreateImage(root, name, color);
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
            }

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(normPos.x * size, normPos.y * size);
            rt.sizeDelta = normSize;
            if (Mathf.Abs(rotZ) > 0.01f)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }
    }
}
