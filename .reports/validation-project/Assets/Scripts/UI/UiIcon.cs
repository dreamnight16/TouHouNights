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
    /// 程序化图标构建器：用 Circle / Square / Triangle / Heart 等基础图元组合出清晰图标，
    /// 让纯代码 UI 也有「美术感」——参考 Kingdom Rush 的彩色兵种图标与方舟的职业图标。
    /// 根节点为无 Image 的 RectTransform，调用方自行定位；所有子图元 raycastTarget=false。
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

        /// <summary>塔职业图标：机枪=子弹 / 狙击=准星 / 导弹=火箭 / 减速=雪花。</summary>
        public static RectTransform CreateTowerIcon(Transform parent, TowerType type, float size, Color color)
        {
            var root = CreateRoot(parent, "TowerIcon_" + type, size);
            var bright = Color.Lerp(color, Color.white, 0.20f);
            var dark = Color.Lerp(color, Color.black, 0.30f);

            switch (type)
            {
                case TowerType.Gun: // 子弹
                    AddImage(root, size, "Body", new Vector2(0f, -0.06f), new Vector2(size * 0.34f, size * 0.60f), color, null);
                    AddImage(root, size, "Nose", new Vector2(0f, 0.34f), new Vector2(size * 0.44f, size * 0.30f), bright,
                        SpriteFactory.Triangle(1f, 0.9f, bright));
                    AddImage(root, size, "FinL", new Vector2(0.24f, -0.26f), new Vector2(size * 0.10f, size * 0.22f), dark, null);
                    AddImage(root, size, "FinR", new Vector2(-0.24f, -0.26f), new Vector2(size * 0.10f, size * 0.22f), dark, null);
                    break;

                case TowerType.Sniper: // 准星
                    AddImage(root, size, "TickV", Vector2.zero, new Vector2(size * 0.11f, size * 0.80f), color, null);
                    AddImage(root, size, "TickH", Vector2.zero, new Vector2(size * 0.80f, size * 0.11f), color, null);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.24f, size * 0.24f), bright,
                        SpriteFactory.Circle(0.5f, bright));
                    break;

                case TowerType.Missile: // 火箭
                    AddImage(root, size, "Body", new Vector2(0f, -0.08f), new Vector2(size * 0.32f, size * 0.56f), color, null);
                    AddImage(root, size, "Nose", new Vector2(0f, 0.34f), new Vector2(size * 0.40f, size * 0.28f), bright,
                        SpriteFactory.Triangle(1f, 0.9f, bright));
                    AddImage(root, size, "FinL", new Vector2(0.26f, -0.30f), new Vector2(size * 0.28f, size * 0.10f), dark, null, -30f);
                    AddImage(root, size, "FinR", new Vector2(-0.26f, -0.30f), new Vector2(size * 0.28f, size * 0.10f), dark, null, 30f);
                    AddImage(root, size, "Flame", new Vector2(0f, -0.40f), new Vector2(size * 0.24f, size * 0.22f),
                        new Color(1f, 0.62f, 0.22f, 1f), SpriteFactory.Triangle(1f, 0.9f, new Color(1f, 0.62f, 0.22f, 1f)), 180f);
                    break;

                case TowerType.Heal: // 圣圣符：竖 + 横十字（医疗母题）
                    AddImage(root, size, "BarV", Vector2.zero, new Vector2(size * 0.30f, size * 0.84f), color, null);
                    AddImage(root, size, "BarH", Vector2.zero, new Vector2(size * 0.84f, size * 0.30f), color, null);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.16f, size * 0.16f), bright,
                        SpriteFactory.Circle(0.5f, bright));
                    AddImage(root, size, "Ring", Vector2.zero, new Vector2(size * 0.94f, size * 0.94f), color,
                        SpriteFactory.Shell(0.46f, 0.05f, color));
                    break;

                default: // Slow：雪花
                    AddImage(root, size, "ArmA", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null);
                    AddImage(root, size, "ArmB", Vector2.zero, new Vector2(size * 0.84f, size * 0.11f), color, null);
                    AddImage(root, size, "ArmC", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null, 60f);
                    AddImage(root, size, "ArmD", Vector2.zero, new Vector2(size * 0.11f, size * 0.84f), color, null, -60f);
                    AddImage(root, size, "Core", Vector2.zero, new Vector2(size * 0.18f, size * 0.18f), bright,
                        SpriteFactory.Circle(0.5f, bright));
                    break;
            }

            return root;
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
