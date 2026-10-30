using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// 과제 2204(D-215 길 그리기 도구 1판): 도현이 그린 길 = docs/map/roads-hand.json. 빌더(RoadNetV2Build)가 원본 노선을 모은 직후 이 파일로 덮어쓴다.
//  roads   = 바뀐 원본 길(같은 id, 점·종류·폭 교체) + 새 길(hand_*) — p 는 1 m 표본(월드, y = _0433 지형), k 는 스플라인 매듭(다시 편집용)
//  deleted = 지운 원본 길 id — 빌더가 안 깔고 옛 길도 끈다
//  파일이 없으면 빌더는 예전과 똑같이 돈다.
[System.Serializable] public class RoadPaintHand { public string id, kind; public float w; public bool walk; public float[] k; public int[] m; public float[] p; }
[System.Serializable] public class RoadPaintFile { public string note = "D-215 길 그리기 — PitCrew/길 그리기 가 쓴다. 손으로 고치지 말 것"; public List<RoadPaintHand> roads = new List<RoadPaintHand>(); public List<string> deleted = new List<string>(); }

public static class RoadPaintData
{
    public static string FilePath => System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/map/roads-hand.json");
    public static bool IsWalk(string kind) => kind == "alley" || kind == "stairs";
    public static RoadPaintFile Load()
    {
        if (!System.IO.File.Exists(FilePath)) return null;
        var f = JsonUtility.FromJson<RoadPaintFile>(System.IO.File.ReadAllText(FilePath, Encoding.UTF8));
        if (f == null) return null; f.roads ??= new List<RoadPaintHand>(); f.deleted ??= new List<string>(); return f;
    }
    public static void Save(RoadPaintFile f)
    {
        if (f.roads.Count == 0 && f.deleted.Count == 0) { if (System.IO.File.Exists(FilePath)) System.IO.File.Delete(FilePath); return; }
        System.IO.File.WriteAllText(FilePath, JsonUtility.ToJson(f, true), new UTF8Encoding(false));
    }
    public static List<Vector3> Pts(RoadPaintHand h) { var r = new List<Vector3>(); if (h.p != null) for (int i = 0; i + 2 < h.p.Length; i += 3) r.Add(new Vector3(h.p[i], h.p[i + 1], h.p[i + 2])); return r; }
}

public static partial class BuildBayLoop
{
    // 길 그리기가 스플라인으로 띄울 원본 노선: 차길 = 씬 RoadSurface 중심선(1 m), 걷는 길 = town-v6b.json knot(빌더가 직선으로 잇는 그대로)
    public struct RoadPaintSrc { public string id, kind; public float w; public bool walk; public List<Vector3> pts; }
    public static List<RoadPaintSrc> RoadPaintSources()
    {
        var res = new List<RoadPaintSrc>();
        var roadsT = GameObject.Find("TownV6_Gray")?.transform.Find("Roads"); if (roadsT == null) return res;
        foreach (var rs in roadsT.GetComponentsInChildren<RoadSurface>(true))
            if (rs.centerline != null && rs.centerline.Length >= 2) res.Add(new RoadPaintSrc { id = rs.roadId, kind = rs.kind, w = rs.width, pts = rs.centerline.ToList() });
        var plan = (Dictionary<string, object>)Json.Parse(System.IO.File.ReadAllText(TownV6Json, Encoding.UTF8));
        foreach (Dictionary<string, object> r in (List<object>)plan["roads"])
        {
            string kind = (string)r["kind"]; if (kind == "main" || kind == "village") continue;
            var ks = ((List<object>)r["knots"]).Cast<List<object>>().Select(k => new Vector3(F(k[0]), F(k[2]), F(k[1]))).ToList();
            if (ks.Count >= 2) res.Add(new RoadPaintSrc { id = (string)r["id"], kind = kind, w = F(r["width"]), walk = true, pts = ks });
        }
        return res;
    }

    // 차길 덮어쓰기: cars 를 모은 직후(재질 고르기 전)에 부른다. 지운 길·걷는 길로 바뀐 길은 빼고(옛 길은 이미 disabled 에 있음), 바뀐 길은 점·종류·폭 교체, 새 길은 더한다
    static string V2HandCars(RoadPaintFile h, List<V2L> cars)
    {
        if (h == null) return "손 길 없음";
        var del = new HashSet<string>(h.deleted); var by = new Dictionary<string, RoadPaintHand>(); foreach (var r in h.roads) by[r.id] = r;
        var had = new HashSet<string>(cars.Select(c => c.id)); int rm = 0, rep = 0, add = 0;
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            var c = cars[i]; by.TryGetValue(c.id, out var hr);
            if (del.Contains(c.id) || (hr != null && hr.walk)) { cars.RemoveAt(i); rm++; continue; }
            if (hr == null) continue;
            c.p = RoadPaintData.Pts(hr).Select(q => q - Vector3.up * RoadColLift).ToList(); c.kind = hr.kind; c.w = hr.w; c.fix = c.p.Select(q => !V2In(q)).ToList(); rep++;
        }
        foreach (var hr in h.roads)
        {
            if (hr.walk || had.Contains(hr.id)) continue;
            var p = RoadPaintData.Pts(hr).Select(q => q - Vector3.up * RoadColLift).ToList(); if (p.Count < 2 || !p.Any(V2In)) continue;
            var same = cars.Where(c => c.kind == hr.kind).ToList();
            cars.Add(new V2L { id = hr.id, kind = hr.kind, w = hr.w, rough = same.Count > 0 ? same.Average(c => c.rough) : 0f, mat = same.Count > 0 ? same[0].mat : cars.FirstOrDefault()?.mat, p = p, fix = p.Select(q => !V2In(q)).ToList() });
            add++;
        }
        return $"손 길(차): 바꿈 {rep}, 지움 {rm}, 새 길 {add}";
    }
}
