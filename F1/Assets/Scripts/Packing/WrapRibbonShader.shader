Shader "PitCrew/WrapRibbon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.42, 0.72, 0.91, 1.0)
        _RimColor  ("Rim Color", Color)  = (1, 1, 1, 1)
        _RimPower  ("Rim Power", Range(0.5, 8)) = 2.5
        _EdgeBoost ("Edge Boost (UV.y)", Range(0, 1)) = 0.4
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.45
        _SpecPower ("Spec Power", Range(8, 256)) = 80
        _SpecStrength ("Spec Strength", Range(0, 4)) = 1.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
                float  _BaseAlpha;
                float  _SpecPower;
                float  _SpecStrength;
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
                float3 V = normalize(GetCameraPositionWS() - i.posW);
                float3 L = normalize(float3(0.45, 1.0, 0.35));
                float3 H = normalize(L + V);

                float ndl = saturate(dot(N, L));
                float ndv = saturate(dot(N, V));
                float ndh = saturate(dot(N, H));

                // Fresnel — 가장자리(시야 grazing)일수록 불투명/밝음
                float fres = pow(1.0 - ndv, _RimPower);

                // 비닐 광택 — 매우 좁고 강한 specular
                float spec = pow(ndh, _SpecPower) * _SpecStrength;

                // Base color (살짝 디퓨즈 + 환경광)
                float3 base = _BaseColor.rgb * (0.75 + 0.25 * ndl);

                // 가장자리(UV.y) 살짝 어둡게 → 두께
                float edgeY = abs(i.uv.y - 0.5) * 2.0;
                float edgeDark = lerp(1.0, 0.8, edgeY * _EdgeBoost);
                base *= edgeDark;

                // 림 highlight + 비닐 spec 합성
                float3 col = base + _RimColor.rgb * (fres * 0.7 + spec);

                // Alpha — 중앙은 투명, 가장자리(fres)와 specular 부근에서 진해짐
                float alpha = saturate(_BaseAlpha + fres * 0.55 + spec * 0.4);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
