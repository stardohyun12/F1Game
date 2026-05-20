using UnityEngine;

/// <summary>
/// 포장 화물의 "카드보드 택배 박스" 외형을 코드로 생성 (회색 큐브 대체).
/// 프로시저럴 텍스처(카드보드 결 + 플랩 이음새 + 테이프 + 라벨) + 플랫 URP 머티리얼.
/// 메시·콜라이더는 안 건드림 → 포장 리본 시스템과 무관 (2026-05-20 game-craft).
/// 단일 공유 머티리얼 캐시 — 앱 수명 동안 1개 재사용.
/// </summary>
public static class CargoBoxAppearance
{
    static Material _cached;

    public static Material CardboardMaterial()
    {
        if (_cached != null) return _cached;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        _cached = new Material(sh) { name = "CargoCardboard(proc)" };
        var tex = BuildCardboardTexture(256);
        _cached.mainTexture = tex;
        // 플랫·무광 카드보드
        if (_cached.HasProperty("_Smoothness")) _cached.SetFloat("_Smoothness", 0.04f);
        if (_cached.HasProperty("_Metallic")) _cached.SetFloat("_Metallic", 0f);
        return _cached;
    }

    static Texture2D BuildCardboardTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { name = "CargoCardboardTex" };
        var baseCol = new Color(0.74f, 0.57f, 0.38f);   // 카드보드 탄
        var seam    = new Color(0.55f, 0.40f, 0.25f);   // 어두운 이음새
        var tape    = new Color(0.86f, 0.80f, 0.62f);   // 반투명 포장 테이프
        var label   = new Color(0.93f, 0.91f, 0.85f);   // 송장 라벨
        var rng = new System.Random(7);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)(size - 1), v = y / (float)(size - 1);
            Color c = baseCol;

            // 카드보드 결 노이즈 (미세 명암)
            float n = (float)rng.NextDouble() * 0.05f - 0.025f;
            c.r += n; c.g += n; c.b += n;

            // 가장자리 살짝 어둡게 (입체감)
            float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
            if (edge < 0.06f) c = Color.Lerp(seam, c, edge / 0.06f);

            // 상단 플랩 이음새 — 가로 중앙선
            if (Mathf.Abs(v - 0.5f) < 0.012f) c = seam;
            // 플랩 접힘 — 세로 중앙(위쪽 절반만)
            if (v > 0.5f && Mathf.Abs(u - 0.5f) < 0.010f) c = Color.Lerp(c, seam, 0.7f);

            // 포장 테이프 — 세로 띠
            if (Mathf.Abs(u - 0.5f) < 0.07f) c = Color.Lerp(c, tape, 0.55f);

            // 송장 라벨 — 우하단 사각
            if (u > 0.58f && u < 0.86f && v > 0.14f && v < 0.34f)
            {
                c = label;
                // 라벨 위 가짜 텍스트 줄
                float ly = (v - 0.14f) / 0.20f;
                if ((ly > 0.30f && ly < 0.38f) || (ly > 0.55f && ly < 0.63f)) c = seam;
            }

            tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true);
        return tex;
    }
}
