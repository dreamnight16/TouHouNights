// 程序化亚克力 UI 材质（Fluent Acrylic + 明日方舟边缘光）
Shader "TowerDefense/UI/Acrylic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BlurTex ("Blurred Screen", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.75
        _TopLight ("Top Light Strength", Range(0, 1)) = 0.30
        _BottomShadow ("Bottom Shadow Strength", Range(0, 1)) = 0.34
        _NoiseAmount ("Acrylic Noise Amount", Range(0, 1)) = 0.02
        _EdgeGlow ("Edge Glow Color", Color) = (0.55, 0.88, 0.95, 0.55)
        _EdgeGain ("Edge Glow Gain", Float) = 5.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BlurTex;
            float4 _MainTex_TexelSize;
            float _BlurStrength;
            float _TopLight;
            float _BottomShadow;
            float _NoiseAmount;
            float4 _EdgeGlow;
            float _EdgeGain;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 objXY : TEXCOORD1;   // 矩形局部坐标（世界空间一致的噪点密度）
                float4 screenPos : TEXCOORD2; // 屏幕坐标（用于采样模糊背景）
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord = v.texcoord;
                o.objXY = v.vertex.xy;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                half a = tex.a * i.color.a;
                half3 base = tex.rgb * i.color.rgb;

                // 0) 真·毛玻璃：混合模糊后的屏幕背景（一帧拖后半帧无感，UI 渲染在相机后）
                half2 screenUV = i.screenPos.xy / i.screenPos.w;
                fixed4 blur = tex2D(_BlurTex, screenUV);
                half blurBlend = _BlurStrength * a;
                base = lerp(base, blur.rgb, blurBlend * 0.55);
                half bgLum = dot(blur.rgb, half3(0.299, 0.587, 0.114));
                base = lerp(base, base * (0.72 + 0.28 * bgLum), blurBlend); // 环境亮度也参与面板明暗

                // 1) 纵向曲面受光：顶部冷光 + 底部内阴影（幂曲线，细腻不脏）
                half y = saturate(i.texcoord.y);
                half light = 1.0 + _TopLight * pow(1.0 - y, 3.0)
                                   - _BottomShadow * 0.6 * pow(y, 2.0);

                // 2) 亚克力磨砂：世界空间一致的亮度噪点（颗粒 1~2 物理像素级，不脏）
                half n = frac(sin(dot(i.objXY * 1.3, half2(12.9898, 78.233))) * 43758.5453);
                light *= 1.0 + (n - 0.5) * _NoiseAmount;

                // 3) alpha 边缘发光：梯度法线 → 细亮边沿（斜切角/描边自动发光）
                half2 t = _MainTex_TexelSize.xy;
                half dAx = tex2D(_MainTex, i.texcoord + half2(t.x, 0)).a
                         - tex2D(_MainTex, i.texcoord - half2(t.x, 0)).a;
                half dAy = tex2D(_MainTex, i.texcoord + half2(0, t.y)).a
                         - tex2D(_MainTex, i.texcoord - half2(0, t.y)).a;
                half edge = saturate(length(half2(dAx, dAy)) * _EdgeGain);

                half3 col = base * light + _EdgeGlow.rgb * _EdgeGlow.a * edge;
                return half4(col, a);
            }
        ENDCG
        }
    }

    Fallback "UI/Default"
}
