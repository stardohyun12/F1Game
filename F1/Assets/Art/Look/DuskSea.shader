// D-227/D-229/D-230 노을 바다(씬 전용 — Map_TownV6 Sea 가 노을일 때만 이 머티리얼로 바뀐다. 끄면 원래 TV6_Sea).
// 2판(D-229, 도현 "A Short Hike 같은 물"): 부드러운 노멀 반사를 버리고 평평한 색 띠로.
// 3판(D-230, 도현 "물 자체 질감이 조금 부족"): 띠 안이 단색 한 덩어리 → 셋을 더한다.
//  · 띠 색 = 거리 띠마다 하늘 반사(가까운 띠 = 하늘 위쪽 보라, 먼 띠 = 지평선 주황·분홍) — 띠 하나에 색 하나라 계단은 그대로.
//    해 빛길은 주황 → 노랑 → 심 3단으로 넓게.
//  · 물결 대시 = 화면 픽셀 단위 짧은 가로 획(가까이 6px · 멀리 2px, 멀수록 촘촘). 시선 방향(눈 위치 고정)에 붙여 둘러봐도 안 미끄러진다.
//    줄(2px)마다 제 속도로 1px 씩 흐르고, 켜짐·꺼짐은 두 겹 노이즈가 천천히 정한다(격자 반복 티 방지). 색은 띠보다 한 단 밝은/어두운 두 색만.
//  · 물가 거품 = 물가까지 거리(_DepthTex.G, 메뉴 PitCrew/노을/물 밑 깊이 굽기) → 바깥 연한 띠 + 안쪽 흰 줄. 굵기는 화면 픽셀 기준(먼 물가도 1.5px·4px)
//    이고 가까운 곳은 m 하한. 천천히 밀려왔다 빠진다(굵기가 숨쉼). 해안·부두 기둥·방파제 공통.
//  · 반짝이 = 화면 픽셀 격자(3px 칸)에 붙은 1px 점, 빛길 안에서만.
Shader "PitCrew/Look/DuskSea"
{
    Properties
    {
        _Deep("Deep (반사 밑색)", Color) = (0.22, 0.18, 0.40, 1)
        _Mid("Mid", Color) = (0.36, 0.23, 0.43, 1)
        _Light("Light", Color) = (0.78, 0.44, 0.47, 1)
        _Glow("Sun path outer", Color) = (0.96, 0.55, 0.40, 1)
        _Hot("Sun path", Color) = (1.00, 0.76, 0.46, 1)
        _Core("Sun path core", Color) = (1.00, 0.93, 0.74, 1)
        _Line("Ripple line", Color) = (0.27, 0.20, 0.40, 1)
        _Shallow("Shallow", Color) = (0.42, 0.36, 0.58, 1)
        _Foam("Foam", Color) = (0.98, 0.89, 0.84, 1)
        _Bands("Band cuts (mid, light, hot, core)", Vector) = (0.30, 0.56, 0.78, 0.97)
        _SkyBands("Sky bands (count, near elev, far elev, reflect min)", Vector) = (5, 0.55, 0.02, 0.40)
        _ReflAz("Reflect azimuth off sun (deg), reflect max", Vector) = (28, 0.92, 0, 0)
        _PathCuts("Sun path cuts (outer, hot, core)", Vector) = (0.22, 0.50, 0.82, 0)
        _PathWidth("Sun path width (rad², near/far)", Vector) = (0.020, 0.030, 0, 0)
        _RippleScale("Ripple scale (across, along sun, speed)", Vector) = (0.10, 0.45, 0.12, 0)
        _Dash("Dash (row px, density near, density far, speed px/s)", Vector) = (2, 0.30, 0.55, 1.0)
        _DashLen("Dash (len near px, len far px, cell near px, cell far px)", Vector) = (7, 2, 18, 6)
        _FoamDepth("Foam depth m (rim, line, shallow)", Vector) = (0.9, 1.6, 2.8, 0)
        _FoamW("Foam width (inner px, outer px, inner m min, outer m min)", Vector) = (2.0, 5.0, 0.5, 1.6)
        _FoamSurge("Foam surge (amount, period s, inner max m, outer max m)", Vector) = (0.45, 11, 6, 14)
        _Sparkle("Sparkle density", Range(0, 1)) = 0.16
        _SparkleRate("Sparkle blink (per sec)", Float) = 0.5
        _DayTint("Day tint", Color) = (0.36, 0.58, 0.70, 1)
        _HorizonFade("Horizon band start/end m", Vector) = (220, 1100, 0, 0)
        _DepthTex("Sea depth (R = 깊이/최대, G = sqrt(물가 거리/400 m))", 2D) = "white" {}
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
                float4 _Deep, _Mid, _Light, _Glow, _Hot, _Core, _Line, _Shallow, _Foam, _DayTint;
                float4 _Bands, _SkyBands, _ReflAz, _PathCuts, _PathWidth, _RippleScale, _Dash, _DashLen, _FoamDepth, _FoamW, _FoamSurge, _HorizonFade, _DepthRect;
                float _Sparkle, _SparkleRate, _Debug;
            CBUFFER_END
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.wpos = TransformObjectToWorld(v.xyz); o.pos = TransformWorldToHClip(o.wpos); return o; }

            // 거리 띠 k(0 = 발밑 ~ count-1 = 수평선)의 색: 그 띠가 비추는 하늘 높이의 색을 물 밑색에 섞는다. k<0 / k>=count 는 한 단 어둡게/밝게
            float3 BandCol(float k, float2 refXZ)
            {
                float cnt = _SkyBands.x;
                float kc = clamp(k, 0, cnt - 1);
                float bc = (kc + .5) / cnt;
                float e = lerp(_SkyBands.y, _SkyBands.z, bc);
                float3 sky = DuskSkyColor(float3(refXZ.x * sqrt(1 - e * e), e, refXZ.y * sqrt(1 - e * e)), 0);
                float3 c = lerp(_Deep.rgb, sky, lerp(_SkyBands.w, _ReflAz.y, bc));
                if (k < 0) c = lerp(_Deep.rgb, c, .72);
                if (k > cnt - 1) c = lerp(c, sky, .6) * 1.08;
                return c;
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
                // 반사에 쓰는 방위: 해에서 _ReflAz.x 도 비켜난 한 방위로 고정(화면 가로로 색이 번지지 않게 — 띠 하나 = 색 하나)
                float ra = radians(_ReflAz.x);
                float2 refXZ = s2 * cos(ra) + r2 * sin(ra);
                // 화면 1px 의 각(라디안) — 360 줄 렌더 기준
                float pixA = 2.0 / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);   // RT 렌더는 투영이 위아래로 뒤집혀 음수가 된다

                // 느린 물결 노이즈(띠 경계를 흔든다): 해와 직각으로 길게. 픽셀보다 잘게 접히면 줄인다
                float2 q = float2(dot(p.xz, r2) * _RippleScale.x, dot(p.xz, s2) * _RippleScale.y + t * _RippleScale.z);
                float fq = max(fwidth(q.x), fwidth(q.y));
                float keep = 1 - smoothstep(.25, .6, fq);
                float n = dkNoise(q) * .65 + dkNoise(q * 2.1 + float2(5.2, t * .05)) * .35;

                // 시선 내림각 → 화면 줄(_Dash.x px). 눈 위치가 고정이면 세상에 붙는다(둘러봐도 안 미끄러짐)
                float dep = -V.y / max(length(V.xz), 1e-4);
                float rowPx = _Dash.x;
                float row = floor(dep / (pixA * rowPx));
                float ax = atan2(V.x, V.z) / pixA;                                       // 방위각 → 가로 px

                // 1) 거리 띠(하늘 반사): 수평선 1 → 화면 아래 0. sqrt 로 수평선 쪽 띠를 얇게, 아래로 갈수록 두껍게. 물결로 경계를 흔들어 count 단
                float gb = 1 - sqrt(saturate(-V.y / .5));
                float g = saturate(1 - (-V.y) / .22);
                float cnt = _SkyBands.x;
                float Ls = saturate(gb + (n - .5) * (.16 * keep + .05));
                float k = min(floor(Ls * cnt), cnt - 1);
                float3 col = BandCol(k, refXZ);
                float3 colUp = lerp(col, BandCol(k + 1, refXZ), .7) + col * .10, colDn = lerp(col, _Deep.rgb * .78, .50);

                // 해 빛길: 주황 → 노랑 → 심. 가장자리를 줄(2px) 단위 가로 토막으로 끊는다. 가까울수록 약하게(먼 쪽이 밝다)
                float2 vxz = normalize(V.xz + 1e-5);
                float daz = acos(clamp(dot(vxz, s2), -1, 1));
                float nP = dkNoise(float2(ax * .05 + row * 3.1, row * .45 + t * .08)) * .7 + n * .3;
                float dz = daz * (1 + (nP - .5) * 1.8);
                float path = exp(-dz * dz / lerp(_PathWidth.x, _PathWidth.y, gb));
                float P = path * (.30 + .70 * gb) + (nP - .5) * .22;
                float inPath = step(_PathCuts.x, P);
                if (P >= _PathCuts.x)
                {
                    float3 pc = P < _PathCuts.y ? _Glow.rgb : (P < _PathCuts.z ? _Hot.rgb : _Core.rgb);
                    colDn = P < _PathCuts.y ? lerp(col, _Glow.rgb, .45) : (P < _PathCuts.z ? _Glow.rgb : _Hot.rgb);
                    colUp = P < _PathCuts.y ? _Hot.rgb : _Core.rgb;
                    col = pc;
                }

                // 2) 물결 대시: 줄마다 제 속도로 1px 씩 흐르고, 가까이 길고(_DashLen.x) 멀리 짧고 촘촘(_DashLen.y)
                float gRow = 1 - sqrt(saturate(((row + .5) * pixA * rowPx) / .5));       // 줄 하나의 먼 정도(줄 안에서 일정)
                float hr = dkHash(float2(row * .731, 7.13)), hr2 = dkHash(float2(row * 1.37, 2.91));
                float spd = _Dash.w * lerp(.5, 1.5, hr) * lerp(1, .45, gRow);           // px/s, 멀수록 느리게
                float shift = floor(floor(t * 4) * .25 * spd + hr * 53);                // 0.25초 박자에만 움직임(연속 프레임 두 번 이동 방지)
                float x = floor(ax) + shift;
                float W = floor(lerp(_DashLen.z, _DashLen.w, gRow));
                float D = max(1, floor(lerp(_DashLen.x, _DashLen.y, gRow) + .5));
                float ci = floor(x / W), fx = x - ci * W;
                float hc = dkHash(float2(ci * .173 + row * 3.71, row * .917 + 1.7));
                float off = floor(hc * (W - D + 1));
                float seg = step(off, fx) * step(fx, off + D - 1);
                // 켜짐: 세상에 붙은 칸 중심(흐름 뺀 위치)에서 두 겹 노이즈(크기·방향 다름) → 천천히 바뀐다
                float cx = ((ci + .5) * W - shift) * pixA;
                float2 nc = float2(cx * 9.0, row * pixA * rowPx * 60.0);
                float nn = dkNoise(nc + float2(t * .035, 0)) * .6 + dkNoise(float2(nc.x * 2.3 + nc.y * 1.1, nc.y * 2.7 - nc.x * .7) + float2(3.7, -t * .05)) * .4;
                float dens = lerp(_Dash.y, _Dash.z, gRow);
                float on = seg * step(1 - dens, nn * .55 + hc * .45) * step(.25, hr2);
                float lite = step(.5, dkHash(float2(ci * .61 + 4.1, row * .29)) * .5 + nn * .5);
                col = lerp(col, lite > .5 ? colUp : colDn, on);

                // 3) 물 밑 깊이·물가 거리 → 얕은 띠 + 거품 2단(바깥 연한 띠 + 안쪽 흰 줄)
                float2 duv = (p.xz - _DepthRect.xy) / _DepthRect.z;
                float inR = step(0, duv.x) * step(duv.x, 1) * step(0, duv.y) * step(duv.y, 1);
                float4 dt = SAMPLE_TEXTURE2D(_DepthTex, sampler_DepthTex, duv);
                float depth = lerp(_DepthRect.w, dt.r * _DepthRect.w, inR);
                float dist = lerp(400, dt.g * dt.g * 400, inR);                            // 400 = PitCrewSunsetMenu.ShoreDistMax
                if (_Debug > .5) return half4(saturate(depth / 8), saturate(dist / 16), inR, 1);
                // 1px 가 덮는 물 m: 화면 기울기 그대로(fwidth), 단 가로·세로 이론값 사이로 잘라 튀는 값을 막는다
                float mppH = d * pixA, mppV = d * pixA / max(-V.y, .003);
                float mpp = clamp(fwidth(dist), mppH, mppV);
                float surge = .5 + .5 * sin(t * 6.2832 / _FoamSurge.y);
                float wob = (dkNoise(p.xz * .35 + float2(t * .06, -t * .04)) - .5) * 1.4;  // px
                float dpx = dist / mpp + wob;
                float sw = 1 + _FoamSurge.x * (surge - .5) * 2;
                float wIn = max(_FoamW.x, _FoamW.z / mpp) * sw;
                float wOut = max(_FoamW.y, _FoamW.w / mpp) * sw;
                // 수평선에 붙은 먼 물가는 1px 가 물 수십 m 라 px 굵기 그대로면 허연 띠가 번진다 → m 상한(흰 줄 _FoamSurge.z · 바깥 띠 .w),
                // 120 m 넘으면 바깥 띠를 접고 흰 줄만 1px
                wIn = min(wIn, _FoamSurge.z * sw / mpp); wOut = min(wOut, _FoamSurge.w * sw / mpp);
                float farK = saturate((d - 120) / 150);
                wIn = lerp(wIn, 1.0, farK); wOut = lerp(wOut, wIn, farK);
                float shallow = step(depth, _FoamDepth.z) * step(.5, dpx);
                col = lerp(col, lerp(_Shallow.rgb, col, max(inPath, step(_Bands.z, Ls))), shallow * step(wOut, dpx) * .9);
                float outer = step(dpx, wOut), inner = step(dpx, wIn);
                float3 foamOut = lerp(col, _Foam.rgb, .62);
                col = lerp(col, foamOut, outer);
                col = lerp(col, _Foam.rgb, inner);
                float rim = outer;

                // 반짝이: 화면 픽셀 격자 3px 칸 → 칸마다 1px, 빛길 안에서만, 반짝 켜졌다 꺼짐
                float2 px = floor(i.pos.xy);
                float2 cid = floor(px / 3);
                float h1 = dkHash(cid * .917 + 1.3), h2 = dkHash(cid * 1.31 + 11.7), h3 = dkHash(cid * .77 + 3.3);
                float ph = frac(t * _SparkleRate * (.6 + h2 * .8) + h3);
                float spk = step(ph, .10) * step(1 - _Sparkle, h1) * step(_PathCuts.y, P);
                float pin = step(abs(px.x - cid.x * 3 - 1), .1) * step(abs(px.y - cid.y * 3 - 1), .1);
                col = lerp(col, _Core.rgb, spk * pin * (1 - rim));

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
