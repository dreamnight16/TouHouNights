using UnityEngine;

namespace TowerDefense.UI
{
    /// <summary>
    /// UI 布局原语：把 UGUI 的 anchor/pivot/sizeDelta 三件套收敛成「离哪条边多远」的一行调用。
    ///
    /// 为什么需要它：重构前每个组件里都散落着 anchorMin/pivot/anchoredPosition/sizeDelta 四行样板，
    /// 而且 y 轴方向（顶部锚点下为负）极易算错，导致元素错位到屏幕外。这里统一约定：
    ///   - At(x, y, w, h)        ：x = 距左缘，y = 距上缘（y 为元素竖直中心）
    ///   - AtRight(right, y, ...) ：right = 距右缘
    ///   - AtBottom(x, bottom, ..) ：bottom = 距下缘
    /// 世界锚定（贴着单位出现的面板）另提供 WorldToCanvas / ToAnchored 两个换算。
    /// </summary>
    public static class TdLayout
    {
        /// <summary>新建一个只带 RectTransform 的子节点。</summary>
        public static RectTransform NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        /// <summary>左上定位（pivot 在左中）：x 距左缘，y 距上缘。</summary>
        public static void At(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>左上定位但以中心为 pivot（需要缩放/旋转的元素用这个）。</summary>
        public static void AtCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>右上定位（pivot 在右中）：right 距右缘。</summary>
        public static void AtRight(RectTransform rt, float right, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-right, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>左下定位（pivot 在左中）：bottom 距下缘。</summary>
        public static void AtBottom(RectTransform rt, float x, float bottom, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, bottom);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>右下定位（pivot 在右中）。</summary>
        public static void AtBottomRight(RectTransform rt, float right, float bottom, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-right, bottom);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>水平居中定位（pivot 在中上）：cx = 相对父节点水平中心的偏移，y = 距上缘。</summary>
        public static void AtMidTop(RectTransform rt, float cx, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>水平居中 + 底部定位（pivot 在中）：cx = 相对水平中心的偏移，bottom = 距下缘。</summary>
        public static void AtMidBottom(RectTransform rt, float cx, float bottom, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, bottom);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>顶部通栏（战局状态层）。</summary>
        public static void TopBand(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, -height);
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>底部通栏（快速操作层）。</summary>
        public static void BottomBand(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, height);
        }

        /// <summary>撑满父节点。</summary>
        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>把元素设为「画布绝对坐标定位」：锚点固定在左下角，pivot 保持在想要的那一点。</summary>
        public static void PinToCanvas(RectTransform rt, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
        }

        /// <summary>世界坐标 → 画布局部坐标（原点在画布中心，单位 = 参考分辨率单位）。</summary>
        public static Vector2 WorldToCanvasLocal(RectTransform canvasRect, Vector3 worldPos, Camera cam)
        {
            var vp = cam.WorldToViewportPoint(worldPos);
            var r = canvasRect.rect;
            return new Vector2((vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);
        }

        /// <summary>画布局部坐标 → anchoredPosition（配合 <see cref="PinToCanvas"/> 使用）。</summary>
        public static Vector2 ToAnchored(RectTransform canvasRect, Vector2 local)
        {
            return local - canvasRect.rect.min;
        }

        /// <summary>
        /// 「贴着世界单位出现的面板」统一摆位：优先放单位右侧，右侧放不下就翻到左侧，
        /// 并夹在上下安全区之间（避免被战局状态层 / 快速操作层压住）。
        /// 返回面板中心的画布局部坐标；<paramref name="flip"/> 表示是否翻到了左侧（供引导线取边）。
        /// </summary>
        public static Vector2 BesideUnit(RectTransform canvasRect, Vector2 unitLocal, float halfW, float halfH,
            float gap, out bool flip)
        {
            var rect = canvasRect.rect;
            float limitX = rect.width * 0.5f - halfW - TdTheme.Space.Sm;
            float limitTop = rect.height * 0.5f - TdTheme.SafeTop - halfH;
            float limitBottom = -rect.height * 0.5f + TdTheme.SafeBottom + halfH;

            flip = unitLocal.x + gap + halfW > limitX;
            float px = flip ? unitLocal.x - gap - halfW : unitLocal.x + gap + halfW;
            px = Mathf.Clamp(px, -limitX, limitX);
            float py = Mathf.Clamp(unitLocal.y, Mathf.Min(limitBottom, limitTop), Mathf.Max(limitBottom, limitTop));
            return new Vector2(px, py);
        }

        // ================================================================
        // 锚点定位原语 —— 新 HUD 一律只用这一套
        //
        // 为什么需要它：旧代码里到处是「相对 1280×720 参考分辨率的绝对 x」，
        // 但 CanvasScaler 的实际画布尺寸随窗口比例变化，那些绝对坐标会被推到画布外，
        // 表现就是「上下操作台显示不全」。修一次数值只能治一个分辨率，治不了根。
        //
        // 这套原语只描述「相对父节点的位置」：父节点是横向铺满的一条带，
        // 子元素用左/中/右锚点 + 自身 pivot 定位，几何上不可能溢出父节点。
        // ================================================================

        /// <summary>通用锚点定位。anchor/pivot ∈ [0,1]，pos 为相对锚点的偏移。</summary>
        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>顶部 / 底部整条带：横向铺满父节点，高度固定，贴着上缘或下缘。</summary>
        public static void Band(RectTransform rt, bool top, float height)
        {
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
        }

        /// <summary>带内左对齐（垂直居中）：left 为距左缘距离。</summary>
        public static void InLeft(RectTransform rt, float left, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(left, 0f);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>带内右对齐（垂直居中）：right 为距右缘距离。</summary>
        public static void InRight(RectTransform rt, float right, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-right, 0f);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>带内水平居中：cx 为相对水平中心的偏移。</summary>
        public static void InCenter(RectTransform rt, float cx, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, 0f);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>带内垂直居中且贴底：bottom 为距下缘距离（用于需要下沉对齐的元素）。</summary>
        public static void InBottomCenter(RectTransform rt, float cx, float bottom, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(cx, bottom);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
