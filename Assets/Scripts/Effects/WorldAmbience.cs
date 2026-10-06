using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 程序化生成天幕、星野、地平线雾和樱瓣，使用 <see cref="WorldArt"/> 的层序。
    /// </summary>
    public sealed class WorldAmbience : MonoBehaviour
    {
        // 为竖屏和相机漂移留出覆盖余量，同时保留可见区域内的渐变层次。
        private const float SkyWidth = 110f;
        private const float SkyHeight = 34f;
        private const float SkyCenterY = -2f;

        // 雾宽度独立于天幕，使径向光晕的左右收口位于超宽画幅外。
        private const float HazeWidth = 240f;

        private SpriteRenderer _stars;
        private float _time;

        public static void Create(Transform parent)
        {
            var go = new GameObject("WorldAmbience");
            go.transform.SetParent(parent, false);
            go.AddComponent<WorldAmbience>().Build(go.transform);
        }

        private void Build(Transform root)
        {
            BuildSky(root);
            BuildStars(root);
            BuildHaze(root);
            BuildPetals(root);
        }

        private void BuildSky(Transform root)
        {
            var sky = NewSprite(root, "Sky", WorldArt.LayerSky);
            sky.sprite = SpriteFactory.SkyGradient(WorldArt.SkyHigh, WorldArt.SkyMid, WorldArt.SkyLow);
            // SkyGradient 贴图为 16×512px @100ppu，即 0.16 × 5.12 世界单位。
            sky.transform.localScale = new Vector3(SkyWidth / 0.16f, SkyHeight / 5.12f, 1f);
            sky.transform.localPosition = new Vector3(0f, SkyCenterY, 0f);
        }

        private void BuildStars(Transform root)
        {
            _stars = NewSprite(root, "Stars", WorldArt.LayerStars);
            // 密度随平铺次数累加；平铺也能避免拉伸放大单颗星点。
            _stars.sprite = SpriteFactory.StarTile(20260911, 8, 5f);
            _stars.drawMode = SpriteDrawMode.Tiled;
            _stars.tileMode = SpriteTileMode.Continuous;
            _stars.size = new Vector2(SkyWidth, SkyHeight * 1.4f);
            _stars.transform.localPosition = new Vector3(0f, SkyCenterY, 0f);
            _stars.color = new Color(0.86f, 0.91f, 1f, 0.55f);
        }

        private void BuildHaze(Transform root)
        {
            var haze = NewSprite(root, "Haze", WorldArt.LayerHaze);
            haze.sprite = SpriteFactory.Glow(1f, Color.white);
            haze.color = WorldArt.Alpha(WorldArt.Haze, 0.20f);
            haze.transform.localPosition = new Vector3(0f, -5.5f, 0f);
            haze.transform.localScale = new Vector3(HazeWidth, 9f, 1f);
        }

        private void BuildPetals(Transform root)
        {
            var go = new GameObject("SakuraPetals");
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            // Box 默认沿 +Z 发射，关闭初速度，由 velocityOverLifetime 控制下落。
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.13f, 0.27f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new ParticleSystem.MinMaxGradient(
                WorldArt.Alpha(WorldArt.Sakura, 0.55f),
                WorldArt.Alpha(Color.Lerp(WorldArt.Sakura, WorldArt.GoldDust, .35f), 0.30f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 220;

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            // 生成区覆盖战场，避免开场等待花瓣从画面上方落入。
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 15f, 0.2f);
            shape.position = new Vector3(0f, 0.5f, 0f);

            // 三个速度轴统一使用曲线模式，避免粒子系统拒绝混合模式。
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            // 每个粒子的生命周期内完成一次横向摇摆。
            var sway = new AnimationCurve(
                new Keyframe(0f, -1f), new Keyframe(0.5f, 1f), new Keyframe(1f, -1f));
            // 常量曲线使 y/z 与 x 保持同一模式。
            var fall = new AnimationCurve(new Keyframe(0f, -1f), new Keyframe(1f, -1f));

            velocity.x = new ParticleSystem.MinMaxCurve(0.35f, sway);
            velocity.y = new ParticleSystem.MinMaxCurve(0.62f, fall);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, fall);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                        new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.8f)));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = WorldArt.LayerPetals;
            var mat = new Material(Shader.Find("Sprites/Default"));
            if (mat != null)
            {
                mat.mainTexture = SpriteFactory.Petal(0.5f, Color.white).texture;
                renderer.sharedMaterial = mat;
            }
        }

        private void Update()
        {
            if (_stars == null) return;
            _time += Time.deltaTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(_time * 0.35f);
            _stars.color = new Color(0.86f, 0.91f, 1f, 0.42f + 0.20f * breathe);
        }

        private static SpriteRenderer NewSprite(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }
    }
}
