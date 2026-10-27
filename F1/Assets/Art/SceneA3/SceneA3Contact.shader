// 장면 A3 M2 — 물체 밑 접지 어둠(구운 AO 원형 데칼). 곱하기 블렌드, 2단 계단(가운데 진하게·둘레 옅게), 그림자 아님.
Shader "PitCrew/SceneA3/Contact"
{
    Properties
    {
        _Color ("Shade color (곱)", Color) = (0.62, 0.66, 0.6, 1)
        _Inner ("Inner radius (0..1)", Range(0,1)) = 0.55
        _Outer ("Outer radius (0..1)", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend DstColor Zero
            ZWrite Off
            Offset -1, -1
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Inner; float _Outer; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv * 2 - 1);
                float k = r < _Inner ? 1.0 : (r < _Outer ? 0.5 : 0.0);
                return half4(lerp(1.0.xxx, _Color.rgb, k), 1);
            }
            ENDHLSL
        }
    }
}
