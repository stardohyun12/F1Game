using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 과제 0134 (D-157): 계획 파일 경로를 받아 짓는 입구 + 마을 v6b 회색 박스.
///  - Build(planPath, scenePath): 기본 경로(bayloop-plan.json → Map_BayLoop)면 기존 BuildScene() 그대로(빌드 결과 불변).
///    meta.version ≥ 6(town-v6*.json)이면 BuildGray — 대조표(docs/map/v6b-bayloop-fields.md)의 빈 필드는 여기서 채운다(안 쓰는 층은 아예 안 짓는다).
///  - 회색 박스: 지형 단색 · 길 종류별 단색 · 건물 = OSM 폴리곤 압출(2~4층, 벽 회백 · 지붕 주황). 질감·소품·튜토리얼 0.
///  - 차·카메라 B·화물·배달(차고·MART·배달점 4 = 지점 5)은 원본 씬(Proto_CoastCourse) 것을 그대로 쓰고 위치·점만 꽂는다.
/// </summary>
public static partial class BuildBayLoop
{
    public const string TownV6Scene = "Assets/Scenes/Map_TownV6.unity";
    static string TownV6Json => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/town-v6b.json"));

    [MenuItem("PitCrew/Map/Build Town V6 Gray (D-157)")]
    public static void MenuTownV6() { Debug.Log("[TownV6] " + Build(TownV6Json, TownV6Scene)); }

    public static string Build(string planPath, string scenePath)
    {
        planPath = System.IO.Path.GetFullPath(planPath);
        if (planPath == JsonPath && scenePath == ScenePath) return BuildScene();
        var plan = (Dictionary<string, object>)Json.Parse(System.IO.File.ReadAllText(planPath, Encoding.UTF8));
        var meta = plan.TryGetValue("meta", out var mo) ? mo as Dictionary<string, object> : null;
        if (meta != null && meta.TryGetValue("version", out var v) && F(v) >= 6f) return BuildGray(plan, scenePath);
        return "지원 안 하는 계획 파일(meta.version<6 이면 기본 경로만): " + planPath;
    }

    // ------------------------------------------------------------------ 회색 박스
    const float GX0 = -392f, GZ0 = -392f, GStep = 2f; const int GN = 393;   // 지형 격자 2m, ±392
    static float[,] gH; static float[] cpH; static int cpN; static float cpX0, cpStep;
    static string grayMatDir;

    class GRoad { public string id, kind; public float width, rough; public List<Vector3> pts = new List<Vector3>(); public bool Car => kind == "main" || kind == "village"; }

    static string BuildGray(Dictionary<string, object> plan, string scenePath)
    {
        var lg = new List<string>(); var sw = System.Diagnostics.Stopwatch.StartNew();
        grayMatDir = scenePath.Replace(".unity", "_Mats"); grayMats.Clear();
        if (!AssetDatabase.IsValidFolder(grayMatDir)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(scenePath).Replace('\\', '/'), System.IO.Path.GetFileName(grayMatDir));
        AssetDatabase.DeleteAsset(scenePath);
        if (!AssetDatabase.CopyAsset(SrcScene, scenePath)) return "copy failed";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects()) if (g.name == "ProtoCourse") Object.DestroyImmediate(g);
        var top = new GameObject("TownV6_Gray").transform;

        // 지형 = controlPoints(8m, 길에 맞춰 깎고 메운 H2) 쌍선형 → 2m 격자
        var terr = (Dictionary<string, object>)plan["terrain"];
        var cps = (List<object>)terr["controlPoints"]; cpStep = F(terr["step"]);
        cpN = Mathf.RoundToInt(Mathf.Sqrt(cps.Count)); cpH = new float[cps.Count];
        cpX0 = F(((List<object>)cps[0])[0]);
        foreach (List<object> p in cps) { int i = Mathf.RoundToInt((F(p[0]) - cpX0) / cpStep), j = Mathf.RoundToInt((F(p[1]) - cpX0) / cpStep); cpH[i * cpN + j] = F(p[2]); }

        // 길: knots [x,z,y] 꺾은선 → 1m
        var rs = new List<GRoad>();
        foreach (Dictionary<string, object> r in (List<object>)plan["roads"])
        {
            var g = new GRoad { id = (string)r["id"], kind = (string)r["kind"], width = F(r["width"]), rough = F(r["roughness"]) };
            var ks = ((List<object>)r["knots"]).Cast<List<object>>().Select(k => new Vector3(F(k[0]), F(k[2]), F(k[1]))).ToList();
            for (int i = 0; i + 1 < ks.Count; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Fl(ks[i + 1] - ks[i]).magnitude));
                for (int s = 0; s < n; s++) g.pts.Add(Vector3.Lerp(ks[i], ks[i + 1], s / (float)n));
            }
            if (ks.Count > 0) g.pts.Add(ks[ks.Count - 1]);
            if (g.pts.Count >= 2) rs.Add(g);
        }
        var ends = DeadEnds(rs, lg);   // v6c(과제 0214-3): 이음매 잘림은 잇고(rs 에 추가 → 지형 마감·RoadSurface 도 같이), 진짜 막다른 끝은 이유를 둔다
        lg.Add(SmoothProfiles(rs));

        gH = new float[GN, GN];
        for (int i = 0; i < GN; i++) for (int j = 0; j < GN; j++) gH[i, j] = CpAt(GX0 + i * GStep, GZ0 + j * GStep);
        // v6d(과제 0246-3): 8 m 격자를 쌍선형으로 펴면 8 m 네모마다 면이 꺾여 위에서 음영이 칸으로 얼룩진다 → 3×3 평균 4번(±8 m)으로 꺾임을 녹인다
        for (int pass = 0; pass < 4; pass++)
        {
            var s = (float[,])gH.Clone();
            for (int i = 1; i < GN - 1; i++) for (int j = 1; j < GN - 1; j++)
            {
                float sum = 0; for (int di = -1; di <= 1; di++) for (int dj = -1; dj <= 1; dj++) sum += s[i + di, j + dj];
                gH[i, j] = sum / 9f;
            }
        }
        // 차길 띠(반폭+2m) 안은 노면 0.25m 아래로 맞춘다 — 8m 격자가 좁은 길을 덮거나 띄우지 않게(대조표: 깎고 메운 지형은 생성기 몫, 여기선 마감만)
        var best = new float[GN, GN];
        for (int i = 0; i < GN; i++) for (int j = 0; j < GN; j++) best[i, j] = 1e9f;
        foreach (var r in rs.Where(x => x.Car)) foreach (var p in r.pts)
        {
            float reach = r.width * .5f + 2f; int i0 = Mathf.FloorToInt((p.x - reach - GX0) / GStep), i1 = Mathf.CeilToInt((p.x + reach - GX0) / GStep);
            int j0 = Mathf.FloorToInt((p.z - reach - GZ0) / GStep), j1 = Mathf.CeilToInt((p.z + reach - GZ0) / GStep);
            for (int i = Mathf.Max(0, i0); i <= Mathf.Min(GN - 1, i1); i++) for (int j = Mathf.Max(0, j0); j <= Mathf.Min(GN - 1, j1); j++)
            {
                float d = new Vector2(GX0 + i * GStep - p.x, GZ0 + j * GStep - p.z).magnitude;
                if (d <= reach && d < best[i, j]) { best[i, j] = d; gH[i, j] = p.y - .25f; }
            }
        }
        // 걷는 길(골목·계단)은 지형 위로 묻히지 않게만
        foreach (var r in rs.Where(x => !x.Car)) foreach (var p in r.pts)
        {
            int i = Mathf.RoundToInt((p.x - GX0) / GStep), j = Mathf.RoundToInt((p.z - GZ0) / GStep);
            if (i >= 0 && j >= 0 && i < GN && j < GN && best[i, j] > 1e8f) gH[i, j] = Mathf.Min(gH[i, j], p.y - .1f);
        }
        TerrainGray(top); lg.Add($"지형 {GN}×{GN} 2m");
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane); sea.name = "Sea"; sea.transform.SetParent(top);
        sea.transform.position = new Vector3(0, 0, 0); sea.transform.localScale = new Vector3(160, 1, 160); Object.DestroyImmediate(sea.GetComponent<Collider>());
        sea.GetComponent<MeshRenderer>().sharedMaterial = GM("Sea", new Color(.36f, .58f, .70f));

        // 길 띠
        var roadsT = new GameObject("Roads").transform; roadsT.SetParent(top);
        int carN = 0; float carM = 0f;
        foreach (var r in rs)
        {
            bool rough = r.kind == "alley" || (r.kind == "village" && r.rough > 0f);
            string key = r.kind == "stairs" ? "Stairs" : r.kind == "main" ? "Main" : rough ? "Stone" : "Village";
            Color c = key switch { "Main" => new Color(.45f, .45f, .47f), "Village" => new Color(.72f, .72f, .70f), "Stone" => new Color(.72f, .56f, .30f), _ => new Color(1f, .45f, .12f) };
            float lift = r.kind == "main" ? .06f : r.Car ? .04f : .02f;   // 겹친 갈림목에서 위계가 위로
            var go = Ribbon(roadsT, "ROAD_" + r.id, r.pts, r.width, lift, GM(key, c), r.Car);
            if (!r.Car) continue;
            carN++; for (int i = 1; i < r.pts.Count; i++) carM += Fl(r.pts[i] - r.pts[i - 1]).magnitude;
            var rsf = go.AddComponent<RoadSurface>(); rsf.roadId = r.id; rsf.kind = rough ? "shortcut" : r.kind; rsf.roughness = r.rough; rsf.width = r.width;
            rsf.centerline = r.pts.Select(p => p + Vector3.up * lift).ToArray(); rsf.closed = false; EditorUtility.SetDirty(rsf);
        }
        lg.Add($"길 {rs.Count}(차길 {carN}개 {carM / 1000f:F1}km)");
        DeadEndMarks(top, ends);
        Piers(top, (Dictionary<string, object>)plan["sea"], lg);

        // 건물: 폴리곤 압출, 상자 안(±390)만
        int nb = Buildings(top, (List<object>)plan["buildings"]); lg.Add($"건물 {nb}채");

        // 배달 — 차고(출발·복귀) · MART · 지점 4. 문 앞 = 그 지점에서 가장 가까운 차길 점(차가 닿게)
        var carPts = rs.Where(x => x.Car).SelectMany(x => x.pts.Select((p, i) => (p, r: x, i))).ToList();
        (Vector3 p, GRoad r, int i) Near(Vector2 q) => carPts.OrderBy(t => (new Vector2(t.p.x, t.p.z) - q).sqrMagnitude).First();
        Vector2 P2(object o) { var l = (List<object>)((Dictionary<string, object>)o)["pos"]; return new Vector2(F(l[0]), F(l[1])); }
        var dT = new GameObject("Delivery").transform; dT.SetParent(top);
        var points = new List<DeliveryPoint>();
        DeliveryPoint Stop(string label, Vector2 q, Color col)
        {
            var n = Near(q); var s = new GameObject("STOP_" + label).transform; s.SetParent(dT); s.position = n.p;
            Box(s, "Beacon", new Vector3(.8f, 6f, .8f), n.p + Vector3.up * 3f, Quaternion.identity, GM("Beacon_" + label, col), false);
            float gy = CpAt(q.x, q.y); Box(s, "House", new Vector3(4f, 4f, 4f), new Vector3(q.x, gy + 2f, q.y), Quaternion.identity, GM("Beacon_" + label, col), false);
            var dp = new GameObject("DP_" + label); dp.transform.SetParent(s); dp.transform.position = n.p + Vector3.up * .6f;
            dp.AddComponent<SphereCollider>(); var dpc = dp.AddComponent<DeliveryPoint>();
            var so = new SerializedObject(dpc); so.FindProperty("label").stringValue = label; so.FindProperty("radius").floatValue = 5f; so.ApplyModifiedPropertiesWithoutUndo();
            lg.Add($"{label} 문앞({n.p.x:F0},{n.p.y:F1},{n.p.z:F0}) 지점까지 {(new Vector2(n.p.x, n.p.z) - q).magnitude:F0}m [{n.r.id}]");
            return dpc;
        }
        var stops = ((List<object>)plan["stops"]).Cast<Dictionary<string, object>>().ToDictionary(s => (string)s["id"], s => P2(s));
        // 순서(가설): 찻집 → 등대 → 언덕 밭 → MART → 꼭대기 집 → 차고 복귀. 대조표의 배달 5쌍을 한 바퀴로 다 지난다.
        points.Add(Stop("tea", stops["tea"], new Color(1f, .42f, .08f)));
        points.Add(Stop("lighthouse", stops["lighthouse"], new Color(1f, .42f, .08f)));
        points.Add(Stop("hill_field", stops["hill_field"], new Color(1f, .42f, .08f)));
        points.Add(Stop("MART", P2(plan["mart"]), new Color(.16f, .42f, .78f)));
        points.Add(Stop("top_house", stops["top_house"], new Color(1f, .42f, .08f)));
        var gq = P2(plan["garage"]); var gn = Near(gq);
        var garage = new GameObject("GARAGE_Return").transform; garage.SetParent(dT); garage.position = gn.p;
        Box(garage, "Garage", new Vector3(6f, 3.5f, 6f), new Vector3(gq.x, CpAt(gq.x, gq.y) + 1.75f, gq.y), Quaternion.identity, GM("Garage", new Color(.30f, .30f, .32f)), false);
        var dm = Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        if (dm != null) { dm.SetPoints(points, garage); EditorUtility.SetDirty(dm); lg.Add($"DeliveryManager 지점 {points.Count} + 복귀 차고"); }
        else lg.Add("DeliveryManager 없음!");

        // 차 · 중심선 · 주문판 건너뛰기 · 카메라 B
        var car = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        var gr = gn.r; int gi = Mathf.Clamp(gn.i, 1, gr.pts.Count - 2); var fw = Fl(gr.pts[gi + 1] - gr.pts[gi - 1]).normalized;
        car.transform.position = gn.p + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation(fw);
        var cso = new SerializedObject(car); cso.FindProperty("fallRespawnY").floatValue = -3f; cso.FindProperty("safePointAboveFall").floatValue = 4f; cso.ApplyModifiedPropertiesWithoutUndo();
        var cl = new GameObject("CourseCenterline").AddComponent<CourseCenterline>(); cl.transform.SetParent(top);
        var cpts = new List<Vector3>(); var cfw = new List<Vector3>();
        foreach (var r in rs.Where(x => x.Car)) for (int i = 0; i < r.pts.Count; i += 2) { cpts.Add(r.pts[i]); cfw.Add(Fl(r.pts[Mathf.Min(i + 1, r.pts.Count - 1)] - r.pts[Mathf.Max(i - 1, 0)]).normalized); }
        cl.points = cpts.ToArray(); cl.forwards = cfw.ToArray(); EditorUtility.SetDirty(cl);
        new GameObject("BayLoopDriveOnly (주문판 건너뛰고 바로 주행)").AddComponent<BayLoopDriveOnly>().transform.SetParent(top);
        var fc = Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        if (fc != null)
        {
            fc.transform.position = car.transform.position - fw * 4.2f + Vector3.up * 1.9f; fc.transform.LookAt(car.transform.position + fw * 8f + Vector3.up);
            var cam = fc.GetComponent<Camera>(); if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1800f);
            CameraFeelSwitcher.BuildPresets(false);
            fc.Preset = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.B); EditorUtility.SetDirty(fc);
        }
        lg.Add($"차 시작 차고 앞({gn.p.x:F0},{gn.p.y:F1},{gn.p.z:F0}) [{gr.id}] 카메라 B");

        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return $"{scenePath} {sw.ElapsedMilliseconds}ms | " + string.Join(" | ", lg);
    }

    static Vector3 Fl(Vector3 v) => new Vector3(v.x, 0f, v.z);   // BuildBayLoop.Flat 은 정규화까지 한다 — 길이용

    // ------------------------------------------------------------------ v6c: 막다른 끝 · 부두
    class DeadEnd { public GRoad r; public Vector3 p, dir; public string why; }

    /// 차길 끝마다: 다른 차길 3 m 안 = 이어짐. 칸 안(±372)에서 끊긴 끝은
    ///  ① 25 m 안에 다른 차길 → 이음매 잘림으로 보고 잇는 차길을 만든다
    ///  ② 10 m 안에 계단·골목 → '사람길로 이어짐'(말뚝 2개)
    ///  ③ 길 폭 ≥ 5 m → 돌아 나오는 광장(14×14) + 낮은 담   ④ 그 밖 → 담(집 문 앞 골목 끝)
    static List<DeadEnd> DeadEnds(List<GRoad> rs, List<string> lg)
    {
        var cars = rs.Where(x => x.Car).ToList(); var outp = new List<DeadEnd>(); var add = new List<GRoad>();
        int nJoin = 0;
        foreach (var r in cars)
            for (int e = 0; e < 2; e++)
            {
                var p = e == 0 ? r.pts[0] : r.pts[r.pts.Count - 1];
                if (Mathf.Abs(p.x) > 372f || Mathf.Abs(p.z) > 372f) continue;
                var q = e == 0 ? r.pts[Mathf.Min(4, r.pts.Count - 1)] : r.pts[Mathf.Max(0, r.pts.Count - 5)];
                var dir = Fl(p - q).normalized;
                float bd = 1e9f; Vector3 bp = p; GRoad br = null;
                foreach (var o in cars.Concat(add))
                {
                    int skip = o == r ? 12 : 0;   // 제 길은 끝 근처 12점을 빼고(고리로 돌아오는 경우만)
                    for (int i = 0; i < o.pts.Count; i++)
                    {
                        if (o == r && (e == 0 ? i < skip : i > o.pts.Count - 1 - skip)) continue;
                        float d = Fl(o.pts[i] - p).magnitude; if (d < bd) { bd = d; bp = o.pts[i]; br = o; }
                    }
                }
                if (bd <= 3f) continue;
                if (bd <= 25f && br != null && br != r)
                {
                    var j = new GRoad { id = $"join_{r.id}_{e}", kind = "village", width = Mathf.Min(r.width, br.width), rough = 0f };
                    int n = Mathf.Max(2, Mathf.CeilToInt(bd)); for (int s = 0; s <= n; s++) j.pts.Add(Vector3.Lerp(p, bp, s / (float)n));
                    add.Add(j); nJoin++; continue;
                }
                bool foot = rs.Where(x => !x.Car).Any(x => x.pts.Any(t => Fl(t - p).magnitude < 10f));
                outp.Add(new DeadEnd { r = r, p = p, dir = dir, why = foot ? "foot" : r.width >= 5f ? "plaza" : "wall" });
            }
        rs.AddRange(add);
        lg.Add($"막다른 끝: 이음 {nJoin} · 사람길 {outp.Count(x => x.why == "foot")} · 광장 {outp.Count(x => x.why == "plaza")} · 담 {outp.Count(x => x.why == "wall")}");
        return outp;
    }

    /// v6d(과제 0246-1): 매듭 사이를 직선으로 이어 매듭마다 경사가 확 꺾인다(0 %→26 %). 바퀴 없이 상자로 미끄러지는 차는
    /// 바닥 틈이 8 cm 라 오목하게 꺾인 곳에 앞 모서리가 박혀 선다(봇 실패 24건의 원인). 차길 높이를 ±4 m 평균 3번으로 둥글리고(끝점 고정),
    /// 다른 차길 위에 얹힌 끝은 그 길의 새 높이로 맞춘 뒤 8 m 에 걸쳐 녹인다(갈림목 턱).
    static string SmoothProfiles(List<GRoad> rs)
    {
        var car = rs.Where(r => r.Car && r.pts.Count >= 3).ToList(); float maxBefore = 0, maxAfter = 0;
        System.Func<List<Vector3>, float> brk = p => { float m = 0; for (int i = 2; i + 2 < p.Count; i++) { float g0 = (p[i].y - p[i - 2].y) / Mathf.Max(.3f, Fl(p[i] - p[i - 2]).magnitude), g1 = (p[i + 2].y - p[i].y) / Mathf.Max(.3f, Fl(p[i + 2] - p[i]).magnitude); m = Mathf.Max(m, Mathf.Abs(g1 - g0)); } return m; };
        foreach (var r in car)
        {
            maxBefore = Mathf.Max(maxBefore, brk(r.pts));
            for (int pass = 0; pass < 3; pass++)
            {
                var y = r.pts.Select(p => p.y).ToArray(); int n = y.Length;
                for (int i = 1; i < n - 1; i++)
                {
                    int w = Mathf.Min(4, Mathf.Min(i, n - 1 - i)); float s = 0; for (int k = -w; k <= w; k++) s += y[i + k];
                    var p = r.pts[i]; p.y = s / (2 * w + 1); r.pts[i] = p;
                }
            }
        }
        int snapped = 0;
        foreach (var r in car) foreach (bool atEnd in new[] { false, true })
        {
            var e = atEnd ? r.pts[r.pts.Count - 1] : r.pts[0]; float bd = 2.5f, ty = 0; bool hit = false;
            foreach (var o in car) if (o != r) for (int i = 3; i < o.pts.Count - 3; i++)
            {
                float d = Fl(o.pts[i] - e).magnitude; if (d < bd) { bd = d; ty = o.pts[i].y; hit = true; }
            }
            if (!hit || Mathf.Abs(ty - e.y) < .005f) continue;
            float dy = ty - e.y, acc = 0; snapped++;
            for (int k = 0; k < r.pts.Count; k++)
            {
                int i = atEnd ? r.pts.Count - 1 - k : k;
                if (k > 0) acc += Fl(r.pts[i] - r.pts[atEnd ? i + 1 : i - 1]).magnitude;
                if (acc > 8f) break;
                var p = r.pts[i]; p.y += dy * (1f - acc / 8f); r.pts[i] = p;
            }
        }
        foreach (var r in car) maxAfter = Mathf.Max(maxAfter, brk(r.pts));
        return $"차길 높이 둥글림 {car.Count}개 · 꺾임 최대 {maxBefore * 100:F0}%→{maxAfter * 100:F0}%(2 m 현) · 갈림목 끝 맞춤 {snapped}";
    }

    static void DeadEndMarks(Transform top, List<DeadEnd> ends)
    {
        var t = new GameObject("DeadEnds").transform; t.SetParent(top);
        var wall = GM("DeadEndWall", new Color(.80f, .76f, .66f)); var post = GM("DeadEndPost", new Color(.25f, .25f, .27f)); var pad = GM("Village", new Color(.72f, .72f, .70f));
        foreach (var d in ends)
        {
            var s = new GameObject($"DEADEND_{d.why}_{d.r.id}").transform; s.SetParent(t); s.position = d.p;
            var rot = Quaternion.LookRotation(d.dir); var rt = rot * Vector3.right;
            if (d.why == "foot")      // 차는 여기까지, 사람길이 이어진다
                foreach (float k in new[] { -.5f, .5f }) Box(s, "Bollard", new Vector3(.35f, 1f, .35f), d.p + rt * (d.r.width * k) + d.dir * 1.2f + Vector3.up * .5f, rot, post, true);
            else if (d.why == "plaza")
            {
                Box(s, "Plaza", new Vector3(14f, .6f, 14f), d.p + d.dir * 7f + Vector3.up * -.24f, rot, pad, true);
                Box(s, "Wall", new Vector3(14f, 1.2f, .6f), d.p + d.dir * 14.3f + Vector3.up * .6f, rot, wall, true);
            }
            else Box(s, "Wall", new Vector3(d.r.width + 1.2f, 1.6f, .6f), d.p + d.dir * 1.5f + Vector3.up * .8f, rot, wall, true);
        }
    }

    /// OSM man_made=pier/breakwater 꺾은선 → 폭 3.5 m 둑(바닥 −2 m ~ 윗면 1.3 m). 물에 닿게 해면 아래까지 내린다.
    static void Piers(Transform top, Dictionary<string, object> sea, List<string> lg)
    {
        if (!sea.TryGetValue("piers", out var po)) return;
        var t = new GameObject("Piers").transform; t.SetParent(top); var mat = GM("Pier", new Color(.55f, .55f, .55f)); int n = 0;
        foreach (List<object> line in (List<object>)po)
        {
            var ps = line.Cast<List<object>>().Select(k => new Vector3(F(k[0]), 0f, F(k[1]))).ToList();
            for (int i = 0; i + 1 < ps.Count; i++)
            {
                var a = ps[i]; var b = ps[i + 1]; float L = (b - a).magnitude; if (L < .3f) continue;
                Box(t, "Pier", new Vector3(3.5f, 3.3f, L + 1.5f), (a + b) * .5f + Vector3.up * -.35f, Quaternion.LookRotation(b - a), mat, true); n++;
            }
        }
        lg.Add($"부두·방파제 {n}칸");
    }

    static float CpAt(float x, float z)
    {
        float fx = Mathf.Clamp((x - cpX0) / cpStep, 0, cpN - 1.001f), fz = Mathf.Clamp((z - cpX0) / cpStep, 0, cpN - 1.001f);
        int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
        float a = cpH[i * cpN + j], b = cpH[(i + 1) * cpN + j], c = cpH[i * cpN + j + 1], d = cpH[(i + 1) * cpN + j + 1];
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    static readonly Dictionary<string, Material> grayMats = new Dictionary<string, Material>();
    static Material GM(string key, Color c)
    {
        if (grayMats.TryGetValue(key, out var m)) return m;
        string p = grayMatDir + "/TV6_" + key + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("PitCrew/LofiPilot/Surface") ?? Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        if (m.HasProperty("_Tint")) m.SetColor("_Tint", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        // 새 LofiPilot 재질 기본 안개는 30~105m 라 마을이 하늘색에 묻힌다 → 빛·안개만 기존 Ground 와 같게
        var g = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/Ground.mat");
        if (g != null) { foreach (var q in new[] { "_Ambient", "_FogColor" }) if (g.HasProperty(q) && m.HasProperty(q)) m.SetColor(q, g.GetColor(q)); foreach (var q in new[] { "_FaceShade", "_FogStart", "_FogEnd" }) if (g.HasProperty(q) && m.HasProperty(q)) m.SetFloat(q, g.GetFloat(q)); }
        EditorUtility.SetDirty(m); grayMats[key] = m; return m;
    }

    static GameObject MeshGo(Transform parent, string name, Mesh mesh, Material mat, bool collider)
    {
        mesh.name = name; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string mp = grayMatDir + "/Mesh_" + name + ".asset"; AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    static void TerrainGray(Transform parent)
    {
        var v = new Vector3[GN * GN]; var t = new int[(GN - 1) * (GN - 1) * 6]; int k = 0;
        for (int i = 0; i < GN; i++) for (int j = 0; j < GN; j++) v[i * GN + j] = new Vector3(GX0 + i * GStep, gH[i, j], GZ0 + j * GStep);
        for (int i = 0; i < GN - 1; i++) for (int j = 0; j < GN - 1; j++)
        {
            int a = i * GN + j, b = (i + 1) * GN + j, c = i * GN + j + 1, d = (i + 1) * GN + j + 1;
            t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d;
        }
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v, triangles = t };
        MeshGo(parent, "TownV6_Terrain", m, GM("Terrain", new Color(.62f, .66f, .48f)), true);
    }

    static GameObject Ribbon(Transform parent, string name, List<Vector3> pts, float width, float lift, Material mat, bool collider)
    {
        var v = new List<Vector3>(); var t = new List<int>(); float h = width * .5f;
        for (int i = 0; i < pts.Count; i++)
        {
            var f = Fl(pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
            var rt = new Vector3(f.z, 0, -f.x); var c = pts[i] + Vector3.up * lift;
            v.Add(c - rt * h); v.Add(c + rt * h);
            if (i > 0) { int a = (i - 1) * 2; t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
        }
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0);
        return MeshGo(parent, name, m, mat, collider);
    }

    static int Buildings(Transform parent, List<object> bs)
    {
        var bT = new GameObject("Buildings").transform; bT.SetParent(parent);
        var wall = GM("Wall", new Color(.90f, .88f, .84f)); var roof = GM("RoofOrange", new Color(.86f, .44f, .22f));
        var wv = new List<Vector3>(); var wt = new List<int>(); var rv = new List<Vector3>(); var rt = new List<int>(); int n = 0, chunk = 0;
        void Flush()
        {
            if (wv.Count == 0) return;
            var wm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; wm.SetVertices(wv); wm.SetTriangles(wt, 0);
            var rm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; rm.SetVertices(rv); rm.SetTriangles(rt, 0);
            MeshGo(bT, $"Walls_{chunk}", wm, wall, true); MeshGo(bT, $"Roofs_{chunk}", rm, roof, false);
            chunk++; wv.Clear(); wt.Clear(); rv.Clear(); rt.Clear();
        }
        foreach (Dictionary<string, object> b in bs)
        {
            var poly = ((List<object>)b["poly"]).Cast<List<object>>().Select(p => new Vector2(F(p[0]), F(p[1]))).ToList();
            if (poly.Count > 1 && poly[0] == poly[poly.Count - 1]) poly.RemoveAt(poly.Count - 1);
            if (poly.Count < 3) continue;
            var cen = poly.Aggregate(Vector2.zero, (s, p) => s + p) / poly.Count;
            if (Mathf.Abs(cen.x) > 390f || Mathf.Abs(cen.y) > 390f) continue;
            if (Area(poly) < 0) poly.Reverse();   // 반시계(위에서 볼 때)로
            float gy = F(b["y"]); float minG = poly.Min(p => CpAt(p.x, p.y));
            long way = b.TryGetValue("osm_way", out var ow) ? System.Convert.ToInt64(ow) : n;
            int floors = 2 + (int)((way * 2654435761L & 0x7fffffff) % 3);
            float y0 = Mathf.Min(gy, minG) - 1f, y1 = gy + floors * 3f;
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var c = poly[(i + 1) % poly.Count]; int s = wv.Count;
                wv.Add(new Vector3(a.x, y0, a.y)); wv.Add(new Vector3(c.x, y0, c.y)); wv.Add(new Vector3(a.x, y1, a.y)); wv.Add(new Vector3(c.x, y1, c.y));
                wt.AddRange(new[] { s, s + 1, s + 2, s + 1, s + 3, s + 2 });   // 반시계 폴리곤 → 바깥 면
            }
            int rs0 = rv.Count; foreach (var p in poly) rv.Add(new Vector3(p.x, y1, p.y));
            foreach (var tri in EarClip(poly)) { rt.Add(rs0 + tri.x); rt.Add(rs0 + tri.z); rt.Add(rs0 + tri.y); }
            n++; if (wv.Count > 60000) Flush();
        }
        Flush(); return n;
    }

    static float Area(List<Vector2> p) { float a = 0; for (int i = 0; i < p.Count; i++) { var u = p[i]; var w = p[(i + 1) % p.Count]; a += u.x * w.y - w.x * u.y; } return a * .5f; }

    static IEnumerable<Vector3Int> EarClip(List<Vector2> p)
    {
        var idx = Enumerable.Range(0, p.Count).ToList(); int guard = 0;
        while (idx.Count > 3 && guard++ < 4 * p.Count)
        {
            bool cut = false;
            for (int k = 0; k < idx.Count; k++)
            {
                int i0 = idx[(k + idx.Count - 1) % idx.Count], i1 = idx[k], i2 = idx[(k + 1) % idx.Count];
                Vector2 a = p[i0], b = p[i1], c = p[i2];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 0) continue;   // 오목 꼭짓점
                bool inside = false;
                foreach (var j in idx) { if (j == i0 || j == i1 || j == i2) continue; if (InTri(p[j], a, b, c)) { inside = true; break; } }
                if (inside) continue;
                yield return new Vector3Int(i0, i1, i2); idx.RemoveAt(k); cut = true; break;
            }
            if (!cut) break;
        }
        if (idx.Count == 3) yield return new Vector3Int(idx[0], idx[1], idx[2]);
        else for (int k = 1; k + 1 < idx.Count; k++) yield return new Vector3Int(idx[0], idx[k], idx[k + 1]);   // 꼬인 폴리곤 폴백(부채)
    }

    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return !(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0)));
    }
}
