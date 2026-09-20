using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Util;

namespace TowerDefense.Effects
{
    /// <summary>
    /// 战场夜色。此前战场浮在一片纯黑里 —— 棋盘像是贴在黑板上的网格，
    /// 这正是它读起来「像工程样品而不像游戏」的根源：没有地平线，没有空气，没有时间。
    ///
    /// 这一层只做四件事，全部在 <see cref="WorldArt"/> 的层序里，绝不碰玩法数据：
    ///   ① 天幕渐变（天顶近黑 → 地平暖灰蓝），给战场一个「地方」；
    ///   ② 星野平铺 + 极慢的明灭，给天空一个「夜晚」；
    ///   ③ 地平线雾，把地面与天空的硬边化掉；
    ///   ④ 飘落樱瓣，给画面一层「正在发生」的空气。
    ///
    /// 全部由 <see cref="SpriteFactory"/> 程序化生成，不引入任何外部美术资源。
    /// </summary>
    public sealed class WorldAmbience : MonoBehaviour
    {
        // 天幕必须同时满足两件事：① 盖住相机漂移后能看到的全部范围（可见区约 33 × 13 世界单位），
        // ② 渐变的三档要落在**可见**的高度里 —— 铺得太高，玩家就只看得到渐变的一小段，
        // 「天顶 → 地平」的层次会整个消失，等于白铺。
        // 高度要按**最高**的那种画幅留量：竖屏相机可视范围接近 20 个世界单位，
        // 铺 22 个单位刚好在顶边露出一条硬边 —— 天空会突然「结束」，比没有天空更糟。
        // 34 个单位覆盖竖屏还留有余量，同时可见区仍占到渐变的 ~60%，三档层次不会丢。
        private const float SkyWidth = 110f;
        private const float SkyHeight = 34f;
        private const float SkyCenterY = -2f;

        // 地平线雾的宽度必须**独立**于天幕。以前它是 SkyWidth * 0.5，在 16:9 上两边刚好出画，
        // 所以没人发现它其实是一块有边界的四边形；相机改成铺满全屏之后，
        // 超宽画幅把它整块露了出来 —— 地平线上浮着一块会突然结束的亮斑。
        // 90 个单位保证在任何画幅下可见的都只是它最亮的中段，看不到边。
        // 240 而不是 90：Glow 的 alpha 是径向衰减的，真正能被看见的只有它最亮的那一段，
        // 90 个单位在超宽画幅下仍然看得到收口。铺到 240 之后可见区落在这块椭圆的中段，
        // 左右两端都远在画外 —— 无论 16:9 还是 32:9 都只剩一条没有起点也没有终点的地平线。
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

        /// <summary>天幕渐变。贴图纵向 512px，直接拉伸到二十几个世界单位仍然平滑。</summary>
        private void BuildSky(Transform root)
        {
            var sky = NewSprite(root, "Sky", WorldArt.LayerSky);
            sky.sprite = SpriteFactory.SkyGradient(WorldArt.SkyHigh, WorldArt.SkyMid, WorldArt.SkyLow);
            // SkyGradient 贴图为 16×512px @100ppu，即 0.16 × 5.12 世界单位。
            sky.transform.localScale = new Vector3(SkyWidth / 0.16f, SkyHeight / 5.12f, 1f);
            sky.transform.localPosition = new Vector3(0f, SkyCenterY, 0f);
        }

        /// <summary>
        /// 星野：四方连续的星点贴图**平铺**（Tiled），靠 alpha 的慢呼吸做出「闪烁」。
        /// 必须是 Tiled —— 拉伸模式会把每颗星一起放大十几倍，星野就成了一片白斑。
        /// </summary>
        private void BuildStars(Transform root)
        {
            _stars = NewSprite(root, "Stars", WorldArt.LayerStars);
            // 密度要按**贴图重复次数**算，不是按每格几颗星：一个 tile 只有 2.56 世界单位，
            // 铺满视野就是几十个 tile 相乘。每格 150 颗 = 屏幕上五万颗星 —— 那不是星空，是雪花噪点。
            // 每格 8 颗 × 约 66 个可见 tile ≈ 500 颗，正好是一片真实的夜空。
            _stars.sprite = SpriteFactory.StarTile(20260911, 8, 5f);
            _stars.drawMode = SpriteDrawMode.Tiled;
            _stars.tileMode = SpriteTileMode.Continuous;
            _stars.size = new Vector2(SkyWidth, SkyHeight * 1.4f);
            _stars.transform.localPosition = new Vector3(0f, SkyCenterY, 0f);
            _stars.color = new Color(0.86f, 0.91f, 1f, 0.55f);
        }

        /// <summary>
        /// 地平线雾：一条压扁的暖色柔光。它的作用不是被看见，而是把战场从「悬浮」里拉回地面 ——
        /// 棋盘下方没有任何过渡时，整块板子看起来像飘在真空里。
        /// </summary>
        private void BuildHaze(Transform root)
        {
            var haze = NewSprite(root, "Haze", WorldArt.LayerHaze);
            haze.sprite = SpriteFactory.Glow(1f, Color.white);
            haze.color = WorldArt.Alpha(WorldArt.Haze, 0.20f);
            haze.transform.localPosition = new Vector3(0f, -5.5f, 0f);
            haze.transform.localScale = new Vector3(HazeWidth, 9f, 1f);
        }

        /// <summary>飘落樱瓣：低速下落 + 横向摇摆，颜色只在暖粉一档里浮动。</summary>
        private void BuildPetals(Transform root)
        {
            var go = new GameObject("SakuraPetals");
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            // Box 形状的默认发射方向是 +Z（朝着相机），靠 startSpeed 根本落不下来 ——
            // 下落完全交给 velocityOverLifetime.y（见下）。
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

            // 生成区覆盖**整个可见战场**，而不是棋盘上方的一条线。
            // 只从顶上撒的话，花瓣要飘十几秒才落到画面里 —— 开场那几秒战场是空的，
            // 之后才「开始下花瓣」，看上去像是中途才想起来要加特效。
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 15f, 0.2f);
            shape.position = new Vector3(0f, 0.5f, 0f);

            // 下落 + 横向摇摆：花瓣不是直直落下的，也不该匀速。
            //
            // 三个轴**必须是同一种模式**。原来这里 x 走曲线、y 走两常量，Unity 会把整条
            // velocityOverLifetime 判为非法并丢弃，同时每帧吐一句
            // 「Particle Velocity curves must all be in the same mode」——
            // 于是 startSpeed=0 的花瓣根本没有速度：它们不落，只是悬在半空原地明灭。
            // 三个轴统一走曲线模式，下落与摇摆才真的发生。
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            // 左右摇摆：一个来回，首尾同相，所以循环点不会跳。
            var sway = new AnimationCurve(
                new Keyframe(0f, -1f), new Keyframe(0.5f, 1f), new Keyframe(1f, -1f));
            // 下落本身是匀速的；快慢差异交给 lifetime（9–16s）去表达，
            // 那比在 y 上做区间随机更容易看出「每片都不一样」。
            var fall = new AnimationCurve(new Keyframe(0f, -1f), new Keyframe(1f, -1f));

            velocity.x = new ParticleSystem.MinMaxCurve(0.35f, sway);
            velocity.y = new ParticleSystem.MinMaxCurve(0.62f, fall);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, fall);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);

            // 出现 → 最亮 → 淡出，保证任何一帧都不存在「突然多出一片花瓣」。
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
            // 极慢的整体明灭。真实星空不会闪，但完全静止的星野看起来就是一张贴图。
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
