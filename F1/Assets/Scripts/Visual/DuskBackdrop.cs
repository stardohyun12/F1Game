using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-236 노을 낚시 자리 연출물 — 수평선에 「보는 곳」을 둔다: 먼 곶·섬, 곶 끝 등대, 작은 배. 전부 단색 실루엣 판(DuskSilhouette).
///  · 아트 에셋이 아니다(D-188 대상 아님): 이 자리에서 보이는 외곽선만 코드로 그린 판이고, 노을(SunsetLook.backdrop)일 때만 켜진다.
///  · 판은 자리(origin)를 향해 선다. 생성물은 씬에 저장 안 함(DontSave) — 켤 때마다 값으로 다시 만든다.
///  · 위치는 자리 기준 방위(°, 0 = +z)·거리(m). 해 방위(SunsetLook.sunAzimuth)를 같이 보며 놓는다.
/// </summary>
[ExecuteAlways]
public sealed class DuskBackdrop : MonoBehaviour
{
    public enum Kind { Headland, Island, Lighthouse, Boat }

    [System.Serializable]
    public struct Piece
    {
        public Kind kind;
        [Tooltip("자리 기준 방위(°, 0 = +z, 90 = +x)")] public float azimuth;
        public float distance;
        [Tooltip("판 너비(m) — 등대·배는 배율(1 = 기본 크기)")] public float width;
        public float height;
        [Tooltip("곶: 1 = 끝(절벽)이 오른쪽, -1 = 왼쪽")] public float tipSide;
        [Range(0f, 1f)] public float haze;
        public float baseY;
        public int seed;
    }

    public Transform origin;
    public Material material;
    public List<Piece> pieces = new List<Piece>();

    readonly List<GameObject> built = new List<GameObject>();
    static readonly int HazeId = Shader.PropertyToID("_Haze");

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
    }

    public void Rebuild()
    {
        Clear();
        if (material == null) return;
        Vector3 o = origin != null ? origin.position : transform.position; o.y = 0f;
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            float a = p.azimuth * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var g = new GameObject("Dusk_" + p.kind + "_" + i) { hideFlags = HideFlags.DontSave };
            g.transform.SetParent(transform, false);
            g.transform.SetPositionAndRotation(o + dir * p.distance + Vector3.up * p.baseY, Quaternion.LookRotation(dir, Vector3.up));
            var mesh = new Mesh { name = g.name, hideFlags = HideFlags.DontSave };
            var v = new List<Vector3>(); var t = new List<int>();
            switch (p.kind)
            {
                case Kind.Headland: Ridge(v, t, p, true); break;
                case Kind.Island: Ridge(v, t, p, false); break;
                case Kind.Lighthouse: Lighthouse(v, t, Mathf.Max(0.1f, p.width)); break;
                case Kind.Boat: Boat(v, t, Mathf.Max(0.1f, p.width)); break;
            }
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var mpb = new MaterialPropertyBlock(); mpb.SetFloat(HazeId, p.haze); r.SetPropertyBlock(mpb);
            built.Add(g);
        }
    }

    // 사각(아래 두 점 x0b·x1b @ y0, 위 두 점 x0t·x1t @ y1)
    static void Quad(List<Vector3> v, List<int> t, float x0b, float x1b, float y0, float x0t, float x1t, float y1)
    {
        int b = v.Count;
        v.Add(new Vector3(x0b, y0, 0)); v.Add(new Vector3(x1b, y0, 0)); v.Add(new Vector3(x1t, y1, 0)); v.Add(new Vector3(x0t, y1, 0));
        t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
    }

    static float Hash(int s, int i) { uint h = (uint)(s * 73856093) ^ (uint)(i * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xffff) / 65535f; }

    // 곶 = 한쪽에서 완만히 올라 봉우리 두어 개, 끝은 짧은 절벽 / 섬 = 양쪽 완만한 혹
    static void Ridge(List<Vector3> v, List<int> t, Piece p, bool headland)
    {
        const int n = 28;
        float w = p.width, h = p.height, side = p.tipSide >= 0 ? 1f : -1f;
        float prevX = 0, prevY = 0;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;   // 0 = 뭍 쪽, 1 = 끝
            float env;
            if (headland) env = Mathf.SmoothStep(0.55f, 1f, Mathf.Min(1f, u * 3f)) * (u < 0.86f ? 1f - 0.35f * u : Mathf.Lerp(0.70f, 0.0f, (u - 0.86f) / 0.14f));
            else env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u)) * (0.85f + 0.15f * Mathf.Sin(u * 9f));
            float bump = 0.82f + 0.18f * Hash(p.seed, i) + 0.12f * Mathf.Sin(u * 11f + p.seed);
            float y = Mathf.Max(0f, h * env * bump);
            if (i == n) y = 0f;
            float x = (u - 0.5f) * w * side;
            if (i > 0)
            {
                // 세로 띠 하나: 아래 -3 m(물 밑) ~ 위는 두 점을 잇는 비탈
                int b = v.Count;
                v.Add(new Vector3(prevX, -3f, 0)); v.Add(new Vector3(x, -3f, 0)); v.Add(new Vector3(x, y, 0)); v.Add(new Vector3(prevX, prevY, 0));
                t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
            }
            prevX = x; prevY = y;
        }
    }

    // 등대(기본 높이 ≈ 16 m): 좁아지는 탑 · 난간 · 등불 칸 · 지붕
    static void Lighthouse(List<Vector3> v, List<int> t, float s)
    {
        Quad(v, t, -1.7f * s, 1.7f * s, -1f, -1.15f * s, 1.15f * s, 11.5f * s);
        Quad(v, t, -1.75f * s, 1.75f * s, 11.5f * s, -1.75f * s, 1.75f * s, 12.4f * s);
        Quad(v, t, -0.85f * s, 0.85f * s, 12.4f * s, -0.85f * s, 0.85f * s, 14.4f * s);
        Quad(v, t, -1.25f * s, 1.25f * s, 14.4f * s, 0f, 0f, 16.0f * s);
    }

    // 작은 고깃배(기본 길이 ≈ 7 m): 선체 · 조타실 · 돛대
    static void Boat(List<Vector3> v, List<int> t, float s)
    {
        Quad(v, t, -2.6f * s, 2.9f * s, -0.4f, -3.4f * s, 3.6f * s, 1.1f * s);
        Quad(v, t, -2.2f * s, -0.4f * s, 1.1f * s, -2.1f * s, -0.5f * s, 2.9f * s);
        Quad(v, t, 0.9f * s, 1.3f * s, 1.1f * s, 0.95f * s, 1.25f * s, 5.6f * s);
    }
}
