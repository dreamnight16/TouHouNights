using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.UI;

public static class BattleUiVerification
{
    [MenuItem("Tools/Tower Defense/Verify UI")]
    public static void Run()
    {
        var canvas = UiFactory.CreateCanvas();
        var root = canvas.gameObject.AddComponent<BattleUiRoot>();
        try
        {
            typeof(BattleUiRoot).GetMethod("ConfigureCanvas", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root, null);
            // 按钮的视觉一律烘进 UiPanel 的顶点（渐变 + 描边 + 受光），ColorBlock 必须保持全白零时长：
            // 它是乘算的，任何非白 tint 都会把顶点里的渐变和受光带一起压平。
            var button = UiKit.Button(canvas.transform, "Probe", "Probe", UiButtonKind.Ghost, null);
            if (!(button.targetGraphic is UiPanel)) throw new Exception("FAIL: button target graphic is not a UiPanel");
            var colors = button.colors;
            if (colors.normalColor != Color.white || colors.highlightedColor != Color.white
                || colors.pressedColor != Color.white || colors.disabledColor != Color.white)
                throw new Exception("FAIL: ColorBlock tints a vertex-baked button and will flatten its gradient");
            if (colors.fadeDuration != 0f) throw new Exception("FAIL: ColorBlock fade fights UiMotion's own fade");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand) throw new Exception("FAIL: ultrawide canvas loses vertical layout space");
            foreach (var route in GameConfig.Routes)
            {
                if (route[route.Length - 1] != GameConfig.BaseCell) throw new Exception("Route misses base");
                for (int i = 1; i < route.Length; i++)
                {
                    var delta = route[i] - route[i - 1];
                    if ((delta.x == 0) == (delta.y == 0)) throw new Exception("Non-orthogonal route");
                    if (Mathf.Abs(route[i].x) > 8 || Mathf.Abs(route[i].y) > 4) throw new Exception("Route outside board");
                }
            }
            for (int i = 1; i <= 5; i++)
            {
                var clip = Resources.Load<AudioClip>("Audio/stage" + i);
                if (clip == null || clip.length < 30) throw new Exception("Missing or invalid stage music " + i);
            }
            Debug.Log("TOUHOU_VERIFY PASS: five music assets and orthogonal routes");
            Debug.Log("BATTLE_UI_VERIFY PASS: vertex-baked button visuals; minimum reference canvas preserved");
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
    }
}
