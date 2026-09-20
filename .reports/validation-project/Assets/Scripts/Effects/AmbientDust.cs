using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 战场氛围光尘（Unity ParticleSystem）：低速漂浮的微光尘埃，
    /// 让战场「空气感」十足——方舟在用粒子烘托页面细节（文章第 7 点）。
    /// 挂在 WorldRoot 下，随重开一并销毁。
    /// </summary>
    public sealed class AmbientDust : MonoBehaviour
    {
        public static void Create(Transform parent)
        {
            var go = new GameObject("AmbientDust");
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0f),
                new Color(0.75f, 0.88f, 1f, 0.20f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(19f, 11f, 0.2f);

            // 明灭：出现→最亮→淡出
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.35f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // Billboard 渲染 + 圆形柔光贴图（最兼容的粒子渲染模式）
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 8;
            var mat = new Material(Shader.Find("Sprites/Default"));
            if (mat != null)
            {
                mat.mainTexture = SpriteFactory.Circle(0.05f, Color.white).texture;
                renderer.sharedMaterial = mat;
            }
        }
    }
}
