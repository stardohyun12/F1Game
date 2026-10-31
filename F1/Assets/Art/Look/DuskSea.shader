// D-227/D-232 노을 바다(씬 전용 — Map_TownV6 Sea 가 노을일 때만 이 머티리얼로 바뀐다. 끄면 원래 TV6_Sea).
// 5판(D-232, 도현 "바다 생김새부터 실제 물처럼, 그 위에 스타일"): 4판(평평한 판에 물결·등고선·계단을 다 그림)을 버리고 두 단계로.
//  A. 생김새 = 실제 물: 자리 둘레 반경 _Disp.w m 격자 판(SeaNear, uv.x = 2 로 표시)만 정점을 Gerstner 파 4개로 움직인다
//     (너울 2 + 잔물결 2, 바다 → 해안). 얕을수록 작게(_DepthTex.R). 노멀은 같은 파를 조각마다 다시 계산 → 바깥 판(Sea)과 음영이 이어진다.
//     바깥 판은 원 안을 잘라 내서(clip) 격자와 안 겹친다. 음영 = 프레넬(하늘 반사) + 깊이 흡수색 + 해 정반사(파 노멀로 부서짐) + 물가·마루 거품 조금.
//  B. 스타일(_Style = 1)은 마지막에 얇게: A 최종 색의 밝기를 4단으로 끊고(색조는 그대로), 흰/살구 선은 물가·부두 둘레·파 마루에만, 빛길 3단 + 반짝이 1px.
Shader "PitCrew/Look/DuskSea"
{
    Properties
    {
        _W0("Wave 0 (travel az deg, length m, height m, phase)", Vector) = (8, 31, 0.32, 0)
        _W1("Wave 1", Vector) = (-22, 17, 0.16, 1.7)
        _W2("Wave 2", Vector) = (24, 4.3, 0.035, 3.1)
        _W3("Wave 3", Vector) = (-30, 2.4, 0.018, 4.4)
        _Detail("Normal-only ripples (length a, slope a, length b, slope b)", Vector) = (1.15, 0.05, 0.7, 0.04)
        _Detail2("Normal-only ripples (travel az a, az b)", Vector) = (14, -34, 0, 0)
        _Spec("6판 ② 노멀 전용 파 8개 (on 0/1, 가장 긴 파장 m, 파장 비, 방위 퍼짐 deg)", Vector) = (0, 12, 1.4, 40)
        _Micro("6판 ③ 비주기 세부 노멀 2겹 (on 0/1, 크기 a m, 크기 b m, 기울기)", Vector) = (0, 0.9, 0.35, 0.05)
        _Dash("6판 ⑤ 빛길 가로 대시 (옆 퍼짐 배수 1 = 끔, 0, 0, 0)", Vector) = (1, 0, 0, 0)
        _Gerst("Gerstner (steepness 0..1, speed, fps 0 = 매끈, shallow fade m)", Vector) = (0.55, 1.0, 0, 2.5)
        _Disp("Near grid (on, center x, center z, radius m)", Vector) = (1, -9, -291, 250)
        _Filt("Normal filter (fold start px/len)", Float) = 0.30
        _ShallowCol("Water shallow", Color) = (0.20, 0.50, 0.50, 1)
        _DeepCol("Water deep", Color) = (0.05, 0.08, 0.19, 1)
        _Absorb("Absorb (depth m to deep, view boost)", Vector) = (2.2, 0.6, 0, 0)
        _Reflect("Sky reflection", Range(0, 1)) = 1.0
        _SceneRefl("Scene reflection (amount, distort, 0, 0)", Vector) = (1, 0.06, 0, 0)
        _Shine("Sun glint sharpness", Float) = 900
        _Glint("Sun glint strength", Float) = 2.2
        _Foam("Foam", Color) = (0.98, 0.89, 0.84, 1)
        _FoamA("Foam A (shore m, shore amount, crest start, crest amount)", Vector) = (0.9, 0.80, 0.82, 0.12)
        _HorizonFade("Horizon fade (start m, end m, amount)", Vector) = (300, 650, 0.5, 0)
        [Header(B style)]
        _Style("Style (0 = A 실제 물, 1 = B 계단·선)", Float) = 1
        _Cuts("Step cuts on lum/horizon (1, 2, 3)", Vector) = (0.18, 0.34, 0.62, 0)
        _Lv("Step levels (0..3)", Vector) = (0.10, 0.24, 0.45, 0.80)
        _StepKeep("Step keeps A color", Range(0, 1)) = 0.35
        _StepFps("Step wave beat fps", Float) = 12
        _GlintCuts("Sun path cuts (outer, hot, core)", Vector) = (0.05, 0.25, 0.80, 0)
        _Glow("Sun path outer", Color) = (0.96, 0.55, 0.40, 1)
        _Hot("Sun path", Color) = (1.00, 0.76, 0.46, 1)
        _Core("Sun path core", Color) = (1.00, 0.93, 0.74, 1)
        _Crest("Crest line (level, px, near m, mix)", Vector) = (0.72, 1.0, 70, 0.45)
        _CrestCol("Crest line", Color) = (1.00, 0.78, 0.70, 1)
        _FoamW("Foam (inner px, ring px, inner m min, ring m min)", Vector) = (2.0, 6.0, 0.5, 1.8)
        _FoamMax("Foam max (inner px, ring px, far inner m, far ring m)", Vector) = (4, 8, 10, 20)
        _FoamSurge("Foam surge (amount, period s, inner max m, ring max m)", Vector) = (0.35, 11, 3, 7)
        _Sparkle("Sparkle density", Range(0, 1)) = 0.16
        _SparkleRate("Sparkle blink (per sec)", Float) = 0.5
        _DayTint("Day tint", Color) = (0.36, 0.58, 0.70, 1)
        _DepthTex("Sea depth (R = 깊이/최대, G = sqrt(물가 거리/400 m))", 2D) = "white" {}
        _DepthRect("Depth rect (xmin, zmin, size, max m)", Vector) = (-400, -400, 800, 8)
        _Ripple("D-235 찌 파문 (속도 m/s, 수명 s, 고리 간격 s, 선 굵기 px)", Vector) = (0.42, 1.8, 0.3, 1.3)
        _RippleCol("D-235 파문 선(B)", Color) = (1.00, 0.84, 0.74, 1)
        _Cool("D-236 차가운 물 (섞음 0..1, 밝은 단 덜 0..1, 0, 0)", Vector) = (0, 0.6, 0, 0)
        _CoolCol("D-236 차가운 물 색(청록~남)", Color) = (0.18, 0.34, 0.50, 1)
        _Debug("Debug (1 깊이, 2 계단 값, 3 노멀, 5 수면 높이 wpos.y — SeaWaves 검산)", Float) = 0
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
            TEXTURE2D(_PCSeaRefl); SAMPLER(sampler_PCSeaRefl); float4 _PCSeaReflOn;   // _PCSeaClock(Dusk.hlsl 에 선언).x = 1 → 시간을 y 로 고정(편집 모드 전/후 캡처용).  6판 평면 반사(SeaReflection.cs) — 좌우 뒤집힌 그림
            float4 _PCRipples[4];   // D-235 찌 파문(SeaWaves.AddRipple): x, z, 시작 시각(Now), 세기 0..1(0 = 빈 칸)
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowCol, _DeepCol, _Foam, _Glow, _Hot, _Core, _CrestCol, _DayTint, _Cool, _CoolCol;
                float4 _W0, _W1, _W2, _W3, _Detail, _Detail2, _Spec, _Micro, _Dash, _Gerst, _Disp, _Absorb, _FoamA, _HorizonFade, _SceneRefl;
                float4 _Cuts, _Lv, _GlintCuts, _Crest, _FoamW, _FoamMax, _FoamSurge, _DepthRect, _DepthTex_ST, _Ripple, _RippleCol;
                float _StepKeep, _StepFps, _Filt, _Reflect, _Shine, _Glint, _Style, _Sparkle, _SparkleRate, _Debug;
            CBUFFER_END
            struct appdata { float4 v : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 xz0 : TEXCOORD1; };   // xz0 = 움직이기 전 xz, z = 격자 여부

            float Lum(float3 c) { return dot(c, float3(.30, .55, .15)); }
            // 파 시간. B(계단)는 _StepFps 박자로만 움직임 — 매끈하게 움직이면 계단 경계 픽셀이 매 프레임 오락가락(5판 실측 깜빡임 450배)
            float Now() { return _PCSeaClock.x > .5 ? _PCSeaClock.y : _Time.y; }
            float WaveTime() { float t = Now(), fps = _Style > .5 ? _StepFps : _Gerst.z; if (fps > .5) t = floor(t * fps) / fps; return t * _Gerst.y; }
            float4 WaveP(int i) { return i == 0 ? _W0 : (i == 1 ? _W1 : (i == 2 ? _W2 : _W3)); }
            float2 WaveDir(float az) { float a = radians(az); return float2(sin(a), cos(a)); }   // 0 = +z(북 = 해안 쪽), 90 = +x

            // 물 밑 깊이(m)·물가 거리(m). 깊이 칸 밖 = 먼 바다(최대 깊이)
            float2 DepthDist(float2 xz, float lod)
            {
                float2 duv = (xz - _DepthRect.xy) / _DepthRect.z;
                float inR = step(0, duv.x) * step(duv.x, 1) * step(0, duv.y) * step(duv.y, 1);
                float4 dt = SAMPLE_TEXTURE2D_LOD(_DepthTex, sampler_DepthTex, duv, lod);
                return float2(lerp(_DepthRect.w, dt.r * _DepthRect.w, inR), lerp(400, dt.g * dt.g * 400, inR));   // 400 = PitCrewSunsetMenu.ShoreDistMax
            }
            float ShallowAtt(float depth) { return saturate(depth / _Gerst.w) * .85 + .15 * saturate(depth / (_Gerst.w * .3)); }

            // D-235 찌 파문: 파문마다 고리 2~3겹, 고리 i 반경 = 속도 × (나이 − i × 간격). 노멀엔 고리 모양 작은 둔덕(m 단위),
            // 선은 화면 px 굵기(fwidth) → 360 줄에서도 1~2 px 로 또렷. 나이는 B 면 파와 같은 _StepFps 박자(매끄럽게 움직이면 선 픽셀이 오락가락).
            float Ripples(float2 xz, inout float3 N)
            {
                float t = Now(), fps = _Style > .5 ? _StepFps : _Gerst.z; if (fps > .5) t = floor(t * fps) / fps;
                float ln = 0;
                [unroll] for (int r = 0; r < 4; r++)
                {
                    float4 rp = _PCRipples[r];
                    float2 dv = xz - rp.xy; float rr = length(dv) + 1e-4, fr = max(fwidth(rr), 1e-4);
                    float age = t - rp.z, life = _Ripple.y * (.75 + .25 * rp.w);
                    float fade = step(1e-3, rp.w) * step(0, age) * step(age, life) * rp.w * pow(saturate(1 - age / life), 1.5);
                    float rings = rp.w > .7 ? 3 : 2;
                    [unroll] for (int q = 0; q < 3; q++)
                    {
                        float aq = age - q * _Ripple.z;
                        float R = _Ripple.x * (.6 + .4 * rp.w) * max(aq, 0);
                        float g = step(0, aq) * step(q + .5, rings) * (1 - q * .25) * fade;
                        float w = .05 + .04 * R, u = (rr - R) / w;
                        N.xz -= (dv / rr) * (-2 * u / w * exp(-u * u) * .012 * g);
                        ln = max(ln, g * step(abs(rr - R) / fr, .5 * _Ripple.w));
                    }
                }
                return ln;
            }

            v2f vert(appdata a)
            {
                v2f o;
                float3 p = TransformObjectToWorld(a.v.xyz);
                float isNear = step(1.5, a.uv.x);
                o.xz0 = float3(p.xz, isNear);
                if (isNear > .5 && _Disp.x > .5)
                {
                    // Gerstner: 정점 간격(중심에서 멀수록 넓음, 고리 비 ≈1.054)보다 파장이 짧으면 그 파는 뺀다 — 격자가 파를 못 그리면 지글거림
                    float r = distance(p.xz, _Disp.yz);
                    float spacing = max(.054 * r, .02);
                    float edge = 1 - smoothstep(_Disp.w * .7, _Disp.w * .97, r);
                    float att = ShallowAtt(DepthDist(p.xz, 0).x) * edge;
                    float t = WaveTime();
                    float3 dsp = 0;
                    [unroll] for (int i = 0; i < 4; i++)
                    {
                        float4 w = WaveP(i); float2 D = WaveDir(w.x);
                        float k = 6.2832 / w.y, A = w.z * att * (1 - smoothstep(.12, .25, spacing / w.y));
                        float Q = _Gerst.x / max(k * w.z * 4, 1e-4);
                        float ph = k * dot(D, p.xz) - sqrt(9.8 * k) * t + w.w;
                        float c = cos(ph), s = sin(ph);
                        dsp += float3(Q * A * D.x * c, A * s, Q * A * D.y * c);
                    }
                    p += dsp;
                }
                o.wpos = p; o.pos = TransformWorldToHClip(p);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 xz = i.xz0.xy;
                // 격자 판과 바깥 판의 경계: 격자는 원 밖을, 바깥 판은 원 안을 버림(노을 바다를 쓰는 두 렌더러만)
                float rc = distance(xz, _Disp.yz);
                if (_Disp.x > .5) clip(i.xz0.z > .5 ? _Disp.w - rc : rc - _Disp.w);
                if (_Debug > 4.5) return half4(i.wpos.y, 0, 0, 1);

                float3 p = i.wpos;
                float3 cam = _WorldSpaceCameraPos;
                float3 V = normalize(p - cam);
                float d = distance(p, cam);
                float t = WaveTime();
                float3 sun = _PCDuskSunDir.xyz;
                float2 dd = DepthDist(xz, 0);
                float depth = dd.x, dist = dd.y;
                float att = ShallowAtt(depth);

                // 1) 파 노멀(정점과 같은 Gerstner 4개 + 노멀만 잔물결 2개). 화면 몇 px 보다 잘게 접히는 파는 빼고 그 기울기는 해 반사 퍼짐으로(Toksvig)
                float2 fw = max(fwidth(xz), 1e-4); float fp = max(fw.x, fw.y);
                float3 N = float3(0, 1, 0); float var = 0, crest = 0, crestA = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float4 w = WaveP(k); float2 D = WaveDir(w.x);
                    float kk = 6.2832 / w.y, A = w.z * att;
                    float Q = _Gerst.x / max(kk * w.z * 4, 1e-4);
                    float ph = kk * dot(D, xz) - sqrt(9.8 * kk) * t + w.w;
                    float f = 1 - smoothstep(_Filt, _Filt * 2, fp / w.y);
                    float c = cos(ph), s = sin(ph);
                    N.xz -= D * kk * A * c * f;
                    N.y -= Q * kk * A * s * f;
                    var += (kk * A) * (kk * A) * .5 * (1 - f);
                    if (k < 2) { crest += w.z * s; crestA += w.z; }
                }
                [unroll] for (int j = 0; j < 2; j++)
                {
                    float L = j == 0 ? _Detail.x : _Detail.z, S = (j == 0 ? _Detail.y : _Detail.w) * att;
                    float2 D = WaveDir(j == 0 ? _Detail2.x : _Detail2.y);
                    float kk = 6.2832 / L;
                    float ph = kk * dot(D, xz) - sqrt(9.8 * kk) * t + 2.3 * j;
                    float f = 1 - smoothstep(_Filt, _Filt * 2, fp / L);
                    N.xz -= D * S * cos(ph) * f;
                    var += S * S * .5 * (1 - f);
                }
                // 6판 ②: 노멀 전용 파 8개 — 파장 _Spec.y 부터 _Spec.z 배씩 짧게, 방위는 ±_Spec.w 안에 흩뿌림(한 방향 원뿔 → 교차하는 잔파)
                if (_Spec.x > .5)
                {
                    [unroll] for (int q = 0; q < 8; q++)
                    {
                        float L = _Spec.y * pow(abs(_Spec.z), -q), S = .04 * att;
                        float hq = frac(sin(q * 12.9898 + 4.1) * 43758.5453) * 2 - 1;
                        float2 D = WaveDir(hq * _Spec.w);
                        float kk = 6.2832 / L, ph = kk * dot(D, xz) - sqrt(9.8 * kk) * t + 1.7 * q + .4;
                        float f = 1 - smoothstep(_Filt, _Filt * 2, fp / L);
                        N.xz -= D * S * cos(ph) * f;
                        var += S * S * .5 * (1 - f);
                    }
                }
                // 6판 ③: 비주기 세부 노멀 2겹 — 값 노이즈 기울기(중앙 차분), 서로 다른 회전·크기·흐름. 사인 합의 반복 무늬를 깸
                if (_Micro.x > .5)
                {
                    [unroll] for (int m = 0; m < 2; m++)
                    {
                        float sc = m == 0 ? _Micro.y : _Micro.z, an = radians(m == 0 ? 35 : -20);
                        float2x2 Rm = float2x2(cos(an), -sin(an), sin(an), cos(an));
                        float2 qn = mul(Rm, xz) / sc + float2(0, t * sqrt(9.8 * sc / 6.2832) / sc) + m * 17.3;
                        const float h = .25;
                        float2 g = float2(dkNoise(qn + float2(h, 0)) - dkNoise(qn - float2(h, 0)), dkNoise(qn + float2(0, h)) - dkNoise(qn - float2(0, h))) / (2 * h);
                        float S = _Micro.w * att / 1.5;
                        float f = 1 - smoothstep(_Filt, _Filt * 2, fp / sc);
                        N.xz -= mul(transpose(Rm), g) * S * f;
                        var += S * S * 1.1 * (1 - f);
                    }
                }
                float rip = Ripples(xz, N);
                N = normalize(N);
                crest =crest / max(crestA, 1e-4) * .5 + .5;                              // 너울 마루 0..1
                if (_Debug > 2.5 && _Debug < 3.5) return half4(N * .5 + .5, 1);
                if (_Debug > .5 && _Debug < 1.5) return half4(saturate(depth / 8), saturate(dist / 16), att, 1);

                // 2) 음영: 프레넬로 물 색 ↔ 하늘 반사. 물 색 = 깊이 흡수(얕음 밝은 청록 → 깊음 짙은 남), 비스듬히 볼수록 물을 길게 지나 짙게
                float NoV = saturate(dot(N, -V));
                float F = .02 + .98 * pow(1 - NoV, 5);
                float3 R = reflect(V, N);
                float3 skyR = DuskSkyColor(float3(R.x, max(R.y, .01), R.z), 0);
                // 6판 ①: 장면 반사(해안 나무·땅·부두·구름). 파 노멀만큼 화면에서 밀어 읽음 → 잔물결에 세로로 찢김
                float2 suv = GetNormalizedScreenSpaceUV(i.pos);
                float2 ruv = float2(1 - suv.x, suv.y) + float2(-N.x, N.z) * _SceneRefl.y;
                float4 refl = SAMPLE_TEXTURE2D(_PCSeaRefl, sampler_PCSeaRefl, saturate(ruv));
                if (_Debug > 3.5 && _Debug < 4.5) return half4(refl.rgb, 1);
                // 반사 RT 는 물체만(투명 바탕, a = 물체 있음) → 하늘 자리는 파 노멀을 그대로 따르는 DuskSkyColor 를 지킴(구름 반사 지글거림도 없음)
                skyR = lerp(skyR, refl.rgb, saturate(refl.a * _PCSeaReflOn.x * _SceneRefl.x));
                float path = depth * (1 + _Absorb.y * (1 / max(-V.y, .05) - 1) * .1);
                float3 body = lerp(_ShallowCol.rgb, _DeepCol.rgb, 1 - exp(-path / _Absorb.x));
                float3 col = lerp(body, skyR, saturate(F * _Reflect));
                float3 hz = DuskSkyColor(float3(V.x, .001, V.z), 0);
                col = lerp(col, hz, smoothstep(_HorizonFade.x, _HorizonFade.y, d) * _HorizonFade.z);

                // 3) 해 정반사: 파 노멀로 부서지는 빛길. 잘게 빠진 파만큼 넓고 흐리게
                float sh = _Shine / (1 + _Shine * var * 4);
                float P = pow(saturate(dot(R, sun)), sh);
                // 6판 ⑤: 옆(수평)으로만 _Dash.x 배 넓게 퍼지는 해 반사 → 파 면 하나가 가로로 긴 대시로 비침
                if (_Dash.x > 1.01)
                {
                    float3 sd = normalize(float3(sun.z, 0, -sun.x)), su = cross(sun, sd);
                    float dx = dot(R, sd), dy = dot(R, su);
                    P = step(0, dot(R, sun)) * exp(-.5 * sh * (dx * dx / _Dash.x + dy * dy));
                }
                P *= min(1, sh / _Shine * 2.5) * _Glint;

                // 4) 거품 조금: 물가(흔들리는 띠) + 너울 마루(노이즈로 듬성)
                float n1 = dkNoise(xz * .45 + float2(t * .05, 0)), n2 = dkNoise(xz * 1.3 - float2(0, t * .08));
                float fwS = _FoamA.x * (.6 + .8 * n1);
                float shoreF = (1 - smoothstep(fwS * .7, fwS, dist)) * step(dist, 390);   // 좁고 또렷한 흰 띠(넓게 번지면 회색 얼룩)
                float crestF = smoothstep(_FoamA.z, 1, crest) * smoothstep(.55, .8, n2) * (1 - smoothstep(60, 140, d));

                float lumHz = max(Lum(hz), 1e-3);
                if (_Debug > 1.5) { float v = Lum(col) / lumHz; return half4(v, v, v, 1); }

                if (_Style < .5)
                {
                    // A: 매끈한 실제 물
                    col += _PCDuskSunCol.rgb * P;
                    col = lerp(col, _Foam.rgb, saturate(shoreF * _FoamA.y + crestF * _FoamA.w));
                    col = lerp(col, _RippleCol.rgb, rip * .35);
                }
                else
                {
                    // B: 밝기 4단(색조는 A 그대로 — 단 하나의 밝기로 맞춤)
                    // 단 색 = 단마다 정한 색(짙은 물 ↔ 해에서 비킨 하늘 높이별 반사)에 A 색을 _StepKeep 만큼만 남김 → 단 안은 거의 한 색
                    float L = max(Lum(col), 1e-4), v = L / lumHz;
                    float kq = step(_Cuts.x, v) + step(_Cuts.y, v) + step(_Cuts.z, v);
                    float lv = kq < .5 ? _Lv.x : (kq < 1.5 ? _Lv.y : (kq < 2.5 ? _Lv.z : _Lv.w));
                    float e = kq < .5 ? .55 : (kq < 1.5 ? .30 : (kq < 2.5 ? .15 : .05));
                    float2 s2 = normalize(sun.xz + 1e-5), r2 = float2(-s2.y, s2.x), refXZ = s2 * cos(.49) + r2 * sin(.49);
                    float3 pal = lerp(_DeepCol.rgb, DuskSkyColor(float3(refXZ.x * sqrt(1 - e * e), e, refXZ.y * sqrt(1 - e * e)), 0), saturate(kq / 3 * .8 + .2));
                    float3 aq = col * (lv * lumHz / L);
                    col = lerp(pal * (lv * lumHz / max(Lum(pal), 1e-4)), aq, _StepKeep);
                    // D-236 7판: 빛길 밖 물은 하늘보다 한 단 차갑게(청록~남) — 따뜻한 하늘·빛길 ↔ 차가운 물. 밝은 단일수록 덜
                    float3 coolM = _CoolCol.rgb * (Lum(col) / max(Lum(_CoolCol.rgb), 1e-4));
                    col = lerp(col, coolM, _Cool.x * saturate(1 - kq / 3 * _Cool.y));
                    // 빛길 3단
                    if (P >= _GlintCuts.x) col = P < _GlintCuts.y ? _Glow.rgb : (P < _GlintCuts.z ? _Hot.rgb : _Core.rgb);
                    // 파 마루 선: 너울 마루 높이 한 값의 등고선 1px, 가까이만, 노이즈로 끊어 짧은 선으로
                    float fc = max(fwidth(crest), 1e-4);
                    float cl = step(abs(crest - _Crest.x), .5 * _Crest.y * fc) * step(.45, n1) * (1 - smoothstep(_Crest.z * .6, _Crest.z, d)) * step(.5, att);
                    col = lerp(col, lerp(col, _CrestCol.rgb, _Crest.w), cl);
                    // D-235 찌 파문 선 1~2 px. 옅게 섞으면 360 줄 1× 에서 안 읽힘(첫 실측 혼합 10%대) → 바닥 .4 + 세기·나이로 .85 까지
                    col = lerp(col, _RippleCol.rgb, step(.01, rip) * min(.85, .4 + rip * .9));
                    // 물가 2겹(4판과 같음): 안쪽 흰 테 + 한 칸 띄운 바깥 선. 1px 가 덮는 물 m 로 px 굵기 환산
                    float pixA = 2.0 / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);
                    float mppH = d * pixA, mppV = d * pixA / max(-V.y, .003);
                    float mpp = clamp(fwidth(dist), mppH, mppV);
                    float surge = .5 + .5 * sin(floor(Now() * 4) * .25 * 6.2832 / _FoamSurge.y);
                    float farK = saturate((d - 120) / 150);
                    float dpx = dist / mpp + (n1 - .5) * 3.0 * (1 - farK * .8);
                    float sw = 1 + _FoamSurge.x * (surge - .5) * 2;
                    float wIn = min(max(_FoamW.x, _FoamW.z / mpp) * sw, _FoamMax.x * sw);
                    float wOut = min(max(_FoamW.y, _FoamW.w / mpp) * sw, _FoamMax.y * sw);
                    wIn = min(wIn, _FoamSurge.z * sw / mpp); wOut = min(wOut, _FoamSurge.w * sw / mpp);
                    wIn = lerp(wIn, min(1.0, _FoamMax.z / mpp), farK); wOut = lerp(wOut, min(2.0, _FoamMax.w / mpp), farK);
                    float ringW = lerp(1.0, .5, farK);
                    dpx = lerp(1e4, dpx, step(dist, 390));
                    float band = step(dpx, wOut + ringW) * step(.0, dpx);
                    col = lerp(col, lerp(col, _Foam.rgb, .30), band);
                    col = lerp(col, lerp(col, _Foam.rgb, .85), step(abs(dpx - wOut), ringW) * (1 - farK * .5));
                    col = lerp(col, _Foam.rgb, step(dpx, wIn));
                    // 반짝이: 화면 3px 칸마다 1px, 빛길 hot 안에서만
                    float2 px = floor(i.pos.xy), cid = floor(px / 3);
                    float h1 = dkHash(cid * .917 + 1.3), h2 = dkHash(cid * 1.31 + 11.7), h3 = dkHash(cid * .77 + 3.3);
                    float ph = frac(Now() * _SparkleRate * (.6 + h2 * .8) + h3);
                    float spk = step(ph, .10) * step(1 - _Sparkle, h1) * step(_GlintCuts.y, P);
                    float pin = step(abs(px.x - cid.x * 3 - 1), .1) * step(abs(px.y - cid.y * 3 - 1), .1);
                    col = lerp(col, _Core.rgb, spk * pin * (1 - band));
                }
                col = lerp(_DayTint.rgb, col, saturate(_PCDusk.x));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
