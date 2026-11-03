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

// ── D-236 7판 도트 하늘(하늘 셰이더만 — 바다 반사는 위 DuskSkyColor 그대로) ──
//  _PCDuskBands = (켬, 단 수, 단 끝 디더 폭, 디더 칸 px) · _PCDuskHalo = (원반°, 빛무리 1단°, 2단°, 세기) · _PCDuskStrips = (켬, 흐름 °/s, 덮는 양, 두께 배수)
float4 _PCDuskBands, _PCDuskHalo, _PCDuskStrips, _PCSeaClock;

// 화면 픽셀 고정 Bayer 4×4(0..1)
float dkBayer2(float2 a) { a = floor(a); return frac(a.x * .5 + a.y * a.y * .75); }
float dkBayer4(float2 a) { return dkBayer2(.5 * a) * .25 + dkBayer2(a); }
// 계단 값 v(0..n) → 정수 단. 각 단 끝 w 만큼만 다음 단과 디더로 섞는다(단 안은 한 색)
float dkStep(float v, float w, float th) { float fi = floor(v), f = v - fi; return fi + step(th, (f - (1 - w)) / max(w, 1e-3)); }

float3 DuskSkyPixel(float3 dir, float2 spos)
{
    dir = normalize(dir);
    if (_PCDuskBands.x < .5 || _PCDusk.x <= 0) return DuskSkyColor(dir, 1);
    float pxs = _PCDuskBands.w > .5 ? _PCDuskBands.w : max(1, round(_ScreenParams.y / 360));
    float th = dkBayer4(floor(spos / pxs));
    float3 sd3 = _PCDuskSunDir.xyz;
    float y = dir.y, el = asin(saturate(y));                                   // 고도(rad)
    float n = max(_PCDuskBands.y, 2);
    // 1) 하늘 바탕: 고도를 n 단으로(지평선 쪽 좁게 — sqrt), 단 대표 고도에서 기존 색을 읽음 → 단 안은 한 색
    float g = sqrt(saturate(el / .9));
    float gi = min(dkStep(g * n, _PCDuskBands.z, th), n - 1);
    float gq = (gi + .5) / n; float yq = sin(gq * gq * .9);
    float3 dq = normalize(float3(dir.x, 0, dir.z)) * sqrt(1 - yq * yq) + float3(0, yq, 0);
    float sd = dot(dir, sd3), toward = saturate(sd * .5 + .5); toward *= toward;
    float3 hor = lerp(_PCDuskAway.rgb, _PCDuskHor.rgb, toward);
    float3 c = lerp(hor, _PCDuskMid.rgb, smoothstep(0.0, 0.20, yq));
    c = lerp(c, _PCDuskTop.rgb, smoothstep(0.12, 0.70, yq));
    // 2) 해 둘레 빛무리 2단(테두리만 디더)
    float ang = degrees(acos(clamp(sd, -1, 1)));
    float hv = 2 - saturate((ang - _PCDuskHalo.y) / 1.2) - saturate((ang - _PCDuskHalo.z) / 2.0);   // 2 = 1단 안, 1 = 2단 안
    float hi = min(dkStep(hv, .5, th), 2);
    c += _PCDuskGlow.rgb * _PCDuskHalo.w * (hi > 1.5 ? .85 : (hi > .5 ? .38 : 0));
    // 3) 해 원반(가장자리 계단 — 픽셀 그대로)
    if (ang < _PCDuskHalo.x) c = _PCDuskSunCol.rgb;
    // 4) 납작한 구름 띠 3줄: 아래가 노을빛, 위는 그늘 보라. 해 앞을 가로질러도 됨. 느리게 흐름
    if (_PCDuskStrips.x > .5)
    {
        float t = _PCSeaClock.x > .5 ? _PCSeaClock.y : _Time.y;
        float2 sf = normalize(float2(sd3.x, sd3.z) + 1e-5);
        float az = degrees(atan2(dir.x * sf.y - dir.z * sf.x, dot(dir.xz, sf)));   // 해 기준 방위(°)
        float eld = degrees(el), pxDeg = max(fwidth(eld), 1e-3);
        float3 lit = lerp(_PCDuskCloudDark.rgb, _PCDuskCloudLit.rgb, saturate(1.2 - abs(az) / 40));
        [unroll] for (int k = 0; k < 3; k++)
        {
            float ce = k == 0 ? 1.6 : (k == 1 ? 4.4 : 8.6);                 // 줄 중심 고도°
            float tk = (k == 0 ? .55 : (k == 1 ? .85 : 1.1)) * _PCDuskStrips.w;
            float a = az + t * _PCDuskStrips.y * (1 + k * .35) + k * 37;
            float m = dkNoise(float2(a / (14 - k * 3), k * 5.1)) * .65 + dkNoise(float2(a / 4.5, k * 9.7)) * .35;
            float thick = tk * saturate((m - (1 - _PCDuskStrips.z)) / .18);
            float c0 = ce + (dkNoise(float2(a / 9, k * 2.3 + 40)) - .5) * tk;
            float u = (eld - (c0 - thick * .35)) / max(thick, 1e-3);      // 0 = 바닥, 1 = 꼭대기(바닥이 평평하게 아래로)
            if (thick > pxDeg * .8 && u >= 0 && u <= 1)
            {
                float3 cc = u < .45 ? lit : lerp(_PCDuskCloudDark.rgb, lit, .35);
                if ((eld - (c0 - thick * .35)) < pxDeg * 1.05) cc = lerp(lit, _PCDuskSunCol.rgb, saturate(1 - abs(az) / 25) * .8);   // 바닥 1px 노을 테
                c = cc;
            }
        }
    }
    if (y < 0) c = hor * lerp(1, .85, saturate(-y * 6));
    float3 day = y >= 0 ? lerp(_PCDayHor.rgb, _PCDayTop.rgb, smoothstep(0, .6, y)) : _PCDayBot.rgb;
    return lerp(day, c, saturate(_PCDusk.x));
}
#endif
