using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    public sealed partial class BattleUiRoot
    {
        private Image Panel(string name, Color color) { return Panel(transform, name, color); }
        private static Image Panel(Transform parent, string name, Color color)
        {
            var image = Image(parent, name, color);
            var edge = Image(image.transform, "UpperEdge", BattleUiTheme.WithAlpha(BattleUiTheme.Paper, .18f));
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = new Vector2(0f, 1f);
            edge.rectTransform.anchorMax = Vector2.one;
            edge.rectTransform.pivot = new Vector2(.5f, 1f);
            edge.rectTransform.sizeDelta = new Vector2(0f, 1f);
            return image;
        }
        private static Image Image(Transform parent, string name, Color color) { return UiFactory.CreateImage(parent, name, color); }
        private static UiText Text(Transform p, string n, string v, int s, Color c, TextAnchor a, float x, float y, float w, float h) { var t = UiFactory.CreateText(p,n,v,s,c,a); PinTopLeft(t.rectTransform,x,y,w,h); t.FitInBox(Mathf.Max(11, s - 4)); return t; }
        private static Button Button(Transform p, string n, string l, System.Action click)
        {
            var image = Image(p, n, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = BattleUiTheme.WithAlpha(BattleUiTheme.Text, .28f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = BattleUiTheme.Surface,
                highlightedColor = Color.Lerp(BattleUiTheme.Surface, BattleUiTheme.Paper, .18f),
                pressedColor = Color.Lerp(BattleUiTheme.Surface, BattleUiTheme.Action, .35f),
                selectedColor = BattleUiTheme.Surface,
                disabledColor = BattleUiTheme.WithAlpha(BattleUiTheme.Disabled, .45f),
                colorMultiplier = 1f,
                fadeDuration = .08f,
            };
            if (click != null) button.onClick.AddListener(() => click());
            var label = UiFactory.CreateText(image.transform, "Label", l, 18, BattleUiTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.Stretch(label.rectTransform);
            label.FitInBox(14);
            UiMotion.Attach(button);
            return button;
        }
        private static void AddDepth(Image image)
        {
            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = BattleUiTheme.Shadow;
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = BattleUiTheme.Edge;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }
        private static void AddCornerMarks(Transform parent)
        {
            CornerMark(parent, "CornerTL", 0f, 1f, 0f, 1f);
            CornerMark(parent, "CornerBR", 1f, 0f, 1f, 0f);
        }
        private static void CornerMark(Transform parent, string name, float ax, float ay, float px, float py)
        {
            var mark = Image(parent, name, BattleUiTheme.WithAlpha(BattleUiTheme.Paper, .34f));
            mark.raycastTarget = false;
            var rect = mark.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(ax, ay);
            rect.pivot = new Vector2(px, py);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(BattleUiTheme.Corner, 1f);
        }
        private static void PinTopLeft(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h); }
        private static void PinTopRight(RectTransform r,float right,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-right,-y);r.sizeDelta=new Vector2(w,h); }
        private static void PinTopCenter(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h); }
        private static void PinBottomLeft(RectTransform r,float x,float b,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(0,0);r.pivot=new Vector2(0,0);r.anchoredPosition=new Vector2(x,b);r.sizeDelta=new Vector2(w,h); }
        private static void PinBottomRight(RectTransform r,float right,float b,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-right,b);r.sizeDelta=new Vector2(w,h); }
        private static void PinLeft(RectTransform r,float left,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(left,y);r.sizeDelta=new Vector2(w,h); }
        private static void PinRight(RectTransform r,float right,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(1,.5f);r.pivot=new Vector2(1,.5f);r.anchoredPosition=new Vector2(-right,y);r.sizeDelta=new Vector2(w,h); }
    }
}
