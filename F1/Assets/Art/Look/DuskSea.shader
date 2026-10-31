// D-227/D-232 노을 바다(씬 전용 — Map_TownV6 Sea 가 노을일 때만 이 머티리얼로 바뀐다. 끄면 원래 TV6_Sea).
// 4판(D-232, 도현 "1판 방향에서 예시(A Short Hike)에 더 가깝게"): 2·3판(색 띠·대시)을 버리고 1판 구조에서 다시 출발.
//  · 물결 덩어리 = 1판 그대로(조각 셰이더 노멀 물결 4개 → 하늘 반사 · 해 반사). 다만 나온 밝기를 4단 색 계단으로 자른다
//    (계단 색 = 해에서 _ReflAz 비킨 하늘을 높이별로 반사 — 한 단 = 한 색, 픽셀 경계 또렷). 움직임은 _Wave.w 박자에만.
//  · 해 빛길 = 1판 해 반사(물결이 잘게 빠진 먼 쪽은 넓고 흐림)를 3단(주황·노랑·심)으로 자른 것.
//  · 물결 선 = Roystan toon water: 노이즈를 왜곡 노이즈로 흔든 뒤 한 값의 등고선만 1~2px 선으로(살구색). 가까이 2px · 멀리 1px,
//    노이즈 칸이 화면에서 작아지는 먼 곳은 칸마다 빠져 성기게. 얕은 물에선 굵어짐(문턱 낮춤).
//  · 깊이 = _DepthTex.R(물 밑 깊이) — 얕은 곳 계단 한두 단 밝게 + _Shallow 쪽으로.
//  · 물가 = _DepthTex.G(물가 거리) → 안쪽 흰 테 + 한 칸 띄운 바깥 선(2겹). 경계를 물결 노이즈로 흔들고 천천히 밀려왔다 빠진다.
Shader "PitCrew/Look/DuskSea"
{
    Properties
    {
        _Deep("Deep (반사 밑색)", Color) = (0.20, 0.16, 0.32, 1)
        _StepA("Step reflect amount (0..3)", Vector) = (0.30, 0.48, 0.64, 0.80)
        _StepE("Step sky elevation (0..3)", Vector) = (0.55, 0.36, 0.22, 0.11)
        _Cuts("Step cuts (1, 2, 3)", Vector) = (0.16, 0.30, 0.62, 0)
        _ReflAz("Reflect azimuth off sun (deg)", Vector) = (28, 0, 0, 0)
        _Reflect("Sky reflection", Range(0, 1)) = 0.85
        _Wave("Wave (amp, speed, filter, fps)", Vector) = (0.10, 0.35, 0.35, 12)
        _Shine("Sun glint sharpness", Float) = 600
        _Glint("Sun glint strength", Float) = 1.6
        _GlintCuts("Sun path cuts (outer, hot, core)", Vector) = (0.025, 0.14, 0.55, 0)
        _Glow("Sun path outer", Color) = (0.96, 0.55, 0.40, 1)
        _Hot("Sun path", Color) = (1.00, 0.76, 0.46, 1)
        _Core("Sun path core", Color) = (1.00, 0.93, 0.74, 1)
        _Rip("Ripple (scale 1/m, distort, drift m/s, fps)", Vector) = (0.30, 0.9, 0.05, 6)
        _RipW("Ripple (px near, px far, shallow boost, far cut)", Vector) = (2.0, 1.0, 1.0, 0.16)
        _RipLv("Ripple contour levels (a, b)", Vector) = (0.40, 0.62, 0, 0)
        _RipCol("Ripple line (rgb, mix)", Color) = (1.00, 0.74, 0.70, 0.32)
        _Shallow("Shallow", Color) = (0.70, 0.50, 0.62, 1)
        _ShallowCut("Shallow m (1 step, 2 step, wobble m)", Vector) = (3.5, 1.5, 1.2, 0)
        _Foam("Foam", Color) = (0.98, 0.89, 0.84, 1)
        _FoamW("Foam (inner px, ring px, inner m min, ring m min)", Vector) = (2.0, 6.0, 0.5, 1.8)
        _FoamMax("Foam max (inner px, ring px, far inner m, far ring m)", Vector) = (5, 12, 10, 20)
        _FoamSurge("Foam surge (amount, period s, inner max m, ring max m)", Vector) = (0.35, 11, 6, 14)
        _Sparkle("Sparkle density", Range(0, 1)) = 0.16
        _SparkleRate("Sparkle blink (per sec)", Float) = 0.5
        _DayTint("Day tint", Color) = (0.36, 0.58, 0.70, 1)
        _HorizonFade("Horizon fade start/end m", Vector) = (150, 1100, 0, 0)
        _DepthTex("Sea depth (R = 깊이/최대, G = sqrt(물가 거리/400 m))", 2D) = "white" {}
        _DepthRect("Depth rect (xmin, zmin, size, max m)", Vector) = (-400, -400, 800, 8)
        _Debug("Debug (1 깊이, 2 계단 값)", Float) = 0
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
                float4 _Deep, _Glow, _Hot, _Core, _RipCol, _Shallow, _Foam, _DayTint;
                float4 _StepA, _StepE, _Cuts, _ReflAz, _Wave, _GlintCuts, _Rip, _RipW, _RipLv, _ShallowCut, _FoamW, _FoamMax, _FoamSurge, _HorizonFade, _DepthRect;
                float _Reflect, _Shine, _Glint, _Sparkle, _SparkleRate, _Debug;
            CBUFFER_END
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.wpos = TransformObjectToWorld(v.xyz); o.pos = TransformWorldToHClip(o.wpos); return o; }

            float Lum(float3 c) { return dot(c, float3(.30, .55, .15)); }

            // 계단 k(0 = 짙음 ~ 3 = 밝음) 색: 해에서 비킨 방위 refXZ 의 하늘을 높이 _StepE[k] 에서 읽어 _StepA[k] 만큼 섞는다
            float3 StepCol(float k, float2 refXZ)
            {
                float a = k < .5 ? _StepA.x : (k < 1.5 ? _StepA.y : (k < 2.5 ? _StepA.z : _StepA.w));
                float e = k < .5 ? _StepE.x : (k < 1.5 ? _StepE.y : (k < 2.5 ? _StepE.z : _StepE.w));
                float h = sqrt(1 - e * e);
                return lerp(_Deep.rgb, DuskSkyColor(float3(refXZ.x * h, e, refXZ.y * h), 0), a);
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 p = i.wpos;
                float3 cam = _WorldSpaceCameraPos;
                float3 V = normalize(p - cam);
                float d = distance(p, cam);
                float t = _Time.y;
                float3 sun = _PCDuskSunDir.xyz;
                float2 s2 = normalize(sun.xz + 1e-5), r2 = float2(-s2.y, s2.x);
                float ra = radians(_ReflAz.x);
                float2 refXZ = s2 * cos(ra) + r2 * sin(ra);
                float pixA = 2.0 / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);      // 화면 1px 의 각(RT 렌더는 투영이 뒤집혀 abs)

                // 1) 1판 물결: 노멀 물결 4개. 픽셀 몇 개보다 잘게 접히면 빼고, 뺀 기울기는 해 반사 꼬리로(Toksvig). 시간은 _Wave.w 박자
                float2 fw = max(fwidth(p.xz), 1e-4); float fp = max(fw.x, fw.y);
                const float2 D[4] = { float2(0.94, 0.33), float2(0.37, 0.93), float2(-0.76, 0.65), float2(0.20, -0.98) };
                const float L[4] = { 9.0, 5.3, 3.1, 1.7 };
                float tw = floor(t * _Wave.w) / _Wave.w * _Wave.y;
                float2 slope = 0; float var = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float kk = 6.2832 / L[k];
                    float w = 1 - smoothstep(_Wave.z, _Wave.z * 2, fp / L[k]);
                    float a = _Wave.x * (k == 0 ? 1 : (k == 1 ? .8 : (k == 2 ? .6 : .45)));
                    float ph = kk * dot(D[k], p.xz) - sqrt(9.8 * kk) * tw + k * 1.7;
                    slope += D[k] * a * cos(ph) * w;
                    var += a * a * .5 * (1 - w);
                }
                float3 N = normalize(float3(-slope.x, 1, -slope.y));
                float3 R = reflect(V, N);
                float F = .02 + .98 * pow(1 - saturate(dot(N, -V)), 5);
                float3 skyR = DuskSkyColor(float3(R.x, max(R.y, .002), R.z), 0);
                float3 cv = lerp(_Deep.rgb, skyR, saturate(F * 1.4) * _Reflect);           // 1판 연속 색
                float3 hz = DuskSkyColor(float3(V.x, .001, V.z), 0);
                // 계단 값: 1판 색의 밝기를 지평선 밝기에 견줌(0 짙음 ~ 1 지평선만큼). 멀리는 1판처럼 지평선 쪽으로
                float v = Lum(cv) / max(Lum(hz), 1e-3);
                v = lerp(v, 1, smoothstep(_HorizonFade.x, _HorizonFade.y, d) * .9);

                // 2) 물 밑 깊이·물가 거리
                float2 duv = (p.xz - _DepthRect.xy) / _DepthRect.z;
                float inR = step(0, duv.x) * step(duv.x, 1) * step(0, duv.y) * step(duv.y, 1);
                float4 dt = SAMPLE_TEXTURE2D(_DepthTex, sampler_DepthTex, duv);
                float depth = lerp(_DepthRect.w, dt.r * _DepthRect.w, inR);
                float dist = lerp(400, dt.g * dt.g * 400, inR);                            // 400 = PitCrewSunsetMenu.ShoreDistMax
                if (_Debug > .5 && _Debug < 1.5) return half4(saturate(depth / 8), saturate(dist / 16), inR, 1);
                if (_Debug > 1.5) return half4(v, v, v, 1);

                // 3) 물결 선 노이즈(Roystan): 세상 좌표 노이즈 → 왜곡 노이즈로 흔들고 → 두 값(_RipLv)의 등고선(먼 곳은 한 값만). 박자 _Rip.w
                float tr = floor(t * _Rip.w) / _Rip.w;
                float2 ru = p.xz * _Rip.x;
                float2 dis = float2(dkNoise(ru * .45 + float2(tr * .04, 1.7)), dkNoise(ru * .45 + float2(5.3, -tr * .04))) - .5;
                float2 ru2 = ru + dis * _Rip.y + s2 * (tr * _Rip.z * _Rip.x);
                float rn = dkNoise(ru2) * .7 + dkNoise(ru2 * 2.07 + 7.1) * .3;

                // 계단 나누기 + 얕은 곳 한두 단 위로(경계는 물결 노이즈로 흔듦)
                float kq = step(_Cuts.x, v) + step(_Cuts.y, v) + step(_Cuts.z, v);
                float dW = depth + (rn - .5) * 2 * _ShallowCut.z;
                float up = step(dW, _ShallowCut.x) + step(dW, _ShallowCut.y);
                float3 col = StepCol(min(kq + up, 3), refXZ);
                col = lerp(col, _Shallow.rgb, up * .28);

                // 4) 해 빛길: 1판 해 반사를 3단으로
                float sh = _Shine / (1 + _Shine * var * 6);
                float glint = pow(saturate(dot(R, sun)), sh) * min(1, sh / _Shine * 3) * _Glint;
                float P = glint * (.85 + .3 * rn) * (.3 + .7 * saturate(d / 40));          // 바로 앞 물결의 해 반사는 약하게(큰 얼룩 방지)
                if (P >= _GlintCuts.x) col = P < _GlintCuts.y ? _Glow.rgb : (P < _GlintCuts.z ? _Hot.rgb : _Core.rgb);

                // 5) 물결 선: 등고선 굵기 = 화면 px(가까이 _RipW.x · 멀리 .y, 얕을수록 굵게). 노이즈 칸이 작아지는 먼 곳은 칸마다 빠짐
                float fq = max(fwidth(ru.x), fwidth(ru.y));                                // 노이즈 칸 / px
                float far = saturate((fq - .02) / .10);
                float shK = saturate(1 - depth / _ShallowCut.x);
                float wpx = lerp(_RipW.x, _RipW.y, far) * (1 + _RipW.z * shK);
                float frn = max(fwidth(rn), 1e-4);
                float lineOn = max(step(abs(rn - _RipLv.x), .5 * wpx * frn), step(abs(rn - _RipLv.y), .5 * wpx * frn) * (1 - far));
                float keepR = step(fq, _RipW.w * (.35 + 1.3 * dkNoise(ru * .2 + 3.3)));
                float onPath = step(_GlintCuts.x, P);
                col = lerp(col, lerp(col, onPath > .5 ? _Core.rgb : _RipCol.rgb, _RipCol.a), lineOn * keepR);

                // 6) 물가 2겹: 안쪽 흰 테 + 한 칸 띄운 바깥 선. 1px 가 덮는 물 m 로 px 굵기 환산(3판과 같음), 경계는 물결 노이즈로 흔듦
                float mppH = d * pixA, mppV = d * pixA / max(-V.y, .003);
                float mpp = clamp(fwidth(dist), mppH, mppV);
                float surge = .5 + .5 * sin(floor(t * 4) * .25 * 6.2832 / _FoamSurge.y);
                float farK = saturate((d - 120) / 150);
                float dpx = dist / mpp + (rn - .5) * 3.0 * (1 - farK * .8);
                float sw = 1 + _FoamSurge.x * (surge - .5) * 2;
                float wIn = max(_FoamW.x, _FoamW.z / mpp) * sw;
                float wOut = max(_FoamW.y, _FoamW.w / mpp) * sw;
                wIn = min(wIn, _FoamMax.x * sw); wOut = min(wOut, _FoamMax.y * sw);                 // 바로 앞 부두가 수십 px 로 번지지 않게
                wIn = min(wIn, _FoamSurge.z * sw / mpp); wOut = min(wOut, _FoamSurge.w * sw / mpp);
                // 먼 물가는 1px 테 + 2px 바깥 선, 단 1px 가 물 수십 m 인 수평선 근처는 m 상한(_FoamMax.zw)으로 사라짐(수평선 흰 띠 방지)
                wIn = lerp(wIn, min(1.0, _FoamMax.z / mpp), farK); wOut = lerp(wOut, min(2.0, _FoamMax.w / mpp), farK);
                float ringW = lerp(1.0, .5, farK);
                float isShore = inR * step(dist, 390);                                       // 깊이 칸 밖(먼 바다)은 물가 아님
                dpx = lerp(1e4, dpx, isShore);
                float band = step(dpx, wOut + ringW) * step(.0, dpx);
                col = lerp(col, lerp(col, _Foam.rgb, .30), band);                             // 테두리 안쪽 물은 한 단 밝게
                float ring = step(abs(dpx - wOut), ringW) * (1 - farK * .5);
                float inner = step(dpx, wIn);
                col = lerp(col, lerp(col, _Foam.rgb, .85), ring);
                col = lerp(col, _Foam.rgb, inner);

                // 반짝이: 화면 픽셀 격자 3px 칸 → 칸마다 1px, 빛길 hot 안에서만
                float2 px = floor(i.pos.xy);
                float2 cid = floor(px / 3);
                float h1 = dkHash(cid * .917 + 1.3), h2 = dkHash(cid * 1.31 + 11.7), h3 = dkHash(cid * .77 + 3.3);
                float ph = frac(t * _SparkleRate * (.6 + h2 * .8) + h3);
                float spk = step(ph, .10) * step(1 - _Sparkle, h1) * step(_GlintCuts.y, P);
                float pin = step(abs(px.x - cid.x * 3 - 1), .1) * step(abs(px.y - cid.y * 3 - 1), .1);
                col = lerp(col, _Core.rgb, spk * pin * (1 - band));

                // 수평선: 먼 물은 하늘 지평선 색 한 단으로(계단 1번)
                col = lerp(col, hz, step(_HorizonFade.y, d + (rn - .5) * 120) * .85);
                col = lerp(_DayTint.rgb, col, saturate(_PCDusk.x));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
