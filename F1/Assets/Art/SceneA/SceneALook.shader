// 장면 A 4판 비교(과제 20261027-0429) — 이 씬 재질 전용. 그림자 없음.
// 색 = 텍스처 × (주변광 ↔ 해 밝기, 계단 옵션) × [V3: 세로 그라데이션 · 땅 닿는 곳 어둡게 · 큰 색 얼룩] → 거리 안개.
Shader "PitCrew/SceneA/Look"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _SunDir ("Sun dir (world, 해 쪽)", Vector) = (0.4, 0.8, -0.45, 0)
        _Ambient ("Ambient (그늘 색)", Color) = (0.8, 0.84, 0.82, 1)
        _Sun ("Sun brightness", Float) = 1.0
        _LightAmt ("Light amount (0 = unlit)", Range(0,1)) = 1
        _Steps ("Shade steps (0 = smooth)", Float) = 0
        _FogColor ("Fog color", Color) = (0.8, 0.84, 0.82, 1)
        _FogStart ("Fog start", Float) = 12
        _FogEnd ("Fog end", Float) = 70
        _FogMax ("Fog max", Range(0,1)) = 0.55
        _GroundY ("Ground y", Float) = 0
        _GradAmt ("V3 vertical gradient ±", Range(0,0.5)) = 0
        _GradH ("V3 gradient height (m)", Float) = 4
        _FootDark ("V3 foot darken", Range(0,0.8)) = 0
        _FootH ("V3 foot height (m)", Float) = 0.5
        _Blotch ("V3 blotch amount", Range(0,0.6)) = 0
        _BlotchSize ("V3 blotch size (m)", Float) = 1.2
        _BlotchHue ("V3 blotch warm/cool tint", Color) = (1.06, 0.98, 0.9, 1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _Sat ("A2d saturation (1 = 그대로)", Range(0,3)) = 1
        _WB ("A2d white balance (곱)", Color) = (1,1,1,1)
        _Lift ("A2d shadow lift (0 = 그대로)", Range(0,0.6)) = 0
        _LiftTint ("A2d shadow lift color (그늘 청록)", Color) = (0.55,0.75,0.78,1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _Tint; float4 _SunDir; float4 _Ambient; float _Sun; float _LightAmt; float _Steps;
                float4 _FogColor; float _FogStart; float _FogEnd; float _FogMax;
                float _GroundY; float _GradAmt; float _GradH; float _FootDark; float _FootH; float _Blotch; float _BlotchSize; float4 _BlotchHue; float _Cutoff; float _Sat; float4 _WB; float _Lift; float4 _LiftTint;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; float3 n : TEXCOORD2; };

            V vert(A i)
            {
                V o; o.wp = TransformObjectToWorld(i.pos.xyz); o.pos = TransformWorldToHClip(o.wp);
                o.n = TransformObjectToWorldNormal(i.n); o.uv = TRANSFORM_TEX(i.uv, _BaseMap); return o;
            }

            float h3(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float vnoise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(h3(i), h3(i + float3(1,0,0)), f.x), lerp(h3(i + float3(0,1,0)), h3(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(h3(i + float3(0,0,1)), h3(i + float3(1,0,1)), f.x), lerp(h3(i + float3(0,1,1)), h3(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _Tint;
                clip(t.a - _Cutoff);
                float3 n = normalize(i.n) * (front ? 1 : -1);
                float ndl = saturate(dot(n, normalize(_SunDir.xyz)));
                if (_Steps > 0.5) ndl = floor(ndl * _Steps + 0.5) / _Steps;
                float3 light = lerp(_Ambient.rgb, _Sun.xxx, ndl);
                float3 c = t.rgb * lerp(1.0.xxx, light, _LightAmt);

                float h = i.wp.y - _GroundY;
                c *= 1 + _GradAmt * (saturate(h / _GradH) * 2 - 1);
                c *= lerp(1 - _FootDark, 1, smoothstep(0, _FootH, h));
                if (_Blotch > 0)
                {
                    float b = vnoise(i.wp / _BlotchSize) * 0.65 + vnoise(i.wp / (_BlotchSize * 0.42) + 7.3) * 0.35; // 0.5~2 m 덩어리
                    b = b * 2 - 1;
                    c *= 1 + _Blotch * b;
                    c = lerp(c, c * _BlotchHue.rgb, saturate(b) * _Blotch * 2);
                }

                // A2d 색 보정(과제 1046): 채도 → 화이트밸런스 → 어두운 쪽만 청록으로 들어올림(검게 안 떨어지게). 기본값이면 그대로
                float lum = dot(c, float3(0.299, 0.587, 0.114));
                c = max(0, lerp(lum.xxx, c, _Sat)) * _WB.rgb;
                c = lerp(c, _LiftTint.rgb, _Lift * saturate(1 - lum * 1.6));

                float d = distance(_WorldSpaceCameraPos, i.wp);
                c = lerp(c, _FogColor.rgb, saturate((d - _FogStart) / max(0.01, _FogEnd - _FogStart)) * _FogMax);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
