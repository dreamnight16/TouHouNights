// Dual Kawase 模糊（成熟图形学算法：菱形5点加权，模糊质量/成本比优于普通高斯）
Shader "TowerDefense/FX/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Distance ("Distance", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass // 0: Down（5-point 菱形）
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distance;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * _Distance;
                fixed4 c = tex2D(_MainTex, i.uv);
                c += tex2D(_MainTex, i.uv + float2(o.x, 0)) * 2.0;
                c += tex2D(_MainTex, i.uv - float2(o.x, 0)) * 2.0;
                c += tex2D(_MainTex, i.uv + float2(0, o.y)) * 2.0;
                c += tex2D(_MainTex, i.uv - float2(0, o.y)) * 2.0;
                return c / 9.0;
            }
            ENDCG
        }
        Pass // 1: Up（4-point 加权还原）
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distance;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * _Distance;
                fixed4 c = tex2D(_MainTex, i.uv + float2(-o.x, 0)) * 0.125;
                c += tex2D(_MainTex, i.uv + float2(o.x, 0)) * 0.125;
                c += tex2D(_MainTex, i.uv + float2(0, -o.y)) * 0.125;
                c += tex2D(_MainTex, i.uv + float2(0, o.y)) * 0.125;
                c += tex2D(_MainTex, i.uv) * 0.5;
                return c;
            }
            ENDCG
        }
    }
}
