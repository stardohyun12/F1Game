using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포장 화물의 "카드보드 택배 박스" 외형을 코드로 생성 (회색 큐브 대체).
/// 종류별(CargoType) 색 틴트 + 컬러 핸들링 스티커 + 취급주의 글리프로 구분.
/// 프로시저럴 텍스처 — 메시·콜라이더 안 건드림 → 포장 리본 시스템과 무관 (2026-05-20).
/// 종류별 머티리얼 캐시 (앱 수명 동안 재사용).
/// </summary>
public static class CargoBoxAppearance
{
    static readonly Dictionary<CargoType, Material> _cache = new();

    public static Material CardboardMaterial(CargoType type = CargoType.Box)
    {
        if (_cache.TryGetValue(type, out var m) && m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(sh) { name = $"CargoCardboard_{type}" };
        mat.mainTexture = BuildTexture(256, type);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        _cache[type] = mat;
        return mat;
    }

    static Texture2D BuildTexture(int size, CargoType type)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { name = $"CargoTex_{type}" };
        var accent = CargoTypePresets.BoxColor(type);              // 종류별 색
        // 카드보드 베이스를 종류색 쪽으로 살짝 틴트 → 트렁크에서도 한눈에 구분
        var baseCol = Color.Lerp(new Color(0.74f, 0.57f, 0.38f), accent, 0.18f);
        var seam = baseCol * 0.72f;
        var tape = Color.Lerp(baseCol, new Color(0.9f, 0.86f, 0.7f), 0.6f);
        var fragile = (type != CargoType.Box);
        var rng = new System.Random(7 + (int)type);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)(size - 1), v = y / (float)(size - 1);
            Color c = baseCol;

            float n = (float)rng.NextDouble() * 0.05f - 0.025f;
            c.r += n; c.g += n; c.b += n;

            float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
            if (edge < 0.06f) c = Color.Lerp(seam, c, edge / 0.06f);

            if (Mathf.Abs(v - 0.5f) < 0.012f) c = seam;                                  // 가로 이음새
            if (v > 0.5f && Mathf.Abs(u - 0.5f) < 0.010f) c = Color.Lerp(c, seam, 0.7f); // 세로 플랩
            if (Mathf.Abs(u - 0.5f) < 0.07f) c = Color.Lerp(c, tape, 0.55f);             // 포장 테이프

            // 핸들링 스티커 — 좌상단, 종류색
            if (u > 0.12f && u < 0.42f && v > 0.60f && v < 0.88f)
            {
                float su = (u - 0.12f) / 0.30f, sv = (v - 0.60f) / 0.28f;
                bool border = su < 0.06f || su > 0.94f || sv < 0.06f || sv > 0.94f;
                c = border ? accent * 0.55f : Color.Lerp(accent, Color.white, 0.25f);
                if (fragile && InExclaim(su, sv)) c = accent * 0.4f; // 취급주의 "!"
            }

            tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true);
        return tex;
    }

    // 스티커 로컬좌표(0..1)에서 "!" 글리프 영역
    static bool InExclaim(float su, float sv)
    {
        bool bar = Mathf.Abs(su - 0.5f) < 0.07f && sv > 0.30f && sv < 0.72f; // 세로 막대
        bool dot = (new Vector2(su - 0.5f, sv - 0.20f)).sqrMagnitude < 0.006f; // 아래 점
        return bar || dot;
    }
}
