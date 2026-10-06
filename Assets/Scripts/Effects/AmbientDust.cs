using UnityEngine;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 低速漂浮的光尘粒子，挂在 WorldRoot 下，随重开销毁。
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

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.35f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

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
