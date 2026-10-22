using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>조각 15a-3 검사 O2: 씬에 실제로 놓인 메시로 "건물 ∩ 길 면"을 잰다(prep 데이터·생성 가정이 아니라 그린 것).
/// - 건물 = OsmBuildings 아래 메시의 지붕(수평 위 면) 삼각형 덩어리 하나 = 한 채. 벽은 수직이라 지붕 = 바닥 윤곽.
/// - 길 = Roads 아래 메시의 위를 향한 삼각형(|n.y| ≥ 0.5).
/// - 0.5m 칸 가운데 점이 건물 지붕 안이고 길 삼각형 안이면 그 칸은 겹침. 상자 전체(반경 제한 없음).
/// - 예외: 그 칸 길 면 높이 + 3m ≤ 건물 바닥(벽 아랫단) — 길이 실제로 건물 밑(땅 밑)을 지날 때만.
/// - 차도·보도 겹침 = FAIL. 어깨(흙 띠) = 숫자만.</summary>
public static class TownGenSceneOverlap
{
    public const float Cell = 0.5f, UnderClearance = 3f;
    public static readonly string[] Car = { "Asphalt", "Alleys", "TownSheet_Lane", "PlazaPockets" };
    public static readonly string[] Walk = { "Sidewalk_Curb", "TownSheet_Walk", "TownSteps", "PublicStairs", "Driveways" };
    public static readonly string[] Soil = { "Shoulder" };

    class Bld { public float minX = 1e9f, minZ = 1e9f, maxX = -1e9f, maxZ = -1e9f, roofY, baseY, area; }

    static long Key(int i, int j) => (long)i * 1000000L + j;

    static bool InTri(Vector3 a, Vector3 b, Vector3 d, float px, float pz)
    {
        float d1 = (b.x - a.x) * (pz - a.z) - (b.z - a.z) * (px - a.x), d2 = (d.x - b.x) * (pz - b.z) - (d.z - b.z) * (px - b.x), d3 = (a.x - d.x) * (pz - d.z) - (a.z - d.z) * (px - d.x);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0; return !(neg && pos);
    }

    /// <summary>삼각형 평면에서 (px,pz) 높이.</summary>
    static float YAt(Vector3 a, Vector3 b, Vector3 d, float px, float pz)
    {
        var n = Vector3.Cross(b - a, d - a); if (Math.Abs(n.y) < 1e-6f) return a.y;
        return a.y - (n.x * (px - a.x) + n.z * (pz - a.z)) / n.y;
    }

    /// <summary>root = 씬 구조 루트(OsmBuildings·Roads 를 자식으로 가짐). 결과 JSON, failCount = 차도·보도 겹침 건물 수(채 기준, 중복 제거).</summary>
    public static JObject Measure(Transform root, out int failBuildings)
    {
        var cellB = new Dictionary<long, int>(); var bl = new List<Bld>();
        var bRoot = root.Find("OsmBuildings");
        if (bRoot != null)
            foreach (Transform c in bRoot)
            {
                var mf = c.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh; var v = m.vertices; var tr = m.triangles;
                var w2 = c.localToWorldMatrix; for (int q = 0; q < v.Length; q++) v[q] = w2.MultiplyPoint3x4(v[q]);
                int cur = -1; bool prevUp = false; float wallMin = float.MaxValue;
                for (int k = 0; k < tr.Length; k += 3)
                {
                    var a = v[tr[k]]; var b = v[tr[k + 1]]; var d = v[tr[k + 2]];
                    var n = Vector3.Cross(b - a, d - a);
                    bool up = n.sqrMagnitude > 1e-8f && Math.Abs(n.normalized.y) > 0.99f && Math.Abs(a.y - b.y) < 0.01f && Math.Abs(a.y - d.y) < 0.01f;
                    if (!up) { if (prevUp) wallMin = float.MaxValue; prevUp = false; wallMin = Math.Min(wallMin, Math.Min(a.y, Math.Min(b.y, d.y))); continue; }
                    if (!prevUp) { cur = bl.Count; bl.Add(new Bld { roofY = a.y, baseY = wallMin == float.MaxValue ? a.y : wallMin }); }
                    prevUp = true;
                    var bi = bl[cur];
                    float x0 = Math.Min(a.x, Math.Min(b.x, d.x)), x1 = Math.Max(a.x, Math.Max(b.x, d.x)), z0 = Math.Min(a.z, Math.Min(b.z, d.z)), z1 = Math.Max(a.z, Math.Max(b.z, d.z));
                    bi.minX = Math.Min(bi.minX, x0); bi.minZ = Math.Min(bi.minZ, z0); bi.maxX = Math.Max(bi.maxX, x1); bi.maxZ = Math.Max(bi.maxZ, z1);
                    for (int i = (int)Math.Floor(x0 / Cell); i <= (int)Math.Floor(x1 / Cell); i++)
                        for (int j = (int)Math.Floor(z0 / Cell); j <= (int)Math.Floor(z1 / Cell); j++)
                        {
                            float px = (i + 0.5f) * Cell, pz = (j + 0.5f) * Cell;
                            if (!InTri(a, b, d, px, pz)) continue;
                            long key = Key(i, j); if (!cellB.ContainsKey(key)) { cellB[key] = cur; bi.area += Cell * Cell; }
                        }
                }
            }

        // kind → building → overlapped cells (예외 칸 따로)
        var hit = new Dictionary<string, Dictionary<int, HashSet<long>>>(); var exempt = new Dictionary<string, int>();
        var rRoot = root.Find("Roads");
        foreach (var kn in Car.Concat(Walk).Concat(Soil))
        {
            var per = new Dictionary<int, HashSet<long>>(); hit[kn] = per; exempt[kn] = 0;
            var go = rRoot != null ? rRoot.Find(kn) : null; if (go == null) continue;
            var mf = go.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; var v = m.vertices; var tr = m.triangles;
            var w2 = go.localToWorldMatrix; for (int q = 0; q < v.Length; q++) v[q] = w2.MultiplyPoint3x4(v[q]);
            var exCells = new HashSet<long>();
            for (int k = 0; k < tr.Length; k += 3)
            {
                var a = v[tr[k]]; var b = v[tr[k + 1]]; var d = v[tr[k + 2]];
                var n = Vector3.Cross(b - a, d - a); if (n.sqrMagnitude < 1e-10f || Math.Abs(n.normalized.y) < 0.5f) continue;
                float x0 = Math.Min(a.x, Math.Min(b.x, d.x)), x1 = Math.Max(a.x, Math.Max(b.x, d.x)), z0 = Math.Min(a.z, Math.Min(b.z, d.z)), z1 = Math.Max(a.z, Math.Max(b.z, d.z));
                for (int i = (int)Math.Floor(x0 / Cell); i <= (int)Math.Floor(x1 / Cell); i++)
                    for (int j = (int)Math.Floor(z0 / Cell); j <= (int)Math.Floor(z1 / Cell); j++)
                    {
                        long key = Key(i, j); if (!cellB.TryGetValue(key, out int bid)) continue;
                        float px = (i + 0.5f) * Cell, pz = (j + 0.5f) * Cell;
                        if (!InTri(a, b, d, px, pz)) continue;
                        if (YAt(a, b, d, px, pz) + UnderClearance <= bl[bid].baseY) { exCells.Add(key); continue; }   // 길이 실제로 건물 밑
                        if (!per.TryGetValue(bid, out var hs)) per[bid] = hs = new HashSet<long>(); hs.Add(key);
                    }
            }
            exempt[kn] = exCells.Count;
        }

        JObject Group(string[] kinds, out HashSet<int> bset)
        {
            var o = new JObject(); bset = new HashSet<int>(); double tot = 0;
            foreach (var kn in kinds)
            {
                var per = hit[kn]; double ar = per.Sum(kv => kv.Value.Count) * Cell * Cell; tot += ar;
                foreach (var b in per.Keys) bset.Add(b);
                o[kn] = new JObject { ["buildings"] = per.Count, ["buildingsGe1m2"] = per.Count(kv => kv.Value.Count * Cell * Cell >= 1f), ["area_m2"] = Math.Round(ar, 2), ["exemptUnderCells"] = exempt[kn] };
            }
            o["buildings"] = bset.Count; o["area_m2"] = Math.Round(tot, 2); return o;
        }
        var carJ = Group(Car, out var carB); var walkJ = Group(Walk, out var walkB); var soilJ = Group(Soil, out var soilB);
        var failSet = new HashSet<int>(carB); failSet.UnionWith(walkB); failBuildings = failSet.Count;

        var list = new JArray();
        foreach (var bid in failSet.Concat(soilB.Where(x => !failSet.Contains(x))).Distinct()
                     .OrderByDescending(x => Car.Concat(Walk).Sum(kn => hit[kn].TryGetValue(x, out var h) ? h.Count : 0)).ThenByDescending(x => hit["Shoulder"].TryGetValue(x, out var h2) ? h2.Count : 0))
        {
            var b = bl[bid]; var by = new JObject();
            foreach (var kn in Car.Concat(Walk).Concat(Soil)) if (hit[kn].TryGetValue(bid, out var h)) by[kn] = Math.Round(h.Count * Cell * Cell, 2);
            list.Add(new JObject { ["fail"] = failSet.Contains(bid), ["centre"] = new JArray(Math.Round((b.minX + b.maxX) / 2, 1), Math.Round((b.minZ + b.maxZ) / 2, 1)), ["bbox"] = new JArray(Math.Round(b.minX, 1), Math.Round(b.minZ, 1), Math.Round(b.maxX, 1), Math.Round(b.maxZ, 1)), ["baseY"] = Math.Round(b.baseY, 2), ["roofY"] = Math.Round(b.roofY, 2), ["footprint_m2"] = Math.Round(b.area, 1), ["overlap_m2"] = by });
            if (list.Count >= 200) break;
        }
        return new JObject
        {
            ["rule"] = $"scene meshes, whole box: OsmBuildings roof (flat up-facing) triangles = footprint, Roads/* up-facing (|n.y|≥0.5) triangles = road surface; {Cell}m cell centre inside both = overlap. Exempt only where road surface + {UnderClearance}m ≤ building wall bottom (road really under). FAIL = any car or walk overlap; shoulder (soil band) reported only.",
            ["buildings"] = bl.Count, ["buildingCells"] = cellB.Count,
            ["car"] = carJ, ["walk"] = walkJ, ["shoulder"] = soilJ,
            ["failBuildings"] = failBuildings, ["pass"] = failBuildings == 0,
            ["overlaps"] = list,
        };
    }
}
