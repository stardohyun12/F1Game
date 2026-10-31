// D-227/D-229 노을 바다(씬 전용 — Map_TownV6 Sea 가 노을일 때만 이 머티리얼로 바뀐다. 끄면 원래 TV6_Sea).
// 2판(D-229, 도현 "물결에서 도트 느낌이 하나도 안 남 · A Short Hike 같은 물"): 부드러운 노멀 반사를 버리고 평평한 색 띠로 다시.
//  · 색 = 띠 5단(깊음·중간·밝음·빛길·빛길 심) 중 하나. 띠 값 = 수평선 쪽으로 갈수록 + 해 빛길 + 느린 물결 노이즈 → 계단으로 자른다.
//    중간 색이 없으니 360 화면에서 띠 경계가 픽셀 계단으로 또렷하다.
//  · 물결 무늬 = 노이즈 등고선(1px 선, 밝은 색). 픽셀보다 촘촘해지는 먼 곳에서는 선을 뺀다(지글거림 방지).
//  · 물가 거품 = 물 밑 깊이(_DepthTex, 메뉴 PitCrew/노을/물 밑 깊이 굽기 — 자리 둘레 320 m 를 레이로 구움) → 흰 띠 2줄(물가 테 + 밀려오는 한 줄)
//    + 얕은 물 밝은 띠. 가장자리는 느린 노이즈로 흔든다. 해안·부두·방파제 둘레 모두. (카메라 깊이 텍스처는 이 RT 카메라에서 비어 나와 안 씀 — 2판 실측)
//  · 반짝이 = 화면 픽셀 격자(3px 칸)에 붙은 1px 점, 빛길 안에서만, 칸마다 제 박자로 반짝 켜졌다 꺼짐.
Shader "PitCrew/Look/DuskSea"
{
    Properties
    {
        _Deep("Deep", Color) = (0.17, 0.13, 0.31, 1)
        _Mid("Mid", Color) = (0.36, 0.23, 0.43, 1)
        _Light("Light", Color) = (0.78, 0.44, 0.47, 1)
        _Hot("Sun path", Color) = (1.00, 0.72, 0.45, 1)
        _Core("Sun path core", Color) = (1.00, 0.93, 0.74, 1)
        _Line("Ripple line", Color) = (0.27, 0.20, 0.40, 1)
        _Shallow("Shallow", Color) = (0.42, 0.36, 0.58, 1)
        _Foam("Foam", Color) = (0.98, 0.89, 0.84, 1)
        _Bands("Band cuts (mid, light, hot, core)", Vector) = (0.30, 0.56, 0.78, 0.97)
        _PathWidth("Sun path width (rad², near/far)", Vector) = (0.012, 0.008, 0, 0)
        _RippleScale("Ripple scale (across, along sun, speed)", Vector) = (0.10, 0.45, 0.12, 0)
        _FoamDepth("Foam depth m (rim, line, shallow)", Vector) = (0.9, 1.6, 2.8, 0)
        _Sparkle("Sparkle density", Range(0, 1)) = 0.16
        _SparkleRate("Sparkle blink (per sec)", Float) = 0.5
        _DayTint("Day tint", Color) = (0.36, 0.58, 0.70, 1)
        _HorizonFade("Horizon band start/end m", Vector) = (220, 1100, 0, 0)
        _DepthTex("Sea depth (R = 깊이/최대, 메뉴 PitCrew/노을/물 밑 깊이 굽기)", 2D) = "white" {}
        _DepthRect("Depth rect (xmin, zmin, size, max m)", Vector) = (-400, -400, 800, 8)
        [Toggle] _Debug("Debug depth", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Art/Look/Dusk.hlsl"
            TEXTURE2D(_DepthTex); SAMPLER(sampler_DepthTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Deep, _Mid, _Light, _Hot, _Core, _Line, _Shallow, _Foam, _DayTint;
                float4 _Bands, _PathWidth, _RippleScale, _FoamDepth, _HorizonFade, _DepthRect;
                float _Sparkle, _SparkleRate, _Debug;
            CBUFFER_END
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.wpos = TransformObjectToWorld(v.xyz); o.pos = TransformWorldToHClip(o.wpos); return o; }

            half4 frag(v2f i) : SV_Target
            {
                float3 p = i.wpos;
                float3 cam = _WorldSpaceCameraPos;
                float3 V = normalize(p - cam);
                float d = distance(p, cam);
                float t = _Time.y;
                float3 sun = _PCDuskSunDir.xyz;
                float2 s2 = normalize(sun.xz + 1e-5), r2 = float2(-s2.y, s2.x);

                // 물결 노이즈: 해와 직각으로 길게(낮은 눈높이에서 가로 줄무늬가 된다). 픽셀보다 잘게 접히면 뺀다
                float2 q = float2(dot(p.xz, r2) * _RippleScale.x, dot(p.xz, s2) * _RippleScale.y + t * _RippleScale.z);
                float fq = max(fwidth(q.x), fwidth(q.y));
                float keep = 1 - smoothstep(.25, .6, fq);
                float n = dkNoise(q) * .65 + dkNoise(q * 2.1 + float2(5.2, t * .05)) * .35;

                // 띠 값: 수평선 쪽(시선이 눕는 만큼) + 해 빛길 + 물결
                float g = saturate(1 - (-V.y) / .22);                                     // 발밑 0 → 수평선 1
                float2 vxz = normalize(V.xz + 1e-5);
                float daz = acos(clamp(dot(vxz, s2), -1, 1));
                float dz = daz * (1 + (n - .5) * 1.2);                                   // 빛길 가장자리를 물결로 끊는다(가로 토막)
                float path = exp(-dz * dz / lerp(_PathWidth.x, _PathWidth.y, g * g));
                float L = g * .62 + path * (.10 + .55 * g) + (n - .5) * (.40 * keep + .10);
                float3 col = L < _Bands.x ? _Deep.rgb : (L < _Bands.y ? _Mid.rgb : (L < _Bands.z ? _Light.rgb : (L < _Bands.w ? _Hot.rgb : _Core.rgb)));

                // 물결 등고선 1px(가까운 물에서만 — 먼 곳은 선이 픽셀보다 촘촘해진다)
                float iso = n * 7.0;
                float fi = max(fwidth(iso), 1e-4);
                // 선 굵기 ≈2px. 선 사이가 8px 보다 좁아지는 곳(낮은 눈높이에서 멀리 눌린 물)은 평행 줄무늬가 되므로 긋지 않는다
                float ln = step(abs(frac(iso) - .5), fi) * keep * step(fi, .125);
                col = lerp(col, L < _Bands.y ? _Line.rgb * 1.35 : _Hot.rgb, ln * step(L, _Bands.z));

                // 물 밑 바닥까지 수직 깊이(m) → 얕은 띠·거품 2줄. 가장자리는 느린 노이즈로 흔든다
                float2 duv = (p.xz - _DepthRect.xy) / _DepthRect.z;
                float inR = step(0, duv.x) * step(duv.x, 1) * step(0, duv.y) * step(duv.y, 1);
                float depth = lerp(_DepthRect.w, SAMPLE_TEXTURE2D(_DepthTex, sampler_DepthTex, duv).r * _DepthRect.w, inR);
                if (_Debug > .5) return half4(saturate(depth / 8), frac(depth), inR, 1);
                float wob = (dkNoise(p.xz * .45 + float2(t * .07, -t * .05)) - .5) * .5 * _FoamDepth.x;
                float dd = depth + wob;
                float surge = frac(t * .09);                                                // 밀려오는 줄: 바깥에서 물가로
                float lineD = lerp(_FoamDepth.y * 1.6, _FoamDepth.x * 1.2, surge);
                float shallow = step(dd, _FoamDepth.z);
                float rim = step(dd, _FoamDepth.x);
                float wave = step(abs(dd - lineD), .09 + .05 * _FoamDepth.x) * (1 - smoothstep(.75, 1, surge));
                col = lerp(col, lerp(_Shallow.rgb, col, step(_Bands.z, L)), shallow * (1 - rim));
                col = lerp(col, _Foam.rgb, saturate(rim + wave) * step(0, depth + .05));

                // 반짝이: 화면 픽셀 격자 3px 칸 → 칸마다 1px, 빛길 안에서만, 반짝 켜졌다 꺼짐
                float2 px = floor(i.pos.xy);
                float2 cid = floor(px / 3);
                float h1 = dkHash(cid * .917 + 1.3), h2 = dkHash(cid * 1.31 + 11.7), h3 = dkHash(cid * .77 + 3.3);
                float ph = frac(t * _SparkleRate * (.6 + h2 * .8) + h3);
                float on = step(ph, .10) * step(1 - _Sparkle, h1) * step(.35, path) * step(_Bands.y, L);
                float pin = step(abs(px.x - cid.x * 3 - 1), .1) * step(abs(px.y - cid.y * 3 - 1), .1);
                col = lerp(col, _Core.rgb, on * pin * (1 - rim));

                // 수평선: 먼 물은 하늘 지평선 색 한 단으로(그라데이션 없이 계단 1번)
                float3 hz = DuskSkyColor(float3(V.x, .001, V.z), 0);
                col = lerp(col, hz, step(_HorizonFade.y, d + (n - .5) * 120) * .85);
                col = lerp(_DayTint.rgb, col, saturate(_PCDusk.x));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
