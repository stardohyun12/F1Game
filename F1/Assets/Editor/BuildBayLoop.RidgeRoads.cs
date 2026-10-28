using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 과제 0351(맵 코멘트 03:47~03:49): 능선(정비소 쪽)으로 올라오는 마을길 3개(id ridge_up_*)만 지금 씬에 더한다.
// 전체 재빌드(MenuTownV6)는 D-203 능선 배치·씬 수정을 지우므로 쓰지 않는다 — 길 띠는 Ribbon 재사용, 지형은 기존 메시 정점만 고친다.
// 지형 연산자 = 리서치 1안(docs/research/2026-10-29-road-on-terrain.md §3): 어깨 1 m 는 길면 −3 cm, 바깥은 [길면 − (e−1)/3, 길면 + (e−1)/2] 로 끼우기.
public static partial class BuildBayLoop
{
    [MenuItem("PitCrew/Map/TownV6 — 능선 길 3개 더하기 (ridge_up_*)")]
    public static void MenuRidgeRoads() { Debug.Log("[RidgeRoads] " + AddRidgeRoads()); }

    public static string AddRidgeRoads()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != TownV6Scene) return "Map_TownV6 를 먼저 연다: " + scene.path;
        var plan = (Dictionary<string, object>)Json.Parse(System.IO.File.ReadAllText(TownV6Json, Encoding.UTF8));
        var rs = new List<GRoad>();
        foreach (Dictionary<string, object> r in (List<object>)plan["roads"])
        {
            if (!((string)r["id"]).StartsWith("ridge_up_")) continue;
            var g = new GRoad { id = (string)r["id"], kind = (string)r["kind"], width = F(r["width"]), rough = F(r["roughness"]) };
            var ks = ((List<object>)r["knots"]).Cast<List<object>>().Select(k => new Vector3(F(k[0]), F(k[2]), F(k[1]))).ToList();
            for (int i = 0; i + 1 < ks.Count; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Fl(ks[i + 1] - ks[i]).magnitude));
                for (int s = 0; s < n; s++) g.pts.Add(Vector3.Lerp(ks[i], ks[i + 1], s / (float)n));
            }
            g.pts.Add(ks[ks.Count - 1]);
            LimitGrade(g.pts, RidgeMaxGrade); rs.Add(g);
        }
        if (rs.Count == 0) return "ridge_up_* 길 없음";

        var roadsT = GameObject.Find("TownV6_Gray/Roads")?.transform; if (roadsT == null) return "TownV6_Gray/Roads 없음";
        var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); if (terrGo == null) return "TownV6_Terrain 없음";
        grayMatDir = TownV6Scene.Replace(".unity", "_Mats");
        // 기존 마을길 재질을 그대로(색을 다시 쓰지 않게 GM 을 거치지 않는다)
        var oldRoads = roadsT.GetComponentsInChildren<RoadSurface>().Where(x => !x.roadId.StartsWith("ridge_up_")).ToList();
        var villageMat = oldRoads.Where(x => x.kind == "village").Select(x => x.GetComponent<MeshRenderer>().sharedMaterial).FirstOrDefault(m => m != null);
        var lg = new List<string>();

        // 1) 지형 — 새 길 3개 주변 정점만. 기존 차길 띠(폭/2 + 1 m) 안 정점은 건드리지 않는다(갈림목에서 옛 길을 묻지 않게).
        var mf = terrGo.GetComponent<MeshFilter>(); var mesh = mf.sharedMesh;
        string ridgePath = grayMatDir + "/Mesh_TownV6_Terrain_ridge.asset";   // 사본에 고친다(_d203·_loopfix 와 같은 방식 — 되돌리기 = 이전 메시로 다시 꽂기)
        if (AssetDatabase.GetAssetPath(mesh) != ridgePath) { lg.Add("지형 원본 " + AssetDatabase.GetAssetPath(mesh)); mesh = Object.Instantiate(mesh); mesh.name = "TownV6_Terrain_ridge"; AssetDatabase.CreateAsset(mesh, ridgePath); mf.sharedMesh = mesh; }
        var v = mesh.vertices;
        const float sh = 1f, cutR = 2f, fillR = 3f, reach = 14f; int moved = 0; float maxD = 0f;
        var segs = new List<(Vector3 a, Vector3 b, float hw)>();
        foreach (var r in rs) for (int i = 1; i < r.pts.Count; i++) segs.Add((r.pts[i - 1], r.pts[i], r.width * .5f));
        var oldSegs = new List<(Vector3 a, Vector3 b, float hw)>();
        Vector2 lo = new Vector2(1e9f, 1e9f), hi = -lo;
        foreach (var s in segs) { lo = Vector2.Min(lo, new Vector2(Mathf.Min(s.a.x, s.b.x), Mathf.Min(s.a.z, s.b.z))); hi = Vector2.Max(hi, new Vector2(Mathf.Max(s.a.x, s.b.x), Mathf.Max(s.a.z, s.b.z))); }
        lo -= Vector2.one * reach; hi += Vector2.one * reach;
        foreach (var o in oldRoads) { var c = o.centerline; if (c == null) continue; for (int i = 1; i < c.Length; i++) if (c[i].x > lo.x - 10 && c[i].x < hi.x + 10 && c[i].z > lo.y - 10 && c[i].z < hi.y + 10) oldSegs.Add((c[i - 1], c[i], o.width * .5f)); }
        for (int k = 0; k < v.Length; k++)
        {
            var p = v[k]; if (p.x < lo.x || p.x > hi.x || p.z < lo.y || p.z > hi.y) continue;
            float bestE = 1e9f, yr = 0f;
            foreach (var s in segs)
            {
                var ab = Fl(s.b - s.a); float t = Mathf.Clamp01(Vector3.Dot(Fl(p - s.a), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                var q = Vector3.Lerp(s.a, s.b, t); float e = Fl(p - q).magnitude - s.hw;
                if (e < bestE) { bestE = e; yr = q.y + RoadColLift; }   // 길면 = 콜라이더 면(+4 cm), 선분 투영 보간
            }
            if (bestE > reach) continue;
            bool underOld = false;
            foreach (var s in oldSegs)
            {
                var ab = Fl(s.b - s.a); float t = Mathf.Clamp01(Vector3.Dot(Fl(p - s.a), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                if (Fl(p - Vector3.Lerp(s.a, s.b, t)).magnitude - s.hw < sh && bestE > 0f) { underOld = true; break; }
            }
            if (underOld) continue;
            // 0404: 길 밑 정점은 −15 cm(지형 삼각형이 길보다 성겨서 −3 cm 면 중간이 1~3 cm 튀어나왔다), 어깨는 −3 cm 그대로
            float y = bestE <= 0f ? yr - .15f : bestE <= sh ? yr - .03f : Mathf.Clamp(p.y, yr - (bestE - sh) / fillR, yr + (bestE - sh) / cutR);
            if (Mathf.Abs(y - p.y) > .001f) { maxD = Mathf.Max(maxD, Mathf.Abs(y - p.y)); v[k].y = y; moved++; }
        }
        mesh.vertices = v; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        var mc = terrGo.GetComponent<MeshCollider>(); if (mc != null) { mc.sharedMesh = null; mc.sharedMesh = mesh; }
        lg.Add($"지형 정점 {moved}개 이동(최대 {maxD:F2} m)");

        // 2) 길 띠 + RoadSurface (BuildGray 의 마을길과 같은 들림·콜라이더)
        foreach (var r in rs)
        {
            var old = roadsT.Find("ROAD_" + r.id); if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = Ribbon(roadsT, "ROAD_" + r.id, r.pts, r.width, .04f, villageMat != null ? villageMat : GM("Village", new Color(.72f, .72f, .70f)), true);
            var rsf = go.AddComponent<RoadSurface>(); rsf.roadId = r.id; rsf.kind = r.kind; rsf.roughness = r.rough; rsf.width = r.width;
            rsf.centerline = r.pts.Select(p => p + Vector3.up * RoadColLift).ToArray(); rsf.closed = false; EditorUtility.SetDirty(rsf);
            float L = 0f, gMax = 0f; for (int i = 1; i < r.pts.Count; i++) { float d = Fl(r.pts[i] - r.pts[i - 1]).magnitude; L += d; if (d > .3f) gMax = Mathf.Max(gMax, Mathf.Abs(r.pts[i].y - r.pts[i - 1].y) / d); }
            lg.Add($"{r.id} {L:F0}m 최대경사 {gMax * 100f:F1}%");
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return string.Join(" | ", lg);
    }

    // 과제 0404: 카트가 올라가게 중심선 높이를 경사 상한 안으로 편다. 양 끝(기존 길과 만나는 점) 높이는 고정.
    // 앞→뒤, 뒤→앞으로 한 번씩 "앞 점 ± 상한×거리" 로 끼운다 — 평균 경사가 상한 밑이면 양 끝이 다 맞는다.
    const float RidgeMaxGrade = .11f;   // 12% 판정에 레이캐스트 2 m 표본 흔들림 몫을 남김
    static void LimitGrade(List<Vector3> p, float gMax)
    {
        int n = p.Count; if (n < 3) return;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 1; i < n - 1; i++) { float d = Fl(p[i] - p[i - 1]).magnitude * gMax; var q = p[i]; q.y = Mathf.Clamp(q.y, p[i - 1].y - d, p[i - 1].y + d); p[i] = q; }
            for (int i = n - 2; i > 0; i--) { float d = Fl(p[i] - p[i + 1]).magnitude * gMax; var q = p[i]; q.y = Mathf.Clamp(q.y, p[i + 1].y - d, p[i + 1].y + d); p[i] = q; }
        }
    }
}
