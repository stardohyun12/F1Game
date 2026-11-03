using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// D-295 지형 틈 규칙: T자 이음매 꿰매기.
/// 길·건물 둘레만 잘게 쪼갠 삼각형(1→4) 옆에 안 쪼갠 큰 삼각형이 붙으면, 큰 변 위에 작은 쪽 정점이 「매달린다」.
/// 그 뒤 깎기·패드가 매달린 정점 높이만 옮기면 큰 변(직선)과 사이가 벌어져 하늘이 비친다(그림3 흰 조각).
/// → 큰 삼각형을 매달린 정점까지 이어 쪼갠다. 정점 위치는 하나도 안 옮긴다(건물·길·DP 높이 그대로).
/// V2RefineTerrain · TerraceBlock · ParcelBuild.Pads 가 지형을 쓸 때마다 마지막에 부른다.
/// </summary>
public static class TerrainSeal
{
    /// <summary>메시를 그 자리에서 꿰맨다. 돌려줌 = 고친 큰 변 수. 정점은 뒤에 붙기만(중심점) — 기존 번호 그대로.</summary>
    public static int Stitch(Mesh mesh, out string note)
    {
        var V = new List<Vector3>(); mesh.GetVertices(V);
        var N = new List<Vector3>(); mesh.GetNormals(N); bool hasN = N.Count == V.Count;
        var tris = new List<int>(); mesh.GetTriangles(tris, 0);
        int fixedEdges = 0, centroids = 0, rounds = 0, holes = 0;
        for (; rounds < 3; rounds++)
        {
            int f = Round(V, hasN ? N : null, tris, ref centroids, out holes);
            fixedEdges += f; if (f == 0) break;
        }
        if (fixedEdges > 0)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(V); if (hasN) mesh.SetNormals(N);
            mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
        }
        note = $"T자 꿰매기 큰 변 {fixedEdges}개(중심점 {centroids}) · {rounds + 1}바퀴 · 남은 열린 변(맵 둘레 포함) {holes}";
        return fixedEdges;
    }

    /// <summary>1 mm 격자 정점 키(축마다 21비트, ±1 km).</summary>
    static long K(Vector3 p) => ((long)(Mathf.RoundToInt(p.x * 1000) + (1 << 20)) << 42) | ((long)(Mathf.RoundToInt(p.y * 1000) + (1 << 20)) << 21) | (long)(Mathf.RoundToInt(p.z * 1000) + (1 << 20));
    /// <summary>long 기본 해시(hi ^ lo)는 변 (a, a+1) 을 해시 20여 개로 몰아 사전이 n² 가 된다 — 섞어서 쓴다.</summary>
    sealed class MixLong : IEqualityComparer<long>
    {
        public static readonly MixLong I = new MixLong();
        public bool Equals(long a, long b) => a == b;
        public int GetHashCode(long k) { ulong x = (ulong)k * 0x9E3779B97F4A7C15UL; return (int)(x >> 32) ^ (int)x; }
    }

    static int Round(List<Vector3> V, List<Vector3> N, List<int> tris, ref int centroids, out int openLeft)
    {
        // 같은 자리 정점은 한 점으로(이음매 정점이 갈라져 있어도 변을 맞게 센다)
        var weld = new int[V.Count]; var first = new Dictionary<long, int>(V.Count, MixLong.I);
        for (int i = 0; i < V.Count; i++) { var k = K(V[i]); if (!first.TryGetValue(k, out int r)) first[k] = r = i; weld[i] = r; }
        var cnt = new Dictionary<long, int>(tris.Count, MixLong.I); var owner = new Dictionary<long, int>(tris.Count, MixLong.I);   // 변 → (삼각형*3+칸)
        for (int t = 0; t < tris.Count; t += 3)
            for (int e = 0; e < 3; e++)
            {
                int a = weld[tris[t + e]], b = weld[tris[t + (e + 1) % 3]]; if (a == b) continue;
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (cnt.TryGetValue(key, out int c)) cnt[key] = c + 1; else { cnt[key] = 1; owner[key] = t + e; }
            }
        var open = new List<long>(); foreach (var kv in cnt) if (kv.Value == 1) open.Add(kv.Key);
        openLeft = open.Count;
        // 매달릴 수 있는 정점 = 열린 변의 끝점. 1 m 칸에 담는다.
        var grid = new Dictionary<(int, int), List<int>>();
        var seen = new HashSet<int>();
        foreach (var key in open)
            foreach (int v in new[] { (int)(key >> 32), (int)(key & 0xffffffff) })
                if (seen.Add(v)) { var p = V[v]; var g = (Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)); if (!grid.TryGetValue(g, out var l)) grid[g] = l = new List<int>(); l.Add(v); }
        // 삼각형 칸별 매달린 정점(변 위 비율 순)
        var hang = new Dictionary<int, List<(float s, int v)>[]>();
        int fixedN = 0;
        foreach (var key in open)
        {
            int slot = owner[key], t = slot - slot % 3, e = slot % 3;
            int a = tris[t + e], b = tris[t + (e + 1) % 3]; Vector3 A = V[a], B = V[b];
            var ab = new Vector2(B.x - A.x, B.z - A.z); float L2 = ab.sqrMagnitude; if (L2 < 1e-6f) continue;
            List<(float, int)> hs = null;
            for (int gz = Mathf.FloorToInt(Mathf.Min(A.z, B.z)); gz <= Mathf.FloorToInt(Mathf.Max(A.z, B.z)); gz++)
                for (int gx = Mathf.FloorToInt(Mathf.Min(A.x, B.x)); gx <= Mathf.FloorToInt(Mathf.Max(A.x, B.x)); gx++)
                {
                    if (!grid.TryGetValue((gx, gz), out var l)) continue;
                    foreach (int v in l)
                    {
                        if (v == weld[a] || v == weld[b]) continue;
                        var p = new Vector2(V[v].x - A.x, V[v].z - A.z); float s = Vector2.Dot(p, ab) / L2;
                        if (s <= 1e-3f || s >= 1 - 1e-3f) continue;
                        if ((ab * s - p).sqrMagnitude > 4e-6f) continue;   // 2 mm 안 = 그 변 위
                        (hs ??= new List<(float, int)>()).Add((s, v));
                    }
                }
            if (hs == null) continue;
            hs.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            if (!hang.TryGetValue(t, out var arr)) hang[t] = arr = new List<(float, int)>[3];
            arr[e] = hs; fixedN++;
        }
        if (fixedN == 0) return 0;
        var nt = new List<int>(tris.Count + fixedN * 9);
        for (int t = 0; t < tris.Count; t += 3)
        {
            if (!hang.TryGetValue(t, out var arr)) { nt.Add(tris[t]); nt.Add(tris[t + 1]); nt.Add(tris[t + 2]); continue; }
            int sides = (arr[0] != null ? 1 : 0) + (arr[1] != null ? 1 : 0) + (arr[2] != null ? 1 : 0);
            if (sides == 1)
            {
                // 매달린 변을 첫 변으로 돌리고 맞은편 꼭짓점에서 부채꼴 — 감는 방향(a→b→c) 그대로
                int e = arr[0] != null ? 0 : arr[1] != null ? 1 : 2;
                int a = tris[t + e], b = tris[t + (e + 1) % 3], c = tris[t + (e + 2) % 3];
                int prev = a; foreach (var (_, v) in arr[e]) { nt.Add(prev); nt.Add(v); nt.Add(c); prev = v; }
                nt.Add(prev); nt.Add(b); nt.Add(c);
            }
            else
            {
                // 두 변 이상: 꼭짓점만으로는 납작한 삼각형이 생긴다 → 무게중심 정점 하나 더해 둘레 부채꼴
                var ring = new List<int>();
                for (int e = 0; e < 3; e++) { ring.Add(tris[t + e]); if (arr[e] != null) foreach (var (_, v) in arr[e]) ring.Add(v); }
                Vector3 A = V[tris[t]], B = V[tris[t + 1]], C = V[tris[t + 2]];
                int g = V.Count; V.Add((A + B + C) / 3f); centroids++;
                if (N != null) N.Add((N[tris[t]] + N[tris[t + 1]] + N[tris[t + 2]]).normalized);
                for (int i = 0; i < ring.Count; i++) { nt.Add(ring[i]); nt.Add(ring[(i + 1) % ring.Count]); nt.Add(g); }
            }
        }
        tris.Clear(); tris.AddRange(nt);
        return fixedN;
    }

    /// <summary>지금 씬 지형 에셋에 같은 규칙을 한 번(= ParcelBuild.Pads 마지막 단계만 다시). 정점은 안 움직이니 건물·길·DP 그대로.</summary>
    [MenuItem("PitCrew/지형 T자 틈 꿰매기 (D-295)")] static void MenuRun() => Run();
    public static string Run()
    {
        if (Application.isPlaying) return "Play 중엔 안 됨";
        var terr = GameObject.Find("TownV6_Gray/TownV6_Terrain"); if (terr == null) return "TownV6_Terrain 없음";
        var mf = terr.GetComponent<MeshFilter>(); var src = mf.sharedMesh; string path = AssetDatabase.GetAssetPath(src);
        if (string.IsNullOrEmpty(path)) return "지형 메시가 에셋이 아님";
        var m = Object.Instantiate(src); m.name = src.name;
        int n = Stitch(m, out string note);
        if (n > 0) { EditorUtility.CopySerialized(m, src); src.name = m.name; AssetDatabase.SaveAssets(); }
        Object.DestroyImmediate(m);
        var mc = terr.GetComponent<MeshCollider>(); if (mc != null) { mc.sharedMesh = null; mc.sharedMesh = src; }
        Physics.SyncTransforms();
        string r = path + " · " + note; Debug.Log("[TerrainSeal] " + r); return r;
    }
}
