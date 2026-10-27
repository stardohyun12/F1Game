// 장면 A6d(과제 1726) 화면 그레인 — 정적 노이즈 텍스처(720 세로 1텍셀)를 화면에 2배 곱하기로 얹는다.
// 결과 = 화면 × (1 + _Amt × (2n − 1)). 명도만 바뀌고 색상은 그대로. 매 프레임 바뀌지 않음(텍스처 고정).
Shader "PitCrew/A6d/Grain"
{
    Properties
    {
        _MainTex ("Noise (linear, 0..1)", 2D) = "gray" {}
        _Amt ("Grain ±", Range(0, 0.15)) = 0.03
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off ZTest Always Cull Off
        Blend DstColor SrcColor
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial) float4 _MainTex_ST; float _Amt; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                return half4((0.5 * (1 + _Amt * (2 * n - 1))).xxx, 1);
            }
            ENDHLSL
        }
    }
}
