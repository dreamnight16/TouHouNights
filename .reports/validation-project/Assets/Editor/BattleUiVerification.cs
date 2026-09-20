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
            var buttonMethod = typeof(BattleUiRoot).GetMethod("Button", BindingFlags.NonPublic | BindingFlags.Static);
            var button = (Button)buttonMethod.Invoke(null, new object[] { canvas.transform, "Probe", "Probe", null });
            var image = (Image)button.targetGraphic;
            if (image.color != Color.white) throw new Exception("FAIL: button base color multiplies the state tint");
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
            Debug.Log("BATTLE_UI_VERIFY PASS: neutral button base; minimum reference canvas preserved");
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
    }
}
