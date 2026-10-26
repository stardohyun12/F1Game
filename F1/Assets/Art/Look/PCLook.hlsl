#ifndef PITCREW_LOOK_INCLUDED
#define PITCREW_LOOK_INCLUDED
// 과제 1548 (D-145): 그림체 3안 비교. 전역 _PCLook 하나로 셰이더가 "읽는 법"만 바꾼다 — 머티리얼·텍스처는 그대로(원본 보존).
//  0 Current    = 지금 판 그대로(이 파일을 거치지 않던 때와 같은 결과)
//  1 Clean      = 아틀라스 칸 안을 가장자리 보존 평균으로 큰 색면 + 3단 툰 빛 + 아주 약한 저주파 얼룩(±1.5%)
//  2 Painterly  = 중간 평균 + 면 방향 따라 길쭉한 월드 붓질 + 따뜻/차가운 물감 얼룩 + 그늘에 차가운 색
//  4 Retro-Clean = (1943, D-146) 머티리얼이 *_retro 텍스처(칸마다 2~4색·큰 텍셀, tools/asset-gen/retro_clean.py)를 쓰고, 여기선 그대로 읽기 + 밝은 3단 면 음영
//  3 Pixel-Soft = 지금 아틀라스·Point 유지, 2×2 텍셀 평균(16px/m) → 국소 평균 쪽으로 대비 45% 줄임 → 밝기 16단계
// 메뉴 PitCrew/Look/* 가 Shader.SetGlobalFloat("_PCLook") 를 넣는다(Editor/PitCrewLookMenu.cs).
//  5 EDC-PS1    = (2024, D-147, docs/research/2026-10-26-edc-visual-grammar.md §6 규칙 1~10) 텍스처는 원본 그대로 두고 읽는 법만:
//                 바이리니어 + 화면 크기 따라 넓어지는 저역 통과(σ≥1.5텍셀 = 밉맵 대신) · 채도 ×0.6 · 그림자 무시 + 면 음영 2단(차 13%)
//                 · 안개/빛은 머티리얼 값 대신 씬 전역(_PCFog*, _PCLight — Scripts/Visual/PCLookScene.cs) · 정점 스냅(480×270 격자, 렌더러별 _PCSnapK 배)
//                 · 디더 없음 · 어파인 _PCAffine(기본 0, 아직 미사용 — 옵션 자리만)
// _PCLook 은 씬의 PCLookScene 이 넣는다 → 그 컴포넌트가 없는 씬(ComplexTrack)은 0 = 지금 판.
float _PCLook;
float4 _PCFogColor, _PCFogParams, _PCLight, _PCSkyColor;   // _PCSkyColor = 하늘 위쪽 색(2144: 그늘 틴트에도 씀) · _PCFogParams = (시작 m, 끝 m, 최대 섞임, 그늘 면 배수 — 5b)
float _PCSnapK, _PCKeep, _PCAffine, _PCField;  // _PCField = 1: 밭(라임 → 바랜 올리브/황토, 5b)           // 렌더러별(MaterialPropertyBlock): 스냅 배수(0=1px) · 1=손픽셀(차·사람 — 블러 제외, 규칙 12)
float4 _PCSunDir, _PCSunGlow;   // 2203: 해 쪽 방향(월드, 해를 향함) · 해 쪽 지평선 금빛(a = 세기, 오후 0)
float _PCSea;   // 2203 렌더러별: 1 = 바다(노을에 차가운 청보라 + 해 반사 띠)
float _PCSnapOn, _PCMip;   // 5c 전역(PCLookScene): 정점 스냅 켬 · 1 = 밉맵 읽기(먼 곳 텍셀 크롤링 제거)
SamplerState sampler_linear_clamp;
int PCMode() { return (int)(_PCLook + 0.5); }

// 규칙 9: 클립 좌표 xy 를 내부 해상도 격자에 붙인다(정적 = 1px, _PCSnapK=2 → 2px)
float4 PCSnap(float4 cs)
{
    if (PCMode() != 5 || _PCSnapOn < .5 || cs.w <= 0) return cs;   // 5c(D-149): 기본 끔 — 움직일 때 정점이 1px씩 튀어 지글거림
    float2 g = float2(240, 135) / max(_PCSnapK, 1);   // NDC 폭 2 = 480px → 반폭당 240칸
    cs.xy = round(cs.xy / cs.w * g) / g * cs.w;
    return cs;
}

// 규칙 5·6: 안개(5 에선 씬 전역 한 색·시간대별 선형 거리·최대 섞임)
half3 PCFog(half3 col, float3 wpos, float3 matFog, float s, float e)
{
    float d = distance(wpos, _WorldSpaceCameraPos);
    if (PCMode() == 5)
    {
        // 2203(TONE 4 노을 색 분리): 해 쪽을 보면 금빛, 반대쪽은 안개색(노을 = 라벤더) — 먼 산이 라벤더로 녹는다
        float3 fc = lerp(_PCFogColor.rgb, _PCSunGlow.rgb, pow(saturate(dot((wpos - _WorldSpaceCameraPos) / max(d, 1e-3), _PCSunDir.xyz)), 8) * _PCSunGlow.a);   // 3→8: 해 쪽 좁게, 먼 산은 라벤더
        return lerp(col, fc, saturate((d - _PCFogParams.x) / max(1, _PCFogParams.y - _PCFogParams.x)) * _PCFogParams.z);
    }
    return lerp(col, matFog, saturate((d - s) / max(1, e - s)));
}
float pcHash(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
float pcNoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(pcHash(i), pcHash(i + float2(1, 0)), f.x), lerp(pcHash(i + float2(0, 1)), pcHash(i + 1), f.x), f.y); }

// 2203(TONE 5 바랜 가을색): Look 5 땅(TerrainSlope) 늦가을 리맵. kind 0 풀 · 1 흙(그루터기) · 2 마른 풀 · 3 바위(그대로)
//  아틀라스 풀 칸의 짙은 녹색·줄무늬 → 밝기를 .30 쪽으로 60% 당겨 줄을 약하게, 색은 옅은 올리브 ↔ 바랜 황금(월드 저주파 얼룩)
half3 PCGround(half3 col, float3 wpos, int kind)
{
    if (PCMode() != 5 || kind == 3) return col;
    half L = dot(col, half3(.299, .587, .114));
    half Lm = lerp(L, kind == 1 ? .27 : .30, .6);
    float nz = pcNoise(wpos.xz * .05 + 2.1) * .65 + pcNoise(wpos.xz * .19 + 5.3) * .35;
    half3 olive = half3(.92, 1.0, .64), straw = half3(1.20, 1.0, .56), stub = half3(1.12, .95, .74);
    half3 t = kind == 1 ? stub : lerp(olive, straw, saturate((nz - (kind == 2 ? .15 : .40)) * 2.2));
    return Lm * t;
}

// 아틀라스 읽기. ts = _MainTex_TexelSize, rect = 이 칸 uv 범위(xy 최소, zw 최대) — 탭이 이웃 칸으로 새지 않게 가둔다
half3 PCTex(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 ts, float4 rect)
{
    int m = PCMode();
    if (m == 0 || m == 4 || (m == 5 && _PCKeep > .5)) return SAMPLE_TEXTURE2D(tex, samp, uv).rgb;
    if (m == 5)
    {
        // 규칙 2: 텍셀 네모가 안 보이게. 바이리니어 9탭(1-2-1) — 간격 = 1.5텍셀, 화면 한 픽셀이 텍셀 여러 개를 덮으면(멀리) 그만큼 넓힌다(밉맵 대용, 원본 텍스처는 밉맵 없음).
        float2 tx = uv * ts.zw;
        float fp = min(max(length(ddx(tx)), length(ddy(tx))), 12);   // 타일 경계 frac 점프에서 폭주하지 않게 상한
        float2 st = sqrt(3.6 + fp * fp * .45) * ts.xy;   // 1-2-1 간격 1.9텍셀 ≈ σ1.35텍셀 (5b: 2.1 뭉개짐 / 1.7 반점 1~2.7% → 중간)
        // 5c: 밉맵이 있으면 화면 한 픽셀 몫(fp)만큼 미리 걸러진 단계를 읽는다 — 9탭이 fp 간격으로 띄엄띄엄 찍혀 생기던 먼 곳 크롤링 제거.
        //     간격은 그 단계 텍셀 기준 1.9 로(뭉개짐 정도는 가까운 곳과 같게). 아틀라스 칸 번짐 때문에 3단계까지만.
        float lod5 = _PCMip > .5 ? clamp(log2(max(fp, 1)) + .3, 0, 3) : 0;
        if (_PCMip > .5) st = 1.9 * exp2(lod5) * ts.xy;
        half3 c5 = 0;
        [unroll] for (int y5 = -1; y5 <= 1; y5++) [unroll] for (int x5 = -1; x5 <= 1; x5++)
            c5 += (x5 == 0 ? 2 : 1) * (y5 == 0 ? 2 : 1) * tex.SampleLevel(sampler_linear_clamp, clamp(uv + float2(x5, y5) * st, rect.xy, rect.zw), lod5).rgb;
        c5 /= 16;
        // 규칙 3: 국소 평균(±3간격 4탭) 쪽으로 대비 40% 줄임 — 1px 반점·센 얼룩(저주파 std 16~21 → ≤10)을 낮추고 더 큰 얼룩은 남긴다
        half3 m5 = 0;
        [unroll] for (int k5 = 0; k5 < 4; k5++) m5 += tex.SampleLevel(sampler_linear_clamp, clamp(uv + float2(k5 < 2 ? -3 : 3, k5 % 2 ? -3 : 3) * st, rect.xy, rect.zw), _PCMip > .5 ? lod5 + 1 : 0).rgb;
        m5 = (m5 * .25 + c5) * .5;
        return lerp(m5, c5, .72);   // 5b: 대비 40%↓ → 28%↓ (검은 창·문틀이 살게, .78 은 반점 과다)
    }
    #define PCS(o) SAMPLE_TEXTURE2D_LOD(tex, samp, clamp(o, rect.xy, rect.zw), 0).rgb
    if (m == 3)
    {
        float2 b = (floor(uv * ts.zw * 0.5) * 2 + 1) * ts.xy;   // 2×2 블록의 가운데(텍셀 경계)
        half3 c = (PCS(b + float2(-.5, -.5) * ts.xy) + PCS(b + float2(.5, -.5) * ts.xy) + PCS(b + float2(-.5, .5) * ts.xy) + PCS(b + float2(.5, .5) * ts.xy)) * .25;
        half3 mm = 0; [unroll] for (int y = -1; y <= 1; y++) [unroll] for (int x = -1; x <= 1; x++) mm += PCS(b + float2(x, y) * 3 * ts.xy); mm /= 9;
        c = lerp(mm, c, .55);
        float L = dot(c, half3(.299, .587, .114));                     // 밝기만 16단계(채널별로 자르면 어두운 색 색조가 튄다)
        return c * ((floor(L * 16 + .5) / 16) / max(L, 1e-3));
    }
    // 1·2: 가장자리 보존 평균(색이 크게 다른 탭 = 다른 칸·창문 테두리는 무게를 거의 안 준다)
    float r = m == 1 ? 4.0 : 1.5; float k = m == 1 ? 14.0 : 24.0;
    half3 ref = (PCS(uv) * 2 + PCS(uv + float2(1, 0) * ts.xy) + PCS(uv - float2(1, 0) * ts.xy) + PCS(uv + float2(0, 1) * ts.xy) + PCS(uv - float2(0, 1) * ts.xy)) / 6;
    half3 s = 0; float ws = 0;
    [unroll] for (int y2 = -2; y2 <= 2; y2++) [unroll] for (int x2 = -2; x2 <= 2; x2++)
    {
        half3 c2 = PCS(uv + float2(x2, y2) * r * ts.xy); half3 d = c2 - ref; float w = exp(-dot(d, d) * k);
        s += c2 * w; ws += w;
    }
    #undef PCS
    return s / ws;
}
half3 PCTex(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 ts) { return PCTex(TEXTURE2D_ARGS(tex, samp), uv, ts, float4(-1e5, -1e5, 1e5, 1e5)); }

// 빛. 지금 판 식(그늘 = 하늘빛×0.36, 양지 = + 해×N·L) 을 안마다 다르게 자른다
float3 PCShade(float3 amb, float3 sunCol, float ndl, float att, float faceShade)
{
    int m = PCMode(); float s = ndl * att;
    if (m == 5)
    {
        // 규칙 7: 실시간 그림자 무시(att 안 씀), 해 쪽/반대쪽 2단 차 13%, 해 쪽만 해 색 틴트 ≤20%. 밝기·시간대 색은 _PCLight(씬 전역) 하나.
        // 5b(D-148 밝고 맑게): 해 쪽 = 해 색 35% + 따뜻하게, 그늘 쪽 = ×_PCFogParams.w(≈.75, 차 25%) + 차갑게
        float t = ndl > .2 ? 1 : 0;
        float3 tint = sunCol / max(max(sunCol.r, max(sunCol.g, sunCol.b)), 1e-3);
        float3 warm = lerp(float3(1, 1, 1), tint, .35) * float3(1.05, 1.0, .90);
        float3 cool = (_PCFogParams.w > 0 ? _PCFogParams.w : .75) * float3(.90, .97, 1.12);
        // 2144(TONE 4 "그늘은 하늘빛"): 그늘에 하늘색(밝기로 정규화)을 15% — 오후 청, 노을 보라
        float3 skN = _PCSkyColor.rgb / max(dot(_PCSkyColor.rgb, float3(.299, .587, .114)), 1e-3);
        if (_PCSkyColor.a > 0 || dot(_PCSkyColor.rgb, 1) > 0) cool *= lerp(float3(1, 1, 1), clamp(skN, .5, 2), .15);
        float3 face = t > .5 ? warm : cool;
        return _PCLight.rgb * lerp(float3(1, 1, 1), face, saturate(faceShade * 2));
    }
    if (m == 1) { s = s < .12 ? 0 : (s < .5 ? .55 : 1); faceShade = max(faceShade, .5); }   // 3단: 그늘·반그늘·해
    if (m == 4)
    {
        // 정점 조명 느낌 3단(그늘·반그늘·해). 그늘을 지금(×0.36)보다 확실히 밝게 — D-146 "어둡고 탁함 금지"
        s = s < .15 ? 0 : (s < .55 ? .6 : 1);
        return lerp(float3(1, 1, 1), amb * .62 + sunCol * s * .82, max(faceShade, .5));
    }
    float3 lit = amb * 0.36 + sunCol * s;
    if (m == 2)
    {
        float t = smoothstep(0, .55, s);
        float3 cool = amb * float3(.80, .90, 1.12) * .40;                 // 그늘 = 차가운 하늘색
        lit = lerp(cool, amb * .15 + sunCol * float3(1.04, 1, .94), t);   // 양지 = 살짝 따뜻하게
        faceShade = max(faceShade, .45);
    }
    return lerp(float3(1, 1, 1), lit, faceShade);
}

// 색면 위 마무리(빛을 곱한 뒤, 안개 전)
half3 PCPost(half3 col, float3 wpos, float3 n)
{
    int m = PCMode();
    if (m == 5)
    {
        if (_PCKeep < .5)
        {
            // 5b: 라임(노란 기 도는 진한 초록) → 바랜 올리브/황토. 밭(_PCField)은 세게, 나머지(풀 등)는 약하게
            half L0 = dot(col, half3(.299, .587, .114));
            half lime = saturate((col.g - max(col.r, col.b)) * 6) * saturate((col.g - col.b) * 3);
            // 2144(TONE 4·6): 밭은 진흙 갈색 대신 밝기를 끌어올린 바랜 올리브 ↔ 황금 풀(월드 저주파 얼룩으로 섞음)
            half Lf = lerp(L0, .30, .5);
            half3 fieldCol = Lf * lerp(half3(.94, 1.0, .62), half3(1.16, 1.02, .58), pcNoise(wpos.xz * .07 + 1.3));
            col = _PCField > .5 ? lerp(col, fieldCol, .85) : lerp(col, L0 * half3(1.10, 1.0, .66), lime * .35);   // 밭 라임은 노랑 쪽(r≈g)이라 색 검출에 안 걸림 → 밭은 무조건
            // 2144(TONE 3 "가까운 것은 또렷"): 채도·대비를 거리로 나눈다. 25m 안 = 채도 ×.86 + 대비 +10%, 60m 밖 = ×.64(먼 것은 안개가 녹임)
            float nearK = 1 - saturate((distance(wpos, _WorldSpaceCameraPos) - 25) / 35);
            col = lerp(dot(col, half3(.299, .587, .114)), col, lerp(.64, .86, nearK));   // 규칙 4: 세계 채도 (5b 측정: ×0.6 → 장면 0.05~0.10, ×0.8 → 0.18~0.24; 목표 0.12~0.18)
            col = max(0, (col - .18) * (1 + .10 * nearK) + .18);
            if (_PCSea > .5 && _PCSunGlow.a > 0)
            {
                // 2203: 노을 바다 = 차가운 청보라 + 해 쪽 반사 띠(정지 — 반짝임 없음, D-149)
                half Ls = dot(col, half3(.299, .587, .114));
                col = lerp(col, Ls * half3(.80, .82, 1.30), .65 * _PCSunGlow.a);
                float3 v = normalize(wpos - _WorldSpaceCameraPos); float3 rf = reflect(v, float3(0, 1, 0));
                col += _PCSunGlow.rgb * pow(saturate(dot(rf, _PCSunDir.xyz)), 18) * .9 * _PCSunGlow.a;
            }
            // 규칙 3 "평평 금지": 0.3~1.5m 월드 얼룩 ±5%(텍스처 없는 도로·단색 면도 저주파 std 3 이상)
            float3 an = abs(n); float2 p = an.y > .5 ? wpos.xz : (an.x > an.z ? wpos.zy : wpos.xy);
            col *= 1 + (pcNoise(p * 1.3) * .55 + pcNoise(p * .45 + 3.7) * .45 - .5) * .10;
        }
        // 5b: 휘도 압축(.82) 뺌 — 물 빠짐의 원인. 대비는 그대로 둔다
    }
    else if (m == 1) col *= 1 + (pcNoise(wpos.xz * .05 + wpos.y * .03) - .5) * .03;
    else if (m == 2)
    {
        float3 an = abs(n); float2 p = an.y > .5 ? wpos.xz : (an.x > an.z ? wpos.zy : wpos.xy);
        float2 dir = an.y > .5 ? float2(.8, .6) : float2(1, .12);        // 바닥은 비스듬히, 벽은 거의 가로로 붓질
        float2 q = float2(dot(p, dir), dot(p, float2(-dir.y, dir.x)));
        q.y += pcNoise(p * .12) * 1.6;                                    // 붓 자국이 곧은 줄이 되지 않게 흔들림
        float st = pcNoise(float2(q.x * .8, q.y * 6)) * .6 + pcNoise(float2(q.x * 1.9, q.y * 13) + 5.1) * .4;
        col *= 1 + (st - .5) * .28;
        float h = pcNoise(p * .09 + 7.3) - .5; col += h * .10 * half3(1, .55, -.45) * max(col.g, .2);   // 물감 얼룩(따뜻↔차가움)
    }
    return col;
}
#endif
