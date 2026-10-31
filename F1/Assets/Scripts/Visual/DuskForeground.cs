using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-236 8판 낚시 자리 앞쪽 — 「내가 부두 끝에 앉아 있다」: 회색 대역 부두(Piers/Pier) 윗면 위에 판자 바닥(줄·틈·바랜 칠)과 소품 실루엣(양동이·도구상자).
///  · 아트 에셋이 아니다(D-188 대상 아님): 이 자리에서만 보이는 대역 모양. 실제 모양은 Codex 단계에서. 부두 위치·형태는 안 바꾼다(위에 얹기만).
///  · 노을 연출물(SunsetLook.backdrop) 아래 두어 노을 때만 켜진다. 생성물은 저장 안 함(DontSave).
///  · 자리 기준 축: origin(Cam_Sunset)의 수평 정면 f, 오른쪽 r. 값은 m.
/// </summary>
[ExecuteAlways]
public sealed class DuskForeground : MonoBehaviour
{
    public Transform origin;
    [Tooltip("부두 윗면 높이(월드 y). 판자는 이 위 3.5 cm.")]
    public float deckY = 1.30f;
    [Tooltip("판자 덮을 범위(자리 기준): x 오른쪽 끝 · y 왼쪽으로 · z 뒤로 · w 앞 끝(부두 끝)")]
    public Vector4 deckRect = new Vector4(1.3f, 8f, 0.3f, 2.2f);   // 오른쪽 1.3 m: 대 밑동(손 0.38 m 오른쪽)이 판자 위에서 나오게 — 회색 부두 B(0.2 m)보다 1.1 m 바깥으로 걸친다
    [Tooltip("판자 폭 · 틈(m)")]
    public Vector2 board = new Vector2(0.145f, 0.02f);
    [Tooltip("바랜 칠 판자 색 몇 가지(판자마다 하나)")]
    public Color[] paint = { new Color(0.42f, 0.32f, 0.27f), new Color(0.46f, 0.35f, 0.28f), new Color(0.39f, 0.31f, 0.27f), new Color(0.44f, 0.34f, 0.30f) };   // 노을 받은 바랜 칠(따뜻하게)
    public Color gapColor = new Color(0.10f, 0.08f, 0.07f);
    [Header("소품(자리 기준 x 오른쪽, y 앞, z 돌림°)")]
    public bool bucket = true;
    public Vector3 bucketAt = new Vector3(-1.4f, 2.05f, 0f);
    public Color bucketColor = new Color(0.36f, 0.35f, 0.34f);   // 옛 청회색(.34,.45,.49)은 앞쪽에서 제일 먼저 눈을 끌었다
    public bool tackleBox = true;
    public Vector3 boxAt = new Vector3(-2.1f, 1.9f, 20f);
    public Color boxColor = new Color(0.31f, 0.40f, 0.32f);

    readonly List<GameObject> built = new List<GameObject>();
    readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();

    void OnEnable() => Rebuild();
    void OnDisable() => Clear();
    void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

    void Clear()
    {
        foreach (var g in built) if (g != null) { if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); }
        built.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if ((c.hideFlags & HideFlags.DontSave) != 0) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
        }
        mats.Clear();
    }

    Material Mat(Color c)
    {
        if (mats.TryGetValue(c, out var m)) return m;
        var sh = Shader.Find("PitCrew/LofiPilot/Surface"); if (sh == null) return null;
        m = new Material(sh) { name = "DuskFG (대역)", hideFlags = HideFlags.DontSave };
        m.SetColor("_Tint", c); m.SetColor("_Ambient", new Color(0.70f, 0.72f, 0.76f));
        m.SetColor("_FogColor", RenderSettings.fogColor); m.SetFloat("_FogStart", 200f); m.SetFloat("_FogEnd", 900f); m.SetFloat("_FaceShade", 0.45f);
        mats[c] = m; return m;
    }

    // 면마다 꼭짓점을 따로(평평한 면 그늘). 재질별로 모음
    sealed class Builder
    {
        public readonly Dictionary<Color, (List<Vector3> v, List<int> t)> parts = new Dictionary<Color, (List<Vector3>, List<int>)>();
        public void Quad(Color c, Vector3 a, Vector3 b, Vector3 cc, Vector3 d)   // a→b→c→d 가 보는 쪽에서 시계 방향
        {
            if (!parts.TryGetValue(c, out var p)) { p = (new List<Vector3>(), new List<int>()); parts[c] = p; }
            int i = p.v.Count; p.v.Add(a); p.v.Add(b); p.v.Add(cc); p.v.Add(d);
            p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2); p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3);
        }
        // 상자: 중심 o, 축 x·y·z(반길이 포함한 벡터)
        public void Box(Color c, Vector3 o, Vector3 x, Vector3 y, Vector3 z, bool bottom = false)
        {
            Vector3 P(float a, float b, float d) => o + x * a + y * b + z * d;
            Quad(c, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));          // 위
            Quad(c, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));      // -z
            Quad(c, P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));          // +z
            Quad(c, P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));      // -x
            Quad(c, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));          // +x
            if (bottom) Quad(c, P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
        }
    }

    static float Hash(int s, int i) { uint h = (uint)(s * 73856093) ^ (uint)(i * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xffff) / 65535f; }

    public void Rebuild()
    {
        Clear();
        if (origin == null) return;
        Vector3 f = origin.forward; f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        Vector3 r = Vector3.Cross(Vector3.up, f), up = Vector3.up;
        Vector3 o = new Vector3(origin.position.x, deckY, origin.position.z);
        var B = new Builder();

        // 틈 = 판자 아래 어두운 바닥 한 장
        float xr = deckRect.x, xl = -deckRect.y, zb = -deckRect.z, zf = deckRect.w;
        Vector3 W(float x, float y, float z) => o + r * x + up * y + f * z;
        B.Quad(gapColor, W(xl, 0.006f, zb), W(xl, 0.006f, zf), W(xr, 0.006f, zf), W(xr, 0.006f, zb));

        // 판자: 앞뒤(f)로 길게, 오른쪽 끝에서 왼쪽으로 줄지어. 판자마다 바랜 칠 하나, 끝은 ±2.5 cm 들쭉날쭉, 가끔 이음매(1.2 cm 틈)
        const float top = 0.035f;
        int n = Mathf.CeilToInt((xr - xl) / (board.x + board.y));
        for (int i = 0; i < n; i++)
        {
            float x1 = xr - i * (board.x + board.y), x0 = Mathf.Max(xl, x1 - board.x);
            if (x1 <= xl) break;
            var c = paint[Mathf.FloorToInt(Hash(3, i) * paint.Length) % paint.Length];
            float front = zf + (Hash(5, i) - 0.5f) * 0.05f;
            float joint = Hash(9, i) < 0.35f ? Mathf.Lerp(zb + 0.3f, zf - 0.4f, Hash(13, i)) : float.NaN;
            void Plank(float z0, float z1)
            {
                B.Box(c, W((x0 + x1) * 0.5f, (top + 0.006f) * 0.5f, (z0 + z1) * 0.5f), r * ((x1 - x0) * 0.5f), up * ((top - 0.006f) * 0.5f), f * ((z1 - z0) * 0.5f));
            }
            if (float.IsNaN(joint)) Plank(zb, front);
            else { Plank(zb, joint - 0.006f); Plank(joint + 0.006f, front); }
        }

        // 양동이: 10각 깔때기(바닥 r .12 → 위 .15, 높이 .30), 안은 어둡게, 손잡이 활
        if (bucket)
        {
            Vector3 bc = W(bucketAt.x, top, bucketAt.y);
            const int s = 10; float r0 = 0.12f, r1 = 0.15f, h = 0.30f;
            var inner = new Color(0.12f, 0.12f, 0.13f);
            for (int k = 0; k < s; k++)
            {
                float a0 = k * Mathf.PI * 2f / s, a1 = (k + 1) * Mathf.PI * 2f / s;
                Vector3 d0 = r * Mathf.Cos(a0) + f * Mathf.Sin(a0), d1 = r * Mathf.Cos(a1) + f * Mathf.Sin(a1);
                B.Quad(bucketColor, bc + d0 * r0, bc + d0 * r1 + up * h, bc + d1 * r1 + up * h, bc + d1 * r0);                       // 바깥
                B.Quad(inner, bc + d1 * (r0 - 0.008f) + up * 0.02f, bc + d1 * (r1 - 0.008f) + up * h, bc + d0 * (r1 - 0.008f) + up * h, bc + d0 * (r0 - 0.008f) + up * 0.02f);   // 안
                B.Quad(inner, bc + up * 0.02f, bc + d1 * (r0 - 0.008f) + up * 0.02f, bc + d0 * (r0 - 0.008f) + up * 0.02f, bc + up * 0.02f);  // 안 바닥
            }
            var hc = new Color(0.30f, 0.30f, 0.30f);
            for (int k = 0; k < 8; k++)   // 손잡이: 오른쪽 귀~왼쪽 귀 반원, 뒤로 25° 누움
            {
                float a = Mathf.PI * (k + 0.5f) / 8f;
                Vector3 axisUp = Quaternion.AngleAxis(-25f, r) * up;
                Vector3 p = bc + up * (h - 0.02f) + r * (Mathf.Cos(a) * r1) + axisUp * (Mathf.Sin(a) * r1 * 0.95f);
                Vector3 tng = (-r * Mathf.Sin(a) + axisUp * Mathf.Cos(a) * 0.95f).normalized;
                B.Box(hc, p, tng * (Mathf.PI * r1 / 16f + 0.004f), Vector3.Cross(f, tng).normalized * 0.004f, f * 0.004f);
            }
        }

        // 도구상자: 몸통 .36×.16×.20 + 뚜껑 + 손잡이
        if (tackleBox)
        {
            Quaternion q = Quaternion.AngleAxis(boxAt.z, up);
            Vector3 bx = q * r, bz = q * f, bc = W(boxAt.x, top, boxAt.y);
            B.Box(boxColor, bc + up * 0.07f, bx * 0.18f, up * 0.07f, bz * 0.10f);
            var lid = new Color(boxColor.r * 1.08f, boxColor.g * 1.08f, boxColor.b * 1.06f);
            B.Box(lid, bc + up * 0.165f, bx * 0.185f, up * 0.025f, bz * 0.105f);
            var hc = new Color(0.16f, 0.15f, 0.14f);
            B.Box(hc, bc + up * 0.215f, bx * 0.07f, up * 0.008f, bz * 0.012f);
            B.Box(hc, bc + up * 0.20f + bx * 0.065f, bx * 0.008f, up * 0.015f, bz * 0.012f);
            B.Box(hc, bc + up * 0.20f - bx * 0.065f, bx * 0.008f, up * 0.015f, bz * 0.012f);
            B.Box(new Color(0.55f, 0.52f, 0.44f), bc + up * 0.13f - bz * 0.101f, bx * 0.02f, up * 0.02f, bz * 0.004f);   // 걸쇠
        }

        foreach (var kv in B.parts)
        {
            var g = new GameObject("DuskFG_" + built.Count) { hideFlags = HideFlags.DontSave };
            g.transform.SetParent(transform, false); g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var mesh = new Mesh { name = g.name, hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(kv.Value.v); mesh.SetTriangles(kv.Value.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mat(kv.Key);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; mr.receiveShadows = true;
            built.Add(g);
        }
    }
}
