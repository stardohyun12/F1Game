using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 조각 15a-4(PD 결정 B 2026-10-20): 건물은 두고 보도 면을 놓인 건물 윤곽에서 잘라낸다.
/// - 위/아래 향한 삼각형(|n.y| ≥ 0.5): xz 평면에서 삼각형 − 건물 윤곽(귀 자르기 볼록 조각마다 볼록 빼기) → 남은 볼록 조각을 원래 평면 높이로 다시 깐다.
/// - 옆면(연석 세로면): 무게중심이 건물 윤곽 안이면 뺀다(벽 안에 숨는 면).
/// - 예외: 삼각형 최저 높이 + 3m ≤ 건물 벽 아랫단이면 그 건물은 안 뺀다(O2 와 같은 "길이 실제로 건물 밑" 규칙).
/// </summary>
public static class TownGenWalkClip
{
    const double Eps = 1e-6, MinArea = 1e-4;

    static double Cross(Vec2 o, Vec2 a, Vec2 b) => (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
    static double Area(List<Vec2> p) { double s = 0; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += a.x * b.z - b.x * a.z; } return s / 2; }

    /// <summary>볼록 다각형 p 를 직선 a→b 의 한쪽(keepLeft = 왼쪽, 반시계 기준 안쪽)으로 자른다.</summary>
    static List<Vec2> ClipHalf(List<Vec2> p, Vec2 a, Vec2 b, bool keepLeft)
    {
        var o = new List<Vec2>(p.Count + 2);
        for (int i = 0; i < p.Count; i++)
        {
            var c = p[i]; var d = p[(i + 1) % p.Count];
            double sc = Cross(a, b, c) * (keepLeft ? 1 : -1), sd = Cross(a, b, d) * (keepLeft ? 1 : -1);
            if (sc >= -Eps) o.Add(c);
            if ((sc > Eps && sd < -Eps) || (sc < -Eps && sd > Eps)) { double t = sc / (sc - sd); o.Add(new Vec2(c.x + (d.x - c.x) * t, c.z + (d.z - c.z) * t)); }
        }
        return o.Count >= 3 ? o : new List<Vec2>();
    }

    /// <summary>볼록 p − 볼록 반시계 q → 볼록 조각들.</summary>
    static IEnumerable<List<Vec2>> Subtract(List<Vec2> p, Vec2[] q)
    {
        var inter = p;
        for (int i = 0; i < q.Length && inter.Count > 0; i++) inter = ClipHalf(inter, q[i], q[(i + 1) % q.Length], true);
        if (inter.Count == 0 || Math.Abs(Area(inter)) < MinArea) { yield return p; yield break; }
        var rest = p;
        for (int i = 0; i < q.Length && rest.Count > 0; i++)
        {
            var outside = ClipHalf(rest, q[i], q[(i + 1) % q.Length], false);
            if (outside.Count > 0 && Math.Abs(Area(outside)) >= MinArea) yield return outside;
            rest = ClipHalf(rest, q[i], q[(i + 1) % q.Length], true);
        }
    }

    class Piece { public Vec2[] tri; public double x0, x1, z0, z1; public double bottomY; public int bid; }

    /// <summary>mb 를 놓인 건물(drop == null) 윤곽에서 잘라낸 새 MeshBuilder. stats = 잘린 삼각형·건물·면적.</summary>
    internal static TownGenSpline.MeshBuilder Clip(TownGenSpline.MeshBuilder mb, List<TownGenOsmBuildings.B> buildings, out JObject stats)
    {
        // 건물 윤곽 → 반시계 볼록 삼각형 조각
        var pieces = new List<Piece>();
        foreach (var b in buildings.Where(x => x.drop == null))
        {
            var r = b.ring; var tri = TownGenOsmBuildings.EarClip(r);
            if (tri == null) tri = Enumerable.Range(1, r.Length - 2).SelectMany(k => new[] { 0, k, k + 1 }).ToList();
            for (int k = 0; k < tri.Count; k += 3)
            {
                var t3 = new[] { r[tri[k]], r[tri[k + 1]], r[tri[k + 2]] };
                if (Cross(t3[0], t3[1], t3[2]) < 0) { var s = t3[1]; t3[1] = t3[2]; t3[2] = s; }
                if (Math.Abs(Cross(t3[0], t3[1], t3[2])) < Eps) continue;
                pieces.Add(new Piece { tri = t3, x0 = t3.Min(p => p.x), x1 = t3.Max(p => p.x), z0 = t3.Min(p => p.z), z1 = t3.Max(p => p.z), bottomY = b.bottomY, bid = b.id });
            }
        }
        var outMb = new TownGenSpline.MeshBuilder();
        int clippedTris = 0, droppedSide = 0, keptUnder = 0; double removedArea = 0; var hitB = new HashSet<int>();
        void Emit(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = outMb.v.Count; outMb.v.Add(a); outMb.v.Add(b); outMb.v.Add(c);
            outMb.uv.Add(new Vector2((a.x + a.y) / 4f, a.z / 4f)); outMb.uv.Add(new Vector2((b.x + b.y) / 4f, b.z / 4f)); outMb.uv.Add(new Vector2((c.x + c.y) / 4f, c.z / 4f));
            outMb.t.Add(i); outMb.t.Add(i + 1); outMb.t.Add(i + 2);
        }
        for (int k = 0; k + 2 < mb.t.Count; k += 3)
        {
            int ia = mb.t[k], ib = mb.t[k + 1], ic = mb.t[k + 2];
            Vector3 a = mb.v[ia], b = mb.v[ib], c = mb.v[ic];
            void Keep() { int i = outMb.v.Count; outMb.v.Add(a); outMb.v.Add(b); outMb.v.Add(c); outMb.uv.Add(mb.uv[ia]); outMb.uv.Add(mb.uv[ib]); outMb.uv.Add(mb.uv[ic]); outMb.t.Add(i); outMb.t.Add(i + 1); outMb.t.Add(i + 2); }
            double x0 = Math.Min(a.x, Math.Min(b.x, c.x)), x1 = Math.Max(a.x, Math.Max(b.x, c.x)), z0 = Math.Min(a.z, Math.Min(b.z, c.z)), z1 = Math.Max(a.z, Math.Max(b.z, c.z));
            float yMin = Math.Min(a.y, Math.Min(b.y, c.y));
            var cand = new List<Piece>();
            foreach (var p in pieces)
            {
                if (p.x1 < x0 || p.x0 > x1 || p.z1 < z0 || p.z0 > z1) continue;
                if (yMin + TownGenSceneOverlap.UnderClearance <= p.bottomY) { keptUnder++; continue; }
                cand.Add(p);
            }
            if (cand.Count == 0) { Keep(); continue; }
            var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) { Keep(); continue; }
            if (Math.Abs(n.normalized.y) < 0.5f)
            {
                var g = new Vec2((a.x + b.x + c.x) / 3.0, (a.z + b.z + c.z) / 3.0);
                var hit = cand.FirstOrDefault(p => Cross(p.tri[0], p.tri[1], g) >= 0 && Cross(p.tri[1], p.tri[2], g) >= 0 && Cross(p.tri[2], p.tri[0], g) >= 0);
                if (hit != null) { droppedSide++; hitB.Add(hit.bid); continue; }
                Keep(); continue;
            }
            // 위/아래 면: xz 반시계로 놓고 건물 조각마다 빼기
            var poly = new List<Vec2> { new Vec2(a.x, a.z), new Vec2(b.x, b.z), new Vec2(c.x, c.z) };
            bool ccw = Area(poly) > 0; if (!ccw) poly.Reverse();
            double area0 = Math.Abs(Area(poly));
            var cur = new List<List<Vec2>> { poly }; bool touched = false;
            foreach (var p in cand)
            {
                var next = new List<List<Vec2>>();
                foreach (var q in cur) { var parts = Subtract(q, p.tri).ToList(); if (parts.Count != 1 || !ReferenceEquals(parts[0], q)) { touched = true; hitB.Add(p.bid); } next.AddRange(parts); }
                cur = next; if (cur.Count == 0) break;
            }
            if (!touched) { Keep(); continue; }
            clippedTris++;
            removedArea += area0 - cur.Sum(q => Math.Abs(Area(q)));
            float Y(double x, double z) => a.y - (float)((n.x * (x - a.x) + n.z * (z - a.z)) / n.y);
            foreach (var q in cur)
            {
                if (!ccw) q.Reverse();   // 원래 감김 유지(앞면 방향 그대로)
                var v0 = new Vector3((float)q[0].x, Y(q[0].x, q[0].z), (float)q[0].z);
                for (int i = 1; i + 1 < q.Count; i++)
                    Emit(v0, new Vector3((float)q[i].x, Y(q[i].x, q[i].z), (float)q[i].z), new Vector3((float)q[i + 1].x, Y(q[i + 1].x, q[i + 1].z), (float)q[i + 1].z));
            }
        }
        stats = new JObject
        {
            ["rule"] = "walk mesh − placed building outlines (xz, ear-clip convex pieces); side faces dropped if centroid inside outline; skip building where tri minY + 3m ≤ wall bottom",
            ["buildingsCut"] = hitB.Count, ["trianglesClipped"] = clippedTris, ["sideFacesDropped"] = droppedSide, ["keptUnderPairs"] = keptUnder,
            ["removedArea_m2"] = Math.Round(removedArea, 2), ["trianglesIn"] = mb.t.Count / 3, ["trianglesOut"] = outMb.t.Count / 3,
        };
        return outMb;
    }
}
