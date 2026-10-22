using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PitCrew.TownGen;

/// <summary>
/// 구조 스펙(D-113) 조각 1 — 상자 A(아말피 → 콘카) 땅만: 지형 두 단계 해상도(차도 양옆 60m 띠 2m · 나머지 8m) + 바다 + 차도 표시 띠(디버그).
/// 집·길 깎기 없음(조각 2부터). 입력 = tools/town-gen/out/structure_a/{heights_8m,network}.json (structure_prep.py).
/// 측정 JSON = tools/town-gen/out/scene_structure_s1/measure.json. 광장 땅 조건(§3: 교차점 반경 60m 안 30×24m · 경사 ≤ 7% · 바다 칸 위 금지) 후보를 재서 보고.
/// </summary>
public static class TownGenStructure
{
    public const string ScenePath = "Assets/Scenes/TownGen/TownGen_Structure_v1.unity";
    public const float HeightScale = 0.3f, BandMeters = 60f, FineStep = 2f, SeaLevel = 0f;

    [MenuItem("PitCrew/TownGen/Build Structure A (s1)")]
    public static void BuildMenu() => Debug.Log(Build(ScenePath, "s1"));

    public static string Build(string scenePath, string prefix)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var dir = Path.Combine(TownGenRunner.ToolsDir, "out", "structure_a");
        var hd = JObject.Parse(File.ReadAllText(Path.Combine(dir, "heights_8m.json")));
        int nx = hd["nx"].Value<int>(), nz = hd["nz"].Value<int>(); float step = hd["step_m"].Value<float>();
        float W = hd["width_m"].Value<float>(), H = hd["height_m"].Value<float>();
        var raw = hd["heights_m"].Select(v => v.Value<float>()).ToArray();
        float Hc(float x, float z)
        {
            float fx = Mathf.Clamp(x / step, 0, nx - 1.0001f), fz = Mathf.Clamp(z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            float a = raw[j * nx + i] * (1 - tx) + raw[j * nx + i + 1] * tx, b = raw[(j + 1) * nx + i] * (1 - tx) + raw[(j + 1) * nx + i + 1] * tx;
            return (a * (1 - tz) + b * tz) * HeightScale;
        }
        var nd = JObject.Parse(File.ReadAllText(Path.Combine(dir, "network.json")));
        var roads = nd["segments"].Select(sg => (hw: sg["highway"].ToString(), refs: sg["ref"].ToString(), pts: sg["points_m"].Select(p => new Vector2(p[0].Value<float>(), p[1].Value<float>())).ToList())).ToList();

        // ---- 길 띠: 8m 칸 가운데가 차도에서 60m 안이면 2m 로 나눔 (공간 해시)
        float hc = 64f; var hash = new Dictionary<(int, int), List<(Vector2 a, Vector2 b)>>();
        foreach (var r in roads)
            for (int k = 0; k + 1 < r.pts.Count; k++)
            {
                var a = r.pts[k]; var b = r.pts[k + 1];
                for (int cx = Mathf.FloorToInt(Mathf.Min(a.x, b.x) / hc) - 1; cx <= Mathf.FloorToInt(Mathf.Max(a.x, b.x) / hc) + 1; cx++)
                    for (int cz = Mathf.FloorToInt(Mathf.Min(a.y, b.y) / hc) - 1; cz <= Mathf.FloorToInt(Mathf.Max(a.y, b.y) / hc) + 1; cz++)
                    { if (!hash.TryGetValue((cx, cz), out var l)) hash[(cx, cz)] = l = new List<(Vector2, Vector2)>(); l.Add((a, b)); }
            }
        float DistRoad(Vector2 p)
        {
            float best = float.MaxValue;
            if (!hash.TryGetValue((Mathf.FloorToInt(p.x / hc), Mathf.FloorToInt(p.y / hc)), out var l)) return best;
            foreach (var (a, b) in l) { var ab = b - a; float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0; best = Mathf.Min(best, Vector2.Distance(p, a + ab * t)); }
            return best;
        }
        int cnx = nx - 1, cnz = nz - 1; var fine = new bool[cnx * cnz]; int fineCells = 0;
        for (int j = 0; j < cnz; j++) for (int i = 0; i < cnx; i++) { var c = new Vector2((i + 0.5f) * step, (j + 0.5f) * step); if (DistRoad(c) <= BandMeters + step * 0.71f) { fine[j * cnx + i] = true; fineCells++; } }

        // ---- 바다: 남쪽 변에서 이어진 해수면 이하(배율 후 ≤ 0.15m) 8m 정점
        var sea = new bool[nx * nz]; var q = new Queue<int>();
        for (int i = 0; i < nx; i++) if (raw[i] * HeightScale <= 0.15f) { sea[i] = true; q.Enqueue(i); }
        while (q.Count > 0) { int id = q.Dequeue(), i = id % nx, j = id / nx; foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int a = i + di, b = j + dj; if (a < 0 || b < 0 || a >= nx || b >= nz) continue; int k = b * nx + a; if (!sea[k] && raw[k] * HeightScale <= 0.15f) { sea[k] = true; q.Enqueue(k); } } }
        int seaPts = sea.Count(x => x);

        // 바다 칸은 해저로: 높이 − 3m × (바다 비율, 8m 정점 겹선형) — DEM 바다가 0m 라 바다 면(0m)과 겹쳐 깜빡이던 것
        float Ground(float x, float z)
        {
            float fx = Mathf.Clamp(x / step, 0, nx - 1.0001f), fz = Mathf.Clamp(z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            float S(int a, int b) => sea[b * nx + a] ? 1f : 0f;
            float w = (S(i, j) * (1 - tx) + S(i + 1, j) * tx) * (1 - tz) + (S(i, j + 1) * (1 - tx) + S(i + 1, j + 1) * tx) * tz;
            return Hc(x, z) - 3f * w;
        }
        // ---- 지형 메시: 2m 격자 키로 정점 공유(8m 정점 = 2m 키 4의 배수 → 두 해상도 경계 틈 없음, 경계 위 2m 점은 8m 변 위 직선 보간이라 T 이음도 평면 위)
        int sub = Mathf.RoundToInt(step / FineStep);
        var vid = new Dictionary<long, int>(); var verts = new List<Vector3>(); var uvs = new List<Vector2>();
        int V(int gx, int gz)
        {
            long key = (long)gx * 100000 + gz;
            if (vid.TryGetValue(key, out int id)) return id;
            float x = gx * FineStep, z = gz * FineStep; id = verts.Count; verts.Add(new Vector3(x, Ground(x, z), z)); uvs.Add(new Vector2(x / 8, z / 8)); vid[key] = id; return id;
        }
        var land = new List<int>(); var sand = new List<int>();
        void Tri(int a, int b, int c) { var l = Mathf.Max(verts[a].y, verts[b].y, verts[c].y) < SeaLevel + 1.5f ? sand : land; l.Add(a); l.Add(b); l.Add(c); }
        for (int j = 0; j < cnz; j++)
            for (int i = 0; i < cnx; i++)
            {
                int n = fine[j * cnx + i] ? sub : 1, s = sub / n;
                for (int b = 0; b < n; b++) for (int a = 0; a < n; a++)
                {
                    int gx = i * sub + a * s, gz = j * sub + b * s;
                    int p00 = V(gx, gz), p10 = V(gx + s, gz), p01 = V(gx, gz + s), p11 = V(gx + s, gz + s);
                    // 8m 칸 변 위에 이웃 2m 점이 있으면(굵은 칸이 고운 칸 옆) 그 점까지 부채꼴로 — T 이음을 실제 정점으로 메움
                    if (n == 1)
                    {
                        var ring = new List<int> { p00 };
                        // 아래 변(→), 오른 변(↑), 위 변(←), 왼 변(↓)
                        AddEdge(ring, i, j, 0); ring.Add(p10); AddEdge(ring, i, j, 1); ring.Add(p11); AddEdge(ring, i, j, 2); ring.Add(p01); AddEdge(ring, i, j, 3);
                        if (ring.Count == 4) { Tri(p00, p01, p11); Tri(p00, p11, p10); }
                        else { var ctr = V2(i, j); for (int k = 0; k < ring.Count; k++) Tri(ctr, ring[(k + 1) % ring.Count], ring[k]); }
                    }
                    else { Tri(p00, p01, p11); Tri(p00, p11, p10); }
                }
            }
        void AddEdge(List<int> ring, int i, int j, int side)
        {
            // 이웃 칸이 고운 칸일 때만 그 변의 2m 중간점 추가
            int ni = i + (side == 1 ? 1 : side == 3 ? -1 : 0), nj = j + (side == 0 ? -1 : side == 2 ? 1 : 0);
            if (ni < 0 || nj < 0 || ni >= cnx || nj >= cnz || !fine[nj * cnx + ni]) return;
            for (int k = 1; k < sub; k++)
            {
                int gx, gz;
                if (side == 0) { gx = i * sub + k; gz = j * sub; }
                else if (side == 1) { gx = (i + 1) * sub; gz = j * sub + k; }
                else if (side == 2) { gx = (i + 1) * sub - k; gz = (j + 1) * sub; }
                else { gx = i * sub; gz = (j + 1) * sub - k; }
                ring.Add(V(gx, gz));
            }
        }
        int V2(int i, int j)
        {
            // 굵은 칸 가운데 점(부채꼴 중심) — 2m 키가 아닌 별도 정점
            float x = (i + 0.5f) * step, z = (j + 0.5f) * step; int id = verts.Count; verts.Add(new Vector3(x, Ground(x, z), z)); uvs.Add(new Vector2(x / 8, z / 8)); return id;
        }

        // ---- 광장 땅 조건(§3) 후보: 교차점(차도 끝·꼭짓점이 3갈래 이상 모이는 점) 반경 60m 안 30×24m, 경사 평균 ≤ 7%, 바다 칸 위 금지
        var nodeDeg = new Dictionary<(int, int), int>();
        foreach (var r in roads) for (int k = 0; k < r.pts.Count; k++) { var key = (Mathf.RoundToInt(r.pts[k].x * 2), Mathf.RoundToInt(r.pts[k].y * 2)); nodeDeg[key] = (nodeDeg.TryGetValue(key, out int d) ? d : 0) + (k == 0 || k == r.pts.Count - 1 ? 1 : 2); }
        var junctions = nodeDeg.Where(kv => kv.Value >= 3).Select(kv => new Vector2(kv.Key.Item1 / 2f, kv.Key.Item2 / 2f)).ToList();
        bool SeaAt(float x, float z) { int i = Mathf.Clamp(Mathf.RoundToInt(x / step), 0, nx - 1), j = Mathf.Clamp(Mathf.RoundToInt(z / step), 0, nz - 1); return sea[j * nx + i]; }
        float Grade(float x, float z) { float gx = (Hc(x + 1, z) - Hc(x - 1, z)) / 2, gz = (Hc(x, z + 1) - Hc(x, z - 1)) / 2; return Mathf.Sqrt(gx * gx + gz * gz); }
        var cands = new List<(Vector2 j, Vector2 c, float yaw, float slope)>(); int juncOk = 0;
        foreach (var jn in junctions)
        {
            (Vector2 c, float yaw, float slope) best = (default, 0, float.MaxValue);
            for (float dx = -60; dx <= 60; dx += 6) for (float dz = -60; dz <= 60; dz += 6)
            {
                if (dx * dx + dz * dz > 3600) continue;
                var c = jn + new Vector2(dx, dz);
                foreach (float yaw in new[] { 0f, 30f, 60f, 90f, 120f, 150f })
                {
                    float r = yaw * Mathf.Deg2Rad; var f = new Vector2(Mathf.Sin(r), Mathf.Cos(r)); var rr = new Vector2(f.y, -f.x);
                    float sum = 0; int nn = 0; bool bad = false;
                    for (float a = -15; a <= 15 && !bad; a += 3) for (float b = -12; b <= 12; b += 3)
                    {
                        var p = c + f * a + rr * b;
                        if (p.x < 0 || p.y < 0 || p.x > W || p.y > H || SeaAt(p.x, p.y)) { bad = true; break; }
                        sum += Grade(p.x, p.y); nn++;
                    }
                    if (bad) continue;
                    float sl = sum / nn; if (sl < best.slope) best = (c, yaw, sl);
                }
            }
            if (best.slope < float.MaxValue) { cands.Add((jn, best.c, best.yaw, best.slope)); if (best.slope <= 0.07f) juncOk++; }
        }

        // ---- 씬
        string prevScene = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("열린 씬에 저장 안 한 변경이 있음 — 중단");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // 메시는 새 씬을 연 뒤에 만든다 — 씬을 바꾸기 전에 만든 Mesh 는 씬 전환 때 파괴돼 빈 렌더러가 됨(조각 1 첫 빌드 실측)
        var mesh = new Mesh { name = "StructureTerrain", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(land, 0); mesh.SetTriangles(sand, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();

        var root = new GameObject("TownGen_Structure_A");
        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null) { light.transform.rotation = Quaternion.Euler(50f, -35f, 0f); light.intensity = 1.2f; light.shadows = LightShadows.Soft; light.color = new Color(1f, .96f, .88f); }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .64f, .66f);
        var mGround = Mat("Ground"); var mSand = Mat("ZoneGroundHarbor"); var mSea = Mat("Sea"); var mRoad = Mat("Asphalt");
        var tGo = new GameObject("Terrain"); tGo.transform.SetParent(root.transform, false);
        tGo.AddComponent<MeshFilter>().sharedMesh = mesh; tGo.AddComponent<MeshRenderer>().sharedMaterials = new[] { mGround, mSand ? mSand : mGround };
        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(root.transform, false);
        seaGo.transform.position = new Vector3(W / 2, SeaLevel, H / 2); seaGo.transform.localScale = new Vector3(W / 10 * 3, 1, H / 10 * 3);
        UnityEngine.Object.DestroyImmediate(seaGo.GetComponent<Collider>()); if (mSea) seaGo.GetComponent<Renderer>().sharedMaterial = mSea;
        // 차도 표시 띠(디버그 — 다음 조각에서 진짜 길 단면·깎기로 바뀜): 지형 + 0.4m, primary 7m · 그 밖 4m
        var rv = new List<Vector3>(); var rt = new List<int>();
        foreach (var r in roads)
        {
            float hw = r.hw.StartsWith("primary") ? 3.5f : 2f;
            var dense = new List<Vector2>();
            for (int k = 0; k + 1 < r.pts.Count; k++) { int m = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(r.pts[k], r.pts[k + 1]) / 4)); for (int t = 0; t < m; t++) dense.Add(Vector2.Lerp(r.pts[k], r.pts[k + 1], (float)t / m)); }
            dense.Add(r.pts[r.pts.Count - 1]);
            for (int k = 0; k < dense.Count; k++)
            {
                var d = (dense[Mathf.Min(k + 1, dense.Count - 1)] - dense[Mathf.Max(k - 1, 0)]).normalized; var nrm = new Vector2(-d.y, d.x) * hw;
                var a = dense[k] + nrm; var b = dense[k] - nrm;
                rv.Add(new Vector3(a.x, Hc(a.x, a.y) + 0.4f, a.y)); rv.Add(new Vector3(b.x, Hc(b.x, b.y) + 0.4f, b.y));
                if (k > 0) { int i0 = rv.Count - 4; rt.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3 }); }
            }
        }
        var rmesh = new Mesh { name = "RoadDebug", indexFormat = IndexFormat.UInt32 }; rmesh.SetVertices(rv); rmesh.SetTriangles(rt, 0); rmesh.RecalculateNormals();
        var rGo = new GameObject("Roads_debug_OSM"); rGo.transform.SetParent(root.transform, false); rGo.AddComponent<MeshFilter>().sharedMesh = rmesh; var rmr = rGo.AddComponent<MeshRenderer>(); if (mRoad) rmr.sharedMaterial = mRoad;
        new GameObject("_Credit: © OpenStreetMap contributors (ODbL) · produced using Copernicus WorldDEM-30").transform.SetParent(root.transform, false);

        // ---- 캡처 2장: 톱다운(정사영) · 아말피 항구 앞 바다 위 눈높이 5m 에서 서쪽 곶(카포 디 콘카, 좌표 추정) 방향
        var shots = Path.Combine(TownGenRunner.ToolsDir, "out", "shots"); Directory.CreateDirectory(shots);
        var cam = Camera.main; cam.farClipPlane = 6000;
        cam.orthographic = true; cam.orthographicSize = H / 2 * 1.02f;
        Shot(cam, new Vector3(W / 2, verts.Max(v => v.y) + 100, H / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top.png"));
        cam.orthographic = false;
        Vector2 Geo(double lat, double lon) => new Vector2((float)((lon - 14.5690) * 111320.0 * Math.Cos(40.6130 * Math.PI / 180)), (float)((lat - 40.6130) * 110574.0));
        var harbor = Geo(40.6335, 14.6025);   // 항구(스펙 §3 추정 좌표)
        // 곶(카포 디 콘카) = 땅(바다 아닌 8m 정점) 중 가장 남쪽 점 — 좌표 추정 대신 데이터에서
        var cape = Vector2.zero; float minZ = float.MaxValue;
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) if (!sea[j * nx + i] && raw[j * nx + i] * HeightScale > 1f && j * step < minZ) { minZ = j * step; cape = new Vector2(i * step, j * step); }
        // 항구에서 남쪽으로 내려가 첫 바다 칸 + 40m
        var eye2 = harbor; for (int k = 0; k < 400 && !SeaAt(eye2.x, eye2.y); k++) eye2.y -= 4; eye2.y -= 40;
        var eye = new Vector3(eye2.x, SeaLevel + 5f, eye2.y); var aim = new Vector3(cape.x, Hc(cape.x, cape.y) + 10, cape.y);
        Shot(cam, eye, Quaternion.LookRotation((aim - eye).normalized), Path.Combine(shots, prefix + "_coast.png"));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", scenePath)));
        EditorSceneManager.SaveScene(scene, scenePath);
        if (!string.IsNullOrEmpty(prevScene)) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);

        // ---- 측정 JSON(§10)
        float bandHa = fineCells * step * step / 10000f, landHa = (cnx * cnz - fineCells) * step * step / 10000f;
        var lengths = new JObject(); foreach (var g in roads.GroupBy(r => r.hw)) lengths[g.Key] = g.Sum(r => Enumerable.Range(1, r.pts.Count - 1).Sum(k => Vector2.Distance(r.pts[k - 1], r.pts[k])));
        var candArr = new JArray(); foreach (var c in cands.OrderBy(c => c.slope).Take(12)) candArr.Add(new JObject { ["junction"] = new JArray(c.j.x, c.j.y), ["center"] = new JArray(c.c.x, c.c.y), ["yawDeg"] = c.yaw, ["meanGrade"] = c.slope, ["ok"] = c.slope <= 0.07f });
        var outDir = Path.Combine(TownGenRunner.ToolsDir, "out", "scene_structure_" + prefix); Directory.CreateDirectory(outDir);
        var measure = new JObject
        {
            ["scene"] = scenePath, ["generated"] = DateTime.Now.ToString("s"), ["box_swne"] = hd["box_swne"], ["size_m"] = new JArray(W, H), ["heightScale"] = HeightScale,
            ["terrain"] = new JObject { ["vertices"] = verts.Count, ["triangles"] = (land.Count + sand.Count) / 3, ["coarseStep"] = step, ["fineStep"] = FineStep, ["bandMeters"] = BandMeters, ["fineCells8m"] = fineCells, ["fineAreaHa"] = bandHa, ["coarseAreaHa"] = landHa, ["maxY"] = verts.Max(v => v.y), ["seaGridPoints8m"] = seaPts },
            ["roads"] = new JObject { ["segments"] = roads.Count, ["lengthByHighway_m"] = lengths },
            ["plazaSite"] = new JObject { ["rule"] = "spec D-113 §3: junction(deg≥3) within 60m, 30x24m rect, mean grade ≤7% (DEM 30m bilinear, ±1m differences), not over sea", ["junctions"] = junctions.Count, ["junctionsWithSite"] = juncOk, ["best"] = candArr, ["demNote"] = "DEM 해상도 30m — 30×24m 평지 판정은 근사(스펙 §3)" },
        };
        File.WriteAllText(Path.Combine(outDir, "measure.json"), measure.ToString(), new UTF8Encoding(false));
        sw.Stop();
        return $"[TownGen] Structure {scenePath} · 정점 {verts.Count} · 삼각형 {(land.Count + sand.Count) / 3} · 고운 칸 {fineCells}({bandHa:0}ha) · 차도 {roads.Count} · 교차점 {junctions.Count} 중 광장 땅 조건 통과 {juncOk} · {sw.ElapsedMilliseconds}ms";
    }

    public const string ScenePathV2 = "Assets/Scenes/TownGen/TownGen_Structure_v2.unity";

    [MenuItem("PitCrew/TownGen/Build Structure A + SS163 (s2)")]
    public static void BuildV2Menu() => Debug.Log(BuildV2(ScenePathV2, "s2"));

    /// <summary>
    /// 조각 2: SS163(아말피 → 콘카) 만 기존 곡선 큰길 코어(SplineTown: 필렛·종단 경사·깎기/메우기·옹벽)로. 집·골목·광장 끔.
    /// 지형 = 코어가 2m 로 깎고 메운 땅을 SS163 양옆 60m 띠에서만 2m 로 그리고, 나머지는 8m(같은 땅의 8m 점). 바다 칸은 해저(−3m)로.
    /// </summary>
    public const string ScenePathV3 = "Assets/Scenes/TownGen/TownGen_Structure_v3.unity";

    [MenuItem("PitCrew/TownGen/Build Structure A + centre (s3)")]
    public static void BuildV3Menu() => Debug.Log(BuildV2(ScenePathV3, "s3", true));

    /// <summary>center = 조각 3: 아말피 항구 둘레 350m 차도 + 골목 + 광장(30×24·≤7%, 항구 300m 안) + 광장 반경 120m 안 집(장면 ①).</summary>
    public const string ScenePathV4 = "Assets/Scenes/TownGen/TownGen_Structure_v4.unity";
    public const string ScenePathV5 = "Assets/Scenes/TownGen/TownGen_Structure_v5.unity";

    [MenuItem("PitCrew/TownGen/Build Structure A density houses (s5)")]
    public static void BuildV5Menu() => Debug.Log(BuildV2(ScenePathV5, "s5", true, false, true));

    public const string ScenePathV6 = "Assets/Scenes/TownGen/TownGen_Structure_v6.unity";
    public const string ScenePathV7 = "Assets/Scenes/TownGen/TownGen_Structure_v7.unity";
    public const string ScenePathV8 = "Assets/Scenes/TownGen/TownGen_Structure_v8.unity";
    public const string ScenePathV9 = "Assets/Scenes/TownGen/TownGen_Structure_v9.unity";

    /// <summary>조각 7b(스펙 §4.3): 콘카 마을 차도 = 입구 길 1개(SS163 에 가장 가까운 마을 OSM 주거도로 덩어리), 나머지 마을 OSM 길은 골목, 마을 OSM steps 는 공용 계단.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + Conca entry road + alleys (s7b)")]
    public static void BuildV9Menu() => Debug.Log(BuildV2(ScenePathV9, "s7b", true, false, true, true, true, true, true));

    public const string ScenePathV10 = "Assets/Scenes/TownGen/TownGen_Structure_v10.unity";
    public const string ScenePathV10Fill = "Assets/Scenes/TownGen/TownGen_Structure_v10_fill.unity";

    /// <summary>조각 10(D-114): v9 길·광장 그대로 + 집 = OSM 실제 건물 윤곽 압출 상자. 규칙 집 끔.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + OSM buildings (s10)")]
    public static void BuildV10Menu() => Debug.Log(BuildV2(ScenePathV10, "s10", true, false, true, true, true, true, true, osm: true));

    /// <summary>조각 10 스위치: OSM 건물 + 실제 건물이 비는 곳(윤곽에서 15m 밖)만 규칙 집으로 채움.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + OSM buildings + rule fill (s10 fill)")]
    public static void BuildV10FillMenu() => Debug.Log(BuildV2(ScenePathV10Fill, "s10fill", true, false, true, true, true, true, true, osm: true, osmFill: true));

    public const string ScenePathV11 = "Assets/Scenes/TownGen/TownGen_Structure_v11.unity";

    /// <summary>조각 11: v10 + 아말피 중심 반경 400m 를 '실제 마을'로 — OSM 보행길 전 종류(OSM 선·폭 그대로, 건물에 막히면 길 폭을 줄임) + 실제 광장 면,
    /// tunnel/covered 길 위 건물은 살림, 중심 건물은 차로 겹침일 때만 뺌. 만든 광장(30×24)은 끔 — 실제 광장이 대신한다.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + Amalfi town core (s11)")]
    public static void BuildV11Menu() => Debug.Log(BuildV2(ScenePathV11, "s11", true, false, true, true, true, true, true, osm: true, townCore: true));

    public const string ScenePathV12 = "Assets/Scenes/TownGen/TownGen_Structure_v12.unity";

    /// <summary>조각 12(D-115): v11 + 중심 living_street·service 를 좁은 차로(OSM width 4m, 골목 단면)로 되돌려 차도 망에 잇고,
    /// 광장 면 안 차도 선은 경계 1m 앞에서 끊는다. 차도가 닿는 광장 둘레마다 정차 자리 1개 + 볼라드 3개. 보행길(footway·steps·pedestrian·path)은 v11 그대로.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + Amalfi core car lanes + plaza pockets (s12)")]
    public static void BuildV12Menu() => Debug.Log(BuildV2(ScenePathV12, "s12", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true));

    public const string ScenePathV13 = "Assets/Scenes/TownGen/TownGen_Structure_v13.unity";

    /// <summary>조각 13(도현 2026-10-20 아이디어 1+4): v12 + 중심 400m 마을 길 폭 = 건물 사이 틈(가운데 선도 틈 가운데로, 최대 3m) + 건물-길 겹침 자동 검사(measure.json overlaps[]).</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + core gap-width paths + overlap check (s13)")]
    public static void BuildV13Menu() => Debug.Log(BuildV2(ScenePathV13, "s13", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true));

    public const string ScenePathV14 = "Assets/Scenes/TownGen/TownGen_Structure_v14.unity";

    /// <summary>조각 14(도현 승인 A 2026-10-20): v13 + 중심 400m 마을 길(좁은 차로·보행·광장·계단 디딤)을 buffer 합집합 − 건물 − 큰길 = 한 장 면으로(TownGenRoadSheet, shapely).</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + core one-sheet road surface (s14)")]
    public static void BuildV14Menu() => Debug.Log(BuildV2(ScenePathV14, "s14", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true, sheet: true));

    public const string ScenePathV15 = "Assets/Scenes/TownGen/TownGen_Structure_v15.unity";

    /// <summary>조각 15a(D-116 윗길 고리): v14 + 윗길 가지(Via Pimenio secondary → Via Primo Maggio, OSM 덩어리 약 9.85km) 차도 + 콘카 Via Miramare 서쪽 끝까지 + 상자 서쪽 경계 22m 잇기. measure.json carGraph.</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + upper road loop (s15)")]
    public static void BuildV15Menu() => Debug.Log(BuildV2(ScenePathV15, "s15", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true, sheet: true, upper: true));

    /// <summary>조각 15a 비교 변형: 콘카 입구를 7b 생성 잇기 대신 실제 OSM 길(Via Miramare 동쪽 → Via Don G. Amodio → SS163 (536,266))로. 측정만(R1 판단 자료).</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A + upper road loop, real Conca entry (s15 variant)")]
    public static void BuildV15RealMenu() => Debug.Log(BuildV2("Assets/Scenes/TownGen/TownGen_Structure_v15_realEntry.unity", "s15real", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true, sheet: true, upper: true, realEntry: true));

    public const string ScenePathV15a2 = "Assets/Scenes/TownGen/TownGen_Structure_v15a2.unity";

    /// <summary>조각 15a-2(15a 기획 판단): 콘카 입구 = 실제 OSM(Via Don G. Amodio, 7b 잇기 0) · 콘카 100m 안 차도 = 항구→콘카 경로 1·2 가 지나는 way 만(town-structure §4.4) · 경계 잇기 헤어핀.</summary>
    public const string ScenePathV15a3 = "Assets/Scenes/TownGen/TownGen_Structure_v15a3.unity";

    /// <summary>조각 15a-3: v15a2 + 차도 tunnel 예외 제거(그린 차도 위 건물은 뺌) + 검사 O2(씬 메시 기준 건물 ∩ 길 면, 상자 전체) + 그 자리 확인 캡처. 15a-4: 보도 면을 놓인 건물 윤곽에서 잘라냄(B).</summary>
    [MenuItem("PitCrew/TownGen/Build Structure A v15a3 (no car-tunnel exception, scene-mesh O2)")]
    public static void BuildV15a3Menu() => Debug.Log(BuildV2(ScenePathV15a3, "s15a3", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true, sheet: true, upper: true, realEntry: true, concaRoutes: true, carTunnel: false, sceneOverlap: true, walkClip: true));

    [MenuItem("PitCrew/TownGen/Build Structure A + upper loop, real Conca entry, route-only village roads (s15a2)")]
    public static void BuildV15a2Menu() => Debug.Log(BuildV2(ScenePathV15a2, "s15a2", true, false, true, true, true, true, true, osm: true, townCore: true, carLanes: true, gapWidth: true, sheet: true, upper: true, realEntry: true, concaRoutes: true));

    /// <summary>조각 12: 중심에서 좁은 차로로 되돌리는 OSM 종류.</summary>
    static readonly HashSet<string> LaneHighways = new HashSet<string> { "living_street", "service" };

    static readonly HashSet<string> CarHighways =new HashSet<string> { "motorway", "trunk", "primary", "secondary", "tertiary", "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link", "residential", "unclassified", "road" };

    /// <summary>규칙 집 후보 거르개(조각 10). null = 거르지 않음.</summary>
    static Func<Vec2, bool> ruleFilter;
    static List<SplineTown.HouseCand> RuleFilter(List<SplineTown.HouseCand> c) => ruleFilter == null ? c : c.Where(x => ruleFilter(x.c)).ToList();

    [MenuItem("PitCrew/TownGen/Build Structure A + Conca hill village (s7)")]
    public static void BuildV8Menu() => Debug.Log(BuildV2(ScenePathV8, "s7", true, false, true, true, true, true));

    [MenuItem("PitCrew/TownGen/Build Structure A + centre alleys/stairs (s5b)")]
    public static void BuildV7Menu() => Debug.Log(BuildV2(ScenePathV7, "s5b", true, false, true, true, true));

    [MenuItem("PitCrew/TownGen/Build Structure A + scenic houses (s6)")]
    public static void BuildV6Menu() => Debug.Log(BuildV2(ScenePathV6, "s6", true, false, true, true));

    [MenuItem("PitCrew/TownGen/Build Structure A density debug (s4)")]
    public static void BuildV4Menu() => Debug.Log(BuildV2(ScenePathV4, "s4", true, true));

    /// <summary>density = 조각 4: center 와 같은 길·광장(집·상점 없음) + 밀도 지도(스펙 D-113 §4) 색칠 겹침판.</summary>
    public static string BuildV2(string scenePath, string prefix, bool center = false, bool density = false, bool densityHouses = false, bool scenic = false, bool relax = false, bool village = false, bool villageEntry = false, bool osm = false, bool osmFill = false, bool townCore = false, bool carLanes = false, bool gapWidth = false, bool sheet = false, bool upper = false, bool realEntry = false, bool concaRoutes = false, bool carTunnel = true, bool sceneOverlap = false, bool walkClip = false)
    {
        ruleFilter = null;
        var harborGeo = new Vec2((14.6025 - 14.5690) * 111320.0 * Math.Cos(40.6130 * Math.PI / 180), (40.6335 - 40.6130) * 110574.0);   // 아말피 항구(스펙 §3 추정 좌표)
        const double centerRoadRadius = 350;
        var concaGeo = new Vec2((14.5725004 - 14.5690) * 111320.0 * Math.Cos(40.6130 * Math.PI / 180), (40.6176092 - 40.6130) * 110574.0);   // OSM place=village 콘카 데이 마리니(씨앗, 스펙 §4 ④)
        const double villageRadius = 80;
        double villageRoadRadius = villageEntry ? villageRadius + 20 : 150;   // s7b: 마을 길은 반경 100m 안만(80~150m 띠의 secondary·주거도로 조각이 잇기 길을 만들었음 — STRUCTURE-7)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var dir = Path.Combine(TownGenRunner.ToolsDir, "out", "structure_a");
        var hd = JObject.Parse(File.ReadAllText(Path.Combine(dir, "heights_8m.json")));
        int nx = hd["nx"].Value<int>(), nz = hd["nz"].Value<int>(); float step = hd["step_m"].Value<float>();
        float W = hd["width_m"].Value<float>(), H = hd["height_m"].Value<float>();
        var raw = hd["heights_m"].Select(v => v.Value<float>()).ToArray();
        var hf = new GridHeightField(raw, nx, nz, step, step);
        var nd = JObject.Parse(File.ReadAllText(Path.Combine(dir, "network.json")));
        const double townCoreRadius = 400;
        // 조각 12: 광장 면(area=yes) 고리 — 차도 선을 이 안에서 자른다
        var plazaRings = carLanes ? JObject.Parse(File.ReadAllText(Path.Combine(dir, "town_paths.json")))["areas"].Select(a => a["ring"].Select(p => new Vec2(p[0].Value<double>(), p[1].Value<double>())).ToArray()).ToList() : new List<Vec2[]>();
        double plazaClipped = 0; var plazaClipWays = new JObject();
        // 조각 15a: 윗길 가지 OSM way 들(SS163 을 뺀 차도 덩어리 중 Via Primo Maggio 가 든 것)
        var upperWays = upper ? TownGenCarGraph.UpperBranchWays(nd) : new HashSet<long>();
        var entryWays15 = realEntry ? TownGenCarGraph.BranchWays(nd, TownGenCarGraph.MiramareWay) : new HashSet<long>();   // 변형: Via Miramare + Via Don G. Amodio 덩어리(OSM 1,333m)
        HashSet<long> concaRouteWays = null;   // 15a-2: 콘카 반경 안에서 차도로 둘 way(경로 1·2). null = 반경 안도 전부
        List<Vec2> entryJoin = null;   // 조각 15a: 콘카 입구 잇기(7b) — 윗길 없는 1차 빌드의 생성 잇기 선을 그대로 실제 길처럼 넣는다
        List<OsmWay> MakeWays(bool tc, bool lanes, HashSet<long> clipSkip = null, bool withUpper = true)
        {
            var wl = new List<OsmWay>();
            foreach (var sg in nd["segments"])
            {
                bool ss163 = sg["ref"].ToString().Contains("SS163");
                if (!ss163 && !center) continue;
                long wid = sg["osm_way"].Value<long>(); bool upWay = upper && withUpper && tc && (upperWays.Contains(wid) || entryWays15.Contains(wid)), miraOn = false;
                // 중심 모드: SS163 은 통째, 다른 차도는 항구 반경 350m 안 구간만(밖으로 나가면 끊어 따로)
                var run = new List<Vec2>(); string hw = sg["highway"].ToString(); bool runUp = upWay;
                void Flush()
                {
                    if (run.Count >= 2)
                    {
                        // 조각 12: 좁은 차로는 큰길로 올리지 않게 service 로 넘긴다(SplineTown 골목 단면) · 광장 면 안 차도 선은 경계 1m 앞에서 자른다
                        var parts = new List<List<Vec2>> { run };
                        if (lanes && !ss163 && (clipSkip == null || !clipSkip.Contains(sg["osm_way"].Value<long>()))) { double rm = 0; parts = TownGenCarLanes.ClipOutside(run, plazaRings, 1.0, ref rm); if (rm > 0 && clipSkip == null) { plazaClipped += rm; plazaClipWays[$"{hw}:{sg["osm_way"]}:{sg["name"]}"] = Math.Round(rm, 1); } }
                        foreach (var pp in parts) { var w = new OsmWay { highway = upWay && runUp ? "secondary" : lanes && LaneHighways.Contains(hw) ? "service" : hw, wayId = sg["osm_way"].Value<long>() }; w.points.AddRange(pp); wl.Add(w); }   // 15a: 윗길 가지는 전부 큰길로(골목·서비스 길로 남으면 덩어리가 끊겨 생성기가 가짜 잇기를 깖)
                    }
                    run = new List<Vec2>();
                }
                foreach (var p in sg["points_m"])
                {
                    var v = new Vec2(p[0].Value<double>(), p[1].Value<double>());
                    bool vIn = village && SplineEdge.Dist(v, concaGeo) <= villageRoadRadius && !(villageEntry && VillageMainTags.Contains(hw));
                    bool coreSkip = tc && !CarHighways.Contains(hw) && !(lanes && LaneHighways.Contains(hw)) && SplineEdge.Dist(v, harborGeo) <= townCoreRadius;   // 조각 11: 중심 보행길은 OSM 선 그대로 따로 그린다(12: living_street·service 는 차로)
                    // 조각 15a: Via Miramare 는 마을 반경에 들어온 뒤 서쪽 끝(상자 경계 잇기)까지 통째로
                    if (upper && withUpper && tc && wid == TownGenCarGraph.MiramareWay && SplineEdge.Dist(v, concaGeo) <= villageRoadRadius) miraOn = true;
                    bool upPt = upWay && (concaRouteWays == null || SplineEdge.Dist(v, concaGeo) > villageRoadRadius || concaRouteWays.Contains(wid));   // 15a-2: 반경 안 경로 밖 way 는 마을 규칙(골목)으로
                    bool inc = !coreSkip && (ss163 || SplineEdge.Dist(v, harborGeo) <= centerRoadRadius || vIn || upPt || miraOn);
                    if (inc && upWay && run.Count > 0 && upPt != runUp) { var last = run[run.Count - 1]; Flush(); run.Add(last); }   // 차도 ↔ 골목 바뀌는 점에서 나눔(이음점 공유)
                    if (inc) { if (run.Count <= 1) runUp = upPt; run.Add(v); } else Flush();
                }
                Flush();
            }
            if (upper && withUpper && tc)
            {
                var jw = new OsmWay { highway = "secondary", wayId = TownGenCarGraph.JoinWayId }; jw.points.Add(TownGenCarGraph.JoinA); jw.points.Add(TownGenCarGraph.JoinB); wl.Add(jw);   // D-116 상자 경계 잇기 22m
                if (entryJoin != null) { var ew = new OsmWay { highway = "secondary", wayId = TownGenCarGraph.EntryJoinWayId }; ew.points.AddRange(entryJoin); wl.Add(ew); }
            }
            return wl;
        }
        var ways = MakeWays(townCore, carLanes);
        // §4.2 ⓑ 중심 안 OSM steps 자리 = 공용 계단(spline §11.2)
        var stairLines = new List<List<Vec2>>();
        if (relax && !townCore)
            foreach (var sg in nd["segments"])
                if (sg["class"]?.ToString() == "steps")
                {
                    var pl = sg["points_m"].Select(q => new Vec2(q[0].Value<double>(), q[1].Value<double>())).ToList();
                    if (SplineEdge.Dist(pl[pl.Count / 2], harborGeo) <= 200 && SplineEdge.PolyLength(pl) >= 4) stairLines.Add(pl);
                }
        // §4.3 s7b: 입구 길 = 반경 100m 안 마을 OSM 주거도로 덩어리 중 SS163 에 가장 가까운 것 · 마을 OSM steps = 공용 계단(이어진 토막은 한 줄로)
        var entryWays = new List<long>(); JObject entryPick = null; var villageStairs = new List<List<Vec2>>();
        if (villageEntry)
        {
            entryPick = PickVillageEntry(nd, concaGeo, villageRoadRadius, entryWays);
            var vs = new List<List<Vec2>>();
            foreach (var sg in nd["segments"])
                if (sg["class"]?.ToString() == "steps")
                {
                    var pl = sg["points_m"].Select(q2 => new Vec2(q2[0].Value<double>(), q2[1].Value<double>())).ToList();
                    if (SplineEdge.Dist(pl[pl.Count / 2], concaGeo) <= villageRoadRadius) vs.Add(pl);
                }
            villageStairs = ChainPolylines(vs).Where(pl => SplineEdge.PolyLength(pl) >= 4).ToList();
            stairLines.AddRange(villageStairs);
        }
        var cfg = AssetDatabase.LoadAssetAtPath<TownGenConfig>(TownGenRunner.ConfigPath);
        var s = cfg != null && cfg.spline != null ? cfg.spline.Clone() : new SplineTownSettings();
        s.boundsXMeters = W; s.boundsZMeters = H; s.heightScale = HeightScale; s.seaLevelMeters = SeaLevel;
        s.seamZMin = -1; s.seamZMax = -1; s.houseCap = 0; s.plazaEnabled = false; s.isolatedDropLengthMeters = 1e9f;
        s.houseModelWidths = new float[0]; s.houseModelDepths = new float[0]; s.houseModelCenterX = new float[0]; s.houseModelCenterZ = new float[0]; s.houseModelNames = new string[0];
        var models = new List<(string name, GameObject go)>(); SplineTownSettings s10 = null;
        if (center)
        {
            // 장면 ①(스펙 D-113 §2): 광장 반경 120m · 집 150~180 목표(상한 180) · 광장 30×24m 경사 ≤ 7%(§3 땅 조건), 항구 300m 안
            var ms = new[] { "CoastalHouse", "CoastalHouseB", "CoastalHouseC", "CoastalHouseD", "CoastalHouseE", "CoastalHouseF", "CornerShop", "CornerShopB", "CornerShopC", "RepairGarage", "RepairGarageB" }
                .Select(n => (name: n, go: AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/LofiPilot/{n}.prefab"))).Where(m => m.go != null)
                .Select(m => { var b = TownGenSpline.PrefabBounds(m.go); return (m.name, m.go, w: b.size.x, d: b.size.z, cx: b.center.x, cz: b.center.z); }).ToList();
            s.houseModelWidths = ms.Select(m => m.w).ToArray(); s.houseModelDepths = ms.Select(m => m.d).ToArray();
            s.houseModelCenterX = ms.Select(m => m.cx).ToArray(); s.houseModelCenterZ = ms.Select(m => m.cz).ToArray(); s.houseModelNames = ms.Select(m => m.name).ToArray();
            models = ms.Select(m => (m.name, m.go)).ToList();
            s.houseCap = density || densityHouses ? 0 : 180; if (density) s.shopsMax = 0; s.plazaEnabled = true;
            s.isolatedDropLengthMeters = new SplineTownSettings().isolatedDropLengthMeters;   // 중심: 항구 둘레 주거 도로 조각(≥80m)도 큰길망에 잇는다(SS163 만일 때의 "모두 버림" 해제)
            s.plazaLength = 30; s.plazaWidth = 24; s.plazaMaxSiteGrade = 0.07f;
            if (village) { s.alleyRelax2X = (float)concaGeo.x; s.alleyRelax2Z = (float)concaGeo.z; s.alleyRelax2Radius = (float)villageRadius + 20; }   // ④ 마을 골목(반경 80m + 골목 중점 여유 20m)
            if (villageEntry) { s.noPromoteX = (float)concaGeo.x; s.noPromoteZ = (float)concaGeo.z; s.noPromoteRadius = (float)villageRoadRadius; s.noPromoteKeepWayIds = entryWays.ToArray(); s.alleyAnchors = villageStairs.SelectMany(pl => new[] { pl[0], pl[pl.Count - 1] }).ToArray(); }   // §4.3 입구 길만 차도 · 계단 끝 = 골목 닻
            if (relax) { s.alleyRelaxX = (float)harborGeo.x; s.alleyRelaxZ = (float)harborGeo.z; s.alleyRelaxRadius = 200; }   // §4.2 ⓐ — 광장은 길 뒤에 정해져 항구 점 기준 200m(광장이 항구에서 약 67m, 도로 거리 150m 근사)
            if (carLanes) s10 = s.Clone();   // 비교용 v10 설정(만든 광장 켬·골목 3m·완화 200m)
            if (townCore) s.plazaEnabled = false;
            if (carLanes) { s.alleyHalfWidthMeters = 2.0f; s.alleyRelaxRadius = (float)townCoreRadius; s.alleyChain = true; }   // OSM width 4m(중심 living_street·service 전부 4) · 중심 400m 전체를 골목 완화 원으로(안 닿는 차로 조각은 30m 안 큰길에 잇기)
            s.centerSearchX = (float)harborGeo.x; s.centerSearchZ = (float)harborGeo.z; s.centerSearchRadius = 300; s.houseRegionRadius = 120; s.plazaJunctionSearchRadius = 60;
        }
        List<TownGenOsmBuildings.B> osmB = null; JObject osmJ = null;
        (List<TownGenTownPaths.PathWay> paths, List<TownGenTownPaths.PathWay> cars, List<TownGenTownPaths.Area> areas, Vec2 harbor, double radius) tp = default;
        int carTunnelWays = 0; Func<Vec2, Vec2, bool> segUnder = null; Func<TownGenOsmBuildings.B, bool> lenient = null; TownGenTownPaths.Result tpr = null; TownGenRoadSheet.Result rsh = null; JObject tpJ = null; TownGenCarLanes.Result clr = null; List<TownGenTownPaths.PathWay> lanePaths = null; JObject laneFallback = null;
        if (townCore)
        {
            tp = TownGenTownPaths.Load(Path.Combine(dir, "town_paths.json"));
            var underCar = tp.cars.Concat(carLanes ? tp.paths.Where(w => LaneHighways.Contains(w.highway)) : Enumerable.Empty<TownGenTownPaths.PathWay>()).Where(w => w.under).Select(w => w.pts).ToList();
            if (carLanes) { lanePaths = tp.paths.Where(w => LaneHighways.Contains(w.highway)).ToList(); tp.paths = tp.paths.Where(w => !LaneHighways.Contains(w.highway)).ToList(); }
            segUnder = (a, b) => { var m = new Vec2((a.x + b.x) / 2, (a.z + b.z) / 2); return underCar.Any(pl => Enumerable.Range(0, pl.Count - 1).Any(k => SegDist(m, pl[k], pl[k + 1]) <= 4)); };
            carTunnelWays = underCar.Count;
            if (!carTunnel) segUnder = null;   // 15a-3 ①: 차도 tunnel/covered 예외 없음 — 생성기는 차도를 땅 위 열린 길로 그리므로 그 위 건물은 빼야 한다(건물 밑 보행길 building_passage 는 TownPaths 가 따로 그리지 않음)
            lenient = carLanes ? (Func<TownGenOsmBuildings.B, bool>)(b => SplineEdge.Dist(b.c, harborGeo) <= townCoreRadius && b.road != "alley") : (b => SplineEdge.Dist(b.c, harborGeo) <= townCoreRadius);   // 12: 좁은 차로 겹침도 차로 겹침 → 뺌
        }
        if (osm) { osmB = TownGenOsmBuildings.Load(Path.Combine(dir, "buildings.json")); if (!osmFill) s.shopsMax = 0; }
        if (concaRoutes) { s.hairpinPads = true; s.padTips = new[] { new Vec2((TownGenCarGraph.JoinA.x + TownGenCarGraph.JoinB.x) / 2, (TownGenCarGraph.JoinA.z + TownGenCarGraph.JoinB.z) / 2) }; }   // 15a-2: U자 = 헤어핀 패드, 경계 잇기는 중간점을 지나게
        JObject entryJoinJ = null;
        if (upper && !realEntry)
        {
            // 조각 15a: 윗길을 넣으면 콘카가 윗길 쪽으로 이어져 생성기가 해안(SS163) 쪽 콘카 입구 잇기를 더는 안 만든다 → 윗길 없이 한 번 돌려 그 잇기 선을 받아 넣는다
            var pre = SplineTown.Build(MakeWays(townCore, carLanes, null, false), hf, s);
            var cands = pre.connectorRaw.Where(pl => pl.Count >= 2 && Math.Min(SplineEdge.Dist(pl[0], concaGeo), SplineEdge.Dist(pl[pl.Count - 1], concaGeo)) <= villageRoadRadius + 60).OrderBy(pl => Math.Min(SplineEdge.Dist(pl[0], concaGeo), SplineEdge.Dist(pl[pl.Count - 1], concaGeo))).ToList();
            entryJoin = cands.FirstOrDefault();
            entryJoinJ = new JObject { ["rule"] = "7b Conca entry join kept: generator connector from a build without the upper road (nearest to the Conca seed), fed back as a road line", ["found"] = entryJoin != null, ["length_m"] = entryJoin == null ? 0 : Math.Round(SplineEdge.PolyLength(entryJoin), 1),
                ["from"] = entryJoin == null ? null : new JArray(Math.Round(entryJoin[0].x, 1), Math.Round(entryJoin[0].z, 1)), ["to"] = entryJoin == null ? null : new JArray(Math.Round(entryJoin[entryJoin.Count - 1].x, 1), Math.Round(entryJoin[entryJoin.Count - 1].z, 1)), ["preConnectors"] = pre.connectorRaw.Count };
            ways = MakeWays(townCore, carLanes);
        }
        JObject concaRoutesJ = null;
        if (upper && concaRoutes)
        {
            // 15a-2 §4.4: 반경 안 윗길·입구 way 를 전부 차도로 한 번 돌려 항구→콘카 경로(서로 다른 것)를 구하고, 그 경로가 지나는 way 만 차도로 남긴다
            var pre = SplineTown.Build(ways, hf, s);
            var preLines = nd["segments"].Where(sg => upperWays.Contains(sg["osm_way"].Value<long>())).Select(sg => sg["points_m"].Select(q2 => new Vec2(q2[0].Value<double>(), q2[1].Value<double>())).ToList()).ToList();
            var preG = TownGenCarGraph.Build(pre, s, preLines, null, harborGeo, concaGeo, harborGeo, townCoreRadius);
            var rt = (JArray)preG["routes"][0]["routes"];
            var routeLines = rt.Where(r => r["distinct"].Value<bool>()).SelectMany(r => r["sceneEdges"].Select(x => pre.edges[x.Value<int>()].pts)).ToList();
            concaRouteWays = new HashSet<long>(); var listJ = new JArray(); double inLen = 0;
            foreach (var sg in nd["segments"])
            {
                long wid = sg["osm_way"].Value<long>(); if (!upperWays.Contains(wid) && !entryWays15.Contains(wid)) continue;
                var pts = sg["points_m"].Select(q2 => new Vec2(q2[0].Value<double>(), q2[1].Value<double>())).Where(v => SplineEdge.Dist(v, concaGeo) <= villageRoadRadius).ToList();
                if (pts.Count == 0) continue;
                int near = pts.Count(v => routeLines.Any(pl => NearPolyline(v, pl, 6)));
                bool keep = near * 2 >= pts.Count;
                double L = 0; for (int k = 1; k < pts.Count; k++) { double d = SplineEdge.Dist(pts[k - 1], pts[k]); if (d < 20) L += d; }
                if (keep) { concaRouteWays.Add(wid); inLen += L; }
                listJ.Add(new JObject { ["way"] = wid, ["name"] = sg["name"]?.ToString(), ["highway"] = sg["highway"]?.ToString(), ["inRadius_m"] = Math.Round(L, 1), ["onRoutePts"] = $"{near}/{pts.Count}", ["car"] = keep });
            }
            concaRoutesJ = new JObject { ["rule"] = "§4.4: within villageRoadRadius of the Conca seed, only OSM ways that the distinct harbor→Conca routes (pass 1, all ways as roads) run along (≥50% of in-radius points within 6m) stay roads; the rest go to the village alley rule", ["radius_m"] = villageRoadRadius,
                ["pass1Routes"] = new JArray(rt.Select(r => new JObject { ["length_m"] = r["length_m"], ["distinct"] = r["distinct"], ["character"] = r["character"] })), ["carWayCount"] = concaRouteWays.Count, ["carInRadius_m"] = Math.Round(inLen, 1), ["ways"] = listJ };
            if (villageEntry) s.noPromoteKeepWayIds = concaRouteWays.ToArray();   // 7b 의 "입구 길 1개" 대신 경로 way 만 차도로 올릴 수 있게
            ways = MakeWays(townCore, carLanes);
        }
        var gw = new TownGenGapWidth.Stats(); JObject ovJ = null;
        if (gapWidth) { var su = segUnder; s.alleyAdjust = r0 => TownGenGapWidth.AdjustLanes(r0, osmB, harborGeo, townCoreRadius, s, su, gw); }   // 조각 13: 깎기 전에 차로 폭 = 건물 사이 틈
        var res = SplineTown.Build(ways, hf, s);
        s.alleyAdjust = null;   // 비교용 다시 생성(Duomo 변형)은 예전 폭
        var t = res.terrain;
        if (osm)
        {
            // 조각 10: 길·광장이 정해진 뒤 윤곽 분류(길 겹침·바다·광장) → 규칙 집 거르개(끔 / 빈 곳만)
            TownGenOsmBuildings.Classify(osmB, res, s, true, null, segUnder, lenient);
            ruleFilter = osmFill ? TownGenOsmBuildings.EmptyAreaFilter(osmB, 15) : (_ => false);
        }

        // 바다: 8m 원래 높이에서 남쪽 변과 이어진 해수면 이하 → 코어 지형(2m)에 해저 −3m × 바다 비율
        var sea = new bool[nx * nz]; var q = new Queue<int>();
        for (int i = 0; i < nx; i++) if (raw[i] * HeightScale <= 0.15f) { sea[i] = true; q.Enqueue(i); }
        while (q.Count > 0) { int id = q.Dequeue(), i = id % nx, j = id / nx; foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int a = i + di, b = j + dj; if (a < 0 || b < 0 || a >= nx || b >= nz) continue; int k = b * nx + a; if (!sea[k] && raw[k] * HeightScale <= 0.15f) { sea[k] = true; q.Enqueue(k); } } }
        float SeaW(float x, float z)
        {
            float fx = Mathf.Clamp(x / step, 0, nx - 1.0001f), fz = Mathf.Clamp(z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            float S(int a, int b) => sea[b * nx + a] ? 1f : 0f;
            return (S(i, j) * (1 - tx) + S(i + 1, j) * tx) * (1 - tz) + (S(i, j + 1) * (1 - tx) + S(i + 1, j + 1) * tx) * tz;
        }
        JObject densityHousesStats = null, scenicStats = null;
        if (densityHouses)
        {
            // 조각 5(§4.1): ① 도로 거리 150m · 60채/ha, ② 350m 까지 · 20채/ha. 필지(10m)마다 확률 p 로 후보(시드 고정), 정면 = 가장 가까운 차도 쪽, 모델 = 상점 빼고 결정적
            var dstat = DensityCore(null, null, null, W, H, raw, nx, nz, step, sea, res, nd, harborGeo, out _, 150f, new[] { 0.60f, 0.20f, 0f, 0f }, false, out var pArr, out var scArr, out int pxN, out var near, true,
                stairLines.Select(pl => { var a = pl[0]; var b = pl[pl.Count - 1]; return t.Surface(a.x, a.z) >= t.Surface(b.x, b.z) ? new Vector2((float)a.x, (float)a.z) : new Vector2((float)b.x, (float)b.z); }).ToList());
            var houseModels = Enumerable.Range(0, s.houseModelNames.Length).Where(k => !s.houseModelNames[k].StartsWith("CornerShop")).ToList();
            var cands = new List<SplineTown.HouseCand>();
            for (int k = 0; k < pArr.Length; k++)
            {
                if (scArr[k] != 0 && scArr[k] != 1 || pArr[k] <= 0) continue;
                uint h = (uint)(k * 2654435761u) ^ 0x9E3779B9u; h ^= h >> 15; h *= 0x85EBCA6Bu; h ^= h >> 13;
                double r = (h % 100000) / 100000.0;
                if (r >= pArr[k]) continue;
                int i = k % pxN, j = k / pxN; var c = new Vec2((i + 0.5) * 10, (j + 0.5) * 10);
                var rn = near[k]; double yaw = Math.Atan2(rn.x - c.x, rn.y - c.z) * 180 / Math.PI;   // 정면 = 길 쪽
                int model = houseModels[(int)(h >> 7) % houseModels.Count];
                {
                    // 길 단면에 걸치면 길에서 바깥으로 밀어 정면을 어깨 끝 + 틈에 맞춘다(큰길 옆 줄짓기와 같은 거리)
                    double dd = Math.Sqrt((c.x - rn.x) * (c.x - rn.x) + (c.z - rn.y) * (c.z - rn.y)), need = s.HalfShoulder + s.houseFrontGapMeters + s.houseModelDepths[model] / 2;
                    if (dd < need && dd > 1e-3) c = new Vec2(rn.x + (c.x - rn.x) / dd * need, rn.y + (c.z - rn.y) / dd * need);
                }
                yaw = Math.Round(yaw / 15) * 15;   // 15° 단위 — 이웃 집 줄이 맞게
                cands.Add(new SplineTown.HouseCand { c = c, yaw = yaw, model = model, scene = scArr[k] });
            }
            // 중심 먼저(광장 가까운 순) — 상한·겹침에서 중심이 이기게
            var pc0 = res.plaza != null ? res.plaza.c : harborGeo;
            cands = cands.OrderBy(cd => cd.scene).ThenBy(cd => SplineEdge.Dist(cd.c, pc0)).ToList();
            cands = RuleFilter(cands);
            var why = SplineTown.PlaceDensityHouses(res, s, cands, 350);
            if (scenic) scenicStats = ScenicHouses(res, s, raw, nx, nz, step, sea, scArr, pxN, harborGeo);
            int byScene0 = 0, byScene1 = 0;
            foreach (var hh in res.houses) { if (hh.shop) continue; int sc; int i = Mathf.Clamp((int)(hh.center.x / 10), 0, pxN - 1), j = Mathf.Clamp((int)(hh.center.z / 10), 0, scArr.Length / pxN - 1); sc = scArr[j * pxN + i]; if (sc == 0) byScene0++; else if (sc == 1) byScene1++; }
            var whyJ = new JObject(); foreach (var kv in why) whyJ[kv.Key] = kv.Value;
            densityHousesStats = new JObject { ["density"] = dstat, ["candidates"] = cands.Count, ["candidatesByScene"] = new JArray(cands.Count(c => c.scene == 0), cands.Count(c => c.scene == 1)), ["placedByScene"] = new JObject { ["1_centre"] = byScene0, ["2_edge"] = byScene1, ["shops"] = res.houses.Count(x => x.shop) }, ["outcome"] = whyJ,
                ["seat"] = new JObject { ["ground"] = res.houses.Count(x => x.seat == 0), ["wall_down"] = res.houses.Count(x => x.seat == 1), ["half_basement"] = res.houses.Count(x => x.seat == 2) }, ["driveways"] = res.houses.Count(x => x.hasDrive), ["stairs"] = res.houses.Count(x => x.hasDrive && x.driveSteps) };
        }
        JObject villageStats = null; Vec2[] vPlazaRect = null; double vPlazaY = 0, vPlazaYaw = 0; Vec2 vPlazaC = default; var vWalls = new List<(Vector3 c, float yaw, float len, float y0, float y1)>();
        if (village) villageStats = Village(res, s, concaGeo, villageRadius, out vPlazaRect, out vPlazaC, out vPlazaYaw, out vPlazaY, vWalls, villageEntry);
        if (villageEntry && villageStats != null) VillageEntryStats(res, villageStats, concaGeo, villageRadius, villageRoadRadius, villageStairs, stairLines.Count, entryPick, (x, z) => hf.HeightAt(x, z) * HeightScale);
        for (int j = 0; j < t.nz; j++) for (int i = 0; i < t.nx; i++) { float w = SeaW((float)(i * t.step), (float)(j * t.step)); if (w > 0) t.h[j * t.nx + i] -= 3 * w; }
        if (osm)
        {
            // 작은 광장까지 정해진 뒤 다시 분류(작은 광장 겹침 포함) → 앉히기(바다 낮춤 뒤 — 해안 건물 밑단이 낮춘 땅까지)
            JObject legacy = null;
            if (townCore)
            {
                // 비교: 조각 10 규칙(보도·골목 겹침도 뺌, tunnel 무시)을 같은 길에 적용했을 때 반경 안에서 빠지는 수
                TownGenOsmBuildings.Classify(osmB, res, s, true, vPlazaRect);
                var inR0 = osmB.Where(b => SplineEdge.Dist(b.c, harborGeo) <= townCoreRadius).ToList();
                legacy = new JObject { ["inRadius"] = inR0.Count, ["dropped"] = inR0.Count(b => b.drop != null), ["droppedByReason"] = new JObject(inR0.Where(b => b.drop != null).GroupBy(b => b.drop).Select(g => new JProperty(g.Key, g.Count()))) };
            }
            var cls = TownGenOsmBuildings.Classify(osmB, res, s, true, vPlazaRect, segUnder, lenient);
            var seat = TownGenOsmBuildings.Seat(osmB, res, s);
            if (townCore)
            {
                var placedB = osmB.Where(b => b.drop == null).ToList();
                if (carLanes)
                {
                    // 조각 12: 차로로 못 그린 living_street·service(어느 차도에도 안 이어짐 등)는 v11 처럼 보행 띠로 남긴다 — 광장 밖 점의 70% 이상이 차도(중심선 2.5m) 위면 차로로 그려진 것
                    double carM = 0, walkM = 0; var walkWays = new JArray();
                    foreach (var w in lanePaths)
                    {
                        var outPts = w.pts.Where(pt => !plazaRings.Any(r => TownGenOsmBuildings.Inside(pt, r))).ToList(); if (outPts.Count == 0) continue;
                        double cov = outPts.Count(pt => res.edges.Any(e => SplineTown.DistToPoly(pt, e.pts) <= 2.5)) / (double)outPts.Count;
                        if (cov >= 0.7) carM += SplineEdge.PolyLength(w.pts); else { walkM += SplineEdge.PolyLength(w.pts); tp.paths.Add(w); walkWays.Add($"{w.highway}:{w.osm}:{w.name} {SplineEdge.PolyLength(w.pts):0}m cover {cov:0.00}"); }
                    }
                    laneFallback = new JObject { ["rule"] = "OSM living_street/service line: ≥70% of points outside plazas within 2.5m of a car road centreline = drawn as car lane, else drawn as 4m walking ribbon (v11 rule)", ["asCarLane_m"] = Math.Round(carM, 1), ["asWalkRibbon_m"] = Math.Round(walkM, 1), ["walkWays"] = walkWays };
                }
                if (gapWidth)
                {
                    // 조각 13: 보행길 가운데 선 2m 점 → 틈 가운데로 옮기고 점마다 반폭
                    var pIdx = new TownGenTownPaths.BIndex(placedB, 10);
                    foreach (var w in tp.paths) { var (pp, hh) = TownGenGapWidth.Fit(TownGenGapWidth.Stations(w.pts), w.highway, w.width, pIdx, gw, $"{w.highway}:{w.osm}:{w.name}", true); w.pts = pp; w.fitHalf = hh; }
                }
                tpr = TownGenTownPaths.Build(tp.paths, tp.areas, placedB, t, SeaLevel, gapWidth);
                // 조각 14: 그린 띠·칸(v13) 대신 한 장 면 — 교차점 지형 평탄화도 여기서(지형 메시 전)
                if (sheet) rsh = TownGenRoadSheet.Build(res, s, tp.paths, tp.areas, tpr.surf, placedB, harborGeo, townCoreRadius, SeaLevel, Path.Combine(TownGenRunner.ToolsDir, "out", "scene_structure_" + prefix));
                var inR = osmB.Where(b => SplineEdge.Dist(b.c, harborGeo) <= townCoreRadius).ToList();
                double CarLen(Func<SplineEdge, bool> f) => res.edges.Where(f).Sum(e => { double L = 0; for (int k = 0; k + 1 < e.pts.Count; k++) if (SplineEdge.Dist(new Vec2((e.pts[k].x + e.pts[k + 1].x) / 2, (e.pts[k].z + e.pts[k + 1].z) / 2), harborGeo) <= townCoreRadius) L += SplineEdge.Dist(e.pts[k], e.pts[k + 1]); return L; });
                tpJ = new JObject
                {
                    ["harbor"] = new JArray(harborGeo.x, harborGeo.z), ["radius_m"] = townCoreRadius, ["source"] = "tools/town-gen/out/structure_a/town_paths.json (town_paths_prep.py, © OpenStreetMap contributors, ODbL)",
                    ["osmLengthInRadius_m"] = JObject.Parse(File.ReadAllText(Path.Combine(dir, "town_paths.json")))["stats"],
                    ["carRoads"] = new JObject { ["rule"] = "SplineTown car roads (generated section: carriageway 6m + sidewalks) clipped to radius by segment midpoint", ["main_m"] = Math.Round(CarLen(e => !e.alley), 1), ["alley_m"] = Math.Round(CarLen(e => e.alley), 1) },
                    ["paths"] = tpr.stats,
                    ["buildings"] = new JObject { ["inRadius"] = inR.Count, ["placed"] = inR.Count(b => b.drop == null), ["dropped"] = inR.Count(b => b.drop != null), ["droppedByReason"] = new JObject(inR.Where(b => b.drop != null).GroupBy(b => b.drop).Select(g => new JProperty(g.Key, g.Count()))), ["overTunnelRoadKept"] = inR.Count(b => b.overUnderRoad && b.drop == null), ["keptOverSidewalkOrAlley"] = inR.Count(b => b.drop == null && (b.road == "sidewalk" || b.road == "alley")), ["v10RuleSameRoads"] = legacy },
                    ["layout"] = TownGenTownPaths.Measure(tpr, tp.areas, placedB, res, s, harborGeo, townCoreRadius),
                };
                if (carLanes)
                {
                    // 조각 12: 광장 정차 자리·볼라드 + 차 도달 BFS + 중심 차도 곡선 수·길이(v10 길 규칙을 같은 땅에 다시 돌려 비교)
                    bool SeaAt(Vec2 p) { int c = (int)Math.Floor(p.x / s.seaCellMeters), rr = (int)Math.Floor(p.z / s.seaCellMeters); return res.sea.InBounds(c, rr) && res.sea.sea[res.sea.Index(c, rr)]; }
                    clr = TownGenCarLanes.Build(res, s, tp.areas, placedB, harborGeo, townCoreRadius, SeaAt);
                    var res10 = SplineTown.Build(MakeWays(false, false), hf, s10);
                    // 변형: Piazza Duomo 를 가로지르는 living_street(359193524 광장 안·551839542 입구) 만 자르지 않으면 — 골짜기 길(Via Lorenzo d'Amalfi)이 이어지나
                    var resDuomo = SplineTown.Build(MakeWays(true, true, new HashSet<long> { 359193524, 551839542 }), hf, s);
                    // 조각 13: 건물 윤곽 ∩ 길 면 겹침(v12 다시 빌드해도 같은 검사 — 비교 기준)
                    ovJ = TownGenGapWidth.Overlaps(placedB, res, s, rsh != null ? rsh.surf.Concat(tpr.surf.Where(x => x.kind == "steps")).ToList() : tpr.surf, harborGeo, townCoreRadius, segUnder, out int ovN, out _);
                    res.report.Check("O1", ovN == 0, $"중심 {townCoreRadius:0}m 건물 윤곽 ∩ 길 면 {ovN}건 (차로 {ovJ["byKind"]["lane"]} · 보행 {ovJ["byKind"]["path"]} · 계단 {ovJ["byKind"]["steps"]} · 광장 {ovJ["byKind"]["plaza"]} · 큰길 차로 {ovJ["byKind"]["main_carriageway"]} · 한 장 면 {ovJ["byKind"]["sheet"]})");
                    if (rsh != null) tpJ["sheet"] = rsh.stats;
                    if (ovN > 0) Debug.LogWarning($"[TownGen] {prefix} 건물-길 겹침 {ovN}건 — measure.json overlaps 참조");
                    if (gapWidth) tpJ["gapWidth"] = gw.ToJson();
                    tpJ["carLanes"] = new JObject
                    {
                        ["rule"] = "living_street·service within 400m → SplineTown narrow lane (alley section, half width 2.0m = OSM width 4m, never promoted to 6m main); car road lines inside plaza areas cut 1m before the outline",
                        ["plazaClipped_m"] = Math.Round(plazaClipped, 1), ["plazaClippedWays"] = plazaClipWays,
                        ["coreCars_v12"] = TownGenCarLanes.CoreCarStats(res, harborGeo, townCoreRadius, TownGenCarLanes.Reachable(res)),
                        ["coreCars_v10rule"] = TownGenCarLanes.CoreCarStats(res10, harborGeo, townCoreRadius, TownGenCarLanes.Reachable(res10)),
                        ["alleysMetric_v12"] = res.report.MetricValue("alleys"), ["alleysMetric_v10rule"] = res10.report.MetricValue("alleys"),
                        ["reach"] = clr.stats, ["laneFallback"] = laneFallback,
                        ["variant_throughPiazzaDuomo"] = new JObject { ["rule"] = "same as v12 but the living_street across Piazza Duomo (OSM 359193524, 551839542) is not cut", ["coreCars"] = TownGenCarLanes.CoreCarStats(resDuomo, harborGeo, townCoreRadius, TownGenCarLanes.Reachable(resDuomo)), ["alleysMetric"] = resDuomo.report.MetricValue("alleys"), ["rejected"] = resDuomo.report.MetricValue("alley_rejected_at") },
                    };
                }
            }
            var st = JObject.Parse(File.ReadAllText(Path.Combine(dir, "buildings_stats.json")));
            var kept = osmB.Where(b => b.drop == null).ToList();
            var placedZ = new JObject(); foreach (var z in new[] { "centre", "edge", "coast", "conca" }) placedZ[z] = kept.Count(b => b.zone == z);
            osmJ = new JObject
            {
                ["source"] = "tools/town-gen/out/structure_a/buildings.json (© OpenStreetMap contributors, ODbL)", ["stats"] = st, ["loaded"] = osmB.Count, ["placed"] = kept.Count, ["placedByZone"] = placedZ,
                ["roadOverlap"] = cls, ["seat"] = seat, ["ruleFill"] = osmFill, ["ruleFillGap_m"] = osmFill ? 15 : (int?)null, ["ruleHouses"] = res.houses.Count,
                ["view"] = new JObject { ["centre_densest"] = DensestPoint(kept, "centre"), ["coast_densest"] = DensestPoint(kept, "coast"), ["conca_densest"] = DensestPoint(kept, "conca") },
            };
        }

        JObject carGraphJ = null;
        if (upper)
        {
            // 조각 15a: 씬 차도 그래프 + 경로 다양성. 윗길 마을 점(가설) = 윗길 선 위 8m 점 중 반경 100m 안 놓인 OSM 건물이 가장 많은 곳
            var upperLines = nd["segments"].Where(sg => upperWays.Contains(sg["osm_way"].Value<long>())).Select(sg => sg["points_m"].Select(q2 => new Vec2(q2[0].Value<double>(), q2[1].Value<double>())).ToList()).ToList();
            var keptB = osmB != null ? osmB.Where(b => b.drop == null).ToList() : new List<TownGenOsmBuildings.B>();
            Vec2 upV = upperLines.Count > 0 ? upperLines[0][0] : harborGeo; int upBest = -1;
            foreach (var pl in upperLines) for (int k = 0; k < pl.Count; k += 4) { if (SplineEdge.Dist(pl[k], concaGeo) <= 200) continue; int n = keptB.Count(b => SplineEdge.Dist(b.c, pl[k]) <= 100); if (n > upBest) { upBest = n; upV = pl[k]; } }   // 콘카 마을(200m) 은 빼고
            carGraphJ = TownGenCarGraph.Build(res, s, upperLines, entryJoin, harborGeo, vPlazaRect != null ? vPlazaC : concaGeo, upV, townCoreRadius);
            carGraphJ["entryJoin7b"] = entryJoinJ;
            if (concaRoutesJ != null) carGraphJ["concaRouteWays"] = concaRoutesJ;
            if (res.maxDevOrig != null) carGraphJ["osmDeviationMaxEdge"] = new JObject { ["orig"] = new JArray(res.maxDevOrig.Select(v => new JArray(Math.Round(v.x, 1), Math.Round(v.z, 1)))), ["out"] = new JArray(res.maxDevOut.Select(v => new JArray(Math.Round(v.x, 1), Math.Round(v.z, 1)))) };
            carGraphJ["upperVillage"] = new JObject { ["rule"] = "hypothesis: point on the upper-branch OSM lines (every 4th point, more than 200m from the Conca seed) with the most placed OSM buildings within 100m", ["at"] = new JArray(Math.Round(upV.x, 1), Math.Round(upV.z, 1)), ["buildingsWithin100m"] = upBest, ["upperBranchWays"] = upperWays.Count, ["upperBranchOsm_m"] = Math.Round(upperLines.Sum(SplineEdge.PolyLength), 0) };
        }

        // 2m 띠: SS163 중심선에서 60m 안 8m 칸
        var mains = res.edges.Where(e => !e.alley).ToList();
        float hc = 64f; var hash = new Dictionary<(int, int), List<(Vector2 a, Vector2 b)>>();
        foreach (var e in res.edges)
            for (int k = 0; k + 1 < e.pts.Count; k++)
            {
                var a = new Vector2((float)e.pts[k].x, (float)e.pts[k].z); var b = new Vector2((float)e.pts[k + 1].x, (float)e.pts[k + 1].z);
                for (int cx = Mathf.FloorToInt(Mathf.Min(a.x, b.x) / hc) - 1; cx <= Mathf.FloorToInt(Mathf.Max(a.x, b.x) / hc) + 1; cx++)
                    for (int cz = Mathf.FloorToInt(Mathf.Min(a.y, b.y) / hc) - 1; cz <= Mathf.FloorToInt(Mathf.Max(a.y, b.y) / hc) + 1; cz++)
                    { if (!hash.TryGetValue((cx, cz), out var l)) hash[(cx, cz)] = l = new List<(Vector2, Vector2)>(); l.Add((a, b)); }
            }
        float DistRoad(Vector2 p)
        {
            float best = float.MaxValue;
            if (!hash.TryGetValue((Mathf.FloorToInt(p.x / hc), Mathf.FloorToInt(p.y / hc)), out var l)) return best;
            foreach (var (a, b) in l) { var ab = b - a; float tt = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0; best = Mathf.Min(best, Vector2.Distance(p, a + ab * tt)); }
            return best;
        }
        int cnx = nx - 1, cnz = nz - 1; var fine = new bool[cnx * cnz]; int fineCells = 0;
        for (int j = 0; j < cnz; j++) for (int i = 0; i < cnx; i++) if (DistRoad(new Vector2((i + 0.5f) * step, (j + 0.5f) * step)) <= BandMeters + step * 0.71f) { fine[j * cnx + i] = true; fineCells++; }
        if (osm)   // 조각 10: 건물 밑 깎기·돋우기가 2m 로 보이게 건물 칸도 2m
            foreach (var b in osmB.Where(b => b.drop == null))
                for (int j = Math.Max(0, (int)(b.minZ / step) - 1); j <= Math.Min(cnz - 1, (int)(b.maxZ / step) + 1); j++)
                    for (int i = Math.Max(0, (int)(b.minX / step) - 1); i <= Math.Min(cnx - 1, (int)(b.maxX / step) + 1); i++)
                        if (!fine[j * cnx + i]) { fine[j * cnx + i] = true; fineCells++; }
        if (tpr != null)   // 조각 11: 보행길·광장 칸도 2m(띠가 8m 면 위에 뜨거나 묻히지 않게)
            foreach (var p in rsh != null ? tpr.finePts.Concat(rsh.finePts) : tpr.finePts)
                for (int dj = -1; dj <= 1; dj++) for (int di = -1; di <= 1; di++)
                {
                    int i = (int)(p.x / step) + di, j = (int)(p.z / step) + dj;
                    if (i >= 0 && j >= 0 && i < cnx && j < cnz && !fine[j * cnx + i]) { fine[j * cnx + i] = true; fineCells++; }
                }

        int sub = Mathf.RoundToInt(step / FineStep);
        var vid = new Dictionary<long, int>(); var verts = new List<Vector3>(); var uvs = new List<Vector2>();
        int V(int gx, int gz)
        {
            long key = (long)gx * 100000 + gz;
            if (vid.TryGetValue(key, out int id)) return id;
            int ti = Mathf.Min(gx, t.nx - 1), tj = Mathf.Min(gz, t.nz - 1);
            float x = gx * FineStep, z = gz * FineStep; id = verts.Count; verts.Add(new Vector3(x, (float)t.h[tj * t.nx + ti], z)); uvs.Add(new Vector2(x / 8, z / 8)); vid[key] = id; return id;
        }
        int V2(int i, int j) { float x = (i + 0.5f) * step, z = (j + 0.5f) * step; int id = verts.Count; verts.Add(new Vector3(x, (float)t.Surface(x, z), z)); uvs.Add(new Vector2(x / 8, z / 8)); return id; }
        var land = new List<int>(); var sand = new List<int>();
        void Tri(int a, int b, int c) { var l = Mathf.Max(verts[a].y, verts[b].y, verts[c].y) < SeaLevel + 1.5f ? sand : land; l.Add(a); l.Add(b); l.Add(c); }
        void AddEdge(List<int> ring, int i, int j, int side)
        {
            int ni = i + (side == 1 ? 1 : side == 3 ? -1 : 0), nj = j + (side == 0 ? -1 : side == 2 ? 1 : 0);
            if (ni < 0 || nj < 0 || ni >= cnx || nj >= cnz || !fine[nj * cnx + ni]) return;
            for (int k = 1; k < sub; k++)
            {
                int gx, gz;
                if (side == 0) { gx = i * sub + k; gz = j * sub; }
                else if (side == 1) { gx = (i + 1) * sub; gz = j * sub + k; }
                else if (side == 2) { gx = (i + 1) * sub - k; gz = (j + 1) * sub; }
                else { gx = i * sub; gz = (j + 1) * sub - k; }
                ring.Add(V(gx, gz));
            }
        }
        for (int j = 0; j < cnz; j++)
            for (int i = 0; i < cnx; i++)
            {
                if (fine[j * cnx + i])
                {
                    for (int b = 0; b < sub; b++)
                        for (int a = 0; a < sub; a++)
                        { int gx = i * sub + a, gz = j * sub + b; int p00 = V(gx, gz), p10 = V(gx + 1, gz), p01 = V(gx, gz + 1), p11 = V(gx + 1, gz + 1); Tri(p00, p01, p11); Tri(p00, p11, p10); }
                    continue;
                }
                int c00 = V(i * sub, j * sub), c10 = V((i + 1) * sub, j * sub), c01 = V(i * sub, (j + 1) * sub), c11 = V((i + 1) * sub, (j + 1) * sub);
                var ring = new List<int> { c00 }; AddEdge(ring, i, j, 0); ring.Add(c10); AddEdge(ring, i, j, 1); ring.Add(c11); AddEdge(ring, i, j, 2); ring.Add(c01); AddEdge(ring, i, j, 3);
                if (ring.Count == 4) { Tri(c00, c01, c11); Tri(c00, c11, c10); }
                else { int ctr = V2(i, j); for (int k = 0; k < ring.Count; k++) Tri(ctr, ring[(k + 1) % ring.Count], ring[k]); }
            }

        // ---- 씬 (메시는 새 씬을 연 뒤에)
        string prevScene = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("열린 씬에 저장 안 한 변경이 있음 — 중단");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var root = new GameObject("TownGen_Structure_A_SS163");
        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null) { light.transform.rotation = Quaternion.Euler(50f, -35f, 0f); light.intensity = 1.2f; light.shadows = LightShadows.Soft; light.color = new Color(1f, .96f, .88f); }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .64f, .66f);
        var mGround = Mat("Ground"); var mSand = Mat("ZoneGroundHarbor"); var mSea = Mat("Sea"); var mAsphalt = Mat("Asphalt"); var mPave = Mat("Pavement"); var mPaint = Mat("RoadPaint");
        var mesh = new Mesh { name = "StructureTerrain", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(land, 0); mesh.SetTriangles(sand, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var tGo = new GameObject("Terrain"); tGo.transform.SetParent(root.transform, false);
        tGo.AddComponent<MeshFilter>().sharedMesh = mesh; tGo.AddComponent<MeshRenderer>().sharedMaterials = new[] { mGround, mSand ? mSand : mGround };
        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(root.transform, false);
        seaGo.transform.position = new Vector3(W / 2, SeaLevel, H / 2); seaGo.transform.localScale = new Vector3(W / 10 * 3, 1, H / 10 * 3);
        UnityEngine.Object.DestroyImmediate(seaGo.GetComponent<Collider>()); if (mSea) seaGo.GetComponent<Renderer>().sharedMaterial = mSea;
        // 길: spline 빌더 단면·교차점 판·옹벽 그대로
        var asphalt = new TownGenSpline.MeshBuilder(); var pave = new TownGenSpline.MeshBuilder(); var shoulder = new TownGenSpline.MeshBuilder(); var paint = new TownGenSpline.MeshBuilder(); var wall = new TownGenSpline.MeshBuilder();
        var mainPolys = mains.Select(TownGenSpline.ClippedPolyline).ToList(); var noMouths = new List<List<Vec2>>();
        foreach (var e in mains) TownGenSpline.Section(e, res, s, asphalt, pave, shoulder, paint, noMouths, mainPolys, mains.IndexOf(e));
        foreach (var jl in res.junctionLayouts) TownGenSpline.JunctionPatch(jl, res, s, asphalt, pave, shoulder, mainPolys);
        foreach (var w in res.walls) TownGenSpline.WallQuad(w, wall);
        var alleyMb = new TownGenSpline.MeshBuilder(); var drive = new TownGenSpline.MeshBuilder();
        foreach (var e in res.edges.Where(e => e.alley)) TownGenSpline.AlleySection(e, res, s, alleyMb, shoulder, rsh == null ? (Func<int, bool>)null : i => TownGenRoadSheet.CoreLaneSeg(e, i, harborGeo, townCoreRadius));
        foreach (var dh in res.houses) if (dh.hasDrive) TownGenSpline.Driveway(dh, res, s, drive);
        var roadsGo = new GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
        int tris = (land.Count + sand.Count) / 3;
        tris += TownGenSpline.Obj("Asphalt", roadsGo.transform, asphalt.Build("Asphalt"), mAsphalt).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        JObject walkClipJ = null;
        if (walkClip && osmB != null) pave = TownGenWalkClip.Clip(pave, osmB, out walkClipJ);   // 15a-4 B: 건물 두고 보도 면 − 건물 윤곽
        tris += TownGenSpline.Obj("Sidewalk_Curb", roadsGo.transform, pave.Build("Sidewalk"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += TownGenSpline.Obj("Shoulder", roadsGo.transform, shoulder.Build("Shoulder"), mGround).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += TownGenSpline.Obj("CenterLine", roadsGo.transform, paint.Build("CenterLine"), mPaint).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += TownGenSpline.Obj("RetainingWalls", root.transform, wall.Build("Walls"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += TownGenSpline.Obj("Alleys", roadsGo.transform, alleyMb.Build("Alleys"), carLanes ? mAsphalt : mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += TownGenSpline.Obj("Driveways", roadsGo.transform, drive.Build("Driveways"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        int stairSteps = 0;
        if (stairLines.Count > 0)
        {
            // 공용 계단: OSM 선 따라 폭 1.5m, 디딤 0.35m — 디딤 윗면 = 그 자리 지형 높이를 0.17m 단위로(지형 위로 0.05m)
            var smb = new TownGenSpline.MeshBuilder();
            foreach (var pl in stairLines)
            {
                double L = SplineEdge.PolyLength(pl); int n = Math.Max(2, (int)(L / 0.35)); double acc = 0; int seg = 0;
                for (int k = 0; k < n; k++)
                {
                    double a0 = L * k / n, a1 = L * (k + 1) / n, am = (a0 + a1) / 2;
                    while (seg + 1 < pl.Count - 1 && acc + SplineEdge.Dist(pl[seg], pl[seg + 1]) < am) { acc += SplineEdge.Dist(pl[seg], pl[seg + 1]); seg++; }
                    double sl = SplineEdge.Dist(pl[seg], pl[seg + 1]); double tt = sl > 1e-6 ? (am - acc) / sl : 0;
                    var m = new Vec2(pl[seg].x + (pl[seg + 1].x - pl[seg].x) * tt, pl[seg].z + (pl[seg + 1].z - pl[seg].z) * tt);
                    float yaw = Mathf.Atan2((float)(pl[seg + 1].x - pl[seg].x), (float)(pl[seg + 1].z - pl[seg].z)) * Mathf.Rad2Deg;
                    float g = (float)t.Surface(m.x, m.z); float top = Mathf.Ceil((g + 0.05f) / 0.17f) * 0.17f;
                    TownGenSpline.Box(smb, new Vector3((float)m.x, 0, (float)m.z), yaw, 1.5f, (float)(L / n) + 0.02f, g - 0.4f, top);
                    stairSteps++;
                }
            }
            tris += TownGenSpline.Obj("PublicStairs", roadsGo.transform, smb.Build("PublicStairs"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }
        if (center)
        {
            tris += TownGenSpline.PlazaMeshes(res, root.transform, mPave, mPave);
            tris += TownGenSpline.HouseObjects(res, scene, root.transform, models, mPave);
        }
        if (tpr != null)
        {
            if (rsh == null)
            {
                tris += TownGenSpline.Obj("TownPaths", roadsGo.transform, tpr.ribbon.Build("TownPaths"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
                tris += TownGenSpline.Obj("TownPlazas", roadsGo.transform, tpr.plaza.Build("TownPlazas"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            }
            else
            {
                tris += TownGenSpline.Obj("TownSheet_Lane", roadsGo.transform, rsh.lane.Build("TownSheet_Lane"), mAsphalt).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
                tris += TownGenSpline.Obj("TownSheet_Walk", roadsGo.transform, rsh.walk.Build("TownSheet_Walk"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            }
            tris += TownGenSpline.Obj("TownSteps", roadsGo.transform, tpr.steps.Build("TownSteps"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }
        if (clr != null)
        {
            tris += TownGenSpline.Obj("PlazaPockets", roadsGo.transform, clr.pockets.Build("PlazaPockets"), mAsphalt).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            tris += TownGenSpline.Obj("PlazaBollards", roadsGo.transform, clr.bollards.Build("PlazaBollards"), mPaint).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }
        if (osm) tris += TownGenOsmBuildings.Build(osmB, root.transform, new[] { Mat("BuildingsWarm"), Mat("BuildingsSand"), Mat("BuildingsCool") });
        JObject o2J = null;
        if (sceneOverlap)
        {
            // 15a-3 ②: O2 = 씬 메시 기준 건물 ∩ 길 면(상자 전체, 차도·보도 FAIL, 흙 띠 숫자만)
            o2J = TownGenSceneOverlap.Measure(root.transform, out int o2Fail);
            if (walkClipJ != null) o2J["walkClip"] = walkClipJ;
            o2J["carTunnelException"] = new JObject { ["on"] = carTunnel, ["carWaysTaggedUnder"] = carTunnelWays };
            res.report.Check("O2", o2Fail == 0, $"씬 메시 건물 ∩ 길 면(상자 전체): 차도 {o2J["car"]["buildings"]}채 {o2J["car"]["area_m2"]}㎡ · 보도 {o2J["walk"]["buildings"]}채 {o2J["walk"]["area_m2"]}㎡ · 흙 띠 {o2J["shoulder"]["buildings"]}채 {o2J["shoulder"]["area_m2"]}㎡(숫자만) / 건물 {o2J["buildings"]}채");
            if (o2Fail > 0) Debug.LogWarning($"[TownGen] {prefix} O2 건물-길 겹침 {o2Fail}채 — o2_scene_overlap.json 참조");
        }
        if (village && vPlazaRect != null)
        {
            // 작은 광장 12×12 포장 + 뒤 6m 띠에 예배당 상자 8×6(높이 6m) + 종탑 3×3(10m) — 새 에셋 0, 돌 포장 재질 placeholder
            var vmb = new TownGenSpline.MeshBuilder(); var cmb = new TownGenSpline.MeshBuilder();
            float yaw = (float)vPlazaYaw; double rr = vPlazaYaw * Math.PI / 180; var f = new Vector3((float)Math.Sin(rr), 0, (float)Math.Cos(rr)); var rgt = new Vector3(f.z, 0, -f.x);
            var c0 = new Vector3((float)vPlazaC.x, 0, (float)vPlazaC.z); float Y = (float)vPlazaY;
            TownGenSpline.Box(vmb, c0 + f * 3f, yaw, 12f, 12f, Y - 0.3f, Y + 0.05f);
            foreach (var w in vWalls) TownGenSpline.Box(vmb, w.c, w.yaw, w.len, 0.45f, w.y0, w.y1);
            var chC = c0 - f * 6f; TownGenSpline.Box(cmb, chC, yaw, 8f, 6f, Y - 0.3f, Y + 6f);
            TownGenSpline.Box(cmb, chC + rgt * 4.5f - f * 1.5f, yaw, 3f, 3f, Y - 0.3f, Y + 10f);
            tris += TownGenSpline.Obj("VillagePlaza", root.transform, vmb.Build("VillagePlaza"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            tris += TownGenSpline.Obj("VillageChapel_placeholder", root.transform, cmb.Build("VillageChapel"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }
        JObject densityStats = null;
        if (density) densityStats = DensityOverlay(root.transform, verts, land.Concat(sand).ToList(), W, H, raw, nx, nz, step, sea, res, nd, harborGeo, out _);
        new GameObject("_Credit: © OpenStreetMap contributors (ODbL) · produced using Copernicus WorldDEM-30").transform.SetParent(root.transform, false);

        // ---- 운전 높이 캡처 3곳: 항구(아말피 쪽) · 곶(가장 남쪽 길 점) · 콘카 입구(서쪽 끝)
        var shots = Path.Combine(TownGenRunner.ToolsDir, "out", "shots"); Directory.CreateDirectory(shots);
        var cam = Camera.main; cam.farClipPlane = 6000; cam.fieldOfView = 60;
        var shotInfo = new JArray();
        if (townCore)
        {
            // 조각 11: QA 용 톱다운 1장 — 항구점 중심 840m 사각(반경 400m + 여유), 3072px = 0.27m/px
            cam.orthographic = true; cam.orthographicSize = 420;
            Shot(cam, new Vector3((float)harborGeo.x, verts.Max(v => v.y) + 100, (float)harborGeo.z), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_core_top.png"), 3072, 3072);
            cam.orthographic = false;
            shotInfo.Add(new JObject { ["name"] = "core_top", ["px"] = new JArray(3072, 3072), ["metersPerPixel"] = 840.0 / 3072, ["centre"] = new JArray(harborGeo.x, harborGeo.z), ["up"] = "north" });
            if (upper)
            {
                // 조각 15a: 땅 전체 톱다운(윗길 고리 보기)
                cam.orthographic = true; cam.orthographicSize = H / 2 * 1.02f;
                int pw = 4096, ph = Mathf.RoundToInt(4096 * H / W);
                Shot(cam, new Vector3(W / 2, verts.Max(v => v.y) + 100, H / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top.png"), pw, ph);
                cam.orthographic = false;
                shotInfo.Add(new JObject { ["name"] = "top", ["px"] = new JArray(pw, ph), ["metersPerPixel"] = H * 1.02f / ph, ["up"] = "north" });
            }
        }
        else if (osm)
        {
            // 조각 10: QA 용 톱다운 1장(땅 전체, 4096 폭)
            cam.orthographic = true; cam.orthographicSize = H / 2 * 1.02f;
            int pw = 4096, ph = Mathf.RoundToInt(4096 * H / W);
            Shot(cam, new Vector3(W / 2, verts.Max(v => v.y) + 100, H / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top.png"), pw, ph);
            cam.orthographic = false;
            shotInfo.Add(new JObject { ["name"] = "top", ["px"] = new JArray(pw, ph), ["metersPerPixel"] = H * 1.02f / ph });
        }
        else if (village)
        {
            // 마을 입구 운전 높이: 씨앗에서 약 100m 인 길 점에서 광장(없으면 씨앗)을 본다
            var vp = new List<(Vec2 p, double y)>();
            foreach (var e in res.edges) for (int k = 0; k < e.pts.Count; k++) { double d = SplineEdge.Dist(e.pts[k], concaGeo); if (d <= 140) vp.Add((e.pts[k], e.y[k])); }
            if (vp.Count > 0)
            {
                var a = vp.OrderBy(x => Math.Abs(SplineEdge.Dist(x.p, concaGeo) - 100)).First();
                var tgt = vPlazaRect != null ? vPlazaC : concaGeo; float ty = vPlazaRect != null ? (float)vPlazaY + 3f : (float)a.y + 3f;
                var eyeV = new Vector3((float)a.p.x, (float)a.y + 1.6f, (float)a.p.z); var aimV = new Vector3((float)tgt.x, ty, (float)tgt.z);
                Shot(cam, eyeV, Quaternion.LookRotation((aimV - eyeV).normalized), Path.Combine(shots, prefix + "_village_entry.png"));
                shotInfo.Add(new JObject { ["name"] = "village_entry", ["eye"] = new JArray(eyeV.x, eyeV.y, eyeV.z), ["aim"] = new JArray(aimV.x, aimV.y, aimV.z) });
            }
            cam.orthographic = true; cam.orthographicSize = 110;
            Shot(cam, new Vector3((float)concaGeo.x, 400, (float)concaGeo.z), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_village_top.png"));
            cam.orthographic = false;
            if (villageEntry)
            {
                // 비스듬히: 광장 남동쪽 위(x+110, y+120, z−130)에서 북서로 내려다본다 — 입구 길·골목·계단이 비탈에 어떻게 앉았나
                var tgtO = vPlazaRect != null ? new Vector3((float)vPlazaC.x, (float)vPlazaY, (float)vPlazaC.z) : new Vector3((float)concaGeo.x, (float)t.Surface(concaGeo.x, concaGeo.z), (float)concaGeo.z);
                var eyeO = tgtO + new Vector3(110f, 120f, -130f);
                Shot(cam, eyeO, Quaternion.LookRotation((tgtO - eyeO).normalized), Path.Combine(shots, prefix + "_village_oblique.png"));
                shotInfo.Add(new JObject { ["name"] = "village_oblique", ["eye"] = new JArray(eyeO.x, eyeO.y, eyeO.z), ["aim"] = new JArray(tgtO.x, tgtO.y, tgtO.z) });
            }
        }
        if (sceneOverlap)
        {
            // 15a-3: 도현 캡처 1번 자리(Via Matteo Camera 터널 위 건물 덩어리, 2718,2291) — 위·비스듬히 2장, 진단(.ai_logs/o1-diag) 과 같은 시점
            var c9 = new Vector3(2718f, 20f, 2291f); cam.fieldOfView = 40;
            var eT = c9 + new Vector3(0, 120, -1); Shot(cam, eT, Quaternion.LookRotation((c9 - eT).normalized), Path.Combine(shots, prefix + "_o2_top_2718_2291.png"));
            cam.fieldOfView = 45; var eO = c9 + new Vector3(-20, 70, -90); Shot(cam, eO, Quaternion.LookRotation((c9 - eO).normalized), Path.Combine(shots, prefix + "_o2_obl_2718_2291.png"));
            if (walkClipJ != null)
            {
                // 15a-4: 보도를 잘라낸 건물 3채 — 보도 높이에서 비스듬히(벽에서 보도가 끝나는지)
                foreach (var (px, py, pz) in new[] { (2867f, 6f, 2333f), (3010f, 6f, 2335f), (2761f, 9f, 2313f) })
                {
                    var cw = new Vector3(px, py, pz); var eW = cw + new Vector3(-28, 22, -34); cam.fieldOfView = 50;
                    string nm = $"walkclip_{(int)px}_{(int)pz}";
                    Shot(cam, eW, Quaternion.LookRotation((cw - eW).normalized), Path.Combine(shots, prefix + "_" + nm + ".png"));
                    shotInfo.Add(new JObject { ["name"] = nm, ["eye"] = new JArray(eW.x, eW.y, eW.z), ["aim"] = new JArray(cw.x, cw.y, cw.z) });
                }
            }
            cam.fieldOfView = 60;
            shotInfo.Add(new JObject { ["name"] = "o2_top_2718_2291", ["eye"] = new JArray(eT.x, eT.y, eT.z), ["aim"] = new JArray(c9.x, c9.y, c9.z) }); shotInfo.Add(new JObject { ["name"] = "o2_obl_2718_2291", ["eye"] = new JArray(eO.x, eO.y, eO.z), ["aim"] = new JArray(c9.x, c9.y, c9.z) });
        }
        if (osm) { }
        else if (relax)
        {
            var pc0 = res.plaza != null ? res.plaza.c : harborGeo;
            var rp = new List<(Vec2 p, double y, SplineEdge e, int i)>();
            foreach (var e in mains) for (int k = 0; k < e.pts.Count; k++) rp.Add((e.pts[k], e.y[k], e, k));
            var a = rp.OrderBy(x => Math.Abs(SplineEdge.Dist(x.p, pc0) - 70)).First();
            var eyeA = new Vector3((float)a.p.x, (float)a.y + 1.6f, (float)a.p.z); var aimA = new Vector3((float)pc0.x, (float)a.y + 3f, (float)pc0.z);
            Shot(cam, eyeA, Quaternion.LookRotation((aimA - eyeA).normalized), Path.Combine(shots, prefix + "_centre_1.png"));
            shotInfo.Add(new JObject { ["name"] = "centre_1", ["eye"] = new JArray(eyeA.x, eyeA.y, eyeA.z) });
            // 둘째: 광장에 가장 가까운 골목 입구에서 골목 안쪽으로
            var al = res.Alleys.OrderBy(e => SplineEdge.Dist(e.pts[0], pc0)).FirstOrDefault();
            if (al != null && al.pts.Count >= 3)
            {
                int k2 = Math.Min(al.pts.Count - 1, 8);
                var eyeB = new Vector3((float)al.pts[0].x, (float)al.y[0] + 1.6f, (float)al.pts[0].z); var aimB = new Vector3((float)al.pts[k2].x, (float)al.y[k2] + 1.4f, (float)al.pts[k2].z);
                Shot(cam, eyeB, Quaternion.LookRotation((aimB - eyeB).normalized), Path.Combine(shots, prefix + "_centre_2.png"));
                shotInfo.Add(new JObject { ["name"] = "centre_2_alley", ["eye"] = new JArray(eyeB.x, eyeB.y, eyeB.z) });
            }
            cam.orthographic = true; cam.orthographicSize = 200;
            Shot(cam, new Vector3((float)pc0.x, 400, (float)pc0.z), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top.png"));
            cam.orthographic = false;
        }
        else if (scenic)
        {
            // 곶 구간 운전 높이 2장: 외딴집 중 곶(가장 남쪽 길 점)에 가까운 두 집을 길에서 40m 앞에 두고 바라봄
            var ss = mains.Where(e => e.highway.StartsWith("primary")).ToList();
            var capeP = ss.SelectMany(e => e.pts).OrderBy(q => q.z).First();
            var sh = res.houses.Where(h => h.scenic).OrderBy(h => SplineEdge.Dist(h.center, capeP)).Take(2).ToList();
            int si = 0;
            foreach (var hsel in sh)
            {
                var e = res.edges[hsel.edge]; int k0 = hsel.idx;
                // 집 앞 길 점에서 40m 뒤(곶 쪽에서 오는 방향)로 물러나 집을 봄
                double acc = 0; int kb = k0; while (kb > 0 && acc < 40) { acc += SplineEdge.Dist(e.pts[kb], e.pts[kb - 1]); kb--; }
                if (acc < 20) { acc = 0; kb = k0; while (kb + 1 < e.pts.Count && acc < 40) { acc += SplineEdge.Dist(e.pts[kb], e.pts[kb + 1]); kb++; } }
                var eye = new Vector3((float)e.pts[kb].x, (float)e.y[kb] + 1.6f, (float)e.pts[kb].z);
                var aim = new Vector3((float)hsel.center.x, (float)hsel.plinthY + 3f, (float)hsel.center.z);
                Shot(cam, eye, Quaternion.LookRotation((aim - eye).normalized), Path.Combine(shots, prefix + "_cape_" + (++si) + ".png"));
                shotInfo.Add(new JObject { ["name"] = "cape_" + si, ["eye"] = new JArray(eye.x, eye.y, eye.z), ["house"] = new JArray(hsel.center.x, hsel.center.z) });
            }
        }
        else if (densityHouses)
        {
            // 운전 높이 2장: 중심 안(광장 쪽을 보며) · 중심→가장자리로 나가는 길(바깥을 보며). 길 점 = 큰길 중심선 점(높이 = 길 높이)
            var pc0 = res.plaza != null ? res.plaza.c : harborGeo;
            var rp = new List<(Vec2 p, double y, SplineEdge e, int i)>();
            foreach (var e in mains) for (int k = 0; k < e.pts.Count; k++) rp.Add((e.pts[k], e.y[k], e, k));
            var a = rp.OrderBy(x => Math.Abs(SplineEdge.Dist(x.p, pc0) - 70)).First();
            var eyeA = new Vector3((float)a.p.x, (float)a.y + 1.6f, (float)a.p.z); var aimA = new Vector3((float)pc0.x, (float)a.y + 3f, (float)pc0.z);
            Shot(cam, eyeA, Quaternion.LookRotation((aimA - eyeA).normalized), Path.Combine(shots, prefix + "_centre.png"));
            var b = rp.OrderBy(x => Math.Abs(SplineEdge.Dist(x.p, pc0) - 160)).First();
            int nb = Math.Min(b.e.pts.Count - 1, b.i + 15), pb = Math.Max(0, b.i - 15);
            var outP = SplineEdge.Dist(b.e.pts[nb], pc0) > SplineEdge.Dist(b.e.pts[pb], pc0) ? nb : pb;
            var eyeB = new Vector3((float)b.p.x, (float)b.y + 1.6f, (float)b.p.z); var aimB = new Vector3((float)b.e.pts[outP].x, (float)b.e.y[outP] + 1.2f, (float)b.e.pts[outP].z);
            Shot(cam, eyeB, Quaternion.LookRotation((aimB - eyeB).normalized), Path.Combine(shots, prefix + "_to_edge.png"));
            shotInfo.Add(new JObject { ["name"] = "centre", ["eye"] = new JArray(eyeA.x, eyeA.y, eyeA.z) }); shotInfo.Add(new JObject { ["name"] = "to_edge", ["eye"] = new JArray(eyeB.x, eyeB.y, eyeB.z) });
            cam.orthographic = true; cam.orthographicSize = 260;
            Shot(cam, new Vector3((float)pc0.x, 400, (float)pc0.z), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top_zoom.png"));
            cam.orthographic = false;
        }
        else if (density)
        {
            cam.orthographic = true; cam.orthographicSize = H / 2 * 1.02f;
            Shot(cam, new Vector3(W / 2, verts.Max(v => v.y) + 100, H / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_density.png"));
            cam.orthographic = false;
        }
        else if (center)
        {
            var c0 = res.plaza != null ? res.plaza.c : harborGeo;
            var pc = new Vector3((float)c0.x, (float)(res.plaza != null ? res.plaza.y : t.Surface(c0.x, c0.z)), (float)c0.z);
            if (res.plaza != null)
            {
                var pz = res.plaza;
                var far = pz.Corners().OrderByDescending(q => SplineEdge.Dist(q, pz.towerC)).First();
                var inward = new Vector3((float)(pz.c.x - far.x), 0, (float)(pz.c.z - far.z)).normalized;
                var eyeP = new Vector3((float)far.x, 0, (float)far.z) + inward * 1.5f; eyeP.y = (float)pz.Y(new Vec2(eyeP.x, eyeP.z)) + 1.6f;
                var tc = new Vector3((float)pz.towerC.x, (float)pz.Y(pz.towerC) + 5f, (float)pz.towerC.z);
                Shot(cam, eyeP, Quaternion.LookRotation((tc - eyeP).normalized), Path.Combine(shots, prefix + "_plaza_drive.png"));
                shotInfo.Add(new JObject { ["name"] = "plaza_drive", ["eye"] = new JArray(eyeP.x, eyeP.y, eyeP.z) });
            }
            cam.orthographic = true; cam.orthographicSize = 160;
            Shot(cam, pc + Vector3.up * 300, Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top_zoom.png"));
            cam.orthographic = false;
        }
        else
        {
        var pts = new List<(Vec2 p, SplineEdge e, double arc)>();
        foreach (var e in mains) { double acc = 0; for (int k = 0; k < e.pts.Count; k++) { if (k > 0) acc += SplineEdge.Dist(e.pts[k - 1], e.pts[k]); pts.Add((e.pts[k], e, acc)); } }
        var spots = new List<(string name, (Vec2 p, SplineEdge e, double arc) at, bool west)>
        {
            ("harbor", pts.OrderBy(x => SplineEdge.Dist(x.p, harborGeo)).First(), true),
            ("cape", pts.OrderBy(x => x.p.z).First(), true),
            ("conca", pts.OrderBy(x => x.p.x).First(), false),
        };
        foreach (var sp in spots)
        {
            var e = sp.at.e; double a0 = Math.Min(e.Length - 1, Math.Max(1, sp.at.arc));
            var st = SplineTown.Station(e, a0);
            var ahead = SplineTown.Station(e, Math.Min(e.Length, a0 + 40)); var back = SplineTown.Station(e, Math.Max(0, a0 - 40));
            var tgt = sp.west == (ahead.p.x < back.p.x) ? ahead : back;   // 항구·곶은 서쪽(콘카)으로, 콘카는 동쪽(아말피)으로
            var eye = new Vector3((float)st.p.x, (float)st.y + 1.6f, (float)st.p.z);
            var aim = new Vector3((float)tgt.p.x, (float)tgt.y + 1.2f, (float)tgt.p.z);
            Shot(cam, eye, Quaternion.LookRotation((aim - eye).normalized), Path.Combine(shots, prefix + "_" + sp.name + ".png"));
            shotInfo.Add(new JObject { ["name"] = sp.name, ["eye"] = new JArray(eye.x, eye.y, eye.z), ["aim"] = new JArray(aim.x, aim.y, aim.z) });
        }
        cam.orthographic = true; cam.orthographicSize = H / 2 * 1.02f;
        Shot(cam, new Vector3(W / 2, verts.Max(v => v.y) + 100, H / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, prefix + "_top.png"));
        cam.orthographic = false;
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        if (!string.IsNullOrEmpty(prevScene)) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);

        var outDir = Path.Combine(TownGenRunner.ToolsDir, "out", "scene_structure_" + prefix); Directory.CreateDirectory(outDir);
        var measure = new JObject
        {
            ["scene"] = scenePath, ["generated"] = DateTime.Now.ToString("s"), ["box_swne"] = hd["box_swne"], ["size_m"] = new JArray(W, H), ["heightScale"] = HeightScale,
            ["input"] = new JObject { ["ways"] = ways.Count, ["filter"] = "network.json segments with ref containing SS163" },
            ["terrain"] = new JObject { ["vertices"] = verts.Count, ["triangles"] = (land.Count + sand.Count) / 3, ["fineCells8m"] = fineCells, ["fineAreaHa"] = fineCells * step * step / 10000f, ["bandMeters"] = BandMeters, ["coreGrid2m"] = new JArray(t.nx, t.nz) },
            ["sceneTriangles"] = tris, ["roadLength_m"] = mains.Sum(e => e.Length), ["roadCurves"] = mains.Count, ["walls"] = res.walls.Count,
            ["report"] = res.report.ToJson(), ["shots"] = shotInfo, ["density"] = densityStats, ["densityHouses"] = densityHousesStats, ["scenic"] = scenicStats, ["village"] = villageStats, ["osmBuildings"] = osmJ, ["townCore"] = tpJ, ["overlapCheck"] = ovJ, ["sceneOverlapO2"] = o2J, ["carGraph"] = carGraphJ,
            ["centreAlleys"] = !relax ? null : new JObject { ["relaxRadius"] = s.alleyRelaxRadius, ["relaxCentre"] = new JArray(s.alleyRelaxX, s.alleyRelaxZ), ["alleys"] = res.Alleys.Count(), ["alleyLength_m"] = res.Alleys.Sum(e => e.Length), ["alleysInRelax"] = res.Alleys.Count(e => SplineEdge.Dist(e.pts[e.pts.Count / 2], new Vec2(s.alleyRelaxX, s.alleyRelaxZ)) <= s.alleyRelaxRadius), ["stairs"] = stairLines.Count, ["stairLength_m"] = stairLines.Sum(SplineEdge.PolyLength), ["stairSteps"] = stairSteps, ["alleyMetric"] = res.report.MetricValue("alleys") },
            ["center"] = !center ? null : new JObject
            {
                ["harborRef"] = new JArray(harborGeo.x, harborGeo.z), ["roadRadius"] = centerRoadRadius, ["houseRegionRadius"] = s.houseRegionRadius,
                ["houseRegionCenter"] = res.houseRegionCenter.HasValue ? new JArray(res.houseRegionCenter.Value.x, res.houseRegionCenter.Value.z) : null,
                ["houses"] = res.houses.Count, ["shops"] = res.houses.Count(h => h.shop), ["alleys"] = res.Alleys.Count(),
                ["seat"] = new JObject { ["ground"] = res.houses.Count(h => h.seat == 0), ["wall_down"] = res.houses.Count(h => h.seat == 1), ["half_basement"] = res.houses.Count(h => h.seat == 2) },
                ["plaza"] = res.plaza == null ? null : new JObject { ["center"] = new JArray(res.plaza.c.x, res.plaza.y, res.plaza.c.z), ["size"] = new JArray(res.plaza.length, res.plaza.width), ["siteGrade"] = res.plaza.slope30, ["seaVisible"] = res.plaza.seeSea, ["coastRoadDist"] = res.plaza.coastDist, ["arms"] = res.plaza.arms, ["roadside"] = res.plaza.roadside, ["terraced"] = res.plaza.terraced, ["floorGrade"] = Math.Sqrt(res.plaza.gx * res.plaza.gx + res.plaza.gz * res.plaza.gz) },
            },
        };
        File.WriteAllText(Path.Combine(outDir, "measure.json"), measure.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "report.txt"), res.report.ToText(), new UTF8Encoding(false));
        if (o2J != null) File.WriteAllText(Path.Combine(outDir, "o2_scene_overlap.json"), o2J.ToString(), new UTF8Encoding(false));
        sw.Stop();
        return $"[TownGen] Structure v2 {scenePath} · 길 {mains.Count}곡선 {mains.Sum(e => e.Length):0}m · 정점 {verts.Count} · 지형 삼각형 {(land.Count + sand.Count) / 3} · 씬 삼각형 {tris} · {sw.ElapsedMilliseconds}ms\n" + res.report.ToText();
    }

    /// <summary>
    /// 밀도 지도(스펙 D-113 §4): 10×10m 필지마다 p = 목표밀도(장면) × f_경사 × f_바다 × f_길.
    /// 장면 = 광장에서 차도 따라 잰 거리 d(≤120 ① / ≤350 ② / 그 밖 ③), 콘카 데이 마리니(OSM place) 반경 80m = ④.
    /// ①②④ 목표 40 / 10 / 25 채/ha(②·④ 는 스펙 범위 8~12·20~30 의 가운데), ③ 외딴집 = SS163 따라 225m 마다 1채(150~300 가운데) → 길 옆 15m 띠에 고르게 나눔.
    /// f_경사 ≤15% 1 · 15~35% 1→0.3 · >35% 0 / f_바다 필지 높이+4m 에서 바다 쪽 5방향 광선 중 하나라도 바다 먼저 → 1, 아니면 0.7 / f_길 차도 ≤15m 1 · ≤40m 0.5 · 그 밖 0.
    /// 합계가 상한 350 을 넘으면 배율 하나로 줄인다. 색: ① 빨강 ② 주황 ③ 노랑 ④ 보라, 진하기 = p, p=0 땅 = 회녹색, 바다 = 파랑.
    /// </summary>
    static JObject DensityOverlay(Transform root, List<Vector3> verts, List<int> tris, float W, float H, float[] raw, int nx, int nz, float step, bool[] sea, SplineTownResult res, JObject nd, Vec2 harborGeo, out Texture2D tex)
        => DensityCore(root, verts, tris, W, H, raw, nx, nz, step, sea, res, nd, harborGeo, out tex, 120f, new[] { 0.40f, 0.10f, 0f, 0.25f }, true, out _, out _, out _, out _);

    /// <summary>밀도 지도 계산(+선택: 색 겹침판). r1 = 중심 도로 거리 반경, target = 장면별 채/필지(0.01ha). pOut·sceneOut = 필지별 확률·장면(−1 바다), roadNear = 필지에서 가장 가까운 차도 점.</summary>
    static JObject DensityCore(Transform root, List<Vector3> verts, List<int> tris, float W, float H, float[] raw, int nx, int nz, float step, bool[] sea, SplineTownResult res, JObject nd, Vec2 harborGeo, out Texture2D tex,
        float r1, float[] target, bool overlay, out float[] pOut, out int[] sceneOut, out int pxOut, out Vector2[] roadNear, bool sceneRoadsOnly = false, List<Vector2> stairTops = null)
    {
        const float cell = 10f;
        int px = Mathf.CeilToInt(W / cell), pz = Mathf.CeilToInt(H / cell);
        float Hs(float x, float z)
        {
            float fx = Mathf.Clamp(x / step, 0, nx - 1.0001f), fz = Mathf.Clamp(z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            return ((raw[j * nx + i] * (1 - tx) + raw[j * nx + i + 1] * tx) * (1 - tz) + (raw[(j + 1) * nx + i] * (1 - tx) + raw[(j + 1) * nx + i + 1] * tx) * tz) * HeightScale;
        }
        bool Sea(float x, float z) { int i = Mathf.Clamp(Mathf.RoundToInt(x / step), 0, nx - 1), j = Mathf.Clamp(Mathf.RoundToInt(z / step), 0, nz - 1); return sea[j * nx + i]; }
        // 차도 그래프(OSM 차도 전체, 5m 로 촘촘히) + 광장에서 도로 거리(다익스트라)
        var nodeOf = new Dictionary<(int, int), int>(); var npos = new List<Vector2>(); var adj = new List<List<(int, float)>>(); var isSS = new List<bool>();
        int Node(Vector2 p, bool ss) { var k = (Mathf.RoundToInt(p.x * 2), Mathf.RoundToInt(p.y * 2)); if (!nodeOf.TryGetValue(k, out int id)) { id = npos.Count; nodeOf[k] = id; npos.Add(p); adj.Add(new List<(int, float)>()); isSS.Add(ss); } else if (ss) isSS[id] = true; return id; }
        // 길 목록: 조각 4 색칠 = OSM 차도 전체 / 조각 5 집 = 씬에 실제로 있는 길(코어 큰길·골목)만 — 그려지지 않는 길 옆에 집이 놓이지 않게
        var roadLines = sceneRoadsOnly
            ? res.edges.Select(e => (pts: e.pts.Select(q => new Vector2((float)q.x, (float)q.z)).ToList(), ss: !e.alley && e.highway.StartsWith("primary"))).ToList()
            : nd["segments"].Where(sg => sg["class"] == null || sg["class"].ToString() == "major" || sg["class"].ToString() == "minor").Select(sg => (pts: sg["points_m"].Select(q => new Vector2(q[0].Value<float>(), q[1].Value<float>())).ToList(), ss: sg["ref"].ToString().Contains("SS163"))).ToList();
        foreach (var rl in roadLines)
        {
            bool ss = rl.ss;
            var pts = rl.pts;
            int prev = -1;
            for (int k = 0; k + 1 < pts.Count; k++)
            {
                int m = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[k], pts[k + 1]) / 5));
                for (int t = (k == 0 ? 0 : 1); t <= m; t++)
                {
                    int id = Node(Vector2.Lerp(pts[k], pts[k + 1], (float)t / m), ss);
                    if (prev >= 0 && prev != id) { float d = Vector2.Distance(npos[prev], npos[id]); adj[prev].Add((id, d)); adj[id].Add((prev, d)); }
                    prev = id;
                }
            }
        }
        var plazaC = res.plaza != null ? new Vector2((float)res.plaza.c.x, (float)res.plaza.c.z) : new Vector2((float)harborGeo.x, (float)harborGeo.z);
        var dist = Enumerable.Repeat(float.MaxValue, npos.Count).ToArray();
        var pq = new SortedSet<(float, int)>();
        for (int i = 0; i < npos.Count; i++) { float d0 = Vector2.Distance(npos[i], plazaC); if (d0 < 60) { dist[i] = d0; pq.Add((d0, i)); } }
        while (pq.Count > 0) { var (d, u) = pq.Min; pq.Remove(pq.Min); if (d > dist[u]) continue; foreach (var (v, w) in adj[u]) if (d + w < dist[v]) { if (dist[v] < float.MaxValue) pq.Remove((dist[v], v)); dist[v] = d + w; pq.Add((dist[v], v)); } }
        // 공간 해시(가장 가까운 차도 점)
        float hc = 40f; var grid = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < npos.Count; i++) { var k = (Mathf.FloorToInt(npos[i].x / hc), Mathf.FloorToInt(npos[i].y / hc)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>(); l.Add(i); }
        (int id, float d) Nearest(Vector2 p, bool ssOnly)
        {
            int cx = Mathf.FloorToInt(p.x / hc), cz = Mathf.FloorToInt(p.y / hc); int best = -1; float bd = float.MaxValue;
            for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++) if (grid.TryGetValue((cx + a, cz + b), out var l)) foreach (var i in l) { if (ssOnly && !isSS[i]) continue; float d = Vector2.Distance(p, npos[i]); if (d < bd) { bd = d; best = i; } }
            return (best, bd);
        }
        // 콘카 데이 마리니(OSM place=village 40.6176092, 14.5725004 — osm/structure_a_places_2026-10-19.json)
        var conca = new Vector2((float)((14.5725004 - 14.5690) * 111320.0 * Math.Cos(40.6130 * Math.PI / 180)), (float)((40.6176092 - 40.6130) * 110574.0));
        // 바다 방향(가장 가까운 바다 8m 점)
        var seaPts = new List<Vector2>(); for (int j = 0; j < nz; j += 2) for (int i = 0; i < nx; i += 2) if (sea[j * nx + i]) seaPts.Add(new Vector2(i * step, j * step));
        var p = new float[px * pz]; var roadNearArr = new Vector2[px * pz]; var sceneOf = new int[px * pz]; var landArea = new float[4]; var placeable = new float[4];
        for (int j = 0; j < pz; j++)
            for (int i = 0; i < px; i++)
            {
                int idx = j * px + i; float x = (i + 0.5f) * cell, z = (j + 0.5f) * cell; var c = new Vector2(x, z);
                if (x > W || z > H || Sea(x, z) || Hs(x, z) < 0.3f) { sceneOf[idx] = -1; continue; }
                var (rid, rd) = Nearest(c, false);
                float roadDist = rid >= 0 && dist[rid] < float.MaxValue ? dist[rid] + rd : float.MaxValue;
                int sc = Vector2.Distance(c, conca) <= 80 ? 3 : roadDist <= r1 ? 0 : roadDist <= 350 ? 1 : 2;
                roadNearArr[idx] = rid >= 0 ? npos[rid] : c;
                sceneOf[idx] = sc; landArea[sc] += cell * cell / 10000f;
                float gx = (Hs(x + 5, z) - Hs(x - 5, z)) / 10, gz = (Hs(x, z + 5) - Hs(x, z - 5)) / 10, slope = Mathf.Sqrt(gx * gx + gz * gz);
                float fSlope = slope <= 0.15f ? 1f : slope <= 0.35f ? Mathf.Lerp(1f, 0.3f, (slope - 0.15f) / 0.2f) : 0f;
                float fRoad = rd <= 15 ? 1f : rd <= 40 ? 0.5f : 0f;
                if (fRoad <= 0 && stairTops != null && stairTops.Any(st => Vector2.Distance(st, c) <= 30)) fRoad = 0.5f;   // §4.2 ⓑ 공용 계단 꼭대기 보행로 30m 안
                if (sc == 2)
                {
                    // ③ 외딴집: SS163 길 옆 15m 안, 경사 ≤ 20% 만 — 길 225m 당 1채를 띠 넓이로 나눔(30m 폭 띠 → 필지당 100/(225×30))
                    var (sid, sdist) = Nearest(c, true);
                    p[idx] = sid >= 0 && sdist <= 15 && slope <= 0.20f ? 100f / (225f * 30f) : 0f;
                    if (p[idx] > 0) placeable[sc] += 0.01f;
                    continue;
                }
                if (fSlope <= 0 || fRoad <= 0) continue;
                placeable[sc] += 0.01f;
                // f_바다: 필지 높이 + 4m 눈에서 가장 가까운 바다 쪽 ±60° 5 광선, 4m 걸음 300m
                float fSea = 0.7f;
                if (seaPts.Count > 0)
                {
                    var ns = seaPts.OrderBy(q => (q - c).sqrMagnitude).First(); float a0 = Mathf.Atan2(ns.y - z, ns.x - x), eye = Hs(x, z) + 4;
                    for (int k = -2; k <= 2 && fSea < 1; k++)
                    {
                        float a = a0 + k * Mathf.PI / 6;
                        for (float d = 4; d <= 300; d += 4) { float qx = x + Mathf.Cos(a) * d, qz = z + Mathf.Sin(a) * d; if (qx < 0 || qz < 0 || qx > W || qz > H) break; if (Sea(qx, qz)) { fSea = 1; break; } if (Hs(qx, qz) > eye) break; }
                    }
                }
                p[idx] = Mathf.Min(1f, target[sc] * fSlope * fSea * fRoad);
            }
        var expected = new float[4]; for (int k = 0; k < p.Length; k++) if (sceneOf[k] >= 0) expected[sceneOf[k]] += p[k];
        float total = expected.Sum(), scale = total > 350 ? 350f / total : 1f;
        pOut = p; sceneOut = sceneOf; pxOut = px; roadNear = roadNearArr;
        tex = null;
        if (!overlay)
        {
            var bs = new JObject(); string[] nm = { "1_centre", "2_edge", "3_scenic_road", "4_hill_village" };
            for (int k = 0; k < 4; k++) bs[nm[k]] = new JObject { ["landHa"] = landArea[k], ["placeableHa"] = placeable[k], ["expectedHouses"] = expected[k] };
            return new JObject { ["r1"] = r1, ["targetPerParcel"] = new JArray(target), ["byScene"] = bs, ["expectedTotal"] = total };
        }
        // 색 텍스처
        tex = new Texture2D(px, pz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color[] hue = { new Color(0.85f, 0.1f, 0.1f), new Color(1f, 0.55f, 0.05f), new Color(1f, 0.9f, 0.1f), new Color(0.6f, 0.2f, 0.85f) };
        float[] pmax = new float[4]; for (int k = 0; k < p.Length; k++) if (sceneOf[k] >= 0) pmax[sceneOf[k]] = Mathf.Max(pmax[sceneOf[k]], p[k]);
        var cols = new Color[px * pz]; var landCol = new Color(0.45f, 0.5f, 0.42f); var seaCol = new Color(0.2f, 0.45f, 0.7f);
        for (int k = 0; k < p.Length; k++)
        {
            int sc = sceneOf[k];
            if (sc < 0) { cols[k] = seaCol; continue; }
            if (p[k] <= 0) { cols[k] = Color.Lerp(landCol, hue[sc], 0.18f); continue; }
            float t = pmax[sc] > 0 ? 0.35f + 0.65f * p[k] / pmax[sc] : 1f;
            cols[k] = Color.Lerp(landCol, hue[sc], t);
        }
        tex.SetPixels(cols); tex.Apply();
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data", "TownGen", "Density"));
        string texPath = "Assets/Data/TownGen/Density/density_s4.png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", texPath), tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(texPath); ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        var texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        string matPath = "Assets/Data/TownGen/Materials/Density_s4.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", texAsset); mat.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        // 겹침판 = 지형 정점 그대로 + 0.4m, UV = 땅 전체
        var ov = verts.Select(v => v + Vector3.up * 0.4f).ToList(); var ouv = verts.Select(v => new Vector2(v.x / (px * cell), v.z / (pz * cell))).ToList();
        var om = new Mesh { name = "DensityOverlay", indexFormat = IndexFormat.UInt32 }; om.SetVertices(ov); om.SetUVs(0, ouv); om.SetTriangles(tris, 0); om.RecalculateNormals(); om.RecalculateBounds();
        var go = new GameObject("DensityOverlay_debug"); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = om; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        string[] names = { "1_centre", "2_edge", "3_scenic_road", "4_hill_village" };
        var bySc = new JObject();
        for (int k = 0; k < 4; k++) bySc[names[k]] = new JObject { ["landHa"] = landArea[k], ["placeableHa"] = placeable[k], ["expectedHouses"] = expected[k], ["expectedScaled"] = expected[k] * scale };
        return new JObject
        {
            ["rule"] = "spec D-113 §4 (target 40/10/25 per ha, scenic 1 per 225m along SS163); road distance from plaza along OSM car roads; Conca = OSM place node",
            ["cellM"] = cell, ["cells"] = new JArray(px, pz), ["byScene"] = bySc, ["expectedTotal"] = total, ["cap"] = 350, ["scaleToCap"] = scale,
            ["plazaRef"] = new JArray(plazaC.x, plazaC.y), ["concaRef"] = new JArray(conca.x, conca.y), ["texture"] = texPath,
        };
    }

    /// <summary>
    /// ③ 외딴집(스펙 D-113 §4·§4.1): SS163 큰길 따라 10m 마다 양쪽 필지 후보(정면 = 길, 정면이 어깨 끝 + 틈) → ③ 장면 칸·필지 경사 ≤ 20%·커브 안쪽(반경 &lt; 15m) 아님만.
    /// 점수 = 바다 보임(필지 높이 + 4m 에서 바다 쪽 5광선) 우선, 다음 낮은 경사. 점수순으로 최소 간격 150m(Poisson-disk, 시드 고정) 를 지키며 최대 15채, 20% 는 옆에 헛간(작은 차고 모델) 1채.
    /// 놓기·앉히기·진입로는 PlaceDensityHouses(기존 규칙). 진입로는 정면 거리라 ≤ 15m.
    /// </summary>
    static JObject ScenicHouses(SplineTownResult res, SplineTownSettings s, float[] raw, int nx, int nz, float step, bool[] sea, int[] scArr, int pxN, Vec2 harborGeo)
    {
        float Hs(double x, double z)
        {
            float fx = Mathf.Clamp((float)x / step, 0, nx - 1.0001f), fz = Mathf.Clamp((float)z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            return ((raw[j * nx + i] * (1 - tx) + raw[j * nx + i + 1] * tx) * (1 - tz) + (raw[(j + 1) * nx + i] * (1 - tx) + raw[(j + 1) * nx + i + 1] * tx) * tz) * HeightScale;
        }
        bool Sea(double x, double z) { int i = Mathf.Clamp(Mathf.RoundToInt((float)x / step), 0, nx - 1), j = Mathf.Clamp(Mathf.RoundToInt((float)z / step), 0, nz - 1); return sea[j * nx + i]; }
        int pzN = scArr.Length / pxN;
        var seaPts = new List<Vec2>(); for (int j = 0; j < nz; j += 2) for (int i = 0; i < nx; i += 2) if (sea[j * nx + i]) seaPts.Add(new Vec2(i * step, j * step));
        var houseModels = Enumerable.Range(0, s.houseModelNames.Length).Where(k => s.houseModelNames[k].StartsWith("CoastalHouse")).ToList();
        int barnModel = Array.FindIndex(s.houseModelNames, n => n == "RepairGarage");
        var spots = new List<(SplineTown.HouseCand c, double score, double slope, bool seaView)>();
        int checkedN = 0, notScenic = 0, steep = 0, innerCurve = 0;
        foreach (var e in res.edges.Where(e => !e.alley && e.highway.StartsWith("primary")))
        {
            double acc = 0;
            for (int k = 1; k + 1 < e.pts.Count; k++)
            {
                acc += SplineEdge.Dist(e.pts[k - 1], e.pts[k]); if (acc < 10) continue; acc = 0;
                var (nxv, nzv) = SplineTown.Normal(e.pts, k);
                foreach (int side in new[] { 1, -1 })
                {
                    checkedN++;
                    int model = houseModels[(int)((uint)(k * 2654435761u + (uint)(side + 1) * 40503u) >> 5) % houseModels.Count];
                    double d = s.HalfShoulder + s.houseFrontGapMeters + s.houseModelDepths[model] / 2;
                    var c = new Vec2(e.pts[k].x + side * nxv * d, e.pts[k].z + side * nzv * d);
                    int pi = Mathf.Clamp((int)(c.x / 10), 0, pxN - 1), pj = Mathf.Clamp((int)(c.z / 10), 0, pzN - 1);
                    if (scArr[pj * pxN + pi] != 2) { notScenic++; continue; }
                    double gx = (Hs(c.x + 5, c.z) - Hs(c.x - 5, c.z)) / 10, gz = (Hs(c.x, c.z + 5) - Hs(c.x, c.z - 5)) / 10, slope = Math.Sqrt(gx * gx + gz * gz);
                    if (slope > 0.20) { steep++; continue; }
                    double r = SplineTown.RadiusAt(e.pts, k, 2);
                    if (r < 15) { double turn = SplineTown.TurnAngle(e.pts[k - 1], e.pts[k], e.pts[k + 1]); if ((turn > 0 && side == 1) || (turn < 0 && side == -1)) { innerCurve++; continue; } }
                    bool seaView = false;
                    if (seaPts.Count > 0)
                    {
                        var ns = seaPts.OrderBy(q => (q.x - c.x) * (q.x - c.x) + (q.z - c.z) * (q.z - c.z)).First(); double a0 = Math.Atan2(ns.z - c.z, ns.x - c.x), eye = Hs(c.x, c.z) + 4;
                        for (int q = -2; q <= 2 && !seaView; q++)
                        {
                            double a = a0 + q * Math.PI / 6;
                            for (double dd = 4; dd <= 300; dd += 4) { double qx = c.x + Math.Cos(a) * dd, qz = c.z + Math.Sin(a) * dd; if (Sea(qx, qz)) { seaView = true; break; } if (Hs(qx, qz) > eye) break; }
                        }
                    }
                    double yaw = Math.Atan2(-side * nxv, -side * nzv) * 180 / Math.PI;
                    spots.Add((new SplineTown.HouseCand { c = c, yaw = yaw, model = model, scene = 2 }, (seaView ? 1 : 0) + (0.2 - slope), slope, seaView));
                }
            }
        }
        // 점수순 + 최소 간격 150m(이미 놓은 집 기준, 직선거리 — 굽은 해안도로라 길 따라 간격보다 짧게 잡힘 → 보수적)
        var chosen = new List<(SplineTown.HouseCand c, bool seaView)>();
        foreach (var sp in spots.OrderByDescending(x => x.score).ThenBy(x => x.c.c.x))
        {
            if (chosen.Count >= 15) break;
            if (chosen.Any(o => SplineEdge.Dist(o.c.c, sp.c.c) < 150)) continue;
            chosen.Add((sp.c, sp.seaView));
        }
        var cands = new List<SplineTown.HouseCand>(); int barns = 0;
        foreach (var ch in chosen)
        {
            cands.Add(ch.c);
            uint h = (uint)(ch.c.c.x * 73856093) ^ (uint)(ch.c.c.z * 19349663);
            if (barnModel >= 0 && h % 5 == 0)
            {
                // 헛간: 같은 필지(정면 방향 기준 오른쪽으로 집 폭/2 + 헛간 폭/2 + 2m)
                double rr = ch.c.yaw * Math.PI / 180, rx = Math.Cos(rr), rz = -Math.Sin(rr);
                double off = s.houseModelWidths[ch.c.model] / 2 + s.houseModelWidths[barnModel] / 2 + 2;
                cands.Add(new SplineTown.HouseCand { c = new Vec2(ch.c.c.x + rx * off, ch.c.c.z + rz * off), yaw = ch.c.yaw, model = barnModel, scene = 2 }); barns++;
            }
        }
        cands = RuleFilter(cands);
        int before = res.houses.Count;
        var why = SplineTown.PlaceDensityHouses(res, s, cands, 350);
        var placed = res.houses.Skip(before).ToList();
        foreach (var h in placed) h.scenic = true;
        int mainHouses = placed.Count(h => h.model != barnModel);
        var gaps = new List<double>();
        var ordered = placed.Where(h => h.model != barnModel).OrderBy(h => h.center.x).ToList();
        for (int k = 1; k < ordered.Count; k++) gaps.Add(SplineEdge.Dist(ordered[k - 1].center, ordered[k].center));
        var whyJ = new JObject(); foreach (var kv in why) whyJ[kv.Key] = kv.Value;
        return new JObject
        {
            ["rule"] = "SS163 sides every 10m, scene 3 parcels, slope ≤20%, not inner side of radius<15m, score = sea view then low slope, min spacing 150m (straight-line), max 15, 20% barn",
            ["spotsChecked"] = checkedN, ["rejected"] = new JObject { ["notScene3"] = notScenic, ["steep"] = steep, ["innerCurve"] = innerCurve }, ["spots"] = spots.Count,
            ["chosen"] = chosen.Count, ["chosenSeaView"] = chosen.Count(c => c.seaView), ["barnsTried"] = barns, ["placedHouses"] = mainHouses, ["placedBarns"] = placed.Count - mainHouses,
            ["outcome"] = whyJ, ["gapsStraight_m"] = new JArray(gaps.Select(g => Math.Round(g))),
            ["driveways"] = placed.Count(h => h.hasDrive), ["maxDrive_m"] = placed.Where(h => h.hasDrive).Select(h => SplineEdge.Dist(h.driveA, h.driveB)).DefaultIfEmpty(0).Max(),
        };
    }

    /// <summary>3km 땅용 재질 사본 — 기존 FarFog(안개 500~1800m)는 다른 씬이 쓰니 건드리지 않고 안개 1500~6000m 사본을 따로.</summary>
    /// <summary>조각 7(스펙 D-113 §4 ④): 콘카 씨앗 반경 80m — 작은 광장 12×12(+뒤 6m 예배당 띠)를 마을 길에 붙여 고르고(땅 높이를 판 높이로, 둘레 옹벽),
    /// 집은 마을 길·골목 따라 양쪽 줄짓기(셋백 ≈ 0.3m, 옆집과 0.4m — 붙은 집), 씨앗 가까운 순, 상한 50. 놓기 검사는 PlaceDensityHouses(기존 규칙).</summary>
    static JObject Village(SplineTownResult res, SplineTownSettings s, Vec2 seed, double R, out Vec2[] plazaRect, out Vec2 plazaC, out double plazaYaw, out double plazaY, List<(Vector3 c, float yaw, float len, float y0, float y1)> walls, bool entryOnly = false)
    {
        var t = res.terrain; plazaRect = null; plazaC = default; plazaYaw = 0; plazaY = 0;
        double Reach(SplineEdge e) => e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder;
        var vEdges = res.edges.Where(e => e.highway != "primary" && e.pts.Any(q => SplineEdge.Dist(q, seed) <= R + 20)).ToList();
        bool IsSea(Vec2 p) { int c = (int)Math.Floor(p.x / s.seaCellMeters), r = (int)Math.Floor(p.z / s.seaCellMeters); return res.sea.InBounds(c, r) && res.sea.sea[res.sea.Index(c, r)]; }
        double SegD(Vec2 p, Vec2 a, Vec2 b) { double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz; double u = L2 > 1e-9 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0; return SplineEdge.Dist(p, new Vec2(a.x + dx * u, a.z + dz * u)); }
        var nearEdges = res.edges.Where(e => e.pts.Any(q => SplineEdge.Dist(q, seed) <= R + 80)).ToList();
        bool ClearOfRoads(Vec2 p, double margin)
        {
            foreach (var e in nearEdges) for (int k = 0; k + 1 < e.pts.Count; k++) if (SegD(p, e.pts[k], e.pts[k + 1]) < Reach(e) + margin) return false;
            foreach (var w in res.walls) if (SegD(p, w.a, w.b) < 0.6) return false;
            return true;
        }
        Vec2[] Rect(Vec2 c, double yawDeg, double w, double d) { double r = yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx; double hw = w / 2, hd = d / 2; return new[] { new Vec2(c.x - rx * hw - fx * hd, c.z - rz * hw - fz * hd), new Vec2(c.x + rx * hw - fx * hd, c.z + rz * hw - fz * hd), new Vec2(c.x + rx * hw + fx * hd, c.z + rz * hw + fz * hd), new Vec2(c.x - rx * hw + fx * hd, c.z - rz * hw + fz * hd) }; }

        // ---- 광장 자리: 마을 길 점마다 양쪽, 12(길 따라) × 18(12 광장 + 6 예배당) 사각형을 길 어깨 바깥 0.3m 에 붙여 본다
        const double PW = 12, PD = 18, maxCutFill = 3.0; int tried = 0, rejRoad = 0, rejCut = 0, rejSea = 0;
        (double score, Vec2 c, double yaw, double Y, double cut, double fill, double grade, double dSeed)? best = null;
        int hostEdge = -1; double hostArc = 0;
        foreach (var e in vEdges)
        {
            if (entryOnly && e.alley) continue;   // §4.3: 광장은 입구 길(차도)에 붙인다 — 차 도달
            for (int k = 1; k + 1 < e.pts.Count; k += 2)
            {
                double arcK = 0; for (int k2 = 1; k2 <= k; k2++) arcK += SplineEdge.Dist(e.pts[k2 - 1], e.pts[k2]);
                if (SplineEdge.Dist(e.pts[k], seed) > R - 10) continue;
                var (nx0, nz0) = SplineTown.Normal(e.pts, k);
                foreach (int sd in new[] { 1, -1 })
                {
                    tried++;
                    double off = Reach(e) + 0.3 + PD / 2; var c = new Vec2(e.pts[k].x + nx0 * sd * off, e.pts[k].z + nz0 * sd * off);
                    double yaw = Math.Atan2(-nx0 * sd, -nz0 * sd) * 180 / Math.PI;
                    double r = yaw * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
                    double Y = e.y[k] + s.curbHeightMeters, mn = 1e9, mx = -1e9; bool bad = false, sea = false;
                    for (double u = -PW / 2; u <= PW / 2 + 1e-6 && !bad; u += 1)
                        for (double v = -PD / 2; v <= PD / 2 + 1e-6; v += 1)
                        {
                            var q = new Vec2(c.x + rx * u + fx * v, c.z + rz * u + fz * v);
                            if (IsSea(q)) { sea = true; bad = true; break; }
                            if (!ClearOfRoads(q, 0.15)) { bad = true; break; }
                            double g = t.Surface(q.x, q.z); mn = Math.Min(mn, g); mx = Math.Max(mx, g);
                        }
                    if (bad) { if (sea) rejSea++; else rejRoad++; continue; }
                    double cut = Math.Max(0, mx - Y), fill = Math.Max(0, Y - mn);
                    if (Math.Max(cut, fill) > maxCutFill) { rejCut++; continue; }
                    double dS = SplineEdge.Dist(c, seed), score = Math.Max(cut, fill) + 0.03 * dS;
                    if (best == null || score < best.Value.score) { best = (score, c, yaw, Y, cut, fill, (mx - mn) / PD, dS); hostEdge = res.edges.IndexOf(e); hostArc = arcK; }
                }
            }
        }
        JObject plazaJ;
        if (best != null)
        {
            var b = best.Value; plazaC = b.c; plazaYaw = b.yaw; plazaY = b.Y;
            plazaRect = Rect(b.c, b.yaw, PW + 2, PD + 2);   // 집 금지 = 판 + 둘레 1m(옹벽 자리)
            // 땅 고르기: 판 안 + 옆·뒤 1m 고리의 2m 격자 점 = 판 높이(길 쪽 앞변은 그대로 — 길 어깨 지형 안 건드림)
            double r = b.yaw * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx; int flat = 0;
            var orig = (double[])t.h.Clone();
            for (int j = 0; j < t.nz; j++)
                for (int i = 0; i < t.nx; i++)
                {
                    double x = i * t.step - b.c.x, z = j * t.step - b.c.z, u = x * rx + z * rz, v = x * fx + z * fz;   // v = 길 쪽(+)
                    if (Math.Abs(u) <= PW / 2 + 1 && v >= -PD / 2 - 1 && v <= PD / 2) { t.h[j * t.nx + i] = b.Y; flat++; }
                }
            double OrigAt(double x, double z) { double fx2 = Math.Min(Math.Max(x / t.step, 0), t.nx - 1.000001), fz2 = Math.Min(Math.Max(z / t.step, 0), t.nz - 1.000001); int i0 = (int)fx2, j0 = (int)fz2; double tx = fx2 - i0, tz = fz2 - j0; double Hh(int a, int c2) => orig[c2 * t.nx + a]; return (Hh(i0, j0) * (1 - tx) + Hh(i0 + 1, j0) * tx) * (1 - tz) + (Hh(i0, j0 + 1) * (1 - tx) + Hh(i0 + 1, j0 + 1) * tx) * tz; }
            // 옹벽: 옆·뒤 경계(고리 바깥 끝)를 2m 토막으로 — 바깥 원래 땅과 판 높이 차가 0.3m 넘는 토막만
            void Side(double u0, double v0, double u1, double v1, double ou, double ov)
            {
                double L = Math.Sqrt((u1 - u0) * (u1 - u0) + (v1 - v0) * (v1 - v0)); int n = Math.Max(1, (int)Math.Ceiling(L / 2));
                for (int k = 0; k < n; k++)
                {
                    double a = (k + 0.5) / n, u = u0 + (u1 - u0) * a, v = v0 + (v1 - v0) * a;
                    double wx = b.c.x + rx * u + fx * v, wz = b.c.z + rz * u + fz * v, dx = rx * ou + fx * ov, dz = rz * ou + fz * ov;
                    double g1 = OrigAt(wx + dx * 1.0, wz + dz * 1.0), g2 = OrigAt(wx + dx * 2.5, wz + dz * 2.5), g = Math.Max(g1, g2), gl = Math.Min(g1, g2);
                    if (g - b.Y < 0.3 && b.Y - gl < 0.3) continue;
                    float y0 = (float)Math.Min(b.Y, gl) - 0.4f, y1 = (float)Math.Max(b.Y + (b.Y - gl > 1 ? 0.9 : 0.1), g + 0.05);   // 메운 쪽(판이 높음) 1m 넘으면 난간 0.9m
                    walls.Add((new Vector3((float)wx, 0, (float)wz), (float)(b.yaw + (Math.Abs(ou) > 0 ? 90 : 0)), (float)(L / n) + 0.45f, y0, y1));
                }
            }
            Side(-PW / 2 - 1, -PD / 2 - 1, -PW / 2 - 1, PD / 2, -1, 0); Side(PW / 2 + 1, -PD / 2 - 1, PW / 2 + 1, PD / 2, 1, 0); Side(-PW / 2 - 1, -PD / 2 - 1, PW / 2 + 1, -PD / 2 - 1, 0, -1);
            plazaJ = new JObject { ["centre"] = new JArray(b.c.x, b.c.z), ["yaw"] = b.yaw, ["levelY"] = b.Y, ["cut_m"] = b.cut, ["fill_m"] = b.fill, ["origSiteGrade"] = b.grade, ["plazaGradeAfter"] = 0, ["distToSeed_m"] = b.dSeed,
                ["size"] = new JArray(12, 12), ["chapel"] = new JObject { ["size"] = new JArray(8, 6, 6), ["tower"] = new JArray(3, 3, 10) }, ["levelledVerts"] = flat, ["wallSegments"] = walls.Count, ["hostEdge"] = hostEdge, ["hostArc_m"] = hostArc, ["tried"] = tried, ["rejected"] = new JObject { ["road"] = rejRoad, ["sea"] = rejSea, ["cutfill_over_3m"] = rejCut } };
        }
        else plazaJ = new JObject { ["found"] = false, ["tried"] = tried, ["rejected"] = new JObject { ["road"] = rejRoad, ["sea"] = rejSea, ["cutfill_over_3m"] = rejCut } };

        // ---- 집: 마을 길·골목 양쪽 줄짓기
        var houseModels = Enumerable.Range(0, s.houseModelNames.Length).Where(k => !s.houseModelNames[k].StartsWith("CornerShop") && !s.houseModelNames[k].StartsWith("RepairGarage")).ToList();
        var cands = new List<SplineTown.HouseCand>(); int inPlaza = 0;
        for (int ei = 0; ei < vEdges.Count; ei++)
        {
            var e = vEdges[ei];
            foreach (int sd in new[] { 1, -1 })
            {
                double acc = 0, next = 2; int seq = 0;
                for (int k = 0; k + 1 < e.pts.Count; k++)
                {
                    double sl = SplineEdge.Dist(e.pts[k], e.pts[k + 1]);
                    if (sl < 1e-6) continue;
                    while (next <= acc + sl)
                    {
                        double tt = (next - acc) / sl; var p = new Vec2(e.pts[k].x + (e.pts[k + 1].x - e.pts[k].x) * tt, e.pts[k].z + (e.pts[k + 1].z - e.pts[k].z) * tt);
                        uint h = (uint)((ei * 7919 + seq * 104729 + (sd > 0 ? 1 : 0)) * 2654435761u) ^ 0x9E3779B9u; h ^= h >> 15; h *= 0x85EBCA6Bu; h ^= h >> 13;
                        int model = houseModels[(int)(h >> 7) % houseModels.Count]; double wM = s.houseModelWidths[model], dM = s.houseModelDepths[model];
                        var (nx0, nz0) = SplineTown.Normal(e.pts, k);
                        double off = Reach(e) + 0.3 + dM / 2, tx = (e.pts[k + 1].x - e.pts[k].x) / sl, tz = (e.pts[k + 1].z - e.pts[k].z) / sl;
                        var c = new Vec2(p.x + tx * wM / 2 + nx0 * sd * off, p.z + tz * wM / 2 + nz0 * sd * off);   // 집 폭 가운데 = 자리 + 폭/2
                        double yaw = Math.Atan2(-nx0 * sd, -nz0 * sd) * 180 / Math.PI;
                        next += wM + 0.4; seq++;   // 다음 자리 = 폭 + 0.4m(붙은 집)
                        if (SplineEdge.Dist(c, seed) > R) continue;
                        if (plazaRect != null && SplineTown.RectOverlap(Rect(c, yaw, wM, dM), plazaRect)) { inPlaza++; continue; }
                        cands.Add(new SplineTown.HouseCand { c = c, yaw = yaw, model = model, scene = 3 });
                    }
                    acc += sl;
                }
            }
        }
        cands = RuleFilter(cands.OrderBy(cd => SplineEdge.Dist(cd.c, seed)).ToList());
        int before = res.houses.Count;
        var why = SplineTown.PlaceDensityHouses(res, s, cands, before + 50);
        var mine = res.houses.Skip(before).ToList();
        int driveOff = 0;
        if (entryOnly) foreach (var h in mine) if (h.hasDrive && h.edge >= 0 && res.edges[h.edge].alley) { h.hasDrive = false; driveOff++; }   // §4.3 골목 집은 진입로 0
        var whyJ = new JObject(); foreach (var kv in why) whyJ[kv.Key] = kv.Value;
        whyJ["in_plaza"] = inPlaza; whyJ["alley_drive_removed"] = driveOff;
        int landS = 0; for (double x = -R; x <= R; x += 2) for (double z = -R; z <= R; z += 2) { if (x * x + z * z > R * R) continue; if (!IsSea(new Vec2(seed.x + x, seed.z + z))) landS++; }
        double landHa = landS * 4 / 10000.0;
        return new JObject
        {
            ["seed"] = new JArray(seed.x, seed.z), ["radius"] = R, ["seedGroundY"] = t.Surface(seed.x, seed.z),
            ["roads"] = new JObject { ["edges"] = vEdges.Count, ["carRoads"] = vEdges.Count(e => !e.alley), ["alleys"] = vEdges.Count(e => e.alley), ["carLength_m"] = vEdges.Where(e => !e.alley).Sum(e => e.Length), ["alleyLength_m"] = vEdges.Where(e => e.alley).Sum(e => e.Length) },
            ["plaza"] = plazaJ,
            ["houses"] = new JObject { ["firstIndex"] = before, ["candidates"] = cands.Count, ["placed"] = mine.Count, ["target"] = "30~50", ["landHa"] = landHa, ["perHa"] = landHa > 0 ? mine.Count / landHa : 0, ["outcome"] = whyJ,
                ["seat"] = new JObject { ["ground"] = mine.Count(x => x.seat == 0), ["wall_down"] = mine.Count(x => x.seat == 1), ["half_basement"] = mine.Count(x => x.seat == 2) }, ["onAlley"] = mine.Count(x => x.alley), ["driveways"] = mine.Count(x => x.hasDrive) },
        };
    }

    static readonly HashSet<string> VillageMainTags = new HashSet<string> { "primary", "secondary", "tertiary", "primary_link", "secondary_link", "tertiary_link", "trunk", "trunk_link" };

    /// <summary>§4.3 입구 길 고르기: 씨앗 반경 r 안 OSM 주거도로(residential·unclassified·living_street) 토막을 같은 좌표 점으로 묶어 덩어리를 만들고,
    /// SS163 에 가장 가까운 점을 가진 덩어리의 way 들을 입구 길로(코어가 SS163 과 끊긴 그 덩어리를 잇기 길로 잇는다).</summary>
    static JObject PickVillageEntry(JObject nd, Vec2 seed, double r, List<long> outWays)
    {
        var resTags = new HashSet<string> { "residential", "unclassified", "living_street" };
        var ss = nd["segments"].Where(sg => sg["ref"].ToString().Contains("SS163")).SelectMany(sg => sg["points_m"].Select(q => new Vec2(q[0].Value<double>(), q[1].Value<double>()))).ToList();
        (long, long) K(Vec2 p) => ((long)Math.Round(p.x * 100), (long)Math.Round(p.z * 100));
        var parent = new Dictionary<(long, long), (long, long)>();
        (long, long) Find((long, long) a) { while (!parent[a].Equals(a)) a = parent[a] = parent[parent[a]]; return a; }
        var segs = new List<(long way, List<Vec2> pts)>();
        foreach (var sg in nd["segments"])
        {
            if (!resTags.Contains(sg["highway"].ToString())) continue;
            var pts = sg["points_m"].Select(q => new Vec2(q[0].Value<double>(), q[1].Value<double>())).Where(q => SplineEdge.Dist(q, seed) <= r).ToList();
            if (pts.Count < 2) continue;
            segs.Add((sg["osm_way"].Value<long>(), pts));
            foreach (var p in pts) if (!parent.ContainsKey(K(p))) parent[K(p)] = K(p);
            for (int k = 1; k < pts.Count; k++) { var a = Find(K(pts[k - 1])); var b = Find(K(pts[k])); if (!a.Equals(b)) parent[a] = b; }
        }
        var comps = segs.GroupBy(x => Find(K(x.pts[0]))).Select(g => new { ways = g.Select(x => x.way).Distinct().ToList(), pts = g.SelectMany(x => x.pts).ToList(), len = g.Sum(x => SplineEdge.PolyLength(x.pts)) })
            .Select(c => new { c.ways, c.len, dSS = c.pts.Min(p => ss.Min(q => SplineEdge.Dist(p, q))), dSeed = c.pts.Min(p => SplineEdge.Dist(p, seed)) }).OrderBy(c => c.dSS).ToList();
        var arr = new JArray(comps.Select(c => new JObject { ["ways"] = new JArray(c.ways), ["length_m"] = c.len, ["distToSS163_m"] = c.dSS, ["distToSeed_m"] = c.dSeed }));
        if (comps.Count > 0) outWays.AddRange(comps[0].ways);
        return new JObject { ["rule"] = "반경 안 주거도로 덩어리 중 SS163 최근접", ["radius"] = r, ["picked"] = comps.Count > 0 ? new JArray(comps[0].ways) : null, ["components"] = arr };
    }

    /// <summary>끝점 좌표가 같고 그 점에 선 끝이 둘뿐이면 한 줄로 이어 붙인다(OSM steps 가 3~6m 토막으로 쪼개져 있음 — 한 계단 = 한 줄).</summary>
    static List<List<Vec2>> ChainPolylines(List<List<Vec2>> lines)
    {
        (long, long) K(Vec2 p) => ((long)Math.Round(p.x * 100), (long)Math.Round(p.z * 100));
        var ls = lines.Select(l => new List<Vec2>(l)).ToList();
        for (bool merged = true; merged;)
        {
            merged = false;
            var deg = new Dictionary<(long, long), int>();
            foreach (var l in ls) foreach (var e in new[] { K(l[0]), K(l[l.Count - 1]) }) deg[e] = deg.TryGetValue(e, out var n) ? n + 1 : 1;
            for (int i = 0; i < ls.Count && !merged; i++)
                for (int j = 0; j < ls.Count && !merged; j++)
                {
                    if (i == j) continue;
                    var a = ls[i]; var b = ls[j]; var ka = K(a[a.Count - 1]);
                    if (deg[ka] != 2) continue;
                    if (ka.Equals(K(b[0]))) a.AddRange(b.Skip(1));
                    else if (ka.Equals(K(b[b.Count - 1]))) { var rb = new List<Vec2>(b); rb.Reverse(); a.AddRange(rb.Skip(1)); }
                    else continue;
                    ls.RemoveAt(j); merged = true;
                }
        }
        return ls;
    }

    /// <summary>§4.3 수용 기준 측정: 반경 안 차도(입구 길) 개수·길이, 마을 근처 잇기 길, 골목·계단 갈래·길이, 집 정면 → 골목·계단·입구 길 거리, 광장 차 도달(그래프 BFS), 주행 거리·시간 추정.</summary>
    static void VillageEntryStats(SplineTownResult res, JObject vj, Vec2 seed, double R, double roadR, List<List<Vec2>> vStairs, int stairTotal, JObject entryPick, Func<double, double, double> origH)
    {
        int Near(SplineEdge e, double r) => e.pts.Count(q => SplineEdge.Dist(q, seed) <= r);
        double LenIn(SplineEdge e, double r) { double L = 0; for (int k = 1; k < e.pts.Count; k++) if (SplineEdge.Dist(e.pts[k], seed) <= r && SplineEdge.Dist(e.pts[k - 1], seed) <= r) L += SplineEdge.Dist(e.pts[k - 1], e.pts[k]); return L; }
        double SegD(Vec2 p, Vec2 a, Vec2 b) { double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz; double u = L2 > 1e-9 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0; return SplineEdge.Dist(p, new Vec2(a.x + dx * u, a.z + dz * u)); }
        double PolyD(Vec2 p, List<Vec2> pl) { double b = 1e9; for (int k = 0; k + 1 < pl.Count; k++) b = Math.Min(b, SegD(p, pl[k], pl[k + 1])); return b; }
        var carIn = res.edges.Where(e => !e.alley && !e.highway.StartsWith("primary") && Near(e, R) > 0).ToList();
        var conn = res.edges.Where(e => e.connector && Near(e, roadR + 100) > 0).ToList();
        var alleys = res.edges.Where(e => e.alley && Near(e, roadR) > 0).ToList();
        var paths = alleys.Select(e => e.pts).Concat(vStairs).Concat(carIn.Select(e => e.pts)).ToList();
        int first = vj["houses"]?["firstIndex"]?.Value<int>() ?? res.houses.Count;
        var vh = res.houses.Skip(first).ToList();
        var front = vh.Select(h => { double rr = h.yawDeg * Math.PI / 180; var fp = new Vec2(h.center.x + Math.Sin(rr) * h.depth / 2, h.center.z + Math.Cos(rr) * h.depth / 2); return paths.Count > 0 ? paths.Min(pl => PolyD(fp, pl)) : 1e9; }).ToList();
        // 차 그래프: 골목 아닌 길의 끝점 노드, 길이 가중
        var adj = new Dictionary<int, List<(int to, double w)>>();
        foreach (var e in res.edges) { if (e.alley) continue; foreach (var (u, v) in new[] { (e.a, e.b), (e.b, e.a) }) { if (!adj.TryGetValue(u, out var l)) adj[u] = l = new List<(int, double)>(); l.Add((v, e.Length)); } }
        Dictionary<int, double> Dijkstra(IEnumerable<(int n, double d)> src)
        {
            var dist = new Dictionary<int, double>(); var pq = new SortedSet<(double d, int n)>();
            foreach (var (n, d) in src) if (!dist.TryGetValue(n, out var o) || d < o) { if (dist.ContainsKey(n)) pq.Remove((o, n)); dist[n] = d; pq.Add((d, n)); }
            while (pq.Count > 0)
            {
                var (d, u) = pq.Min; pq.Remove(pq.Min);
                if (!adj.TryGetValue(u, out var l)) continue;
                foreach (var (v, w) in l) { double nd2 = d + w; if (!dist.TryGetValue(v, out var o) || nd2 < o) { if (dist.ContainsKey(v)) pq.Remove((o, v)); dist[v] = nd2; pq.Add((nd2, v)); } }
            }
            return dist;
        }
        (int ei, double arc) Foot(Vec2 p)
        {
            int be = -1; double bd = 1e18, ba = 0;
            for (int i = 0; i < res.edges.Count; i++) { var e = res.edges[i]; if (e.alley) continue; double acc = 0; for (int k = 0; k + 1 < e.pts.Count; k++) { double sl = SplineEdge.Dist(e.pts[k], e.pts[k + 1]); double d = SegD(p, e.pts[k], e.pts[k + 1]); if (d < bd) { bd = d; be = i; ba = acc + sl / 2; } acc += sl; } }
            return (be, ba);
        }
        IEnumerable<(int, double)> Src(int ei, double arc) { var e = res.edges[ei]; return new[] { (e.a, arc), (e.b, Math.Max(0, e.Length - arc)) }; }
        var pj = vj["plaza"] as JObject; int host = pj?["hostEdge"]?.Value<int>() ?? -1; double hostArc = pj?["hostArc_m"]?.Value<double>() ?? 0;
        // 콘카 입구 = 광장에서 차 그래프로 가장 가까운 SS163 노드
        var ssNodes = new HashSet<int>(); foreach (var e in res.edges) if (!e.alley && e.highway.StartsWith("primary")) { ssNodes.Add(e.a); ssNodes.Add(e.b); }
        JObject drive = null; bool reach = false;
        if (host >= 0 && !res.edges[host].alley)
        {
            var fromPlaza = Dijkstra(Src(host, hostArc));
            var gate = ssNodes.Where(n => fromPlaza.ContainsKey(n)).OrderBy(n => fromPlaza[n]).Select(n => (int?)n).FirstOrDefault();
            reach = gate.HasValue;
            if (reach && res.plaza != null)
            {
                var af = Foot(res.plaza.c); var fromAmalfi = Dijkstra(Src(af.ei, af.arc));
                double toGate = fromAmalfi.TryGetValue(gate.Value, out var g) ? g : -1, entry = fromPlaza[gate.Value];
                drive = new JObject { ["gateNode"] = new JArray(res.nodes[gate.Value].x, res.nodes[gate.Value].z), ["amalfiPlaza_to_concaGate_m"] = toGate, ["concaGate_to_villagePlaza_m"] = entry,
                    ["estimate_s"] = new JObject { ["rule"] = "추정: SS163 15m/s(가설) · 입구 길 10m/s(가설), 정지·감속 없음", ["amalfi_to_gate"] = toGate / 15, ["gate_to_plaza"] = entry / 10, ["total"] = toGate / 15 + entry / 10, ["amalfi_to_gate_min"] = toGate / 15 / 60, ["total_min"] = (toGate / 15 + entry / 10) / 60 } };
            }
        }
        vj["entry"] = new JObject
        {
            ["pick"] = entryPick,
            ["carRoadsInRadius"] = carIn.Count, ["carRoadsInRadiusLength_m"] = carIn.Sum(e => LenIn(e, R)), ["carRoadsTotalLength_m"] = carIn.Sum(e => e.Length),
            ["carRoads"] = new JArray(carIn.Select(e => new JObject { ["highway"] = e.highway, ["connector"] = e.connector, ["length_m"] = e.Length, ["inRadius_m"] = LenIn(e, R), ["y0"] = e.y[0], ["y1"] = e.y[e.y.Count - 1] })),
            ["connectorsNearVillage"] = conn.Count, ["connectors"] = new JArray(conn.Select(e => new JObject { ["from"] = new JArray(e.pts[0].x, e.pts[0].z), ["to"] = new JArray(e.pts[e.pts.Count - 1].x, e.pts[e.pts.Count - 1].z), ["length_m"] = e.Length })),
            ["alleys"] = alleys.Count, ["alleyLength_m"] = alleys.Sum(e => e.Length),
            ["alleySteep"] = new JObject { ["rule"] = "원래 땅(깎기 전) 종단이 12% 넘는 골목 구간 — 스펙 §4.3 은 계단으로 바꾸라 함. 지금은 12% 종단 + 깎기/메우기로 둠(미구현)",
                ["over12pct_m"] = alleys.Sum(e => { double L = 0; for (int k = 1; k < e.pts.Count; k++) { double sl = SplineEdge.Dist(e.pts[k - 1], e.pts[k]); if (sl > 1e-6 && Math.Abs(origH(e.pts[k].x, e.pts[k].z) - origH(e.pts[k - 1].x, e.pts[k - 1].z)) / sl > 0.12) L += sl; } return L; }),
                ["maxCutFill_m"] = alleys.Count == 0 ? 0 : alleys.Max(e => Enumerable.Range(0, e.pts.Count).Max(k => Math.Abs(e.y[k] - origH(e.pts[k].x, e.pts[k].z)))) },
            ["villageStairs"] = vStairs.Count, ["villageStairLength_m"] = vStairs.Sum(SplineEdge.PolyLength),
            ["alleyPlusStairLength_m"] = alleys.Sum(e => e.Length) + vStairs.Sum(SplineEdge.PolyLength), ["stairsTotalMap"] = stairTotal,
            ["houses"] = vh.Count, ["houseFrontToPath_max_m"] = front.Count > 0 ? front.Max() : 0, ["housesOver15m"] = front.Count(d => d > 15),
            ["plazaHostIsCarRoad"] = host >= 0 && !res.edges[host].alley, ["plazaCarReachable_BFS"] = reach, ["drive"] = drive,
        };
    }

    static bool NearPolyline(Vec2 p, List<Vec2> pl, double r)
    {
        for (int k = 0; k + 1 < pl.Count; k++) if (SegDist(p, pl[k], pl[k + 1]) <= r) return true;
        return false;
    }

    static double SegDist(Vec2 p, Vec2 a, Vec2 b)
    {
        double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz;
        double u = L2 > 1e-12 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0;
        return SplineEdge.Dist(p, new Vec2(a.x + dx * u, a.z + dz * u));
    }

    static Material Mat(string name)
    {
        var src = TownGenScenes.FarFog(name); if (src == null) return null;
        string path = $"Assets/Data/TownGen/Materials/{name}_Structure.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); } else m.CopyPropertiesFromMaterial(src);
        if (m.HasProperty("_FogStart")) m.SetFloat("_FogStart", 1500f);
        if (m.HasProperty("_FogEnd")) m.SetFloat("_FogEnd", 6000f);
        EditorUtility.SetDirty(m); return m;
    }

    /// <summary>조각 10 도현이 볼 지점: 그 구역에서 반경 40m 안 건물이 가장 많은 건물 중심(x,z, 그 수).</summary>
    static JArray DensestPoint(List<TownGenOsmBuildings.B> bs, string zone)
    {
        var z = bs.Where(b => b.zone == zone).ToList(); if (z.Count == 0) return null;
        var best = z.OrderByDescending(b => z.Count(o => SplineEdge.Dist(o.c, b.c) <= 40)).First();
        return new JArray(Math.Round(best.c.x), Math.Round(best.c.z), z.Count(o => SplineEdge.Dist(o.c, best.c) <= 40));
    }

    static void Shot(Camera cam, Vector3 pos, Quaternion rot, string path, int pw = 1280, int ph = 720)
    {
        cam.transform.SetPositionAndRotation(pos, rot);
        var rt = new RenderTexture(pw, ph, 24); var prev = cam.targetTexture; cam.targetTexture = rt; cam.aspect = (float)pw / ph; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = prev; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
}
