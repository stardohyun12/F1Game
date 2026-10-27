// 장면 A7(과제 20261027-2050, D-185) — SceneA3Look 사본에 게임 안 실시간 빛을 붙임.
// _A7Lit 1: URP 주광(방향광) 1개의 방향·색 + 실시간 그림자를 읽어 3단 계단(해 받는 면 / 옆 / 그늘). 그림자 안 = 그늘 단.
//   그늘 색 = 전역 _A7Shade(하늘·안개색 쪽 청록, A7LightToggles 가 넣음). 검정으로 안 떨어짐.
// _A7Lit 0: A3 와 같음(먼 배경·바다처럼 빛이 구워진 판).
// R2 / R3 / 안개 / 색 보정은 A3 그대로.
Shader "PitCrew/SceneA7/Lit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseMapR2 ("R2 texture (32 texel/m)", 2D) = "black" {}
        _R2 ("R2 on", Float) = 0
        _HasR2 ("has R2 texture", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _SunDir ("Sun dir (world, 해 쪽)", Vector) = (0.4, 0.8, -0.45, 0)
        _Ambient ("Ambient (그늘 색)", Color) = (0.8, 0.84, 0.82, 1)
        _Sun ("Sun brightness", Float) = 1.0
        _LightAmt ("Light amount (0 = unlit)", Range(0,1)) = 1
        _Steps ("Shade steps (0 = smooth)", Float) = 0
        _Ramp ("R1 ramp on", Float) = 0
        _R1Ok ("R1 eligible", Float) = 0
        _RampSun ("R1 ramp sun dir", Vector) = (-0.5, 0.75, 0.45, 0)
        _RampT ("R1 thresholds (bright, mid)", Vector) = (0.6, 0.2, 0, 0)
        _RampMid ("R1 mid level", Range(0,1)) = 0.5
        _RampDark ("R1 shade multiplier × fog colour", Range(0,1)) = 0.72
        _Outline ("R3 outline on", Float) = 0
        _R3Ok ("R3 eligible", Float) = 0
        _OutlinePx ("R3 outline px @ render size", Float) = 1
        _OutlineDark ("R3 outline = colour ×", Range(0,1)) = 0.45
        // A3s(과제 1141) 색 보정 — 전부 0 이면 A3 와 같음. 계산은 화면 감마(≈sRGB) 값에서
        _S1 ("S1 highlight rolloff on", Float) = 0
        _S1P ("S1 (knee, shoulder, exposure, -)", Vector) = (0.5, 0.45, 0.95, 0)
        _S2 ("S2 one-light tint on", Float) = 0
        _Warm ("S2 warm (곱)", Color) = (1, 0.9, 0.78, 1)
        _WarmAmt ("S2 warm amount", Range(0,1)) = 0.16
        _ShadeTeal ("S2 shadow tone", Color) = (0.62, 0.78, 0.8, 1)
        _SplitAmt ("S2 shadow tone amount", Range(0,1)) = 0.2
        _S3 ("S3 soft borders on", Float) = 0
        _S3Flat ("S3 lightness pull to mid", Range(0,0.5)) = 0.12
        _Haze ("S3 haze color", Color) = (0.86, 0.84, 0.76, 1)
        _HazeP ("S3 haze (start, end, max, -)", Vector) = (20, 220, 0.22, 0)
        _T1 ("T1 sun-side bright outline on", Float) = 0
        _T2 ("T2 lambert ambient in shade on", Float) = 0
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
        // A6d(과제 1726) 표면 고정 노이즈 — 0 이면 그대로. 물체 좌표 칸(1/_SNDens m)마다 명도만 ±_SN (색상 그대로), 칸이 화면 1px 보다 작아지면 사라짐(지글거림 방지)
        _SN ("A6d surface grain ±", Range(0,0.15)) = 0
        _SNDens ("A6d surface grain cells per m", Float) = 8
        _A7Lit ("A7 real light on", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); TEXTURE2D(_BaseMapR2);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float4 _BaseMapR2_ST; float _R2; float _HasR2; float4 _Tint; float4 _SunDir; float4 _Ambient; float _Sun; float _LightAmt; float _Steps;
            float _Ramp; float _R1Ok; float4 _RampSun; float4 _RampT; float _RampMid; float _RampDark; float _Outline; float _R3Ok; float _OutlinePx; float _OutlineDark;
            float _S1; float4 _S1P; float _S2; float4 _Warm; float _WarmAmt; float4 _ShadeTeal; float _SplitAmt; float _S3; float _S3Flat; float4 _Haze; float4 _HazeP; float _T1; float _T2;
            float4 _FogColor; float _FogStart; float _FogEnd; float _FogMax;
            float _GroundY; float _GradAmt; float _GradH; float _FootDark; float _FootH; float _Blotch; float _BlotchSize; float4 _BlotchHue; float _Cutoff; float _Sat; float4 _WB; float _Lift; float4 _LiftTint;
            float _SN; float _SNDens; float _A7Lit;
        CBUFFER_END
        // A7 전역(A7LightToggles): 그늘 색, 3단 문턱(해 받는 면, 옆), 옆 단 높이
        float4 _A7Shade; float4 _A7T; float _A7Mid;
        half4 SampleBase(float2 uv)
        {
            if (_R2 > 0.5 && _HasR2 > 0.5) return SAMPLE_TEXTURE2D(_BaseMapR2, sampler_BaseMap, uv) * _Tint;
            return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _Tint;
        }
        float3 Fog(float3 c, float3 wp)
        {
            float d = distance(_WorldSpaceCameraPos, wp);
            return lerp(c, _FogColor.rgb, saturate((d - _FogStart) / max(0.01, _FogEnd - _FogStart)) * _FogMax);
        }
        // A3s 색 보정(안개 뒤 마지막). S3 공기층 → S1 어깨 → S3 명도 차 줄임 → S2 한 빛
        float3 Grade(float3 c, float3 wp)
        {
            if (_S1 + _S2 + _S3 < 0.5) return c;
            const float3 W = float3(0.299, 0.587, 0.114);
            float3 g = pow(max(c, 0), 1.0 / 2.2);
            if (_S3 > 0.5) g = lerp(g, _Haze.rgb, saturate((distance(_WorldSpaceCameraPos, wp) - _HazeP.x) / max(1, _HazeP.y - _HazeP.x)) * _HazeP.z);
            float L = max(1e-4, dot(g, W));
            if (_S1 > 0.5)
            {
                float x = L * _S1P.z, k = _S1P.x;
                float L2 = x > k ? k + (x - k) / (1 + (x - k) / _S1P.y) : x;
                g *= L2 / L; L = max(1e-4, L2);
            }
            if (_S3 > 0.5) { float L3 = lerp(L, 0.55, _S3Flat); g *= L3 / L; L = max(1e-4, L3); }
            if (_S2 > 0.5)
            {
                g = lerp(g, g * _Warm.rgb / dot(_Warm.rgb, W), _WarmAmt);                       // 밝기는 두고 색만 크림~살구로
                float sh = saturate(1 - L / 0.5);                                                // 어두운 쪽일수록
                g = lerp(g, L * _ShadeTeal.rgb / dot(_ShadeTeal.rgb, W), sh * _SplitAmt);          // 그늘 = 같은 청록 한 색
                g = min(g, 0.93);                                                                // 순백 없앰
            }
            return pow(max(g, 0), 2.2);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; float3 n : TEXCOORD2; float3 op : TEXCOORD3; };

            V vert(A i)
            {
                V o; o.wp = TransformObjectToWorld(i.pos.xyz); o.pos = TransformWorldToHClip(o.wp);
                o.n = TransformObjectToWorldNormal(i.n); o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                // A6d: 물체에 붙은 미터 좌표(움직이는 카트도 결이 따라감). 면 안쪽으로 칸 0.37 만큼 밀어 칸 경계에 걸친 평면의 깜빡임을 막음
                float3 s = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
                o.op = i.pos.xyz * s * _SNDens - normalize(i.n) * 0.37; return o;
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
                half4 t = SampleBase(i.uv);
                clip(t.a - _Cutoff);
                float3 n = normalize(i.n) * (front ? 1 : -1);
                float ndl = saturate(dot(n, normalize(_SunDir.xyz)));
                if (_Steps > 0.5) ndl = floor(ndl * _Steps + 0.5) / _Steps;
                float3 light = lerp(_Ambient.rgb, _Sun.xxx, ndl);
                float3 c = t.rgb * lerp(1.0.xxx, light, _LightAmt);

                // A7: 실제 해(주광) + 그림자 → 3단. 해 받는 면 = 해 색, 옆 = 해 색과 그늘 색 사이, 그늘·그림자 = 그늘 색
                if (_A7Lit > 0.5)
                {
                    Light ml = GetMainLight(TransformWorldToShadowCoord(i.wp));
                    float k = dot(n, ml.direction);
                    float lv = k > _A7T.x ? 1.0 : (k > _A7T.y ? _A7Mid : 0.0);
                    if (ml.shadowAttenuation < 0.5) lv = 0.0;                                  // 단단한 그림자: 반쯤도 없음
                    c = t.rgb * lerp(_A7Shade.rgb, ml.color, lv);
                }
                // R1: 해 방향 계단 3단. 밝은 단 = 1(텍스처 그대로), 그늘 = 안개색 × _RampDark
                if (_Ramp > 0.5 && _R1Ok > 0.5)
                {
                    float k = dot(n, normalize(_RampSun.xyz));
                    float lv = k > _RampT.x ? 1.0 : (k > _RampT.y ? _RampMid : 0.0);
                    c *= lerp(_FogColor.rgb * _RampDark, 1.0.xxx, lv);
                    if (_T2 > 0.5 && lv < 0.25) c *= n.y > 0.35 ? 1.0 : (n.y > -0.2 ? 0.9 : 0.8);   // T2: 그늘 안에서도 위/옆/아래 면 2단 차
                }

                float h = i.wp.y - _GroundY;
                c *= 1 + _GradAmt * (saturate(h / _GradH) * 2 - 1);
                c *= lerp(1 - _FootDark, 1, smoothstep(0, _FootH, h));
                if (_Blotch > 0)
                {
                    float b = vnoise(i.wp / _BlotchSize) * 0.65 + vnoise(i.wp / (_BlotchSize * 0.42) + 7.3) * 0.35;
                    b = b * 2 - 1;
                    c *= 1 + _Blotch * b;
                    c = lerp(c, c * _BlotchHue.rgb, saturate(b) * _Blotch * 2);
                }
                if (_SN > 0)
                {
                    float px = max(max(fwidth(i.op.x), fwidth(i.op.y)), fwidth(i.op.z));   // 화면 1px 당 칸 수
                    c *= 1 + _SN * (h3(floor(i.op) + 0.5) * 2 - 1) * saturate((1 - px) * 2);
                }
                float lum = dot(c, float3(0.299, 0.587, 0.114));
                c = max(0, lerp(lum.xxx, c, _Sat)) * _WB.rgb;
                c = lerp(c, _LiftTint.rgb, _Lift * saturate(1 - lum * 1.6));
                return half4(Grade(Fog(c, i.wp), i.wp), 1);
            }
            ENDHLSL
        }

        // A7 그림자 드리우기(잎 알파도 잘라서). 빛 구운 판(_A7Lit 0)도 드리운다
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i)
            {
                V o; float3 wp = TransformObjectToWorld(i.pos.xyz); float3 nw = TransformObjectToWorldNormal(i.n);
                float4 cp = TransformWorldToHClip(ApplyShadowBias(wp, nw, _LightDirection));
                #if UNITY_REVERSED_Z
                    cp.z = min(cp.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cp.z = max(cp.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.pos = cp; o.uv = TRANSFORM_TEX(i.uv, _BaseMap); return o;
            }
            half4 frag(V i) : SV_Target { clip(SampleBase(i.uv).a - _Cutoff); return 0; }
            ENDHLSL
        }

        // R3 외곽선: 뒤집은 껍데기. 꺼져 있으면 정점을 화면 밖으로 접어 그리지 않음
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; float sun : TEXCOORD2; };
            V vert(A i)
            {
                V o; o.wp = TransformObjectToWorld(i.pos.xyz); float4 cp = TransformWorldToHClip(o.wp);
                float3 nw = TransformObjectToWorldNormal(i.n); o.sun = dot(normalize(nw), normalize(_RampSun.xyz));
                float3 nc = mul((float3x3)UNITY_MATRIX_VP, nw);
                float2 dir = nc.xy / max(1e-5, length(nc.xy));
                cp.xy += dir * (_OutlinePx * 2.0 / _ScreenParams.xy) * cp.w;
                o.pos = (_Outline > 0.5 && _R3Ok > 0.5) ? cp : float4(0, 0, -2, 1);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap); return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 t = SampleBase(i.uv);
                clip(t.a - _Cutoff);
                float3 c = max(0, lerp(dot(t.rgb, float3(0.299, 0.587, 0.114)).xxx, t.rgb, _Sat * 1.15)) * _WB.rgb * _OutlineDark;
                if (_T1 > 0.5 && i.sun > 0.45) c = min(1, t.rgb * _WB.rgb * 1.35 + 0.04);   // T1(약식): 해를 보는 쪽 실루엣은 밝은 선
                return half4(Grade(Fog(c, i.wp), i.wp), 1);
            }
            ENDHLSL
        }
    }
}
