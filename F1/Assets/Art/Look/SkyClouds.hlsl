// D-244 "멈춰 있는 사진 같음" — 하늘 구름 두 겹(뭉게·새털)과 땅 구름 그림자가 같이 쓰는 한 장의 구름. 값은 SunsetLook 전역.
//  구름은 월드 높이 H 의 판 위 무늬 → 머리 위는 크고 빠르게, 지평선 쪽은 작고 느리게 보인다(시차). 땅 그림자는 해 방향으로 같은 판을 읽는다.
//  _PCSkyClouds = (뭉게 덮는 양 0..1 · 새털 덮는 양 · 모양 변형 속도 · 땅 그림자 세기) · _PCSkyWind = (뭉게 m/s · 방향° · 새털 m/s · 방향°)
//  _PCSeaClock.x = 1 → 시간을 y 로 고정(편집 모드 전/후 캡처용, 바다와 같은 시계)
#ifndef PC_SKYCLOUDS_INCLUDED
#define PC_SKYCLOUDS_INCLUDED
float4 _PCSkyClouds, _PCSkyWind, _PCSeaClock;

#define SC_H1 900.0    // 뭉게 판 높이(m)
#define SC_S1 700.0    // 뭉게 무늬 한 칸(m)
#define SC_H2 2400.0   // 새털
#define SC_S2 1500.0
#define SC_SG 180.0    // 땅 그림자 무늬 한 칸(m) — 700m 그대로면 마을(1km)이 한두 칸 안에 들어가 그림자가 거의 안 보인다(D-244 캡처). 바람은 하늘과 같다

float SkyNow() { return _PCSeaClock.x > .5 ? _PCSeaClock.y : _Time.y; }
float scHash(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
float scNoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(scHash(i), scHash(i + float2(1, 0)), f.x), lerp(scHash(i + float2(0, 1)), scHash(i + 1), f.x), f.y); }
float2 scWind(float sp, float deg) { float a = radians(deg); return float2(sin(a), cos(a)) * sp; }

// 큰 덩이는 바람 따라 미끄러지고, 잔 덩이 두 겹은 서로 다른 쪽으로 천천히 밀려 가장자리 모양이 바뀐다(노이즈 시간축)
float SkyCloudDen(float2 p, float t, float morph)
{
    float2 m1 = float2(.7, -.4) * t * morph, m2 = float2(-.3, .9) * t * morph;
    return scNoise(p) * .55 + scNoise(p * 2.2 + 5.3 + m1) * .30 + scNoise(p * 4.7 + 1.7 + m2) * .15;
}
float SkyCloudTh(float cover) { return lerp(.80, .36, saturate(cover)); }
// 월드 xz → 뭉게 무늬 좌표
float2 SkyCloudP1(float2 xz, float t) { return (xz - scWind(_PCSkyWind.x, _PCSkyWind.y) * t) / SC_S1; }

// 땅 구름 그림자: 1 = 해 그대로, 1 - 세기 = 구름 아래. 해 쪽 면에만(그늘 면은 원래 해가 없다). 2단(도트) — 경계는 무늬가 커서 지글거림 없음
float SkyCloudShade(float3 wpos, float3 toLight, float ndl)
{
    if (_PCSkyClouds.w <= 0 || _PCSkyClouds.x <= 0 || toLight.y < .05) return 1;
    float t = SkyNow();
    float2 xz = wpos.xz + toLight.xz / toLight.y * (SC_H1 - wpos.y);
    float n = SkyCloudDen((xz - scWind(_PCSkyWind.x, _PCSkyWind.y) * t) / SC_SG, t, _PCSkyClouds.z);
    return n > SkyCloudTh(_PCSkyClouds.x) ? 1 - _PCSkyClouds.w * saturate(ndl * 4) : 1;
}
#endif
