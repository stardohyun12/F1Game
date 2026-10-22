using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PitCrew.TownGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 곡선 큰길 마을 1차(스펙 docs/superpowers/specs/2026-10-20-spline-town-design.md, D-111) — 길만.
/// 메뉴 PitCrew/TownGen/Build Spline Town → Assets/Scenes/TownGen/TownGen_Spline.unity
/// 지형(DEM × 배율 0.4, 2m 메시, 길 밑 깎기/메우기) + 큰길 단면(아스팔트·중앙 점선·연석·보도·어깨) + 교차점 원판 + 옹벽 + 바다.
/// 집·골목은 2차. 기존 TownGen 씬 4개와 메인 ComplexTrack 은 건드리지 않는다(빌드 뒤 원래 씬을 다시 연다).
/// 로직은 TownGen.Core/SplineTown(엔진 독립, dotnet test). 여기선 메시·씬·캡처만.
/// </summary>
public static class TownGenSpline
{
    const string ScenePath = "Assets/Scenes/TownGen/TownGen_Spline.unity";
    static string res0Variant = "";

    [MenuItem("PitCrew/TownGen/Build Spline Town")]
    public static void BuildMenu() => Debug.Log(Build());

    static SplineTownSettings LoadSettings()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TownGenConfig>(TownGenRunner.ConfigPath);
        return cfg != null && cfg.spline != null ? cfg.spline.Clone() : new SplineTownSettings();
    }

    public static string Build() => Build(ScenePath, "Spline");

    /// <summary>scenePath 에 저장, 캡처는 shots/{shotPrefix}_top|slope|drive.png. 4차부터 도현이 보는 씬을 덮지 않게 다른 경로로 만든다.</summary>
    public static string Build(string scenePath, string shotPrefix)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var s = LoadSettings();
        var outDir = Path.Combine(TownGenRunner.ToolsDir, "out", shotPrefix == "Spline" ? "scene_spline" : "scene_" + shotPrefix.ToLowerInvariant());
        Directory.CreateDirectory(outDir);

        // 입력: 공통 고도맵(실제 높이) + OSM 원본 태그 포함 도로망
        string sfx = string.IsNullOrEmpty(s.inputVariant) ? "" : "_" + s.inputVariant;
        var hd = JObject.Parse(File.ReadAllText(Path.Combine(TownGenRunner.ToolsDir, "out", $"heights_480x400{sfx}.json")));
        var hf = new GridHeightField(hd["heights_m"].Select(v => v.Value<float>()).ToArray(), hd["nx"].Value<int>(), hd["nz"].Value<int>(), hd["step_m"].Value<double>(), hd["step_m"].Value<double>());
        var nd = JObject.Parse(File.ReadAllText(Path.Combine(TownGenRunner.ToolsDir, "out", $"osm_network{sfx}.json")));
        res0Variant = s.inputVariant;
        var ways = new List<OsmWay>();
        foreach (var sg in nd["segments"])
        {
            var w = new OsmWay { highway = sg["highway"]?.ToString() ?? "", wayId = sg["osm_way"]?.Value<long>() ?? 0 };
            foreach (var p in sg["points_m"]) w.points.Add(new Vec2(p[0].Value<double>(), p[1].Value<double>()));
            ways.Add(w);
        }
        if (ways.All(w => w.highway == "")) throw new InvalidDataException("osm_network.json 에 highway 태그 없음 — py -X utf8 tools/town-gen/osm_to_network.py 다시 실행");

        // 건물 모델 치수를 먼저 재서 코어에 넘긴다(모델 먼저 → 필지 폭 = 모델 폭 + 틈 = 붙은 줄집)
        var models = new[] { "CoastalHouse", "CoastalHouseB", "CoastalHouseC", "CoastalHouseD", "CoastalHouseE", "CoastalHouseF", "CornerShop", "CornerShopB", "CornerShopC", "RepairGarage", "RepairGarageB" }
            .Select(n => (name: n, go: AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/LofiPilot/{n}.prefab"))).Where(m => m.go != null)
            .Select(m => { var b = PrefabBounds(m.go); return (m.name, m.go, w: b.size.x, d: b.size.z, cx: b.center.x, cz: b.center.z); }).ToList();
        s.houseModelWidths = models.Select(m => m.w).ToArray();
        s.houseModelDepths = models.Select(m => m.d).ToArray();
        s.houseModelCenterX = models.Select(m => m.cx).ToArray();
        s.houseModelCenterZ = models.Select(m => m.cz).ToArray();
        s.houseModelNames = models.Select(m => m.name).ToArray();
        var res = SplineTown.Build(ways, hf, s);
        res.report.Metric("input", $"변형 \"{res0Variant}\" (heights_480x400{sfx}.json · osm_network{sfx}.json) · 높이 배율 {s.heightScale}");
        var again = SplineTown.Build(ways, hf, s);   // M8 결정성
        res.report.Check("M8-hash", res.report.MetricValue("hash") == again.report.MetricValue("hash"), $"두 번 생성 해시 {res.report.MetricValue("hash")} / {again.report.MetricValue("hash")}");

        // ---- 씬
        string prevScene = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("열린 씬에 저장 안 한 변경이 있음 — 중단(메인 씬 보호)");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var root = new GameObject("TownGen_Spline");
        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null) { light.transform.rotation = Quaternion.Euler(55f, -35f, 0f); light.intensity = 1.2f; light.shadows = LightShadows.Soft; light.color = new Color(1f, .96f, .88f); }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .64f, .66f);

        var mGround = TownGenScenes.FarFog("Ground");
        var mAsphalt = TownGenScenes.FarFog("Asphalt");
        var mPave = TownGenScenes.FarFog("Pavement");
        var mPaint = TownGenScenes.FarFog("RoadPaint");
        var mSea = TownGenScenes.FarFog("Sea");
        var mWall = TownGenScenes.FarFog("Pavement"); // 석축 placeholder: 기존 돌 포장 재질 (새 에셋 0). 건물 재질은 아틀라스 UV 라 벽에 검게 나옴

        int tris = 0;
        // 지형: 해수면 바로 위 낮은 삼각형은 모래(기존 키트 항구 바닥 재질) — 물과 잔디 사이 전이(그래픽 결함 3)
        var mSand = TownGenScenes.FarFog("ZoneGroundHarbor");
        var tGo = Obj("Terrain", root.transform, TerrainMesh(res.terrain, s.seaLevelMeters + 1.5f), mGround);
        tGo.GetComponent<MeshRenderer>().sharedMaterials = new[] { mGround, mSand ? mSand : mGround };
        tris += tGo.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        // 땅 둘레 바깥 지형(160~200m): 가장자리 높이를 이어받아 회색 허공·잘린 지도판 인상 제거. 남쪽은 바다 밑으로 내려감.
        var ring = Obj("Surroundings", root.transform, SurroundMesh(res.terrain, s, s.seaLevelMeters + 1.5f), mGround);
        ring.GetComponent<MeshRenderer>().sharedMaterials = new[] { mGround, mSand ? mSand : mGround };
        tris += ring.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;

        // 바다: 해수면 높이의 넓은 평면 하나. 해안선은 지형이 해수면을 지나는 곳에서 저절로 생긴다(직선 모서리 없음).
        // 땅 안 해수면 이하 점은 전부 남쪽 바다와 이어져 있음(실측 348/348) → 연못 안 생김.
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Sea"; sea.transform.SetParent(root.transform, false);
        sea.transform.position = new Vector3(s.boundsXMeters / 2, s.seaLevelMeters, s.boundsZMeters / 2);
        sea.transform.localScale = new Vector3(300, 1, 300);
        UnityEngine.Object.DestroyImmediate(sea.GetComponent<Collider>());
        if (mSea) sea.GetComponent<Renderer>().sharedMaterial = mSea;

        // 길
        var asphalt = new MeshBuilder(); var pave = new MeshBuilder(); var shoulder = new MeshBuilder(); var paint = new MeshBuilder(); var wall = new MeshBuilder();
        var alleyMb = new MeshBuilder();
        // 골목 입구(앞 4m) — 큰길 어깨를 비울 자리
        var mouths = res.Alleys.Select(al => { var mp = new List<Vec2> { al.pts[0] }; double acc = 0; for (int i = 1; i < al.pts.Count && acc < 4; i++) { acc += SplineEdge.Dist(al.pts[i - 1], al.pts[i]); mp.Add(al.pts[i]); } return mp; }).ToList();
        // 다른 큰길 노면(끊은 구간만) — 어깨가 그 위로 넘어가지 않게
        var mains = res.edges.Where(x => !x.alley).ToList();
        var mainPolys = mains.Select(x => ClippedPolyline(x)).ToList();
        foreach (var e in res.edges)
        {
            if (e.alley) AlleySection(e, res, s, alleyMb, shoulder);
            else Section(e, res, s, asphalt, pave, shoulder, paint, mouths, mainPolys, mains.IndexOf(e));
        }
        foreach (var jl in res.junctionLayouts) JunctionPatch(jl, res, s, asphalt, pave, shoulder, mainPolys);
        var drive = new MeshBuilder();
        foreach (var dh in res.houses) if (dh.hasDrive) Driveway(dh, res, s, drive);
        foreach (var w in res.walls) WallQuad(w, wall);
        var roads = new GameObject("Roads"); roads.transform.SetParent(root.transform, false);
        tris += Obj("Asphalt", roads.transform, asphalt.Build("Asphalt"), mAsphalt).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("Sidewalk_Curb", roads.transform, pave.Build("Sidewalk"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("Shoulder", roads.transform, shoulder.Build("Shoulder"), mGround).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("CenterLine", roads.transform, paint.Build("CenterLine"), mPaint).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("RetainingWalls", root.transform, wall.Build("Walls"), mWall).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("Alleys", roads.transform, alleyMb.Build("Alleys"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += Obj("Driveways", roads.transform, drive.Build("Driveways"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        tris += PlazaMeshes(res, root.transform, mPave, mWall);

        tris += HouseObjects(res, scene, root.transform, models.Select(m => (m.name, m.go)).ToList(), mWall);
        res.report.Metric("scene_triangles", tris.ToString());

        new GameObject("_Credit: © OpenStreetMap contributors (ODbL) · produced using Copernicus WorldDEM-30").transform.SetParent(root.transform, false);

        // 확인용 캡처 3장: 위 · 비탈 · 운전 높이
        var cam = Camera.main;
        cam.farClipPlane = 3000;
        var shots = Path.Combine(TownGenRunner.ToolsDir, "out", "shots");
        Directory.CreateDirectory(shots);
        var longest = res.edges.OrderByDescending(e => e.Length).First();
        int mid = longest.pts.Count / 2;
        Vector3 P(SplineEdge e, int i, float up) => new Vector3((float)e.pts[i].x, (float)e.y[i] + up, (float)e.pts[i].z);
        // 위: 땅 전체 내려다봄
        Shot(cam, new Vector3(s.boundsXMeters / 2, 470, s.boundsZMeters / 2), Quaternion.Euler(90, 0, 0), Path.Combine(shots, shotPrefix + "_top.png"));
        // 비탈: 가장 높은 큰길 점을 남쪽 아래에서 올려다봄
        SplineEdge he = null; int hi = 0; double hy = double.MinValue;
        foreach (var e in res.edges) for (int i = 0; i < e.pts.Count; i++) if (e.y[i] > hy) { hy = e.y[i]; he = e; hi = i; }
        var hp = P(he, hi, 0);
        // 비탈: 남쪽 바다 위 낮은 각도에서 비탈 전체(굽이 길·옹벽)를 올려다봄. 가장 높은 큰길 점 쪽으로.
        var from = new Vector3(Mathf.Lerp(s.boundsXMeters / 2, hp.x, 0.5f), 70, -70);
        var lookAt = new Vector3(Mathf.Lerp(s.boundsXMeters / 2, hp.x, 0.5f), hp.y * 0.5f, s.boundsZMeters * 0.55f);
        Shot(cam, from, Quaternion.LookRotation((lookAt - from).normalized), Path.Combine(shots, shotPrefix + "_slope.png"));
        // 운전 높이: 가장 긴 큰길 가운데, 차로 위 1.6m 에서 진행 방향
        int ia = Math.Max(0, mid - 4), ib = Math.Min(longest.pts.Count - 1, mid + 4);
        var dir = (P(longest, ib, 0) - P(longest, ia, 0)).normalized;
        var right = Vector3.Cross(Vector3.up, dir).normalized;
        var eye = P(longest, ia, 1.6f) + right * 1.5f;
        Shot(cam, eye, Quaternion.LookRotation(dir + Vector3.down * 0.05f), Path.Combine(shots, shotPrefix + "_drive.png"));
        if (res.plaza != null)
        {
            var pz = res.plaza; var pc = new Vector3((float)pz.c.x, (float)pz.y, (float)pz.c.z);
            string pre = shotPrefix.StartsWith("Spline_") ? shotPrefix.Substring(7) : shotPrefix;
            Shot(cam, pc + new Vector3(0, 110, -35), Quaternion.LookRotation((pc - (pc + new Vector3(0, 110, -35))).normalized), Path.Combine(shots, pre + "_plaza_top.png"));
            var tc = new Vector3((float)pz.towerC.x, (float)pz.Y(pz.towerC), (float)pz.towerC.z);
            var away = (pc - tc); away.y = 0; away = away.normalized;
            // 광장 안, 종탑에서 가장 먼 모서리에서 1.5m 안쪽 — 광장 바닥·상점·종탑이 한 화면에
            var far = pz.Corners().OrderByDescending(q => SplineEdge.Dist(q, pz.towerC)).First();
            var inward = new Vector3((float)(pz.c.x - far.x), 0, (float)(pz.c.z - far.z)).normalized;
            var eyeP = new Vector3((float)far.x, 0, (float)far.z) + inward * 1.5f; eyeP.y = (float)pz.Y(new Vec2(eyeP.x, eyeP.z)) + 1.6f;
            Shot(cam, eyeP, Quaternion.LookRotation((tc + Vector3.up * 5f - eyeP).normalized), Path.Combine(shots, pre + "_plaza_drive.png"));
        }
        cam.transform.position = new Vector3(s.boundsXMeters / 2, 140, -170); cam.transform.LookAt(new Vector3(s.boundsXMeters / 2, 30, 200));

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes", "TownGen"));
        EditorSceneManager.SaveScene(scene, scenePath);
        if (!string.IsNullOrEmpty(prevScene)) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);

        sw.Stop();
        res.report.Metric("total_ms", sw.ElapsedMilliseconds.ToString());
        File.WriteAllText(Path.Combine(outDir, "report.txt"), res.report.ToText(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "report.json"), res.report.ToJson().ToString(), new UTF8Encoding(false));
        // 측정용 JSON(§10: QA·아트는 에디터 없이 이걸로 판정) — 집 앉히기·진입로·보고 수치
        {
            var houses = new JArray();
            foreach (var h in res.houses)
            {
                var corners = new JArray(); foreach (var c in h.Corners()) corners.Add(new JArray(c.x, c.z));
                houses.Add(new JObject
                {
                    ["center"] = new JArray(h.center.x, h.center.z), ["pivot"] = new JArray(h.pivot.x, h.pivot.z), ["yawDeg"] = h.yawDeg, ["width"] = h.width, ["depth"] = h.depth,
                    ["model"] = h.model >= 0 && h.model < models.Count ? models[h.model].name : "", ["row"] = h.row, ["alley"] = h.alley, ["corners"] = corners,
                    ["floorY"] = h.plinthY, ["terrainMin"] = h.terrainMin, ["terrainMax"] = h.terrainMax,
                    ["seat"] = h.seat == 0 ? "ground" : h.seat == 1 ? "wall_down" : "half_basement", ["seatBottom"] = h.seatBottom,
                    ["separatePlinth"] = false, ["shop"] = h.shop,
                    ["hasDrive"] = h.hasDrive, ["driveSteps"] = h.driveSteps, ["driveA"] = new JArray(h.driveA.x, h.driveAY, h.driveA.z), ["driveB"] = new JArray(h.driveB.x, h.driveBY, h.driveB.z),
                });
            }
            var measure = new JObject { ["scene"] = scenePath, ["generated"] = DateTime.Now.ToString("s"), ["report"] = res.report.ToJson(), ["houses"] = houses };
            if (res.plaza != null)
            {
                var pz = res.plaza; var pcs = new JArray(); foreach (var q in pz.Corners()) pcs.Add(new JArray(q.x, pz.Y(q), q.z));
                measure["plaza"] = new JObject
                {
                    ["center"] = new JArray(pz.c.x, pz.y, pz.c.z), ["length"] = pz.length, ["width"] = pz.width, ["yawDeg"] = pz.yawDeg, ["corners"] = pcs,
                    ["floorGrade"] = Math.Sqrt(pz.gx * pz.gx + pz.gz * pz.gz), ["junctionNode"] = new JArray(res.nodes[pz.node].x, res.nodes[pz.node].z), ["arms"] = pz.arms,
                    ["slope30"] = pz.slope30, ["seaVisible"] = pz.seeSea, ["seaVisibleMethod"] = "19 rays ±45° toward nearest sea cell, 2m step ≤150m, eye 1.6m, terrain only (buildings not counted)", ["coastRoadDist"] = pz.coastDist, ["relaxed"] = pz.relaxed, ["roadside"] = pz.roadside, ["terraced"] = pz.terraced, ["tierHeightDiffAtCenter"] = pz.terraced ? pz.YTier(pz.c, true) - pz.YTier(pz.c, false) : 0.0,
                    ["tower"] = new JObject { ["center"] = new JArray(pz.towerC.x, pz.Y(pz.towerC), pz.towerC.z), ["size"] = pz.towerSize, ["height"] = pz.towerHeight, ["yawDeg"] = pz.towerYaw, ["placeholder"] = true },
                    ["shops"] = res.houses.Count(h => h.shop),
                };
            }
            File.WriteAllText(Path.Combine(outDir, "measure.json"), measure.ToString(), new UTF8Encoding(false));
        }
        return $"[TownGen] Spline: {scenePath} 저장\n" + res.report.ToText();
    }

    /// <summary>마을 중심 광장(§11.3): 돌 포장(평면 높이, 테라스면 두 단 + 석축·계단) + 종탑 자리표시 상자. 반환 = 삼각형 수.</summary>
    internal static int PlazaMeshes(SplineTownResult res, Transform root, Material mPave, Material mWall)
    {
        int tris = 0;
        if (res.plaza != null)
        {
            var pz = res.plaza; var pmb = new MeshBuilder(); var cs = pz.Corners();
            int nu = 2 * Mathf.CeilToInt((float)pz.length / 4), nv = 2 * Mathf.CeilToInt((float)pz.width / 4);   // 짝수 — 단 경계가 격자선에 오게
            Vec2 Q(float a, float b) => new Vec2(cs[2].x + (cs[1].x - cs[2].x) * a + (cs[3].x - cs[2].x) * b, cs[2].z + (cs[1].z - cs[2].z) * a + (cs[3].z - cs[2].z) * b);
            for (int a = 0; a < nu; a++) for (int b = 0; b < nv; b++)
            {
                bool up = pz.Upper(Q((a + 0.5f) / nu, (b + 0.5f) / nv));
                Vector3 PP(float u, float v) { var q = Q(u, v); return new Vector3((float)q.x, (float)pz.YTier(q, up) + 0.02f, (float)q.z); }
                pmb.Quad(PP((float)a / nu, (float)b / nv), PP((float)(a + 1) / nu, (float)b / nv), PP((float)(a + 1) / nu, (float)(b + 1) / nv), PP((float)a / nu, (float)(b + 1) / nv), Vector3.up);
            }
            if (pz.terraced)
            {
                // 두 단 사이 석축(가운데 선, 아래 단 높이 → 위 단 높이) + 계단 1곳(가운데, 폭 3m, 아래 단 쪽으로 내려옴)
                var ax = new Vector3((float)pz.tierAxis.x, 0, (float)pz.tierAxis.z); var px = new Vector3(-ax.z, 0, ax.x);
                float halfSpan = (float)(Math.Abs(pz.tierAxis.x * Math.Sin(pz.yawDeg * Math.PI / 180) + pz.tierAxis.z * Math.Cos(pz.yawDeg * Math.PI / 180)) > 0.5 ? pz.width : pz.length) / 2;
                var c3 = new Vector3((float)pz.c.x, 0, (float)pz.c.z); var wmb = new MeshBuilder();
                int segs = 8;
                for (int k = 0; k < segs; k++)
                {
                    float t0 = -halfSpan + 2 * halfSpan * k / segs, t1 = -halfSpan + 2 * halfSpan * (k + 1) / segs;
                    var a0 = c3 + px * t0; var a1 = c3 + px * t1; var q0 = new Vec2(a0.x, a0.z); var q1 = new Vec2(a1.x, a1.z);
                    float l0 = (float)pz.YTier(q0, false) + 0.02f, l1 = (float)pz.YTier(q1, false) + 0.02f, u0 = (float)pz.YTier(q0, true) + 0.02f, u1 = (float)pz.YTier(q1, true) + 0.02f;
                    wmb.Quad(new Vector3(a0.x, Mathf.Min(l0, u0) - 0.3f, a0.z), new Vector3(a1.x, Mathf.Min(l1, u1) - 0.3f, a1.z), new Vector3(a1.x, Mathf.Max(l1, u1), a1.z), new Vector3(a0.x, Mathf.Max(l0, u0), a0.z), -ax, true);
                }
                float yl = (float)pz.YTier(pz.c, false) + 0.02f, yu = (float)pz.YTier(pz.c, true) + 0.02f, dh = yu - yl;
                if (Mathf.Abs(dh) > 0.1f)
                {
                    int ns = Mathf.CeilToInt(Mathf.Abs(dh) / 0.17f); float tread = 0.3f, yawS = Mathf.Atan2(ax.x, ax.z) * Mathf.Rad2Deg;
                    for (int k = 0; k < ns; k++)
                    {
                        // 위 단 가장자리에서 아래 단 쪽으로 한 디딤씩
                        var m = c3 - ax * (tread * (k + 0.5f));
                        float top = yu - dh * (k + 1) / ns;
                        Box(wmb, new Vector3(m.x, 0, m.z), yawS, 3f, tread, yl - 0.3f, top);
                    }
                }
                tris += Obj("PlazaTerraceWall", root, wmb.Build("PlazaTerraceWall"), mWall).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            }
            // 가장자리 연석(0.15m) — 둘레 지형이 광장보다 낮은 쪽에서 판 끝이 뜨지 않게 0.6m 아래까지
            for (int k = 0; k < 4; k++) { var p0 = new Vector3((float)cs[k].x, 0, (float)cs[k].z); var p1 = new Vector3((float)cs[(k + 1) % 4].x, 0, (float)cs[(k + 1) % 4].z); float y0 = (float)pz.Y(cs[k]) + 0.02f, y1 = (float)pz.Y(cs[(k + 1) % 4]) + 0.02f; var o = (p0 + p1) / 2 - new Vector3((float)pz.c.x, 0, (float)pz.c.z); pmb.Quad(new Vector3(p0.x, y0, p0.z), new Vector3(p1.x, y1, p1.z), new Vector3(p1.x, y1 - 0.6f, p1.z), new Vector3(p0.x, y0 - 0.6f, p0.z), o); }
            tris += Obj("Plaza", root, pmb.Build("Plaza"), mPave).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            var tmb = new MeshBuilder(); float ty = (float)pz.Y(pz.towerC);
            Box(tmb, new Vector3((float)pz.towerC.x, 0, (float)pz.towerC.z), (float)pz.towerYaw, (float)pz.towerSize, (float)pz.towerSize, ty - 0.5f, ty + (float)pz.towerHeight);
            tris += Obj("BellTower_placeholder", root, tmb.Build("BellTower"), mWall).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }

        return tris;
    }

    /// <summary>집(§2.6·§11.1): 프리팹 놓기 + 벽 내림·반지하 덩어리. M4-float 검사를 보고서에 남긴다. 반환 = 삼각형 수.</summary>
    internal static int HouseObjects(SplineTownResult res, UnityEngine.SceneManagement.Scene scene, Transform root, List<(string name, GameObject go)> models, Material mWall)
    {
        int tris = 0;
        // 집(§2.6): 기존 Codex 건물 11종 중 필지 폭·깊이에 맞는 것(결정적), 바닥 = 기단 윗면. 기단 = 돌 상자(지형 최저 −0.3 → 기단 윗면).
        var mBuild = new[] { "BuildingsWarm", "BuildingsSand", "BuildingsCool" }.Select(TownGenScenes.FarFog).ToArray();
        var housesRoot = new GameObject("Houses"); housesRoot.transform.SetParent(root, false);
        var plinth = new MeshBuilder();
        int placed = 0, noModel = 0, floatBad = 0; float worstFloat = 0;
        foreach (var house in res.houses)
        {
            if (house.model < 0 || house.model >= models.Count) { noModel++; continue; }
            var m = models[house.model];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(m.go, scene);
            go.transform.SetParent(housesRoot.transform, false);
            go.transform.SetPositionAndRotation(new Vector3((float)house.pivot.x, (float)house.plinthY, (float)house.pivot.z), Quaternion.Euler(0, (float)house.yawDeg, 0));
            var mat = mBuild[house.hash % 3];
            if (mat) foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.sharedMaterials = rr.sharedMaterials.Select(_ => mat).ToArray();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh) tris += mf.sharedMesh.triangles.Length / 3;
            // M4: 렌더 바닥 = 기단 윗면 (|Δ| ≤ 0.10)
            float dy = Mathf.Abs(Bounds(go).min.y - (float)house.plinthY);
            worstFloat = Mathf.Max(worstFloat, dy); if (dy > 0.10f) floatBad++;
            // §11.1 앉히기: 땅에 바로(0) = 아래 덩어리 없음 / 벽 내림(1)·반지하(2) = 몸체와 같은 크기로 땅까지(따로 튀어나온 받침대 아님)
            if (house.seat >= 1) Box(plinth, new Vector3((float)house.center.x, 0, (float)house.center.z), (float)house.yawDeg, (float)house.width, (float)house.depth, (float)house.seatBottom, (float)house.plinthY);
            placed++;
        }
        tris += Obj("Plinths", housesRoot.transform, plinth.Build("Plinths"), mWall).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        res.report.Check("M4-float", floatBad == 0, $"건물 바닥 − 기단 윗면 |Δ| 최대 {worstFloat:0.000}m (≤0.10), 위반 {floatBad} / 놓은 집 {placed} (맞는 모델 없어 뺌 {noModel})");
        return tris;
    }

    internal static Bounds PrefabBounds(GameObject prefab)
    {
        var rr = prefab.GetComponentsInChildren<Renderer>(); var b = rr[0].bounds;
        foreach (var r in rr) b.Encapsulate(r.bounds);
        return b;
    }

    static Bounds Bounds(GameObject go)
    {
        var rr = go.GetComponentsInChildren<Renderer>(); var b = rr[0].bounds;
        foreach (var r in rr) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>yaw 로 돌린 상자(옆면 4 + 윗면) — 집 기단.</summary>
    internal static void Box(MeshBuilder mb, Vector3 c, float yawDeg, float w, float d, float y0, float y1)
    {
        var rot = Quaternion.Euler(0, yawDeg, 0);
        Vector3 P(float x, float z, float y) { var v = rot * new Vector3(x, 0, z) + c; v.y = y; return v; }
        float hw = w / 2, hd = d / 2;
        var c0 = new[] { P(-hw, -hd, y0), P(hw, -hd, y0), P(hw, hd, y0), P(-hw, hd, y0) };
        var c1 = new[] { P(-hw, -hd, y1), P(hw, -hd, y1), P(hw, hd, y1), P(-hw, hd, y1) };
        for (int k = 0; k < 4; k++)
        {
            int n = (k + 1) % 4;
            var outward = ((c0[k] + c0[n]) / 2 - new Vector3(c.x, y0, c.z)); outward.y = 0;
            mb.Quad(c0[k], c0[n], c1[n], c1[k], outward);
        }
        mb.Quad(c1[0], c1[1], c1[2], c1[3], Vector3.up);
    }

    /// <summary>골목 단면(§2.4): 밝은 돌 3m(연석·보도·중앙선 없음) + 어깨 1m(안쪽 골목 높이 → 바깥 지형 면, 지형 격자선마다 점을 넣어 면에 딱 붙음).</summary>
    internal static void AlleySection(SplineEdge e, SplineTownResult res, SplineTownSettings s, MeshBuilder stone, MeshBuilder shoulder, Func<int, bool> skipSeg = null)
    {
        int n = e.pts.Count;
        var c = new Vector3[n]; var nl = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            c[i] = new Vector3((float)e.pts[i].x, (float)e.y[i], (float)e.pts[i].z);
            var (nx, nz) = SplineTown.Normal(e.pts, i); nl[i] = new Vector3((float)nx, 0, (float)nz);
        }
        // 조각 13: 점마다 반폭(e.halfW — 건물 사이 틈), 없으면 설정값
        float Hw(int i) => (float)e.AlleyHalf(i, s.alleyHalfWidthMeters);
        float so = s.alleyShoulderMeters;
        for (int i = 0; i + 1 < n; i++)
        {
            if (skipSeg != null && skipSeg(i)) continue;   // 조각 14: 중심 길 면이 대신 그림
            int j = i + 1; float hi = Hw(i), hj = Hw(j);
            stone.Quad(c[i] - nl[i] * hi, c[i] + nl[i] * hi, c[j] + nl[j] * hj, c[j] - nl[j] * hj, Vector3.up);
            foreach (int side in new[] { 1, -1 })
                ShoulderStrip(shoulder, res.terrain, c[i] + nl[i] * (side * hi), c[j] + nl[j] * (side * hj), c[i] + nl[i] * (side * (hi + so)), c[j] + nl[j] * (side * (hj + so)));
        }
    }

    /// <summary>
    /// 어깨 띠 한 칸: 안쪽 두 점(길 쪽 높이) → 바깥 두 점(지형 면). 바깥 선이 지형 격자선·대각선을 지나는 곳마다 점을 넣어
    /// 바깥 끝이 지형 삼각형 위에 정확히 놓이게 한다(M1: 지형 상한 고정이 만든 접힘을 선형 보간이 가로지르던 것).
    /// </summary>
    internal static void ShoulderStrip(MeshBuilder mb, TerrainGrid g, Vector3 in0, Vector3 in1, Vector3 out0, Vector3 out1)
    {
        var ts = SplineTown.GridCrossings(g, out0.x, out0.z, out1.x, out1.z);
        for (int k = 0; k + 1 < ts.Count; k++)
        {
            float t0 = (float)ts[k], t1 = (float)ts[k + 1];
            Vector3 O(float t) { var p = Vector3.Lerp(out0, out1, t); p.y = (float)g.Surface(p.x, p.z); return p; }
            mb.Quad(Vector3.Lerp(in0, in1, t0), O(t0), O(t1), Vector3.Lerp(in0, in1, t1), Vector3.up);
        }
    }

    /// <summary>
    /// 교차점 판: 원판(가운데 = 교차점 높이, 둘레 = 붙는 길 끊는 선 + 길 사이 모서리 곡선) + 모서리마다 연석·보도·어깨.
    /// 붙는 길은 끊는 점(clip)까지만 그리므로 겹침·보도가 차로를 가로지르는 일이 없다(3차 그래픽 결함 1).
    /// </summary>
    internal static void JunctionPatch(JunctionLayout jl, SplineTownResult res, SplineTownSettings s, MeshBuilder asphalt, MeshBuilder pave, MeshBuilder shoulder, List<List<Vec2>> otherRoads)
    {
        float hc = s.HalfCarriageway, hs = s.HalfSection, ho = s.HalfShoulder, curb = s.curbHeightMeters;
        var node = res.nodes[jl.node];
        var ctr = new Vector3((float)node.x, (float)jl.y, (float)node.z);
        Vector3 V(Vec2 p, double y) => new Vector3((float)p.x, (float)y, (float)p.z);
        // 판: 코어가 정한 둘레(겹친 점 정리됨)와 높이(평면)
        int n = jl.ringP.Count;
        for (int k = 0; k < n; k++) asphalt.Tri(ctr, V(jl.ringP[k], jl.ringY[k]), V(jl.ringP[(k + 1) % n], jl.ringY[(k + 1) % n]));
        // 모서리마다 연석·보도·어깨(높이 = 같은 평면)
        int m = jl.arms.Count;
        for (int k = 0; k < m; k++)
        {
            var A = jl.arms[k]; var B = jl.arms[(k + 1) % m];
            int NS = SplineTown.CornerSamples(A, B);
            var c0 = SplineTown.CornerPath(A, B, node, hc, NS); var c1 = SplineTown.CornerPath(A, B, node, hs, NS); var c2 = SplineTown.CornerPath(A, B, node, ho, NS);
            var y0 = SplineTown.CornerY(res, jl, A, B, c0, hc); var y1 = SplineTown.CornerY(res, jl, A, B, c1, hc);
            for (int q = 0; q + 1 < c0.Count; q++)
            {
                if (SplineEdge.Dist(c0[q], c0[q + 1]) < 0.02 && SplineEdge.Dist(c2[q], c2[q + 1]) < 0.02) continue;   // 모인 점(가는 면) 건너뜀
                var i0 = V(c0[q], y0[q]); var i1 = V(c0[q + 1], y0[q + 1]);
                var mid = (i0 + i1) / 2; var toC = ctr - mid; toC.y = 0;
                pave.Quad(i0, i1, i1 + Vector3.up * curb, i0 + Vector3.up * curb, toC);
                var s0 = V(c1[q], y1[q] + curb); var s1 = V(c1[q + 1], y1[q + 1] + curb);
                pave.Quad(i0 + Vector3.up * curb, s0, s1, i1 + Vector3.up * curb, Vector3.up);
                var om = new Vec2((c2[q].x + c2[q + 1].x + c1[q].x + c1[q + 1].x) / 4, (c2[q].z + c2[q + 1].z + c1[q].z + c1[q + 1].z) / 4);
                if (OnOtherRoad(om, otherRoads, s, -1)) continue;
                ShoulderStrip(shoulder, res.terrain, s0, s1, V(c2[q], 0), V(c2[q + 1], 0));
            }
        }
    }

    /// <summary>점이 다른 큰길 보도 바깥 끝(+0.2m) 안이거나 교차점 판 안이면 true — 어깨가 다른 길 노면 위로 넘어가 녹색 면이 노출되던 것(4차 그래픽).</summary>
    internal static bool OnOtherRoad(Vec2 p, List<List<Vec2>> roads, SplineTownSettings s, int self)
    {
        for (int i = 0; i < roads.Count; i++) if (i != self && SplineTown.DistToPoly(p, roads[i]) < s.HalfSection + 0.2) return true;
        return false;
    }

    /// <summary>
    /// 집 진입로: 경사판은 길 끝(보도 높이) → 현관(기단 높이) 직선, 계단은 디딤 0.18m 안팎. 밑 지형은 코어가 진입로 아래로 눌러 두었으므로 면을 띄우지 않는다(양 끝 높이 정확).
    /// 옆면·계단 밑은 0.25m 간격으로 잰 가장 낮은 지형 − 0.3 까지(하부 뜸 방지, BUG-011).
    /// </summary>
    internal static void Driveway(HousePlot hp, SplineTownResult res, SplineTownSettings s, MeshBuilder mb)
    {
        var A = new Vector3((float)hp.driveA.x, (float)hp.driveAY, (float)hp.driveA.z);
        var B = new Vector3((float)hp.driveB.x, (float)hp.driveBY, (float)hp.driveB.z);
        var u = B - A; u.y = 0; float len = u.magnitude; if (len < 0.3f) return; u /= len;
        var side = new Vector3(-u.z, 0, u.x); float w = (float)hp.driveW / 2;
        var g = res.terrain;
        // 사각형 (along a0~a1, 옆 l0~l1) 안 가장 낮은 지형
        float MinGround(float a0, float a1, float l0, float l1)
        {
            float mn = float.MaxValue;
            for (float a = a0; a <= a1 + 1e-4f; a += Mathf.Max(0.05f, Mathf.Min(0.25f, (a1 - a0) / 2)))
                for (float l = l0; l <= l1 + 1e-4f; l += Mathf.Max(0.05f, Mathf.Min(0.25f, (l1 - l0) / 2 + 1e-3f)))
                { var p = A + u * a + side * l; mn = Mathf.Min(mn, (float)g.Surface(p.x, p.z)); }
            return mn;
        }
        // 입구 디딤판: 길 쪽 두 끝 = 보도 높이(왼·오), 안쪽 끝 = 진입로 시작 높이
        float ld = (float)hp.driveLanding;
        if (ld > 0.01f)
        {
            var aL = A + side * w; aL.y = (float)hp.driveAYL; var aR = A - side * w; aR.y = (float)hp.driveAYR;
            var bC = A + u * ld; var bL = bC + side * w; var bR = bC - side * w; bL.y = bR.y = A.y;
            float bot = MinGround(0, ld, -w, w) - 0.3f;
            mb.Quad(aR, aL, bL, bR, Vector3.up);
            mb.Quad(aL, bL, new Vector3(bL.x, bot, bL.z), new Vector3(aL.x, bot, aL.z), side);
            mb.Quad(aR, bR, new Vector3(bR.x, bot, bR.z), new Vector3(aR.x, bot, aR.z), -side);
        }
        var A0 = A + u * ld; float run = len - ld;
        if (!hp.driveSteps)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(run / 0.5f));
            Vector3 prevL = default, prevR = default; float prevBot = 0;
            for (int k = 0; k <= n; k++)
            {
                float t = (float)k / n, along = ld + run * t;
                var c = A0 + u * (run * t); c.y = Mathf.Lerp(A.y, B.y, t);
                var L = c + side * w; var R = c - side * w;
                if (k > 0)
                {
                    float bot = Mathf.Min(prevBot, MinGround(ld + run * (k - 1) / n, along, -w, w)) - 0.3f;
                    mb.Quad(prevR, prevL, L, R, Vector3.up);
                    mb.Quad(prevL, L, new Vector3(L.x, bot, L.z), new Vector3(prevL.x, bot, prevL.z), side);
                    mb.Quad(prevR, R, new Vector3(R.x, bot, R.z), new Vector3(prevR.x, bot, prevR.z), -side);
                    prevBot = bot + 0.3f;
                }
                else prevBot = MinGround(ld, ld + 0.01f, -w, w);
                prevL = L; prevR = R;
            }
            return;
        }
        float dy = B.y - A.y; int ns = SplineTown.DriveStepCount(hp);
        float tread = run / ns, yaw = Mathf.Atan2(u.x, u.z) * Mathf.Rad2Deg;
        for (int k = 0; k < ns; k++)
        {
            var mid = A0 + u * (tread * (k + 0.5f));
            float top = A.y + dy * (k + 1) / ns;
            float gmin = MinGround(ld + tread * k, ld + tread * (k + 1), -w, w);
            Box(mb, new Vector3(mid.x, 0, mid.z), yaw, w * 2, tread, Mathf.Min(gmin, top) - 0.3f, top);
        }
    }

    // ------------------------------------------------------------------ 메시
    internal class MeshBuilder
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
        /// <summary>네 점 a-b-c-d(둘레 순서). facing = 앞면이 봐야 할 방향 — 감김을 자동으로 맞춘다(Unity: 앞면 법선 = Cross(v1−v0, v2−v0)).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, bool twoSided = false)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(new Vector2((a.x + a.y) / 4f, a.z / 4f)); uv.Add(new Vector2((b.x + b.y) / 4f, b.z / 4f)); uv.Add(new Vector2((c.x + c.y) / 4f, c.z / 4f)); uv.Add(new Vector2((d.x + d.y) / 4f, d.z / 4f));
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0;
            if (!flip) t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            else t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            if (twoSided) { if (flip) t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); else t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 }); }
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
            uv.Add(new Vector2((a.x + a.y) / 4f, a.z / 4f)); uv.Add(new Vector2((b.x + b.y) / 4f, b.z / 4f)); uv.Add(new Vector2((c.x + c.y) / 4f, c.z / 4f));
            if (Vector3.Cross(b - a, c - a).y >= 0) t.AddRange(new[] { i, i + 1, i + 2 }); else t.AddRange(new[] { i, i + 2, i + 1 });
        }
        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    internal static GameObject Obj(string name, Transform parent, Mesh mesh, Material mat)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); if (mat) mr.sharedMaterial = mat;
        return go;
    }

    /// <summary>
    /// 땅(480×400) 바깥 둘레 지형 — 8m 격자, 땅 안 칸은 비움. 높이 = 가장 가까운 땅 가장자리 높이에서
    /// 남쪽은 바다 쪽으로 12% 내려가(최저 −8m) 바다 밑, 북쪽은 15%·동서는 8% 로 오르는 산. 경계 이음매는 땅 쪽 치마가 가린다.
    /// </summary>
    static Mesh SurroundMesh(TerrainGrid g, SplineTownSettings s, float sandBelow)
    {
        float W = s.boundsXMeters, D = s.boundsZMeters, step = 8f;
        float x0 = -200, x1 = W + 200, z0 = -240, z1 = D + 200;
        int nx = (int)((x1 - x0) / step) + 1, nz = (int)((z1 - z0) / step) + 1;
        var v = new Vector3[nx * nz]; var uv = new Vector2[v.Length];
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float x = x0 + i * step, z = z0 + j * step;
                float cx = Mathf.Clamp(x, 0, W), cz = Mathf.Clamp(z, 0, D);
                float h = (float)g.Surface(cx, cz);
                float dx = Mathf.Max(0, Mathf.Max(-x, x - W)), dzN = Mathf.Max(0, z - D), dzS = Mathf.Max(0, -z);
                if (dzS > 0) h = Mathf.Max(-8f, h - dzS * 0.12f) - dx * 0.02f;
                else h += dzN * 0.15f + dx * 0.08f;
                v[j * nx + i] = new Vector3(x, h, z); uv[j * nx + i] = new Vector2(x / 8, z / 8);
            }
        var grass = new List<int>(); var sand = new List<int>();
        for (int j = 0; j + 1 < nz; j++)
            for (int i = 0; i + 1 < nx; i++)
            {
                float cx0 = x0 + i * step, cz0 = z0 + j * step;
                bool inside = cx0 >= 0 && cx0 + step <= W && cz0 >= 0 && cz0 + step <= D;
                if (inside) continue;
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                var tl = Mathf.Max(v[a].y, v[c].y, v[d].y) < sandBelow ? sand : grass; tl.AddRange(new[] { a, c, d });
                var tr = Mathf.Max(v[a].y, v[d].y, v[b].y) < sandBelow ? sand : grass; tr.AddRange(new[] { a, d, b });
            }
        // 둘레 치마: 땅(2m 격자) 가장자리에서 3m 아래로 — 바깥 8m 격자와의 이음매 틈을 가린다(양면).
        // Terrain 메시에 넣으면 QA M7(지형 정점 전수 대조)이 치마 정점을 3m 깎기로 센다 → 여기(Surroundings)에 둔다.
        var vl = new List<Vector3>(v); var ul = new List<Vector2>(uv);
        void Skirt(Vector3 p0, Vector3 p1)
        {
            int k = vl.Count;
            vl.Add(p0); vl.Add(p1); vl.Add(p1 + Vector3.down * 3); vl.Add(p0 + Vector3.down * 3);
            for (int q = 0; q < 4; q++) ul.Add(new Vector2((vl[k + q].x + vl[k + q].z) / 8, vl[k + q].y / 8));
            grass.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3, k, k + 2, k + 1, k, k + 3, k + 2 });
        }
        Vector3 E(int i, int j) => new Vector3((float)(i * g.step), (float)g.h[j * g.nx + i], (float)(j * g.step));
        for (int i = 0; i + 1 < g.nx; i++) { Skirt(E(i, 0), E(i + 1, 0)); Skirt(E(i, g.nz - 1), E(i + 1, g.nz - 1)); }
        for (int j = 0; j + 1 < g.nz; j++) { Skirt(E(0, j), E(0, j + 1)); Skirt(E(g.nx - 1, j), E(g.nx - 1, j + 1)); }
        var m = new Mesh { name = "Surroundings", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
        m.SetVertices(vl); m.SetUVs(0, ul); m.SetTriangles(grass, 0); m.SetTriangles(sand, 1); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh TerrainMesh(TerrainGrid g, float sandBelow)
    {
        var v = new Vector3[g.nx * g.nz]; var uv = new Vector2[v.Length];
        for (int j = 0; j < g.nz; j++)
            for (int i = 0; i < g.nx; i++)
            {
                v[j * g.nx + i] = new Vector3((float)(i * g.step), (float)g.h[j * g.nx + i], (float)(j * g.step));
                uv[j * g.nx + i] = new Vector2((float)(i * g.step / 8), (float)(j * g.step / 8));
            }
        var tri = new List<int>(); var sand = new List<int>();
        for (int j = 0; j + 1 < g.nz; j++)
            for (int i = 0; i + 1 < g.nx; i++)
            {
                int a = j * g.nx + i, b = a + 1, c = a + g.nx, d = c + 1;
                // TerrainGrid.Surface 와 같은 대각선. 세 점 모두 해수면+1.5m 아래면 모래.
                (Mathf.Max(v[a].y, v[c].y, v[d].y) < sandBelow ? sand : tri).AddRange(new[] { a, c, d });
                (Mathf.Max(v[a].y, v[d].y, v[b].y) < sandBelow ? sand : tri).AddRange(new[] { a, d, b });
            }
        var vl = new List<Vector3>(v); var ul = new List<Vector2>(uv);
        var m = new Mesh { name = "SplineTerrain", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
        m.SetVertices(vl); m.SetUVs(0, ul); m.SetTriangles(tri, 0); m.SetTriangles(sand, 1); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static bool NearWall(SplineTownResult res, double x, double z)
    {
        foreach (var w in res.walls)
        {
            double dx = w.b.x - w.a.x, dz = w.b.z - w.a.z, l2 = dx * dx + dz * dz;
            double tt = l2 > 1e-12 ? Math.Max(0, Math.Min(1, ((x - w.a.x) * dx + (z - w.a.z) * dz) / l2)) : 0;
            double px = w.a.x + dx * tt - x, pz = w.a.z + dz * tt - z;
            if (px * px + pz * pz < 0.36) return true;
        }
        return false;
    }

    /// <summary>
    /// 큰길 단면: 아스팔트(±3m, 길 높이) · 연석 세운 면(±3m, 0.15m) · 보도(±3~4m, +0.15) · 어깨(±4~6m: 안쪽 보도 높이 → 바깥 지형 면)
    /// 중앙 흰 점선(3m 칠 · 3m 띔, 폭 0.15m). 교차점 쪽 끝은 clipA/clipB 에서 끊고 교차점 판이 잇는다.
    /// 골목 입구(골목 시작 둘레) 어깨는 비운다 — 골목 돌길이 그 자리를 덮는다.
    /// </summary>
    internal static void Section(SplineEdge e, SplineTownResult res, SplineTownSettings s, MeshBuilder asphalt, MeshBuilder pave, MeshBuilder shoulder, MeshBuilder paint, List<List<Vec2>> mouths, List<List<Vec2>> otherRoads, int self)
    {
        double L = e.Length, a0 = e.clipA, a1 = L - e.clipB;
        if (a1 - a0 < 0.2) return;
        var arcs = new List<double> { a0 };
        double acc = 0;
        for (int i = 1; i < e.pts.Count; i++) { acc += SplineEdge.Dist(e.pts[i - 1], e.pts[i]); if (acc > a0 + 0.05 && acc < a1 - 0.05) arcs.Add(acc); }
        arcs.Add(a1);
        int n = arcs.Count;
        var c = new Vector3[n]; var nl = new Vector3[n]; var bank = new float[n];
        for (int k = 0; k < n; k++)
        {
            var st = SplineTown.Station(e, arcs[k]);
            c[k] = new Vector3((float)st.p.x, (float)st.y, (float)st.p.z); nl[k] = new Vector3((float)st.n.x, 0, (float)st.n.z);
            bank[k] = (float)SplineTown.BankAt(e, arcs[k], s);
        }
        float hc = s.HalfCarriageway, hs = s.HalfSection, ho = s.HalfShoulder, curb = s.curbHeightMeters;
        double mouthR = s.alleyHalfWidthMeters + s.alleyShoulderMeters;
        bool AtMouth(Vector3 p)
        {
            var v = new Vec2(p.x, p.z);
            foreach (var mp in mouths) if (SplineTown.DistToPoly(v, mp) < mouthR) return true;
            return false;
        }
        // 교차점 판이 평면인 쪽은 단면이 옆으로 기운다(끊는 선에서 판 평면과 정점이 맞물리고 bankRunMeters 에 걸쳐 평평해짐)
        Vector3 Off(int i, float d, float up) => c[i] + nl[i] * d + Vector3.up * (up + bank[i] * d);
        for (int i = 0; i + 1 < n; i++)
        {
            int j = i + 1;
            asphalt.Quad(Off(i, -hc, 0), Off(i, hc, 0), Off(j, hc, 0), Off(j, -hc, 0), Vector3.up);
            foreach (int side in new[] { 1, -1 })
            {
                float sd = side;
                // 연석 세운 면 · 보도
                pave.Quad(Off(i, sd * hc, 0), Off(j, sd * hc, 0), Off(j, sd * hc, curb), Off(i, sd * hc, curb), -nl[i] * sd);
                pave.Quad(Off(i, sd * hc, curb), Off(i, sd * hs, curb), Off(j, sd * hs, curb), Off(j, sd * hc, curb), Vector3.up);
                var oi = c[i] + nl[i] * (sd * ho); var oj = c[j] + nl[j] * (sd * ho);
                var qm = (oi + oj + Off(i, sd * hs, 0) + Off(j, sd * hs, 0)) / 4;
                if (mouths.Count > 0 && AtMouth(qm)) continue;
                if (OnOtherRoad(new Vec2(qm.x, qm.z), otherRoads, s, self)) continue;
                ShoulderStrip(shoulder, res.terrain, Off(i, sd * hs, curb), Off(j, sd * hs, curb), oi, oj);
            }
        }
        // 중앙 점선(호 길이 기준 3m 칠 · 3m 띔)
        double period = s.dashLengthMeters + s.dashGapMeters;
        float w = s.dashWidthMeters / 2;
        Vector3 P((Vec2 p, double y, Vec2 n) st, float lat) => new Vector3((float)(st.p.x + st.n.x * lat), (float)st.y + .012f, (float)(st.p.z + st.n.z * lat));
        for (double d0 = Math.Ceiling(a0 / period) * period; d0 + s.dashLengthMeters <= a1; d0 += period)
        {
            var p0 = SplineTown.Station(e, d0); var p1 = SplineTown.Station(e, d0 + s.dashLengthMeters);
            paint.Quad(P(p0, -w), P(p0, w), P(p1, w), P(p1, -w), Vector3.up);
        }
    }

    /// <summary>큰길 중심선 중 실제로 그린 구간(clipA ~ 길이 − clipB).</summary>
    internal static List<Vec2> ClippedPolyline(SplineEdge e)
    {
        double L = e.Length, a0 = e.clipA, a1 = L - e.clipB, acc = 0;
        var r = new List<Vec2>();
        if (a1 - a0 < 0.2) return r;
        r.Add(SplineTown.Station(e, a0).p);
        for (int i = 1; i < e.pts.Count; i++) { acc += SplineEdge.Dist(e.pts[i - 1], e.pts[i]); if (acc > a0 && acc < a1) r.Add(e.pts[i]); }
        r.Add(SplineTown.Station(e, a1).p);
        return r;
    }

    static void Disc(Vec2 center, double y, double radius, MeshBuilder mb)
    {
        int seg = 28;
        var ctr = new Vector3((float)center.x, (float)y, (float)center.z);
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
            var p0 = ctr + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (float)radius;
            var p1 = ctr + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (float)radius;
            int i = mb.v.Count;
            mb.v.Add(ctr); mb.v.Add(p0); mb.v.Add(p1);
            mb.uv.Add(new Vector2(ctr.x / 4, ctr.z / 4)); mb.uv.Add(new Vector2(p0.x / 4, p0.z / 4)); mb.uv.Add(new Vector2(p1.x / 4, p1.z / 4));
            if (Vector3.Cross(p0 - ctr, p1 - ctr).y > 0) mb.t.AddRange(new[] { i, i + 1, i + 2 }); else mb.t.AddRange(new[] { i, i + 2, i + 1 });
        }
    }

    internal static void WallQuad(WallSegment w, MeshBuilder mb)
    {
        var a0 = new Vector3((float)w.a.x, (float)w.botA, (float)w.a.z); var b0 = new Vector3((float)w.b.x, (float)w.botB, (float)w.b.z);
        var b1 = new Vector3((float)w.b.x, (float)w.topB, (float)w.b.z); var a1 = new Vector3((float)w.a.x, (float)w.topA, (float)w.a.z);
        mb.Quad(a0, b0, b1, a1, Vector3.Cross(b0 - a0, Vector3.up), twoSided: true);
    }

    static void Shot(Camera cam, Vector3 pos, Quaternion rot, string path)
    {
        cam.transform.SetPositionAndRotation(pos, rot);
        var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            cam.targetTexture = rt; cam.aspect = 1280f / 720; cam.Render();
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally { cam.targetTexture = null; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    }
}
