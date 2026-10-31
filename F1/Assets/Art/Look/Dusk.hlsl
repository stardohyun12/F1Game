// D-227 노을 하늘(DuskSky)과 바다 반사(DuskSea)가 같이 쓰는 하늘 색. 값은 전부 SunsetLook 이 넣는 씬 전역(Linear).
//  _PCDusk.x = 섞임(0 낮 ~ 1 노을) · _PCDuskSunDir = 보이는 해 방향(빛 방향과 따로 — 해는 수평선 3~4°, 빛은 더 높게 속임)
#ifndef PC_DUSK_INCLUDED
#define PC_DUSK_INCLUDED
float4 _PCDusk, _PCDuskSunDir, _PCDuskGlow;
float4 _PCDuskHor, _PCDuskAway, _PCDuskMid, _PCDuskTop, _PCDuskSunCol, _PCDuskCloudLit, _PCDuskCloudDark;
float4 _PCDayTop, _PCDayHor, _PCDayBot;

float dkHash(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
float dkNoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(dkHash(i), dkHash(i + float2(1, 0)), f.x), lerp(dkHash(i + float2(0, 1)), dkHash(i + 1), f.x), f.y); }

// clouds = 1: 구름 띠까지(하늘), 0: 반사용(바다 — 구름 반사는 지글거림 원인이라 뺌)
float3 DuskSkyColor(float3 dir, float clouds)
{
    dir = normalize(dir);
    float y = dir.y, yy = max(y, 0);
    float3 sd3 = _PCDuskSunDir.xyz;
    float sd = dot(dir, sd3);
    float toward = saturate(sd * .5 + .5); toward *= toward;
    float3 hor = lerp(_PCDuskAway.rgb, _PCDuskHor.rgb, toward);          // 지평선: 해 쪽 주황 ↔ 반대 라벤더
    float3 c = lerp(hor, _PCDuskMid.rgb, smoothstep(0.0, 0.20, yy));      // 위로 분홍
    c = lerp(c, _PCDuskTop.rgb, smoothstep(0.12, 0.70, yy));              // 꼭대기 보라·청
    float s = saturate(sd);
    c += _PCDuskGlow.rgb * (pow(s, 28) * .9 + pow(s, 5) * .30 * (1 - smoothstep(0, .30, yy))) * _PCDuskGlow.a;
    if (clouds > .5)
    {
        // 구름 띠(가로로 긴 얇은 층) — 해 기준 방위각이라 이음매가 해 반대쪽 등 뒤로 간다. 정지(움직이면 경계 픽셀이 깜빡임)
        float2 sf = normalize(float2(sd3.x, sd3.z) + 1e-5); float2 dxz = dir.xz;
        float az = atan2(dxz.x * sf.y - dxz.y * sf.x, dot(dxz, sf));
        float band = smoothstep(.025, .06, yy) * (1 - smoothstep(.16, .26, yy));
        float n = dkNoise(float2(az * 4.0, yy * 38)) * .6 + dkNoise(float2(az * 11.0 + 3.1, yy * 90)) * .4;
        float m = smoothstep(.56, .60, n * band + band * .18);
        float3 cc = lerp(_PCDuskCloudDark.rgb, _PCDuskCloudLit.rgb, pow(toward, 1.4));
        c = lerp(c, cc, m * .92);
    }
    if (sd > .99972 && clouds > .5) c = _PCDuskSunCol.rgb;               // 해 원반(각반지름 ≈1.35°, 가장자리 딱딱 — 도트)
    if (y < 0) c = hor * lerp(1, .85, saturate(-y * 6));
    float3 day = y >= 0 ? lerp(_PCDayHor.rgb, _PCDayTop.rgb, smoothstep(0, .6, y)) : _PCDayBot.rgb;
    return lerp(day, c, saturate(_PCDusk.x));
}
#endif
