p='F1/Assets/Art/Look/PCLook.hlsl'
s=open(p,encoding='utf-8').read()
R=[
('float _PCSnapOn, _PCMip;','float4 _PCSunDir, _PCSunGlow;   // 2203: 해 쪽 방향(월드, 해를 향함) · 해 쪽 지평선 금빛(a = 세기, 오후 0)\nfloat _PCSea;   // 2203 렌더러별: 1 = 바다(노을에 차가운 청보라 + 해 반사 띠)\nfloat _PCSnapOn, _PCMip;'),
('    if (PCMode() == 5) return lerp(col, _PCFogColor.rgb, saturate((d - _PCFogParams.x) / max(1, _PCFogParams.y - _PCFogParams.x)) * _PCFogParams.z);',
 '''    if (PCMode() == 5)
    {
        // 2203(TONE 4 노을 색 분리): 해 쪽을 보면 금빛, 반대쪽은 안개색(노을 = 라벤더) — 먼 산이 라벤더로 녹는다
        float3 fc = lerp(_PCFogColor.rgb, _PCSunGlow.rgb, pow(saturate(dot((wpos - _WorldSpaceCameraPos) / max(d, 1e-3), _PCSunDir.xyz)), 3) * _PCSunGlow.a);
        return lerp(col, fc, saturate((d - _PCFogParams.x) / max(1, _PCFogParams.y - _PCFogParams.x)) * _PCFogParams.z);
    }'''),
('float pcNoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(pcHash(i), pcHash(i + float2(1, 0)), f.x), lerp(pcHash(i + float2(0, 1)), pcHash(i + 1), f.x), f.y); }',
 '''float pcNoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(pcHash(i), pcHash(i + float2(1, 0)), f.x), lerp(pcHash(i + float2(0, 1)), pcHash(i + 1), f.x), f.y); }

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
}'''),
('            col = max(0, (col - .18) * (1 + .10 * nearK) + .18);',
 '''            col = max(0, (col - .18) * (1 + .10 * nearK) + .18);
            if (_PCSea > .5 && _PCSunGlow.a > 0)
            {
                // 2203: 노을 바다 = 차가운 청보라 + 해 쪽 반사 띠(정지 — 반짝임 없음, D-149)
                half Ls = dot(col, half3(.299, .587, .114));
                col = lerp(col, Ls * half3(.80, .82, 1.30), .65 * _PCSunGlow.a);
                float3 v = normalize(wpos - _WorldSpaceCameraPos); float3 rf = reflect(v, float3(0, 1, 0));
                col += _PCSunGlow.rgb * pow(saturate(dot(rf, _PCSunDir.xyz)), 18) * .9 * _PCSunGlow.a;
            }'''),
]
for a,b in R:
    assert a in s,a[:50]; s=s.replace(a,b)
open(p,'w',encoding='utf-8').write(s)

p='F1/Assets/Art/BayLoop/Terrain/TerrainSlope.shader'
s=open(p,encoding='utf-8').read()
R=[('''    half3 col;
    // 과제 1943''','''    half3 col; int gk=3;   // 2203: 땅 종류(PCGround — Look 5 에서만 늦가을 리맵)
    // 과제 1943'''),
('''    else if(d>=_DryDeg) col=Tile(_TileDry,worldUV);
    else {
     col=Tile(_TileGrass,worldUV);''','''    else if(d>=_DryDeg) { col=Tile(_TileDry,worldUV); gk=2; }
    else {
     col=Tile(_TileGrass,worldUV); gk=0;'''),
('''     if(_SoilAmount>0 && nz>_PatchThreshold) col=Tile(_TileSoil,worldUV);
    }''','''     if(_SoilAmount>0 && nz>_PatchThreshold) { col=Tile(_TileSoil,worldUV); gk=1; }
    }
    if(sg.g>=0) col=PCGround(col,i.pos,gk);''')]
for a,b in R:
    assert a in s,a[:50]; s=s.replace(a,b)
open(p,'w',encoding='utf-8').write(s)

p='F1/Assets/Art/Look/SkyGrad.shader'
s=open(p,encoding='utf-8').read()
R=[('            float4 _PCFogColor, _PCSkyColor;','            float4 _PCFogColor, _PCSkyColor, _PCSunDir, _PCSunGlow;   // 2203: 해 쪽 지평선 금빛'),
('''                return half4(lerp(_PCFogColor.rgb, _PCSkyColor.rgb, t), 1);''','''                float3 col = lerp(_PCFogColor.rgb, _PCSkyColor.rgb, t);
                float sd = saturate(dot(normalize(i.dir), _PCSunDir.xyz));
                col = lerp(col, _PCSunGlow.rgb, pow(sd, 3) * (1 - t * .7) * _PCSunGlow.a);   // 2203: 노을 = 해 쪽 금빛 → 위·반대쪽 라벤더
                return half4(col, 1);''')]
for a,b in R:
    assert a in s,a[:50]; s=s.replace(a,b)
open(p,'w',encoding='utf-8').write(s)

p='F1/Assets/Scripts/Visual/PCLookScene.cs'
s=open(p,encoding='utf-8').read()
R=[
('struct Tod { public Color fog, sky; public float s, e, max, shade; public Color light; public float pool; }',
 '// 2203: fog 는 "열쇠"(무드 에셋 fogColor 와 같아야 이 줄이 골린다) — 실제 안개색을 바꾸려면 haze(a>0 이면 무드 색 대신), glow = 해 쪽 지평선 금빛(a = 세기).\n    struct Tod { public Color fog, sky; public float s, e, max, shade; public Color light; public float pool; public Color haze, glow; }'),
('new Tod { fog = new Color(1.00f, 0.78f, 0.52f), sky = new Color(0.60f, 0.56f, 0.74f), s = 40, e = 620, max = .55f, shade = .70f, light = new Color(1.42f, 1.20f, 0.96f), pool = .7f },   // 노을 (2144: 지평선 금빛·위 라벤더, 그늘은 하늘 보라 틴트)',
 'new Tod { fog = new Color(0.96f, 0.76f, 0.62f), sky = new Color(0.50f, 0.52f, 0.74f), s = 40, e = 620, max = .55f, shade = .66f, light = new Color(1.46f, 1.18f, 0.90f), pool = .7f,\n                  haze = new Color(0.78f, 0.70f, 0.82f, 1f), glow = new Color(1.00f, 0.72f, 0.40f, 1f) },   // 노을 (2203: 열쇠를 Sunset_Backlight 값으로 되돌림 — 2124·2144 의 fog 변경은 열쇠만 바꿔 화면엔 안 먹었음. 안개 = 라벤더, 해 쪽만 금빛, 그늘은 하늘 보라 틴트)'),
('        Shader.SetGlobalColor(FogCId, fc.linear);',
 '''        // 2203: haze 를 준 줄은 무드 안개색 대신 그 색(가중 섞기 — 이웃 줄은 무드 색)
        Color hz = Color.clear, gl = Color.clear; float hw = 0;
        foreach (var t in Table)
        {
            float d = (t.fog.r - fc.r) * (t.fog.r - fc.r) + (t.fog.g - fc.g) * (t.fog.g - fc.g) + (t.fog.b - fc.b) * (t.fog.b - fc.b);
            float w = 1f / ((d + 1e-4f) * (d + 1e-4f)) / ws;
            hz += (t.haze.a > 0 ? t.haze : fc) * w; gl += t.glow * w; hw += w;
        }
        fc = hz / hw; fc.a = 1; FogColor = fc;
        var sl = sun != null ? sun : RenderSettings.sun;
        Shader.SetGlobalVector(SunDirId, sl != null ? -sl.transform.forward : Vector3.up);
        Shader.SetGlobalVector(SunGlowId, new Vector4(gl.linear.r, gl.linear.g, gl.linear.b, gl.a / hw));
        Shader.SetGlobalColor(FogCId, fc.linear);'''),
('SkyId = Shader.PropertyToID("_PCSkyColor"), FieldId = Shader.PropertyToID("_PCField");',
 'SkyId = Shader.PropertyToID("_PCSkyColor"), FieldId = Shader.PropertyToID("_PCField"),\n        SunDirId = Shader.PropertyToID("_PCSunDir"), SunGlowId = Shader.PropertyToID("_PCSunGlow"), SeaId = Shader.PropertyToID("_PCSea");'),
('''            float field = on && r.sharedMaterial.name.StartsWith("BL_Field") ? 1f : 0f;   // 5b: 라임 밭 → 바랜 올리브/황토
            r.GetPropertyBlock(mpb);
            if (Mathf.Approximately(mpb.GetFloat(SnapId), k) && Mathf.Approximately(mpb.GetFloat(KeepId), keep) && Mathf.Approximately(mpb.GetFloat(FieldId), field)) continue;
            mpb.SetFloat(SnapId, k); mpb.SetFloat(KeepId, keep); mpb.SetFloat(FieldId, field); r.SetPropertyBlock(mpb);''',
'''            float field = on && r.sharedMaterial.name.StartsWith("BL_Field") ? 1f : 0f;   // 5b: 라임 밭 → 바랜 올리브/황토
            float sea = on && r.sharedMaterial.name == "BL_Sea" ? 1f : 0f;   // 2203: 노을 바다 청보라 + 해 반사
            r.GetPropertyBlock(mpb);
            if (Mathf.Approximately(mpb.GetFloat(SnapId), k) && Mathf.Approximately(mpb.GetFloat(KeepId), keep) && Mathf.Approximately(mpb.GetFloat(FieldId), field) && Mathf.Approximately(mpb.GetFloat(SeaId), sea)) continue;
            mpb.SetFloat(SnapId, k); mpb.SetFloat(KeepId, keep); mpb.SetFloat(FieldId, field); mpb.SetFloat(SeaId, sea); r.SetPropertyBlock(mpb);'''),
]
for a,b in R:
    assert a in s,a[:60]; s=s.replace(a,b)
open(p,'w',encoding='utf-8').write(s)
print("ok")
