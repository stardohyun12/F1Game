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
/// 마을 생성기 실험 — 도현 씬 뷰 판정용 씬 2개(D-109·"씬 뷰에서 볼 수 있을 정도만").
///  - PitCrew/TownGen/Build OSM Scene      → Assets/Scenes/TownGen/TownGen_OSM.unity
///  - PitCrew/TownGen/Build RoadGen Scene  → Assets/Scenes/TownGen/TownGen_RoadGen.unity
///  - PitCrew/TownGen/Build Both Scenes
/// 지형(공통 고도맵) + 도로 + 기존 Codex 건물(새 아트 0). 게임 시스템 없음 — 둘러보기 전용.
/// 메인 ComplexTrack 씬은 열지도 저장하지도 않는다(빌드 뒤 원래 열려 있던 씬을 다시 연다).
/// 입력: tools/town-gen/out/heights_480x400.json · osm_network.json (osm_to_network.py 가 만든다).
/// </summary>
public static class TownGenScenes
{
    const string SceneDir = "Assets/Scenes/TownGen";
    const float RoadLift = 0.06f;     // 도로를 지형 위로 띄우는 높이(z-fighting 방지)
    const float BuildingSink = 0.08f; // 건물 바닥을 가장 낮은 모서리보다 이만큼 묻는다(뜸 방지)
    const float MaxBuildingRise = 2.5f; // 칸 안 고도차가 이보다 크면 건물 안 놓음(절벽)

    [MenuItem("PitCrew/TownGen/Build OSM Scene")] public static void BuildOsmMenu() => Debug.Log(BuildOsm());
    [MenuItem("PitCrew/TownGen/Build RoadGen Scene")] public static void BuildRoadGenMenu() => Debug.Log(BuildRoadGen());
    [MenuItem("PitCrew/TownGen/Build Both Scenes")] public static void BuildBothMenu() => Debug.Log(BuildBoth());

    public static string BuildBoth() => BuildOsm() + "\n" + BuildRoadGen();

    [MenuItem("PitCrew/TownGen/Build Both Scenes (height x0.3)")] public static void BuildBothH03Menu() => Debug.Log(BuildBoth(0.3f, "_h03"));

    // 비교판 씬용 덮어쓰기: 고도 배율과 씬 이름 접미사. null/"" 이면 TownGenConfig 값·기본 이름.
    static float? s_heightScale; static string s_suffix = "";

    /// <summary>고도 배율만 바꾼 비교판 두 씬 (예: 0.3, "_h03" → TownGen_OSM_h03.unity). 기본 씬은 그대로 둔다.</summary>
    public static string BuildBoth(float heightScale, string suffix)
    {
        s_heightScale = heightScale; s_suffix = suffix ?? "";
        try { return BuildBoth(); }
        finally { s_heightScale = null; s_suffix = ""; }
    }

    public static string BuildOsm()
    {
        var errors = new List<string>(); var warnings = new List<string>();
        var net = PitCrew.TownGen.RoadNetwork.Parse(File.ReadAllText(Path.Combine(TownGenRunner.ToolsDir, "out", "osm_network.json")), errors, warnings);
        if (net == null) throw new InvalidDataException("osm_network.json: " + string.Join("; ", errors));
        return Build("OSM", net, "© OpenStreetMap contributors (ODbL) · DEM: produced using Copernicus WorldDEM-30");
    }

    /// <summary>roadgen 기본 설정 결과를 마을 크기로 줄여(골목 한 조각 ≈ 40m) 같은 땅에 올린다. 지형은 생성에 안 들어감(뒷단 경사 끊기가 처리).</summary>
    public static string BuildRoadGen(int seed = 1, float streetMeters = 40f, int segmentLimit = 2500)
    {
        int oldLimit = RoadGen.Config.segmentCountLimit;
        try
        {
            RoadGen.Config.segmentCountLimit = segmentLimit;
            UnityEngine.Random.InitState(seed); RoadGen.Perlin.Seed(seed);
            RoadGen.RoadNetworkGenerator.Generate(out var segs, out _, out _);
            float k = streetMeters / RoadGen.Config.streetSegmentLength;
            var s = TownGenRunner.LoadSettings();
            // 원점(첫 큰길) = 항구 띠 가운데, 큰길이 동서로 해안을 따라감
            double ox = s.boundsXMeters / 2, oz = 40;
            var net = new PitCrew.TownGen.RoadNetwork { source = "roadgen", seed = seed, boundsX = s.boundsXMeters, boundsZ = s.boundsZMeters };
            int id = 0;
            foreach (var g in segs)
            {
                var a = new Vec2(ox + g.Start.x * k, oz + g.Start.y * k);
                var b = new Vec2(ox + g.End.x * k, oz + g.End.y * k);
                bool inA = a.x >= 0 && a.z >= 0 && a.x <= net.boundsX && a.z <= net.boundsZ;
                bool inB = b.x >= 0 && b.z >= 0 && b.x <= net.boundsX && b.z <= net.boundsZ;
                if (!inA && !inB) continue;
                net.segments.Add(new RoadSegment { id = ++id, cls = g.Highway ? RoadClass.Major : RoadClass.Minor, points = new List<Vec2> { a, b } });
            }
            Directory.CreateDirectory(Path.Combine(TownGenRunner.ToolsDir, "out"));
            File.WriteAllText(Path.Combine(TownGenRunner.ToolsDir, "out", "roadgen_network.json"), net.ToJson(), new UTF8Encoding(false));
            return Build("RoadGen", net, $"roadgen (pboechat, MIT) seed {seed}, 골목 조각 {streetMeters}m 로 축소, 생성 {segs.Count}조각 중 땅 안 {net.segments.Count}");
        }
        finally { RoadGen.Config.segmentCountLimit = oldLimit; }
    }

    // ------------------------------------------------------------------ 공통
    class Terrain
    {
        public int nx, nz; public float step; public float[] h;
        /// <summary>지형 메시와 똑같은 삼각형 면 위 높이 — 도로·건물이 메시에 정확히 붙게.</summary>
        public float Surface(float x, float z)
        {
            float fx = Mathf.Clamp(x / step, 0, nx - 1.0001f), fz = Mathf.Clamp(z / step, 0, nz - 1.0001f);
            int i = (int)fx, j = (int)fz; float tx = fx - i, tz = fz - j;
            float h00 = h[j * nx + i], h10 = h[j * nx + i + 1], h01 = h[(j + 1) * nx + i], h11 = h[(j + 1) * nx + i + 1];
            // 메시 삼각형: (00,01,11) / (00,11,10) — 대각선 00→11
            return tz >= tx ? h00 + tx * (h11 - h01) + tz * (h01 - h00) : h00 + tx * (h10 - h00) + tz * (h11 - h10);
        }
    }

    static Terrain LoadTerrain()
    {
        var d = JObject.Parse(File.ReadAllText(Path.Combine(TownGenRunner.ToolsDir, "out", "heights_480x400.json")));
        var t = new Terrain { nx = d["nx"].Value<int>(), nz = d["nz"].Value<int>(), step = d["step_m"].Value<float>() };
        t.h = d["heights_m"].Select(v => v.Value<float>()).ToArray();
        return t;
    }

    static string Build(string name, PitCrew.TownGen.RoadNetwork net, string credit)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var settings = TownGenRunner.LoadSettings();
        if (s_heightScale.HasValue) settings.heightScale = s_heightScale.Value;
        name += s_suffix;
        var terrain = LoadTerrain();
        for (int k = 0; k < terrain.h.Length; k++) terrain.h[k] *= settings.heightScale;
        var hf = new GridHeightField(terrain.h, terrain.nx, terrain.nz, terrain.step, terrain.step);
        var backendSettings = settings.Clone(); backendSettings.heightScale = 1f; // terrain.h 에 이미 곱함
        var result = TownBackend.Run(net, hf, backendSettings);
        var grid = result.grid;

        // 산출물(보고·그림) — 검사기는 이번 범위 밖이라 보고만 남긴다
        var outDir = Path.Combine(TownGenRunner.ToolsDir, "out", "scene_" + name.ToLowerInvariant());
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "report.txt"), result.report.ToText(), new UTF8Encoding(false));
        File.WriteAllBytes(Path.Combine(outDir, "preview.png"), TownGenRunner.Preview(grid, 8));

        string prevScene = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("열린 씬에 저장 안 한 변경이 있음 — 중단(메인 씬 보호)");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var root = new GameObject("TownGen_" + name);
        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null) { light.transform.rotation = Quaternion.Euler(55f, -35f, 0f); light.intensity = 1.2f; light.shadows = LightShadows.Soft; light.color = new Color(1f, .96f, .88f); }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .64f, .66f);

        var mGround = FarFog("Ground");
        var mRoad = FarFog("Asphalt");
        var mSea = FarFog("Sea");
        var mPave = FarFog("Pavement");
        var mBuild = new[] { "BuildingsWarm", "BuildingsSand", "BuildingsCool" }.Select(FarFog).ToArray();

        // 지형
        var tGo = MeshObject("Terrain", root.transform, TerrainMesh(terrain), mGround);
        tGo.AddComponent<MeshCollider>();
        // 바다: y=0 평면 (지형이 0 아래인 곳만 보임)
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Sea"; sea.transform.SetParent(root.transform, false);
        // 바다 평면: 해수면 높이, 남쪽 −300m 부터 뒷단이 바다로 판정한 칸의 북쪽 끝까지(D-110 — 도로 금지 칸과 같은 범위)
        float seaTop = (grid.seaTopRow + 1) * settings.cellSizeMeters, seaBottom = -300f;
        sea.transform.position = new Vector3(settings.boundsXMeters / 2, settings.seaLevelMeters, (seaTop + seaBottom) / 2);
        sea.transform.localScale = new Vector3(120, 1, (seaTop - seaBottom) / 10f);
        UnityEngine.Object.DestroyImmediate(sea.GetComponent<Collider>());
        if (mSea) sea.GetComponent<Renderer>().sharedMaterial = mSea;

        // 도로
        float cell = settings.cellSizeMeters;
        var roadStats = new RoadStats();
        MeshObject("Roads_Major", root.transform, RoadMesh(grid, terrain, CellKind.Major, settings.laneWidthMeters * 2, roadStats), mRoad);
        MeshObject("Roads_Minor", root.transform, RoadMesh(grid, terrain, CellKind.Minor, settings.laneWidthMeters, roadStats), mPave ? mPave : mRoad);

        // 건물: 도로에 붙은 빈칸마다 하나, 도로를 바라봄. 바닥 = 칸 네 모서리 중 가장 낮은 곳 − sink (뜨지 않게)
        var bRoot = new GameObject("Buildings"); bRoot.transform.SetParent(root.transform, false);
        int placed = 0, skippedCliff = 0, skippedSea = 0, floating = 0; float maxBury = 0;
        var dirs = new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
        for (int r = 0; r < grid.H; r++)
            for (int c = 0; c < grid.W; c++)
            {
                int i = grid.Index(c, r);
                if (grid.IsRoad(i) || grid.stepsDecor[i]) continue;
                Vector2Int face = default; bool near = false;
                foreach (var d in dirs) if (grid.IsRoad(c + d.x, r + d.y)) { face = d; near = true; break; }
                if (!near) continue;
                float cx = (c + .5f) * cell, cz = (r + .5f) * cell, half = 3.6f;
                // 칸 전체(±4m)를 1m 격자로 훑어 최저·최고 — 건물(최대 7.1m, 회전 포함)이 칸 안 어디에 걸려도 바닥이 지형 위로 뜨지 않게
                float lo = float.MaxValue, hi = float.MinValue;
                for (float ox = -cell / 2; ox <= cell / 2 + 1e-3f; ox += 1f)
                    for (float oz = -cell / 2; oz <= cell / 2 + 1e-3f; oz += 1f)
                    { float y = terrain.Surface(cx + ox, cz + oz); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                _ = half;
                if (lo < 0.4f) { skippedSea++; continue; }
                if (hi - lo > MaxBuildingRise) { skippedCliff++; continue; }
                int hash = Math.Abs(c * 157 + r * 73);
                string asset = hash % 5 == 0 ? "CornerShop" + new[] { "", "B", "C" }[(hash / 7) % 3] : "CoastalHouse" + new[] { "", "B", "C", "D", "E", "F" }[(hash / 7) % 6];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/LofiPilot/{asset}.prefab");
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.transform.SetParent(bRoot.transform, false);
                float baseY = lo - BuildingSink;
                go.transform.SetPositionAndRotation(new Vector3(cx, baseY, cz), Quaternion.Euler(0, Mathf.Atan2(face.x, face.y) * Mathf.Rad2Deg, 0));
                if (mBuild[hash % 3]) foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.sharedMaterials = rr.sharedMaterials.Select(_ => mBuild[hash % 3]).ToArray();
                // 안전선 측정: 실제 렌더 바닥 네 모서리 아래 지형 — 바닥이 지형보다 위면 뜬 것
                var b = Bounds(go);
                foreach (var (px, pz) in new[] { (b.min.x, b.min.z), (b.max.x, b.min.z), (b.min.x, b.max.z), (b.max.x, b.max.z) })
                {
                    float gap = b.min.y - terrain.Surface(px, pz);
                    if (gap > 0.01f) floating++;
                    maxBury = Mathf.Max(maxBury, -gap);
                }
                placed++;
            }

        // 출발점 표시(초록 기둥) — 둘러볼 때 방향 잡기용
        var start = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        start.name = "StartMarker"; start.transform.SetParent(root.transform, false);
        float sx = (grid.Col(grid.start) + .5f) * cell, sz = (grid.Row(grid.start) + .5f) * cell;
        start.transform.position = new Vector3(sx, terrain.Surface(sx, sz) + 1.5f, sz); start.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
        UnityEngine.Object.DestroyImmediate(start.GetComponent<Collider>());

        // 카메라: 남쪽 바다 위에서 마을을 내려다봄
        var cam = Camera.main;
        if (cam != null) { cam.transform.position = new Vector3(settings.boundsXMeters / 2, 140, -170); cam.transform.LookAt(new Vector3(settings.boundsXMeters / 2, 40, 200)); cam.farClipPlane = 2000; }
        File.WriteAllText(Path.Combine(outDir, "credit.txt"), credit, new UTF8Encoding(false));
        new GameObject("_Credit: " + credit).transform.SetParent(root.transform, false); // 출처는 하이어라키 이름으로만 (스크립트 없음 → Missing Script 없음)

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes", "TownGen"));
        string path = $"{SceneDir}/TownGen_{name}.unity";
        EditorSceneManager.SaveScene(scene, path);
        if (!string.IsNullOrEmpty(prevScene)) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);

        sw.Stop();
        return $"[TownGen] {name}: {path} 저장 ({sw.ElapsedMilliseconds}ms)\n" +
               $"  고도 배율 {settings.heightScale} · 도로 칸 {grid.CountRoad()} · 경사 {settings.maxSlope * 100:0}% 로 지운 칸 {result.report.MetricValue("steep_cut_cells")} · A* 새로 깐 칸 {result.report.MetricValue("connector_cells_astar")} · 버린 고립 칸 {result.report.MetricValue("dropped_isolated_cells")} · 승격 footway {result.report.MetricValue("promoted_footway_a")}(a)+{result.report.MetricValue("promoted_footway_b")}(b)\n" +
               $"  {result.report.MetricValue("road_cells")} · 래스터 직후 {result.report.MetricValue("raster_road_cells")}\n" +
               $"  건물 {placed} (절벽 건너뜀 {skippedCliff}, 바다 건너뜀 {skippedSea}) · 뜬 모서리 {floating} · 최대 묻힘 {maxBury:0.00}m\n" +
               $"  도로 정점 {roadStats.verts} · 지형 대비 높이차 최소 {roadStats.minGap:0.000} / 최대 {roadStats.maxGap:0.000} m (목표 {RoadLift})";
    }

    /// <summary>
    /// 해안마을 재질의 TownGen 전용 복사본 — 셰이더 자체 안개가 45~165m 라 마을 전체를 둘러보면 다 안개에 묻힌다.
    /// 원본(Assets/Art/CoastalTown)은 건드리지 않고 Assets/Data/TownGen/Materials 에 복사해 안개만 멀리.
    /// </summary>
    internal static Material FarFog(string name)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/CoastalTown/{name}.mat");
        if (src == null) return null;
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data", "TownGen", "Materials"));
        string path = $"Assets/Data/TownGen/Materials/{name}_FarFog.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
        else m.CopyPropertiesFromMaterial(src);
        if (m.HasProperty("_FogStart")) m.SetFloat("_FogStart", 500f);
        if (m.HasProperty("_FogEnd")) m.SetFloat("_FogEnd", 1800f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Bounds Bounds(GameObject go)
    {
        var rr = go.GetComponentsInChildren<Renderer>(); var b = rr[0].bounds;
        foreach (var r in rr) b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); if (mat) mr.sharedMaterial = mat;
        return go;
    }

    static Mesh TerrainMesh(Terrain t)
    {
        var v = new Vector3[t.nx * t.nz]; var uv = new Vector2[v.Length];
        for (int j = 0; j < t.nz; j++)
            for (int i = 0; i < t.nx; i++)
            {
                v[j * t.nx + i] = new Vector3(i * t.step, t.h[j * t.nx + i], j * t.step);
                uv[j * t.nx + i] = new Vector2(i * t.step / 8f, j * t.step / 8f);
            }
        var tri = new List<int>();
        for (int j = 0; j + 1 < t.nz; j++)
            for (int i = 0; i + 1 < t.nx; i++)
            {
                int a = j * t.nx + i, b = a + 1, c = a + t.nx, d = c + 1;
                tri.AddRange(new[] { a, c, d, a, d, b }); // 대각선 a(00)→d(11), Terrain.Surface 와 같은 분할
            }
        var m = new Mesh { name = "TownTerrain", indexFormat = IndexFormat.UInt32 };
        m.vertices = v; m.uv = uv; m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    class RoadStats { public int verts; public float minGap = float.MaxValue, maxGap = float.MinValue; }

    /// <summary>
    /// 도로 칸마다 가운데 정사각형 + 이웃 도로 칸 쪽 팔(동·북만 — 중복 없음). 폭 = 차로 폭(큰길 2배).
    /// 정점은 1m 간격으로 잘게 나눠 지형 면(Terrain.Surface) + RoadLift 에 붙인다 — 묻힘·뜸 방지.
    /// </summary>
    static Mesh RoadMesh(TownGrid g, Terrain t, CellKind kind, float width, RoadStats st)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        float cell = (float)g.cell, hw = width / 2f;
        void Quad(float x0, float z0, float x1, float z1)
        {
            int nxs = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / 1f)), nzs = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / 1f));
            int baseIdx = verts.Count;
            for (int j = 0; j <= nzs; j++)
                for (int i = 0; i <= nxs; i++)
                {
                    float x = Mathf.Lerp(x0, x1, i / (float)nxs), z = Mathf.Lerp(z0, z1, j / (float)nzs);
                    float y = t.Surface(x, z) + RoadLift;
                    verts.Add(new Vector3(x, y, z)); uvs.Add(new Vector2(x / 4f, z / 4f));
                    float gap = y - t.Surface(x, z); st.minGap = Mathf.Min(st.minGap, gap); st.maxGap = Mathf.Max(st.maxGap, gap);
                }
            for (int j = 0; j < nzs; j++)
                for (int i = 0; i < nxs; i++)
                {
                    int a = baseIdx + j * (nxs + 1) + i, b = a + 1, c = a + nxs + 1, d = c + 1;
                    tris.AddRange(new[] { a, c, d, a, d, b });
                }
        }
        for (int r = 0; r < g.H; r++)
            for (int c = 0; c < g.W; c++)
            {
                int i = g.Index(c, r);
                if (g.kind[i] != kind) continue;
                float cx = (c + .5f) * cell, cz = (r + .5f) * cell;
                Quad(cx - hw, cz - hw, cx + hw, cz + hw);
                // 팔: 이웃이 도로면 이어 붙임. 큰길↔골목 연결은 골목 폭으로(골목 메시 쪽에서 그림)
                if (g.IsRoad(c + 1, r) && (kind == CellKind.Minor || g.kind[g.Index(c + 1, r)] == kind)) Quad(cx + hw, cz - hw, cx + cell - hw, cz + hw);
                if (g.IsRoad(c, r + 1) && (kind == CellKind.Minor || g.kind[g.Index(c, r + 1)] == kind)) Quad(cx - hw, cz + hw, cx + hw, cz + cell - hw);
                if (kind == CellKind.Minor)
                {
                    // 서·남 이웃이 큰길이면 큰길 메시가 안 그리니 골목이 그린다
                    if (g.IsRoad(c - 1, r) && g.kind[g.Index(c - 1, r)] == CellKind.Major) Quad(cx - cell + hw, cz - hw, cx - hw, cz + hw);
                    if (g.IsRoad(c, r - 1) && g.kind[g.Index(c, r - 1)] == CellKind.Major) Quad(cx - hw, cz - cell + hw, cx + hw, cz - hw);
                }
            }
        st.verts += verts.Count;
        var m = new Mesh { name = "Roads_" + kind, indexFormat = IndexFormat.UInt32 };
        m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
