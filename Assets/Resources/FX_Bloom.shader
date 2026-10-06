// Bloom：亮度提取、横纵均值模糊、叠加回原画面。
Shader "TowerDefense/FX/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurTex ("Blur", 2D) = "black" {}
        _Threshold ("Threshold", Range(0, 1)) = 0.70
        _Intensity ("Intensity", Range(0, 4)) = 1.2
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0: Bright Extract
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Threshold;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float b = saturate((lum - _Threshold) / max(1e-4, 1.0 - _Threshold));
                return fixed4(c.rgb * b, 1);
            }
            ENDCG
        }

        Pass // 1: Blur H
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 s = _MainTex_TexelSize.xy;
                fixed4 c = tex2D(_MainTex, i.uv);
                c += tex2D(_MainTex, i.uv + float2(s.x, 0)); c += tex2D(_MainTex, i.uv - float2(s.x, 0));
                c += tex2D(_MainTex, i.uv + float2(s.x * 2, 0)); c += tex2D(_MainTex, i.uv - float2(s.x * 2, 0));
                c += tex2D(_MainTex, i.uv + float2(s.x * 3, 0)); c += tex2D(_MainTex, i.uv - float2(s.x * 3, 0));
                return c / 7.0;
            }
            ENDCG
        }

        Pass // 2: Blur V
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 s = _MainTex_TexelSize.xy;
                fixed4 c = tex2D(_MainTex, i.uv);
                c += tex2D(_MainTex, i.uv + float2(0, s.y)); c += tex2D(_MainTex, i.uv - float2(0, s.y));
                c += tex2D(_MainTex, i.uv + float2(0, s.y * 2)); c += tex2D(_MainTex, i.uv - float2(0, s.y * 2));
                c += tex2D(_MainTex, i.uv + float2(0, s.y * 3)); c += tex2D(_MainTex, i.uv - float2(0, s.y * 3));
                return c / 7.0;
            }
            ENDCG
        }

        Pass // 3: Combine
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BlurTex;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                fixed4 bloom = tex2D(_BlurTex, i.uv);
                return src + bloom * _Intensity;
            }
            ENDCG
        }
    }
}
