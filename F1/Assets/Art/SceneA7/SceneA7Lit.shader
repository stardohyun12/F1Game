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
            float _SN; float _SNDens; float _A7Lit; float4 _BaseMap_TexelSize;
        CBUFFER_END
        // A7 전역(A7LightToggles): 그늘 색, 3단 문턱(해 받는 면, 옆), 옆 단 높이
        float4 _A7Shade; float4 _A7T; float _A7Mid;
        // 실내 도트(과제 20261104-0501, A7ShadeGlobals 가 넣음): 방 상자 안 조각만 바다 화면과 같은 문법으로.
        //  _A7InBox = 월드 → 상자(-1..1) 행렬 · _A7InP = (켬, 밝기 단 수, 근거리 안개 최대, 안개 끝 m)
        //  ① 텍스처 걸러 읽기(텍셀 네모·나무결·벽 얼룩 없앰) ② 빛 3단(해 자국 / 면 방향 2단) ③ 근거리 안개(그늘색) ④ 밝기 n단 + Bayer 4×4 디더(렌더 픽셀 고정)
        //  _A7InQ = (거르는 탭 간격 텍셀, 국소 평균 쪽 대비 줄임 0..1, 단 끝 디더 폭 0..1, 단 자를 때 결 빼기 0..1)
        float4x4 _A7InBox; float4 _A7InP; float4 _A7InQ;
        // 디오라마(과제 20261104-1559, D-298, RoomDiorama 가 저해상 카메라에만 넣음 — 기본 0 = 아무 일 없음):
        //  _A7Dio = (켬, 해 세기 배수(1 − 안개), 카메라 쪽 벽 버리는 문턱(상자 좌표), 천장 버리는 문턱) · _A7DioAmb = (실내 바탕빛 rgb — 시간대별, 그림자에서 지붕을 빼는 집 범위(상자 좌표))
        //  켜지면 방 상자 밖·천장·카메라 쪽 벽을 버린다(배경 검정 — 과제 20261104-2033 「창 밖은 다시 검정」).
        //  상자 안은 해(그림자 포함) × 배수 + 시간대 바탕빛 + 추가 광원(책상 전등)
        float4 _A7Dio; float4 _A7DioAmb;
        // 과제 20261105-0630: 디오라마 텍스처 읽기 _A7DioTex = (방식 0 점 그대로 / 1 걸러 읽기 / 2 밉 바이어스 +2, 탭 간격 텍셀, 화면 픽셀 크기 배수, 면 결(_SN) 빼기 0..1)
        //  실내 텍스처는 밉맵 없음·Point(실측 mips=1) — 5~12 m 에서 화면 1픽셀에 텍셀 여러 개가 들어 픽셀마다 다른 잔점이 찍힌다. 1 = 화면 픽셀이 덮는 텍셀 폭만큼 넓혀 걸러 읽음(밉 대신)
        float4 _A7DioTex;
        // 그림자 패스용: 집 범위 안 천장 위(지붕·천장판)는 그림자를 안 드리운다 — 그림에서 자른 지붕이 그림자로 남아 방을 덮던 것(과제 20261104-2033 원인)
        bool A7DioShadowCut(float3 wp)
        {
            if (_A7Dio.x < 0.5 || _A7InP.x < 0.5) return false;
            float3 b = mul(_A7InBox, float4(wp, 1)).xyz;
            return max(abs(b.x), abs(b.z)) <= _A7DioAmb.w && b.y > _A7Dio.w;
        }
        SAMPLER(sampler_linear_repeat);
        bool A7Inside(float3 wp)
        {
            if (_A7InP.x < 0.5) return false;
            float3 b = abs(mul(_A7InBox, float4(wp, 1)).xyz);
            return max(b.x, max(b.y, b.z)) <= 1;
        }
        bool A7DioCut(float3 wp)
        {
            if (_A7Dio.x < 0.5 || _A7InP.x < 0.5) return false;
            float3 b = mul(_A7InBox, float4(wp, 1)).xyz;
            float3 c = mul(_A7InBox, float4(_WorldSpaceCameraPos, 1)).xyz;
            if (max(abs(b.x), max(abs(b.y), abs(b.z))) > 1 || b.y > _A7Dio.w) return true;
            if (abs(c.x) > 1 && b.x * sign(c.x) > _A7Dio.z) return true;
            if (abs(c.z) > 1 && b.z * sign(c.z) > _A7Dio.z) return true;
            return false;
        }
        // ① 바이리니어 9탭(1-2-1, 간격 _A7InQ.x 텍셀) → 국소 평균(±2.5간격) 쪽으로 대비 _A7InQ.y 만큼 줄임. 큰 모양(문·포스터 그림)은 남고 1텍셀 결은 사라진다
        static float a7InK = 1;   // 넓은 평균 / 이 픽셀 텍스처 밝기(선형). 외곽선 패스는 1
        half3 A7InTex(float2 uv)
        {
            float2 st = _BaseMap_TexelSize.xy * _A7InQ.x;
            half3 c = 0;
            [unroll] for (int y = -1; y <= 1; y++) [unroll] for (int x = -1; x <= 1; x++)
                c += (x == 0 ? 2 : 1) * (y == 0 ? 2 : 1) * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_linear_repeat, uv + float2(x, y) * st, 0).rgb;
            c /= 16;
            half3 m = 0;
            [unroll] for (int k = 0; k < 4; k++) m += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_linear_repeat, uv + float2(k < 2 ? -2.5 : 2.5, k % 2 ? -2.5 : 2.5) * st, 0).rgb;
            c = lerp(c, m * 0.25, _A7InQ.y);
            // 단을 자를 밝기는 텍스처 결이 아니라 넓은 평균(밉 _A7InQ.w) 기준 — 결이 단 문턱에 걸려 벽 전체가 얼룩지는 것 막기
            half3 wide = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_linear_repeat, uv, 3).rgb;
            float3 lw = float3(0.299, 0.587, 0.114);
            a7InK = clamp(max(1e-4, dot(wide, lw)) / max(1e-4, dot(c, lw)), 0.25, 4);
            return c * _Tint.rgb;
        }
        // 디오라마 _A7DioTex.x = 1: 탭 간격 = max(고정 텍셀, 화면 픽셀이 덮는 텍셀 × 배수) — 1-2-1 9탭이 픽셀 한 칸 넓이를 평균. 큰 얼룩(여러 픽셀)은 남는다
        // fp = 화면 1픽셀이 덮는 텍셀 수(분기 밖에서 ddx/ddy 로 잼 — 분기 안 미분은 정의 안 됨)
        half3 A7DioTex(float2 uv, float fp)
        {
            float2 st = _BaseMap_TexelSize.xy * max(_A7DioTex.y, fp * _A7DioTex.z);
            half3 c = 0;
            [unroll] for (int y = -1; y <= 1; y++) [unroll] for (int x = -1; x <= 1; x++)
                c += (x == 0 ? 2 : 1) * (y == 0 ? 2 : 1) * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_linear_repeat, uv + float2(x, y) * st, 0).rgb;
            c /= 16;
            half3 m = 0;
            [unroll] for (int k = 0; k < 4; k++) m += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_linear_repeat, uv + float2(k < 2 ? -2.5 : 2.5, k % 2 ? -2.5 : 2.5) * st, 0).rgb;
            float3 lw = float3(0.299, 0.587, 0.114);
            a7InK = clamp(max(1e-4, dot(m * 0.25, lw)) / max(1e-4, dot(c, lw)), 0.25, 4);   // 단 자를 때 결 빼기(_A7InQ.w)가 디오라마에서도 먹게
            return c * _Tint.rgb;
        }
        float A7InBayer2(float2 a) { a = floor(a); return frac(a.x * .5 + a.y * a.y * .75); }
        float A7InBayer4(float2 a) { return A7InBayer2(.5 * a) * .25 + A7InBayer2(a); }
        // ③④ 마지막(화면 감마 값): 근거리 안개 → 밝기 n단(디더). 색상은 그대로, 밝기만 자른다
        float3 A7InPost(float3 lin, float3 wp, float2 spos)
        {
            float d = distance(_WorldSpaceCameraPos, wp);
            if (_A7Dio.x < 0.5) lin = lerp(lin, _A7Shade.rgb * 0.95, saturate((d - 0.6) / max(0.5, _A7InP.w - 0.6)) * _A7InP.z);   // 디오라마: 밝은 근거리 안개 없음(어두운 방)
            float3 g = pow(max(lin, 0), 1.0 / 2.2);
            float L = max(1e-4, dot(g, float3(0.299, 0.587, 0.114)));
            float n = max(2, _A7InP.y);
            // 하늘(Dusk.hlsl dkStep)처럼 단 끝 _A7InQ.z 폭만 디더 — 면 전체가 체커가 되지 않게
            // 자를 밝기 = 텍스처 결을 _A7InQ.w 만큼 빼고 본 밝기(0 = 결째로 자름 → 얼룩이 단으로 뭉개짐, 1 = 빛·안개만 자르고 결은 비율로 되얹음)
            float Ls = L * pow(a7InK, _A7InQ.w / 2.2);
            float v = Ls * n, fi = floor(v), f = v - fi, w = max(1e-3, _A7InQ.z);
            float Lq = (fi + step(A7InBayer4(spos), (f - (1 - w)) / w)) / n;
            g *= max(Lq, 0.5 / n) / Ls;
            return pow(max(g, 0), 2.2);
        }
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
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
                float2 uvDx = ddx(i.uv), uvDy = ddy(i.uv);
                half4 t = SampleBase(i.uv);
                clip(t.a - _Cutoff);
                if (A7DioCut(i.wp)) discard;
                bool inside = A7Inside(i.wp);
                bool dio = inside && _A7Dio.x > 0.5;
                if (inside && !dio) t.rgb = A7InTex(i.uv);   // 디오라마는 재질 텍스처(Point) 그대로 — 걸러 읽지 않음
                else if (dio && _A7DioTex.x > 1.5) t.rgb = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, i.uv, uvDx * 4, uvDy * 4).rgb * _Tint.rgb;   // 측정용: 밉 바이어스 +2(밉 없으면 그대로)
                else if (dio && _A7DioTex.x > 0.5) t.rgb = A7DioTex(i.uv, max(length(uvDx * _BaseMap_TexelSize.zw), length(uvDy * _BaseMap_TexelSize.zw)));
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
                    // 실내 ②: 해 자국이 아닌 곳(그늘 한 색)을 면 방향 3단으로 — 바닥 = 밝은 단, 해 쪽(창 쪽)을 보는 벽 = 가운데 단, 천장·등진 벽 = 어두운 단. 지금 해 방향에서 매 프레임 계산
                    if (inside && lv < 0.5)
                    {
                        float2 sxz = normalize(ml.direction.xz + 1e-4), nxz = n.xz / max(1e-4, length(n.xz));
                        float face = n.y > 0.5 ? 1.0 : (n.y < -0.5 ? 0.0 : (dot(nxz, sxz) > -0.2 ? 0.5 : 0.0));
                        c *= face > 0.75 ? 1.16 : (face > 0.25 ? 1.06 : 0.84);   // 1.06 = 벽 결이 밝기 단 문턱에 안 걸리는 값(0.96 은 벽 전체가 디더 얼룩, 실측)
                    }
                }
                // 디오라마: 해(창으로 들어온 빛 — 단단한 그림자, 2단) × (1 − 안개) + 약한 바탕빛 + 추가 광원(책상 전등). 전등은 4단 계단(garage A7LitPoint 와 같은 식)
                if (dio)
                {
                    Light sl = GetMainLight(TransformWorldToShadowCoord(i.wp));
                    float sk = dot(n, sl.direction);
                    float sv = (sk > 0.3 ? 1.0 : (sk > 0.02 ? 0.6 : 0.0)) * step(0.5, sl.shadowAttenuation) * _A7Dio.y;
                    c = t.rgb * (_A7DioAmb.rgb + sl.color * sv);
                    InputData inputData = (InputData)0; inputData.positionWS = i.wp; inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light al = GetAdditionalLight(lightIndex, i.wp);
                        float k = max(max(al.color.r, al.color.g), max(al.color.b, 1e-4));
                        float a = al.distanceAttenuation * saturate(dot(n, al.direction)) * k;
                        a = floor(a * 4 + 0.35) / 4;
                        c += t.rgb * (al.color / k) * a;
                    LIGHT_LOOP_END
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
                    c *= 1 + _SN * (dio ? 1 - _A7DioTex.w : 1) * (h3(floor(i.op) + 0.5) * 2 - 1) * saturate((1 - px) * 2);
                }
                float lum = dot(c, float3(0.299, 0.587, 0.114));
                c = max(0, lerp(lum.xxx, c, _Sat)) * _WB.rgb;
                c = lerp(c, _LiftTint.rgb, _Lift * saturate(1 - lum * 1.6));
                c = Grade(Fog(c, i.wp), i.wp);
                if (inside) c = A7InPost(c, i.wp, i.pos.xy);
                return half4(c, 1);
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
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; };
            V vert(A i)
            {
                V o; float3 wp = TransformObjectToWorld(i.pos.xyz); float3 nw = TransformObjectToWorldNormal(i.n);
                float4 cp = TransformWorldToHClip(ApplyShadowBias(wp, nw, _LightDirection));
                #if UNITY_REVERSED_Z
                    cp.z = min(cp.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cp.z = max(cp.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.pos = cp; o.uv = TRANSFORM_TEX(i.uv, _BaseMap); o.wp = wp; return o;
            }
            half4 frag(V i) : SV_Target { clip(SampleBase(i.uv).a - _Cutoff); if (A7DioShadowCut(i.wp)) discard; return 0; }
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
                if (A7DioCut(i.wp)) discard;
                float3 c = max(0, lerp(dot(t.rgb, float3(0.299, 0.587, 0.114)).xxx, t.rgb, _Sat * 1.15)) * _WB.rgb * _OutlineDark;
                if (_T1 > 0.5 && i.sun > 0.45) c = min(1, t.rgb * _WB.rgb * 1.35 + 0.04);   // T1(약식): 해를 보는 쪽 실루엣은 밝은 선
                c = Grade(Fog(c, i.wp), i.wp);
                if (A7Inside(i.wp)) c = A7InPost(c, i.wp, i.pos.xy);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
