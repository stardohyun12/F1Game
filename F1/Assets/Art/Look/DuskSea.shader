// D-227 노을 바다(씬 전용 — Map_TownV6 Sea 가 노을일 때만 이 머티리얼로 바뀐다. 끄면 원래 TV6_Sea).
//  · 느린 물결 = 조각 셰이더 노멀(정점 121개 판이라 정점 물결은 의미 없음). 물결 4개, 픽셀보다 잘게 접히는 물결은 거리에 따라 빼고(지글거림 방지)
//    뺀 만큼 해 반사 꼬리를 넓힌다(Toksvig) → 멀리서도 해 쪽 빛길이 한 줄로 길게 남는다.
//  · 반짝이 = 해 빛길 안의 월드 칸(칸 크기 = 화면 2~4px 로 거리마다 2의 거듭제곱) 하나하나가 2~3초에 한 번 0.5초쯤 켜졌다 꺼진다 — 깜빡이되 프레임마다 뒤집히지 않는다.
//  · 해안 거품 = _ShoreTex(에디터가 지형 레이로 구운 해안 거리, R 1 = 물가) 위 가는 줄 한 줄, 천천히 밀려왔다 빠짐.
Shader "PitCrew/Look/DuskSea"
{
    Properties
    {
        _Deep("Deep (노을)", Color) = (0.13, 0.12, 0.26, 1)
        _DayTint("Day tint", Color) = (0.36, 0.58, 0.70, 1)
        _Reflect("Sky reflection", Range(0, 1)) = 0.85
        _Shine("Sun glint sharpness", Float) = 600
        _Glint("Sun glint strength", Float) = 1.6
        _Sparkle("Sparkle strength", Float) = 1.3
        _SparkleRate("Sparkle blink (per sec)", Float) = 0.45
        _WaveAmp("Wave steepness", Range(0, 0.4)) = 0.14
        _WaveSpeed("Wave speed", Float) = 0.35
        _HorizonFade("Horizon fade start/end m", Vector) = (150, 1100, 0, 0)
        _ShoreTex("Shore (R: 1 = 물가)", 2D) = "black" {}
        _ShoreRect("Shore rect (xmin, zmin, size)", Vector) = (-400, -400, 800, 0)
        _Foam("Foam color", Color) = (0.95, 0.80, 0.78, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Art/Look/Dusk.hlsl"
            TEXTURE2D(_ShoreTex); SAMPLER(sampler_ShoreTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Deep, _DayTint, _HorizonFade, _ShoreRect, _Foam, _ShoreTex_ST;
                float _Reflect, _Shine, _Glint, _Sparkle, _SparkleRate, _WaveAmp, _WaveSpeed;
            CBUFFER_END
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.wpos = TransformObjectToWorld(v.xyz); o.pos = TransformWorldToHClip(o.wpos); return o; }

            half4 frag(v2f i) : SV_Target
            {
                float3 p = i.wpos;
                float3 V = normalize(p - _WorldSpaceCameraPos);
                float d = distance(p, _WorldSpaceCameraPos);
                float2 fw = max(fwidth(p.xz), 1e-4); float fp = max(fw.x, fw.y);   // 이 픽셀이 덮는 물 넓이(m)
                // 물결 4개(방향·파장 m). 픽셀 2개보다 잘게 접히면 뺀다 — 뺀 기울기 제곱은 var 로 모아 반사 꼬리를 넓힌다
                const float2 D[4] = { float2(0.94, 0.33), float2(0.37, 0.93), float2(-0.76, 0.65), float2(0.20, -0.98) };
                const float L[4] = { 9.0, 5.3, 3.1, 1.7 };
                float2 slope = 0; float var = 0; float t = _Time.y * _WaveSpeed;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float kk = 6.2832 / L[k];
                    float w = 1 - smoothstep(.20, .45, fp / L[k]);
                    float a = _WaveAmp * (k == 0 ? 1 : (k == 1 ? .8 : (k == 2 ? .6 : .45)));
                    float ph = kk * dot(D[k], p.xz) - sqrt(9.8 * kk) * t + k * 1.7;
                    slope += D[k] * a * cos(ph) * w;
                    var += a * a * .5 * (1 - w);
                }
                float3 N = normalize(float3(-slope.x, 1, -slope.y));
                float3 R = reflect(V, N);
                float3 sun = _PCDuskSunDir.xyz;
                float F = .02 + .98 * pow(1 - saturate(dot(N, -V)), 5);
                float3 sky = DuskSkyColor(float3(R.x, max(R.y, .002), R.z), 0);
                float3 col = lerp(_Deep.rgb, sky, saturate(F * 1.4) * _Reflect);
                // 해 빛길: 잘게 뺀 물결만큼 꼬리를 넓힘(멀리 = 흐릿하고 긴 띠)
                float sh = _Shine / (1 + _Shine * var * 6);
                float glint = pow(saturate(dot(R, sun)), sh) * min(1, sh / _Shine * 3);
                col += _PCDuskSunCol.rgb * glint * _Glint;
                // 반짝이: 평평한 물 반사 방향이 해와 가까운 띠 안에서만(가로 좁게, 세로 길게)
                float3 R0 = reflect(V, float3(0, 1, 0));
                float2 sxz = normalize(sun.xz + 1e-5), rxz = normalize(R0.xz + 1e-5);
                float daz = acos(clamp(dot(sxz, rxz), -1, 1));
                float del = asin(saturate(R0.y)) - asin(saturate(sun.y));
                float path = exp(-daz * daz / (.0045 + .010 * saturate(-del * 3))) * exp(-max(del, 0) * max(del, 0) / .004);
                float2 cell = exp2(ceil(log2(fw * 3)));                       // 칸 = 화면 3~6px(2의 거듭제곱 → 정지 화면에서 안 바뀜)
                float2 id = floor(p.xz / cell), f = frac(p.xz / cell) - .5;
                float r1 = dkHash(id + cell * 7.1), r2 = dkHash(id * 1.31 + 11.7), r3 = dkHash(id * .77 + 3.3);
                float ph2 = frac(_Time.y * _SparkleRate * (.6 + r2 * .8) + r3);
                float blink = smoothstep(0, .12, ph2) * (1 - smoothstep(.12, .30, ph2));   // 켜짐 ≈0.4초, 꺼짐 1.6~3초
                float dot1 = step(max(abs(f.x), abs(f.y)), .2);
                col += _PCDuskSunCol.rgb * step(.80, r1) * blink * dot1 * path * _Sparkle * saturate(1.3 - d / 500);
                // 해안 거품 한 줄
                float2 suv = (p.xz - _ShoreRect.xy) / _ShoreRect.z;
                float shore = SAMPLE_TEXTURE2D(_ShoreTex, sampler_ShoreTex, suv).r;
                float surge = .55 + .12 * sin(_Time.y * .55 + dkNoise(p.xz * .08) * 6.2832);
                float foam = smoothstep(surge - .05, surge, shore) * (1 - smoothstep(surge + .10, surge + .16, shore));
                foam = max(foam, smoothstep(.86, .92, shore));                // 바로 물가는 늘 흰 테
                col = lerp(col, _Foam.rgb * lerp(.75, 1.1, saturate(dot(sun, float3(0, 1, 0)) * 4)), foam * saturate(1.6 - fp / .9) * .85);
                // 수평선: 하늘 지평선 색으로 녹임(이음매 없이)
                float3 hz = DuskSkyColor(float3(V.x, .001, V.z), 0);
                col = lerp(col, hz, smoothstep(_HorizonFade.x, _HorizonFade.y, d) * .9);
                col = lerp(_DayTint.rgb, col, saturate(_PCDusk.x));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
