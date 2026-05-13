Shader "PitCrew/WrapRibbon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.42, 0.72, 0.91, 1.0)
        _RimColor  ("Rim Color", Color)  = (1, 1, 1, 1)
        _RimPower  ("Rim Power", Range(0.5, 8)) = 2.5
        _EdgeBoost ("Edge Boost (UV.y)", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float  _RimPower;
                float  _EdgeBoost;
            CBUFFER_END

            struct A { float4 pos:POSITION; float3 nrm:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float3 nrmW:TEXCOORD0; float3 posW:TEXCOORD1; float2 uv:TEXCOORD2; };

            V vert(A i)
            {
                V o;
                float3 worldPos = TransformObjectToWorld(i.pos.xyz);
                o.posW = worldPos;
                o.pos = TransformWorldToHClip(worldPos);
                o.nrmW = TransformObjectToWorldNormal(i.nrm);
                o.uv = i.uv;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float3 N = normalize(i.nrmW);
                float3 L = normalize(float3(0.4, 1, 0.3));
                float ndl = saturate(dot(N, L));
                float3 diffuse = _BaseColor.rgb * (0.55 + 0.45 * ndl);

                // 가장자리 (UV.y가 0 또는 1에 가까울수록) 약간 어둡게 — 천 두께 인상
                float edgeY = abs(i.uv.y - 0.5) * 2.0;
                float edgeDark = lerp(1.0, 0.65, edgeY * _EdgeBoost);
                diffuse *= edgeDark;

                // Rim (시야 방향 기반) — 흰 가장자리 = 비닐/광택 느낌
                float3 V = normalize(GetCameraPositionWS() - i.posW);
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float3 col = diffuse + _RimColor.rgb * rim * 0.6;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
