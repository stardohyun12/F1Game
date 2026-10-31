using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 과제 2356(D-217 길 그리기 3판): 길 따라 필지 나누기 + 회색 덩어리(층수·지붕 모양만, 외관 X). AI·랜덤 생성 아님 — 규칙 + 고정 seed(같은 입력 = 같은 결과).
//  입력 = 빌더 결과(RoadsV2 차길·갈림목·걷는 길 메시, TerraceBlock 축대·보도·진입로) + 도현 배치(D198_Buildings·FieldPatch) + Roadside 소품 + docs/map/vistas.json + road-sections.json(앞띠 폭).
//  0.5 m 격자 → 블록(축대가 땅을 끊음 = 단 경계 존중) → Vanegas 2012 OBB 분할(길에 면한 앞폭 5~10 m) → 필지마다 덩어리 1채(붙은 집 묶음은 2~3채).
//  결과 = 씬 루트 Parcels_Plan(덩어리 + 범위 밖 OSM 회색 건물 사본), docs/map/parcels-home.json · parcels-home.png. 범위 안 OSM 원본(Buildings)은 끈다 — 「필지 끄기」로 복구.
public static class ParcelBuild
{
    const string RootName = "Parcels_Plan", TownScene = "Assets/Scenes/Map_TownV6.unity", Mats = "Assets/Scenes/Map_TownV6_Mats/";
    const string PrefKey = "PitCrew.Parcels.OnEnter";
    const float C = .5f;                       // 격자 칸
    const int Seed = 217;
    // 규칙 수치(가설 — 보고서 표 참고)
    const float FrontReach = 1.5f, MaxDepth = 22f, MinArea = 25f, MinFront = 4.5f;
    const float FootPad = 3f, JuncPad = 4f, MouthPad = 3f, YardR = 30f, YardFallback = 14f;
    const float Bury = .3f, MinStep = .4f;
    // 2판(D-218) 비탈 앉히기: 경사 35% 넘으면 비움 · Δ ≤ 0.68 낮은 기단(A) · Δ ≤ 2.1 바닥 = 최저 + 0.5, 오르막 쪽 묻힘(B 패드+축대) · 그 위는 둘로 나눈 단차 집(C)
    // 3판(D-220): 경사 60% 까지 — 오르막 쪽 묻힘 1.6 넘고 3.1 까지면 반지하 한 채(C1, 아래층이 내리막 쪽에 드러남) · 그보다 크면 경사 쪽 둘로(C2)
    const float MaxSlope = .60f, LowD = .68f, PadLift = .5f, BuryMax = 1.6f, HillBury = 3.1f, BackMin = 30f;
    const string MixPath = "/../../docs/map/parcel-types.json";
    static readonly string[] TypeIds = { "detached", "row", "bungalow", "narrow3", "shop", "flat", "stepped2", "barn" };
    static readonly string[] ZoneIds = { "tomo", "camo", "posi", "ridge", "outer" };
    class Zm { public float row, g0, g1, s0, s1; public Dictionary<string, float> w = new Dictionary<string, float>(); }
    static Dictionary<string, Zm> mix; static byte[] zc; static List<Rect> ridgeR; static float[] dOff;
    // 마당 패드: 앞 1.5 m · 옆 0.8 m, 깎기·돋우기 각 0.65 m 까지, 둘레 2 m 에서 원지형으로, 길·앞띠·축대·비움에서 1 m 안은 안 건드림
    const float Apron = 1.5f, ApronSide = .8f, FillMax = .5f, CutMax = .65f, Ring = 2f, Guard = 1f, WallMinH = .3f;
    const float MergeA = 55f, MergeMax = 260f, RowShare = .5f;
    const string BaseTerrain = Mats + "Mesh_TownV6_Terrain_terrace1.asset", ParcelTerrain = Mats + "Mesh_TownV6_Terrain_parcels.asset";
    const string TreePrefab = "Assets/Art/LofiPilot/CoastalTree.prefab", ShrubPrefab = "Assets/Art/LofiPilot/Shrub.prefab";
    static Rect area;

    public static bool OnEnter { get => EditorPrefs.GetBool(PrefKey, true); set => EditorPrefs.SetBool(PrefKey, value); }
    [MenuItem("PitCrew/필지 다시 나누기")] static void MenuRun() => Debug.Log("[Parcels] " + Run());
    [MenuItem("PitCrew/필지: 길 그리기 Enter 때 같이")] static void MenuToggle() => OnEnter = !OnEnter;
    [MenuItem("PitCrew/필지: 길 그리기 Enter 때 같이", true)] static bool MenuToggleV() { Menu.SetChecked("PitCrew/필지: 길 그리기 Enter 때 같이", OnEnter); return true; }
    [MenuItem("PitCrew/필지 끄기 (OSM 회색 건물 되돌리기)")] static void MenuOff() => Debug.Log("[Parcels] " + Remove());

    // ── 격자 ──────────────────────────────────────────────────
    enum T : byte { Out, Land, Road, Strip, Wall, Excl }
    static Vector2 g0; static int gw, gh; static T[] type; static float[] dist, soff; static int[] src; static bool[] front, foot;
    static int[] owner; static int tagTok; static int[] tag;
    static Vector2[] centers; static float radius;
    static readonly RaycastHit[] buf = new RaycastHit[64];

    class Parcel { public int id; public List<int> cells; public Vector2 toRoad, t, ctr; public float frontW, depth, area, fitW, slope = -1; public string road, empty, zone = "outer", type; public bool back; public Mass mass, mass2; public List<string> types = new List<string>(); public int group = -1; public HashSet<int> nb = new HashSet<int>(); }
    class Mass
    {
        public int parcel, group, member, floors, tier = -1, idx, doorSide = -1, tone; public Vector2 c, f; public float w, d, b, y0, gMin, gMax, H, floorH, pitch, rise, bot, bury, slope, expo, gx, gz; public string roof, seat, type; public bool ridgeX, plinth, attL, attR, door, doorBack, splitT, shutter, barnDoor, roofRoom; public Mass pair; public Transform go;
        public Mass Clone() => (Mass)MemberwiseClone();
    }
    class Left { public string kind; public float area; public Vector2 ctr; public List<int> cells; }

    public static string Remove()
    {
        var root = GameObject.Find(RootName); if (root != null) Object.DestroyImmediate(root);
        var b = Top()?.Find("Buildings"); if (b != null) b.gameObject.SetActive(true);
        var terr = Top()?.Find("TownV6_Terrain"); var baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BaseTerrain); if (terr != null && baseMesh != null) SetTerrain(terr, baseMesh);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "필지 지움 · OSM 회색 건물 다시 켬 · 지형 terrace1 로 되돌림";
    }
    static StringBuilder tqs; static System.Diagnostics.Stopwatch tsw; static void Tq(string n) => tqs?.Append($" {n}{tsw.Elapsed.TotalSeconds:F1}");
    static void SetTerrain(Transform terr, Mesh m)
    {
        var mf = terr.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != m) mf.sharedMesh = m;
        var mc = terr.GetComponent<MeshCollider>(); if (mc != null && mc.sharedMesh != m) { mc.sharedMesh = null; mc.sharedMesh = m; }
        Physics.SyncTransforms();
    }
    static Transform Top() => GameObject.Find("TownV6_Gray")?.transform;

    public static string Run()
    {
        if (Application.isPlaying) return "Play 중엔 안 됨";
        if (EditorSceneManager.GetActiveScene().path != TownScene) return "Map_TownV6 아님";
        skipRoot.Clear(); var sw = System.Diagnostics.Stopwatch.StartNew(); tqs = new StringBuilder(); tsw = sw;
        var tbl = JsonUtility.FromJson<RoadsideBuild.RsFile>(System.IO.File.ReadAllText(RoadsideBuild.TablePath, Encoding.UTF8));
        radius = tbl.radius; centers = new Vector2[tbl.centers.Length / 2]; for (int i = 0; i < centers.Length; i++) centers[i] = new Vector2(tbl.centers[2 * i], tbl.centers[2 * i + 1]);
        var top = Top();
        // 2판: 입력 지형은 매번 TerraceBlock 결과(terrace1) — 패드는 그 위에 새로 깎는다
        var terr = top.Find("TownV6_Terrain"); var baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BaseTerrain);
        if (terr == null || baseMesh == null) return "지형 없음(TownV6_Terrain · terrace1)";
        Tq("tbl"); SetTerrain(terr, baseMesh); Tq("setT");
        var tb0 = terr.GetComponent<MeshRenderer>().bounds; area = Rect.MinMaxRect(tb0.min.x + 2, tb0.min.z + 2, tb0.max.x - 2, tb0.max.z - 2);
        var root = GameObject.Find(RootName); if (root != null) Object.DestroyImmediate(root);
        root = new GameObject(RootName);
        var osm = ClipOsm(top, root.transform);
        Physics.SyncTransforms(); Tq("osm");

        // 1. 격자 분류
        string gnote = Classify(top, tbl);
        Tq("classify"); var fnote = FillSunk(terr, ref baseMesh, out var filled); gnote += " · " + fnote; Tq("fill");
        mix = LoadMix(); dOff = new float[type.Length];
        // 2. 블록 → 분할
        var land = new List<int>(); for (int i = 0; i < type.Length; i++) if (type[i] == T.Land) land.Add(i);
        var blocks = Comps(land);
        var parcels = new List<Parcel>(); var lefts = new List<Left>();
        int bi = 0; foreach (var b in blocks) Split(b, bi++, parcels, lefts);
        Tq("split"); int nSplit = parcels.Count; Merge(parcels); Tq("merge");
        cellsOf = parcels;
        foreach (var p in parcels) foreach (var c in p.cells) { int i = c % gw, j = c / gw; foreach (var q in new[] { c - 1, c + 1, c - gw, c + gw }) if (q >= 0 && q < owner.Length && owner[q] >= 0 && owner[q] != p.id && Mathf.Abs(q % gw - i) <= 1) p.nb.Add(owner[q]); }
        Tq("nb"); var blk = new bool[type.Length]; for (int k = 0; k < blk.Length; k++) blk[k] = type[k] != T.Land || foot[k]; DT(blk, out dBlk, out _);
        double tSplit = sw.Elapsed.TotalSeconds;
        // 3. 덩어리(앉히기 A/B/C 포함)
        var vis = Vistas(); var props = Props();
        Tq("vis"); var masses = new List<Mass>(); Masses(parcels, masses, vis, props);
        double tMass = sw.Elapsed.TotalSeconds;
        // 4. 지형 패드 + 축대 → 새 땅으로 바닥·밑면 다시 읽기
        var mats = Materials();
        var tnote = Pads(terr, baseMesh, masses, root.transform, out var walls);
        if (filled != null) Object.DestroyImmediate(filled);   // 메운 땅은 패드 결과(parcels 에셋)에 들어갔다
        Physics.SyncTransforms(); Reseat(masses);
        double tPad = sw.Elapsed.TotalSeconds;
        // 5. 씬에 놓기 + 빈 필지 나무
        var mroot = new GameObject("Masses").transform; mroot.SetParent(root.transform, false);
        nRepair = Repair(parcels, masses); nWin = nDoor = nStoop = 0;
        for (int i = 0; i < masses.Count; i++) { masses[i].idx = i; masses[i].go = Spawn(masses[i], mroot, mats); }
        var snote = Stairs(parcels, masses, root.transform, mats);
        var gnote2 = Greens(parcels, root.transform) + " · " + snote;
        // 6. 검사 + 기록
        nPits = Pits(parcels, lefts);
        var chk = Check(parcels, masses, props);
        chk.terrain = tnote; chk.walls = walls; chk.time = $"[{tqs}] 나누기 {tSplit:F1}s · 덩어리 {tMass - tSplit:F1}s · 지형 {tPad - tMass:F1}s · 놓기·검사 {sw.Elapsed.TotalSeconds - tPad:F1}s";
        var json = Json(parcels, lefts, masses, chk, gnote + $" · 합치기 {nSplit}→{parcels.Count}", osm + " · " + gnote2);
        System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/map/parcels-home.json"), json, new UTF8Encoding(false));
        Png(parcels, masses, System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/map/parcels-home.png"));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        int built = parcels.Count(p => p.mass != null), sea = parcels.Count(p => p.mass == null && p.empty == "땅 못 읽음·바다");
        return $"필지 {sw.Elapsed.TotalSeconds:F1}s ({chk.time}) · 블록 {blocks.Count} · 필지 {parcels.Count}(합치기 전 {nSplit}) · 채움 {built}/{parcels.Count} = {100f * built / Mathf.Max(1, parcels.Count):F0}% · 바다 뺀 땅 필지 {built}/{parcels.Count - sea} = {100f * built / Mathf.Max(1, parcels.Count - sea):F0}% · 바닥 내림 {nLowered} · 덩어리 {masses.Count} · " + chk.summary + " · " + tnote + " · " + gnote2 + " · " + osm;
    }

    // ── 범위 안 OSM 회색 건물 숨기기: 원본은 끄고, 범위 밖 건물만 잘라 사본 ────────
    static string ClipOsm(Transform top, Transform root)
    {
        var b = top.Find("Buildings"); if (b == null) return "OSM 없음";
        var holder = new GameObject("_OSM_Outside").transform; holder.SetParent(root, false);
        int cut = 0, kept = 0;
        foreach (Transform ch in b)
        {
            var mf = ch.GetComponent<MeshFilter>(); var mr = ch.GetComponent<MeshRenderer>(); if (mf == null || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; var v = m.vertices; var wv = v.Select(q => ch.TransformPoint(q)).ToArray();
            // 위치가 같은 정점끼리 묶어 건물 하나 = 이어진 삼각형 덩어리
            var uf = Enumerable.Range(0, v.Length).ToArray(); int Find(int x) { while (uf[x] != x) { uf[x] = uf[uf[x]]; x = uf[x]; } return x; }
            var weld = new Dictionary<(int, int, int), int>();
            for (int i = 0; i < v.Length; i++) { var k = (Mathf.RoundToInt(wv[i].x * 100), Mathf.RoundToInt(wv[i].y * 100), Mathf.RoundToInt(wv[i].z * 100)); if (weld.TryGetValue(k, out var o)) uf[Find(i)] = Find(o); else weld[k] = i; }
            for (int s = 0; s < m.subMeshCount; s++) { var tr = m.GetTriangles(s); for (int t = 0; t < tr.Length; t += 3) { uf[Find(tr[t + 1])] = Find(tr[t]); uf[Find(tr[t + 2])] = Find(tr[t]); } }
            var sum = new Dictionary<int, (Vector2 s, int n)>();
            for (int i = 0; i < v.Length; i++) { int r = Find(i); sum.TryGetValue(r, out var a); sum[r] = (a.s + new Vector2(wv[i].x, wv[i].z), a.n + 1); }
            var inside = new HashSet<int>(sum.Where(kv => InR(kv.Value.s / kv.Value.n)).Select(kv => kv.Key));
            var nm = Object.Instantiate(m); nm.name = m.name + "_outside"; int kept0 = kept;
            for (int s = 0; s < m.subMeshCount; s++)
            {
                var tr = m.GetTriangles(s); var keep = new List<int>();
                for (int t = 0; t < tr.Length; t += 3) { if (inside.Contains(Find(tr[t]))) { cut++; continue; } keep.Add(tr[t]); keep.Add(tr[t + 1]); keep.Add(tr[t + 2]); kept++; }
                nm.SetTriangles(keep, s);
            }
            if (kept == kept0) { Object.DestroyImmediate(nm); continue; }   // 2판(맵 전체): 밖에 남는 게 없으면 사본 안 만듦
            var go = new GameObject(ch.name + "_outside"); go.transform.SetParent(holder, false); go.transform.SetPositionAndRotation(ch.position, ch.rotation); go.transform.localScale = ch.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = nm; var r2 = go.AddComponent<MeshRenderer>(); if (mr != null) r2.sharedMaterials = mr.sharedMaterials;
            if (ch.GetComponent<MeshCollider>() != null) go.AddComponent<MeshCollider>().sharedMesh = nm;
        }
        b.gameObject.SetActive(false); if (holder.childCount == 0) Object.DestroyImmediate(holder.gameObject);
        return $"OSM 회색 건물 범위 안 삼각형 {cut} 숨김(밖 {kept} 유지)";
    }

    // ── 격자 분류 ──────────────────────────────────────────────
    static string Classify(Transform top, RoadsideBuild.RsFile tbl)
    {
        float mnx = area.xMin, mxx = area.xMax, mnz = area.yMin, mxz = area.yMax;   // 2판: 맵 전체(지형 경계 안)
        g0 = new Vector2(Mathf.Floor(mnx), Mathf.Floor(mnz)); gw = Mathf.CeilToInt((mxx - g0.x) / C); gh = Mathf.CeilToInt((mxz - g0.y) / C);
        int N = gw * gh; type = new T[N]; var road = new bool[N]; var junc = new bool[N]; var walkw = new bool[N]; var wall = new bool[N]; foot = new bool[N]; var off = new float[N];
        for (int k = 0; k < N; k++) type[k] = InR(P(k)) ? T.Land : T.Out;
        float SecOff(string kind, float w, bool walk)
        {
            var s = tbl.sections.FirstOrDefault(x => x.walk == walk && x.kinds.Contains(kind) && w >= x.minW && w < x.maxW); if (s == null) return .5f;
            return s.slots.Where(q => q.name == "연석" || q.name == "보도" || q.name == "소품 줄").Sum(q => Mathf.Max(0, q.w));
        }
        // 3판: 길 칸마다 구역(D-205 — RoadsideBuild.Zone 과 같은 규칙: 이름의 tomo/camo/posi/ridge, 걷는 길·손 길 camo, 나머지 outer)
        zc = Enumerable.Repeat((byte)255, N).ToArray();
        byte ZoneByName(string n) { int z = n.Contains("ridge") ? 3 : n.Contains("tomo") ? 0 : n.Contains("camo") ? 1 : n.Contains("posi") ? 2 : 4; if (z == 4 && (n.Contains("hand_") || n.StartsWith("ROAD_V2p"))) z = 1; return (byte)z; }
        foreach (Transform ch in top.Find("RoadsV2"))
        {
            if (!ch.gameObject.activeInHierarchy) continue;
            float o = .5f; bool j = ch.name.StartsWith("ROAD_V2j");
            var rs = ch.GetComponent<RoadSurface>(); if (rs != null) o = SecOff(rs.kind, rs.width, false); else if (j) o = SecOff("village", 5, false);
            RasterMesh(ch, road, 0, off, o, j ? null : zc, ZoneByName(ch.name)); if (j) RasterMesh(ch, junc, 0, null, 0);
        }
        Tq("cRoads");
        var tb = top.Find("TerraceBlock");
        if (tb != null)
        {
            var dr = tb.Find("Driveway"); if (dr != null) RasterMesh(dr, road, 0, off, .5f, zc, 3);
            var wk = tb.Find("Walkways"); if (wk != null) RasterMesh(wk, walkw, 0, off, .3f, zc, 1);
            var rw = tb.Find("RetainingWalls"); if (rw != null) RasterMesh(rw, wall, .2f, null, 0);
            var fp = tb.Find("FieldPatch"); if (fp != null) foreach (var mf in fp.GetComponentsInChildren<MeshFilter>()) RasterMesh(mf.transform, foot, 0, null, 0);
            var dp = tb.Find("DirtPath_door_garden"); if (dp != null) RasterMesh(dp, foot, 0, null, 0);
        }
        Tq("cTerr"); var d198 = top.Find("D198_Buildings"); if (d198 != null) foreach (var mf in d198.GetComponentsInChildren<MeshFilter>()) RasterMesh(mf.transform, foot, 0, null, 0);
        ridgeR = new List<Rect>();
        if (d198 != null && tbl.ridgeCore != null) foreach (var id in tbl.ridgeCore)
        {
            var bd = d198.Find(id); if (bd == null) continue; var rs = bd.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var x in rs) bb.Encapsulate(x.bounds);
            ridgeR.Add(Rect.MinMaxRect(bb.min.x - tbl.ridgeRadius, bb.min.z - tbl.ridgeRadius, bb.max.x + tbl.ridgeRadius, bb.max.z + tbl.ridgeRadius));
        }
        // 길·보도에서 거리(벡터 전파) — 가장 가까운 길 칸과 그 길의 앞띠 폭
        var seed = new bool[N]; for (int k = 0; k < N; k++) seed[k] = road[k] || walkw[k];
        Tq("cD198"); DT(seed, out dist, out src); soff = off; Tq("cDT1");
        for (int k = 0; k < N; k++)
        {
            if (type[k] == T.Out) continue;
            if (road[k]) type[k] = T.Road; else if (wall[k]) type[k] = T.Wall; else if (walkw[k] || (src[k] >= 0 && dist[k] <= off[src[k]] + C * .5f)) type[k] = T.Strip;
        }
        // 비움: 도현 건물 +3 m · 갈림목 +4 m · 걷는 길 입구 3 m · 집 마당 단
        DT(foot, out var dF, out _); DT(junc, out var dJ, out _);
        var ms = new bool[N]; foreach (var m in Mouths()) { int mk = Idx(m); if (mk >= 0) ms[mk] = true; }
        Tq("cDT2"); DT(ms, out var dM, out _); int nEx = 0; Tq("cDT4");
        for (int k = 0; k < N; k++)
        {
            if (type[k] != T.Land) continue;
            if (dF[k] <= FootPad || dJ[k] <= JuncPad || dM[k] < MouthPad) { type[k] = T.Excl; nEx++; }
        }
        string yard = Yard(d198); Tq("cYard");
        front = new bool[N]; for (int k = 0; k < N; k++) front[k] = type[k] == T.Land && src[k] >= 0 && dist[k] <= off[src[k]] + FrontReach + C;
        tag = new int[N];
        int Cn(T t) => type.Count(x => x == t);
        return $"격자 {gw}×{gh}(0.5 m) · 땅 {Cn(T.Land) * C * C:F0}㎡ · 길 {Cn(T.Road) * C * C:F0} · 앞띠 {Cn(T.Strip) * C * C:F0} · 축대 {Cn(T.Wall) * C * C:F0} · 비움 {Cn(T.Excl) * C * C:F0}㎡ · " + yard;
    }
    static List<Vector2> Mouths()
    {
        var res = new List<Vector2>(); var hand = RoadPaintData.Load(); var del = new HashSet<string>(hand?.deleted ?? new List<string>());
        var srcs = BuildBayLoop.RoadPaintSources().Where(s => s.walk && !del.Contains(s.id)).Select(s => s.pts).ToList();
        if (hand != null) foreach (var h in hand.roads) if (h.walk) srcs.Add(RoadPaintData.Pts(h));
        foreach (var p in srcs) if (p.Count >= 2) foreach (var e in new[] { p[0], p[p.Count - 1] }) { var q = new Vector2(e.x, e.z); if (InR(q)) res.Add(q); }
        return res;
    }
    // 집 마당 = 집에서 길·앞띠·축대에 안 막히고 닿는 땅. 30 m 안에서 닫히면 통째로 비움, 안 닫히면 집 둘레 14 m.
    static string Yard(Transform d198)
    {
        var h = d198 != null ? d198.Find("house_home") : null; if (h == null) return "집 없음";
        var hc = new Vector2(h.position.x, h.position.z); int s = Idx(hc); if (s < 0) return "집 격자 밖";
        var seen = new HashSet<int> { s }; var q = new Queue<int>(); q.Enqueue(s); bool open = false;
        while (q.Count > 0)
        {
            int c = q.Dequeue(); int i = c % gw, j = c / gw;
            foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int ni = i + di, nj = j + dj; if (ni < 0 || nj < 0 || ni >= gw || nj >= gh) continue; int n = nj * gw + ni;
                if (seen.Contains(n) || !(type[n] == T.Land || type[n] == T.Excl)) continue;
                if ((P(n) - hc).magnitude > YardR) { open = true; continue; }
                seen.Add(n); q.Enqueue(n);
            }
        }
        if (!open) { foreach (var c in seen) type[c] = T.Excl; return $"집 마당 닫힌 단 {seen.Count * C * C:F0}㎡ 비움"; }
        for (int k = 0; k < type.Length; k++) if (type[k] == T.Land && (P(k) - hc).magnitude < YardFallback) type[k] = T.Excl;
        return "집 마당 안 닫힘 → 둘레 14 m 비움";
    }

    // ── 블록 나누기(Vanegas 2012 OBB 분할, 격자판) ──────────────────
    static void Split(List<int> block, int bi, List<Parcel> outP, List<Left> lefts)
    {
        var st = new Stack<(List<int> c, int lv, bool back)>(); st.Push((block, 0, false));
        // 3판: 길에 안 닿는 땅(축대로 둘러싸인 깎은 단 = 「빈 구덩이」 포함)·22 m 뒤 빈터는 한 번만 「골목 안쪽 필지」로 — 길에 가장 가까운 1.5 m 띠를 앞선으로 삼는다
        void Promote(List<int> cs) { float mn = cs.Min(c => Dep(c)); foreach (var c in cs) { dOff[c] += mn; front[c] = Dep(c) <= FrontReach + C; } st.Push((cs, 0, true)); }
        while (st.Count > 0)
        {
            var (cells, lv, back) = st.Pop(); float area = cells.Count * C * C;
            if (area < MinArea) { lefts.Add(Lf("빈터", cells)); continue; }
            var F = cells.Where(c => front[c]).ToList();
            if (F.Count == 0) { if (!back && area >= BackMin || back && area >= 60 && area <= 400 && lv < 40) { Promote(cells); continue; } lefts.Add(Lf(area >= 60 ? "밭·빈터" : "빈터", cells)); continue; }
            Vector2 ns = Vector2.zero; foreach (var c in F) { var d = P(src[c]) - P(c); if (d.sqrMagnitude > 1e-6f) ns += d.normalized; }
            float R = ns.magnitude / F.Count; var toRoad = ns.sqrMagnitude > 1e-8f ? ns.normalized : Vector2.up; var t = new Vector2(toRoad.y, -toRoad.x);
            var rng = new Rng(Fnv($"{Seed}|{cells[0]}|{cells.Count}|{lv}"));
            List<List<int>> kids = null;
            if (lv < 40 && R < .65f)
            {
                // 여러 쪽이 길(양쪽 길 사이 블록·모퉁이): 칸마다 「가장 가까운 길 방향」으로 가른다 = 등 맞댄 필지선(가운데선)
                // 길 방향들의 분산 주축(맞은편 두 길 → 그 축, ㄱ자 모퉁이 → 두 방향 사이) 부호로 둘
                var mn = ns / F.Count; double xx = 0, xz = 0, zz = 0;
                foreach (var c in F) { var d = (P(src[c]) - P(c)).normalized - mn; xx += d.x * d.x; xz += d.x * d.y; zz += d.y * d.y; }
                float th = .5f * Mathf.Atan2((float)(2 * xz), (float)(xx - zz)); var un = new Vector2(Mathf.Cos(th), Mathf.Sin(th));
                kids = Cut(cells, c => src[c] >= 0 && Vector2.Dot((P(src[c]) - P(c)).normalized - mn, un) >= 0);
                if (kids.Count >= 2 && kids.All(k => k.Count < cells.Count)) { for (int i = kids.Count - 1; i >= 0; i--) st.Push((kids[i], lv + 1, back)); continue; }
                kids = null;
            }
            if (kids == null && lv < 40 && R < .65f && area > 500)
            {
                // 큰 블록·여러 쪽이 길: OBB 긴 축에 수직으로 반 가름(±10%)
                Pca(cells, out var mean, out var u); float lo = 1e9f, hi = -1e9f; foreach (var c in cells) { float x = Vector2.Dot(P(c) - mean, u); lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x); }
                float cut = (lo + hi) * .5f + (rng.F() * 2 - 1) * .1f * (hi - lo);
                kids = Cut(cells, c => Vector2.Dot(P(c) - mean, u) < cut);
            }
            else
            {
                float tmin = 1e9f, tmax = -1e9f; foreach (var c in F) { float x = Vector2.Dot(P(c), t); tmin = Mathf.Min(tmin, x); tmax = Mathf.Max(tmax, x); }
                float Lfr = tmax - tmin + C, W = Width(rng); int n = Mathf.Max(Mathf.RoundToInt(Lfr / W), Mathf.CeilToInt(Lfr / 10.5f - .001f));   // 10.5 m 넘는 앞폭은 반드시 한 번 더
                if (n >= 2 && lv < 40)
                {
                    // 앞선을 n 칸 폭으로 — 가운데(n/2)에서 한 번 가르고 양쪽은 새 seed 로 폭을 다시 뽑는다(줄 맞춤 X)
                    float cut = tmin + Lfr * (n / 2) / n + (rng.F() * 2 - 1) * .5f;
                    kids = Cut(cells, c => Vector2.Dot(P(c), t) < cut);
                }
                else
                {
                    // 마지막 필지: 22 m 보다 깊은 뒤는 마당·밭·빈터로 남김
                    // 3판: 비탈(앞선 → 12 m 안쪽 높이 차 15% 이상)이면 깊이 13 m 까지만 — 그 뒤는 골목 안쪽 필지로 올라가 층층이 됨
                    float maxD = MaxDepth;
                    {
                        var fm = Mean(F); var inner = cells.Where(c => Dep(c) > 10 && Dep(c) < 13).ToList();
                        if (inner.Count > 4) { var im = Mean(inner); float y0 = TopY(fm), y1 = TopY(im); if (!float.IsNaN(y0) && !float.IsNaN(y1) && Mathf.Abs(y1 - y0) / Mathf.Max(1f, (im - fm).magnitude) >= .15f) maxD = 13f; }
                    }
                    var keep = cells.Where(c => Dep(c) <= maxD).ToList(); var bk = cells.Where(c => Dep(c) > maxD).ToList();
                    var kc = Comps(keep); var main = kc.OrderByDescending(k => k.Count(c => front[c])).ThenBy(k => k[0]).FirstOrDefault();
                    foreach (var k in kc) if (k != main) bk.AddRange(k);
                    // 골목 안쪽의 또 안쪽: 구덩이 크기(60~400㎡)만 한 번 더
                    if (bk.Count > 0) foreach (var k in Comps(bk.OrderBy(x => x).ToList())) { float ka = k.Count * C * C; if (!back && ka >= BackMin || back && ka >= 60 && ka <= 400) Promote(k); else lefts.Add(Lf(k.Count * C * C >= 60 ? "뒤 마당·밭" : "뒤 빈터", k)); }
                    // 3판: 길에 좁은 목으로만 닿은 넓은 땅(깎은 단 안쪽 등) → 폭이 MinFront+1 m 되는 깊이부터를 골목 안쪽 필지로, 목은 남김
                    if (main != null && Lfr < MinFront && main.Count * C * C >= 60 && !(back && main.Count * C * C > 400))
                    {
                        float best = -1;
                        for (float d = 1; d <= 12; d += .5f)
                        {
                            float a0 = 1e9f, a1 = -1e9f; foreach (var c in main) { float dp = Dep(c); if (dp < d || dp >= d + 1.5f) continue; float x = Vector2.Dot(P(c), t); a0 = Mathf.Min(a0, x); a1 = Mathf.Max(a1, x); }
                            if (a1 > a0 && a1 - a0 + C >= MinFront + 1) { best = d; break; }
                        }
                        if (best > 0)
                        {
                            var neck = main.Where(c => Dep(c) < best).ToList(); var rest = main.Where(c => Dep(c) >= best).ToList();
                            foreach (var c in neck) front[c] = false; if (neck.Count > 0) lefts.Add(Lf("골목 목", neck));
                            foreach (var c in rest) { dOff[c] += best; front[c] = Dep(c) <= FrontReach + C; }
                            foreach (var k in Comps(rest.OrderBy(x => x).ToList())) st.Push((k, 0, true));
                            continue;
                        }
                    }
                    if (main == null || Lfr < MinFront || main.Count * C * C < MinArea) { if (main != null) lefts.Add(Lf("좁은 틈", main)); continue; }
                    var p = new Parcel { id = outP.Count, cells = main, toRoad = toRoad, t = t, frontW = Lfr, area = main.Count * C * C, back = back };
                    p.depth = main.Max(c => Dep(c)); p.ctr = Mean(main); p.road = back ? "골목 안쪽" : RoadName(F[F.Count / 2]);
                    outP.Add(p); continue;
                }
            }
            if (kids == null || kids.Count == 0 || kids.Any(k => k.Count == cells.Count)) { lefts.Add(Lf("분할 실패", cells)); continue; }
            for (int i = kids.Count - 1; i >= 0; i--) st.Push((kids[i], lv + 1, back));
        }
    }
    // 2판(D-218 2): 55 ㎡ 안 되는 필지는 길 방향이 같은(30° 안) 이웃 중 맞닿은 변이 가장 긴 필지에 합친다(합쳐 260 ㎡ 까지). 작은 것부터, id 순 — 결정적.
    static void Merge(List<Parcel> ps)
    {
        owner = Enumerable.Repeat(-1, type.Length).ToArray();
        foreach (var p in ps) foreach (var c in p.cells) owner[c] = p.id;
        foreach (var p in ps.OrderBy(x => x.area).ThenBy(x => x.id).ToList())
        {
            if (p.cells == null || p.area >= MergeA) continue;
            var cnt = new Dictionary<int, int>();
            foreach (var c in p.cells) { int i = c % gw; foreach (var q in new[] { c - 1, c + 1, c - gw, c + gw }) { if (q < 0 || q >= owner.Length || Mathf.Abs(q % gw - i) > 1) continue; int o = owner[q]; if (o >= 0 && o != p.id) { cnt.TryGetValue(o, out var n); cnt[o] = n + 1; } } }
            var best = cnt.Where(kv => ps[kv.Key].cells != null && Vector2.Angle(ps[kv.Key].toRoad, p.toRoad) < 30 && ps[kv.Key].area + p.area <= MergeMax).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key).DefaultIfEmpty(-1).First();
            if (best < 0) continue;
            var q2 = ps[best]; q2.cells.AddRange(p.cells); q2.cells.Sort(); foreach (var c in p.cells) owner[c] = q2.id; q2.area += p.area;
            var F = q2.cells.Where(c => front[c]).ToList();
            if (F.Count > 0) { float a = F.Min(c => Vector2.Dot(P(c), q2.t)), b = F.Max(c => Vector2.Dot(P(c), q2.t)); q2.frontW = b - a + C; }
            q2.depth = q2.cells.Max(c => Dep(c)); q2.ctr = Mean(q2.cells);
            p.cells = null;
        }
        ps.RemoveAll(p => p.cells == null);
        for (int i = 0; i < ps.Count; i++) ps[i].id = i;
        owner = Enumerable.Repeat(-1, type.Length).ToArray();
        foreach (var p in ps) foreach (var c in p.cells) owner[c] = p.id;
    }
    static float Width(Rng r) { float u = r.F(); return u < .7f ? 6 + 2 * r.F() : u < .85f ? 5 + r.F() : 8 + 2 * r.F(); }   // 70% 6~8 · 15% 5~6 · 15% 8~10
    static List<List<int>> Cut(List<int> cells, System.Func<int, bool> left)
    {
        var a = cells.Where(left).ToList(); var b = cells.Where(c => !left(c)).ToList(); var res = new List<List<int>>();
        if (a.Count > 0) res.AddRange(Comps(a)); if (b.Count > 0) res.AddRange(Comps(b)); return res;
    }
    static Left Lf(string k, List<int> c) => new Left { kind = k, area = c.Count * C * C, ctr = Mean(c), cells = c };
    // 앞선에서 깊이: 길 앞띠 끝에서 잰 거리 − 골목 안쪽 필지면 그 필지 앞선까지 거리
    static float Dep(int c) => src[c] >= 0 ? dist[c] - soff[src[c]] - dOff[c] : 0;
    static string RoadName(int c) { return src[c] >= 0 ? (soff[src[c]] > 2.6f ? "main" : soff[src[c]] > 2f ? "village" : soff[src[c]] > .4f ? "lane·walk" : "보도") : "?"; }

    // ── 덩어리 ──────────────────────────────────────────────────
    class Vis { public List<(Vector2 p, float eye, float a0, float a1)> ov = new List<(Vector2, float, float, float)>(); public List<(Vector2 a, Vector2 b, float eye)> los = new List<(Vector2, Vector2, float)>(); }
    static void Masses(List<Parcel> parcels, List<Mass> masses, Vis vis, List<(Vector2 c, Vector2 h)> props)
    {
        foreach (var p in parcels) p.zone = ZoneOf(p);
        // 붙은 집 묶음: 앞 방향이 비슷한(25° 안) 이웃 필지를 +t 쪽으로 2~3채, 구역 비율(rowShare). 옆에 묶음이 이미 둘 연속이면 안 묶는다(같은 유형 3연속 금지)
        int gid = 0;
        foreach (var p in parcels)
        {
            if (p.group >= 0) continue; var rng = new Rng(Fnv($"{Seed}|grp|{p.id}"));
            if (rng.F() >= mix[p.zone].row) continue; int want = rng.F() < .6f ? 2 : 3; var mem = new List<Parcel> { p }; var cur = p;
            if (RowRun(p, parcels)) continue;
            while (mem.Count < want)
            {
                var nx = cur.nb.Select(i => parcels[i]).Where(q => q.group < 0 && !mem.Contains(q) && Vector2.Angle(q.toRoad, p.toRoad) < 25 && Vector2.Dot(q.ctr - cur.ctr, p.t) > 2f).OrderBy(q => Vector2.Dot(q.ctr - cur.ctr, p.t)).FirstOrDefault();
                if (nx == null) break; mem.Add(nx); cur = nx;
            }
            if (mem.Count < 2 || GroupRun(mem, parcels)) continue; foreach (var q in mem) q.group = gid; gid++;
            foreach (var q in mem) q.type = "row";
        }
        // 남은 3연속(묶음-묶음-묶음): 가운데 묶음을 풀어 한 채씩(유형은 아래에서 따로 뽑음)
        foreach (var g in parcels.Where(p => p.group >= 0).GroupBy(p => p.group).OrderBy(g => g.Key).ToList())
        {
            var mem = g.ToList(); if (mem[0].group < 0) continue;
            int n = mem.SelectMany(p => SideNb(p, parcels)).Where(q => q.group >= 0 && q.group != g.Key && q.type == "row").Select(q => q.group).Distinct().Count();
            if (n >= 2) foreach (var p in mem) { p.group = -1; p.type = null; }
        }
        var groups = parcels.GroupBy(p => p.group < 0 ? -1 - p.id : p.group).OrderBy(g => g.Min(p => p.id)).Select(g => (mem: g.OrderBy(p => Vector2.Dot(p.ctr, g.First().t)).ToList(), type: (string)null)).ToList();
        // 안 들어가면: 묶음은 풀어 한 채씩, 한 채 유형은 단독 박공으로 한 번 더(대기열 뒤에)
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var mem = groups[gi].mem; bool att = mem.Count > 1; var p0 = mem[0]; var zm = mix[p0.zone];
            var f0 = Vector2.zero; foreach (var p in mem) f0 += p.toRoad * p.frontW; f0.Normalize();
            var set = new HashSet<int>(mem.Select(p => p.id));
            var rng = new Rng(Fnv($"{Seed}|mass|{p0.id}|{groups[gi].type}"));
            string ty = att ? "row" : groups[gi].type ?? PickType(p0, parcels, zm, rng);
            if (!att) p0.type = ty;
            var Fc = mem.SelectMany(p => p.cells).Where(c => front[c]).OrderBy(c => Vector2.Dot(P(c), new Vector2(f0.y, -f0.x))).ToList();
            if (Fc.Count == 0) { foreach (var p in mem) p.empty = "앞선 없음"; continue; }
            // 유형별 치수(가설): 앞 물러남·옆 틈은 구역 표, 가게는 0.3~0.6, 단층 마당집은 +1 m
            float b = att ? zm.s0 + rng.F() * (Mathf.Min(zm.s1, 1.2f) - zm.s0) : ty == "shop" ? .3f + rng.F() * .3f : zm.s0 + rng.F() * (zm.s1 - zm.s0) + (ty == "bungalow" ? 1f : 0);
            int fl0 = ty == "bungalow" || ty == "barn" ? 1 : ty == "narrow3" ? 3 : ty == "detached" || ty == "stepped2" ? 2 : rng.F() < .6f ? 2 : 3;
            float D = ty switch { "bungalow" => 5.5f + rng.F() * 1.5f, "shop" => 8 + rng.F() * 2, "stepped2" => 9 + rng.F() * 2, "barn" => 6 + rng.F() * 2.5f, "row" => 6.5f + rng.F() * 2.5f, _ => 7 + rng.F() * 2 };
            float needOne = ty == "narrow3" ? 3.8f : ty == "bungalow" ? 5f : ty == "barn" ? 6f : ty == "shop" ? 4.5f : 3.6f;
            bool ok = false; float x0 = 0, x1 = 0;
            // 안 들어가면: 깊이 줄임 → 물러남 0.8 m → 앞선을 뒤로(밀어 넣기). 그래도 안 되면 다른 유형/빈 필지.
            var tries = new[] { (0f, 0f, 0f), (-1.5f, 0f, 0f), (-1.5f, -9f, 0f), (-3f, -9f, 0f), (-3f, -9f, -1.5f), (-4f, -9f, -3f), (-9f, -9f, -1f) };
            float gL = zm.g0 + rng.F() * (zm.g1 - zm.g0), gR = zm.g0 + rng.F() * (zm.g1 - zm.g0);
            float bb = b, DD = D, zz = 0, bestW = 0; Vector2 f = f0, t = new Vector2(f0.y, -f0.x);
            var mc = Fc[Fc.Count / 2]; var fm = (P(src[mc]) - P(mc)).sqrMagnitude > 1e-6f ? (P(src[mc]) - P(mc)).normalized : f0;
            var dirs = new List<Vector2> { f0, fm }; foreach (var a in new[] { 12f, -12f, 25f, -25f }) dirs.Add(Rot(f0, a));
            // 4판(판정 결함 3 「집 방향이 제멋대로」): 옆 필지에 이미 선 집이 있고 각도가 20° 안이면 그 방향을 먼저 — 한 줄이 같은 쪽을 본다
            var nbF = mem.SelectMany(p => SideNb(p, parcels)).Where(q => q.mass != null && Vector2.Angle(q.mass.f, f0) < 20).Select(q => q.mass.f).FirstOrDefault();
            if (nbF != Vector2.zero) dirs.Insert(0, nbF);
            // 둘째 판(한 채·단독만): 옆 틈 0.25 m · 폭 3.2 m 까지 — 얕고 좁은 필지의 작은 집
            for (int pass = 0; pass < (att || ty != "detached" ? 1 : 2) && !ok; pass++)
            {
                if (pass == 1) { gL = gR = .25f; }
                float need = pass == 0 ? (att ? 3.6f * mem.Count : needOne) : 3.2f;
                foreach (var fd in dirs)
                {
                    f = fd; t = new Vector2(f.y, -f.x);
                    var F = Fc.Select(c => Vector2.Dot(P(c), f)).OrderBy(z => z).ToList(); float zF = F[F.Count / 2];
                    foreach (var (dD, db, dz) in tries)
                    {
                        DD = Mathf.Max(dD < -5 ? (pass == 1 ? 3.6f : 4f) : 4.5f, D + dD); bb = db < -5 ? (pass == 1 ? .5f : Mathf.Min(b, .8f)) : b; zz = zF + dz;
                        if (Fit(set, f, t, zz - bb - DD, zz - bb, out x0, out x1)) { bestW = Mathf.Max(bestW, x1 - x0); if (x1 - x0 - gL - gR >= need) { ok = true; break; } }
                    }
                    if (ok) break;
                }
            }
            if (!ok)
            {
                if (att) { foreach (var p in mem) { p.group = -1; p.type = null; groups.Add((new List<Parcel> { p }, null)); } continue; }
                if (ty != "detached") { groups.Add((mem, "detached")); continue; }
                foreach (var p in mem) { p.empty = "덩어리 안 들어감(폭·모양)"; p.fitW = bestW; p.type = null; }
                continue;
            }
            x0 += gL; x1 -= gR; float total = x1 - x0;
            // 유형 폭 상한: 좁은 3층 4~5 · 단층 마당집 6~8(한쪽으로 붙여 마당) · 그 밖 한 채 10 m
            float cap = att ? 10f * mem.Count : ty == "narrow3" ? 4 + rng.F() : ty == "bungalow" ? 6 + 2 * rng.F() : 10f;
            if (total > cap) { bool side = ty == "narrow3" || ty == "bungalow" || ty == "barn"; float mid = side ? (rng.F() < .5f ? x0 + cap * .5f : x1 - cap * .5f) : (x0 + x1) * .5f; x0 = mid - cap * .5f; x1 = mid + cap * .5f; total = cap; }
            float sumW = mem.Sum(p => p.frontW); float xa = x0; string prevRoof = null; int prevFl = 0;
            bool avoidC2 = Avoid(p0, parcels).Contains("stepped2");
            for (int k = 0; k < mem.Count; k++)
            {
                var p = mem[k]; float w = k == mem.Count - 1 ? x1 - xa : total * p.frontW / sumW; float xb = xa + w;
                var r2 = new Rng(Fnv($"{Seed}|m|{p.id}"));
                var m = new Mass { parcel = p.id, group = att ? p.group : -1, member = k, f = f, b = bb, d = DD, w = w, attL = att && k > 0, attR = att && k < mem.Count - 1, type = ty };
                m.floorH = 2.75f + r2.F() * .35f; m.pitch = 24 + r2.F() * 8;
                switch (ty)
                {
                    case "row":
                        // 붙은 집: 옆 집과 층·지붕을 다르게
                        m.floors = k == 0 ? fl0 : (r2.F() < .65f ? (prevFl == 2 ? 3 : 2) : prevFl);
                        var rf = new[] { "gable", "hip", "flat" }.Where(x => x != prevRoof).ToArray(); m.roof = rf[(int)(r2.F() * rf.Length) % rf.Length]; m.ridgeX = true; break;
                    case "detached": m.floors = 2; m.roof = "gable"; m.ridgeX = r2.F() < .6f; m.pitch = 26 + r2.F() * 6; break;
                    case "bungalow": m.floors = 1; m.roof = r2.F() < .5f ? "hip" : "gable"; m.ridgeX = r2.F() < .7f; break;
                    case "narrow3": m.floors = 3; if (r2.F() < .6f) { m.roof = "gable"; m.ridgeX = false; } else m.roof = "flat"; break;
                    case "shop": m.floors = fl0; m.roof = r2.F() < .5f ? "flat" : "gable"; m.ridgeX = true; m.shutter = true; break;
                    case "flat": m.floors = fl0; m.roof = "flat"; m.roofRoom = true; break;
                    case "stepped2": m.floors = 2; m.roof = r2.F() < .5f ? "gable" : "flat"; m.ridgeX = true; break;
                    case "barn": m.floors = 1; m.floorH = 3.8f + r2.F() * .6f; m.roof = "gable"; m.ridgeX = true; m.pitch = 14 + r2.F() * 4; m.barnDoor = true; break;
                }
                prevRoof = m.roof; prevFl = m.floors; m.tone = Tone(m.type);
                // 앞면 가운데(피벗) = x 가운데, z = 앞선 - 물러남
                float xc = (xa + xb) * .5f, zc0 = zz - bb; m.c = t * xc + f * zc0;
                xa = xb;
                // 비탈 앉히기 — A 낮은 기단 · B 바닥 = 최저 + 0.5(패드+축대) · C1 반지하 한 채 · C2 경사 쪽 둘로 · 60% 넘으면 비움
                string why = SeatAll(m, att, avoidC2, out var m2); p.slope = m.slope; if (why != null) { p.empty = why; continue; }
                Heights(m, p, parcels); if (m2 != null) Heights(m2, p, parcels);
                why = Blocked(m, vis, props) ?? (m2 != null ? Blocked(m2, vis, props) : null);
                // 전망에 걸리면 단층 평지붕으로 한 번 더(같은 시선 검사를 통과할 때만) — 깎아 놓은 단이 비지 않게
                if (why != null && why.StartsWith("전망") && m2 == null && !att && m.seat != "C1") { m.floors = 1; m.roof = "flat"; m.roofRoom = m.shutter = m.barnDoor = false; m.type = "bungalow"; m.tone = Tone(m.type); Heights(m, p, parcels); why = Blocked(m, vis, props); }
                if (why != null) { p.empty = why; continue; }
                // 옆 필지 덩어리와 처마·기단이 겹치면: 박공 방향을 길 따라로(옆 처마 없음) → 그래도면 모임지붕 → 뺀다
                bool Hit(Mass x) => x != null && masses.Any(o => (o.c - x.c).sqrMagnitude < 900 && !(o.group >= 0 && o.group == x.group) && PolyPoly(Corners(o, 0), Corners(x, 0)));
                if ((Hit(m) || Hit(m2)) && m.roof == "gable" && !m.ridgeX && m2 == null) { m.ridgeX = true; Heights(m, p, parcels); }
                if ((Hit(m) || Hit(m2)) && m.roof == "gable" && m2 == null && m.type != "detached" && m.type != "barn") { m.roof = "hip"; Heights(m, p, parcels); }
                if (Hit(m) || Hit(m2)) { p.empty = "옆집과 겹침"; continue; }
                p.type = m.type; p.mass = m; masses.Add(m); if (m2 != null) { p.mass2 = m2; masses.Add(m2); }
            }
            foreach (var p in mem) if (p.mass == null) p.type = null;
        }
        // 묶음에서 빠진 이웃이 생기면 붙은 쪽 표시를 끈다(뒤에 틈이 생긴 쪽은 사실상 따로 집)
        foreach (var m in masses) if (m.group >= 0)
        {
            m.attL = m.attL && masses.Any(o => o.group == m.group && o.member == m.member - 1);
            m.attR = m.attR && masses.Any(o => o.group == m.group && o.member == m.member + 1);
        }
        // 묶음이 한 채만 남으면 그 집은 단독으로 센다
        foreach (var g in masses.Where(m => m.group >= 0).GroupBy(m => m.group).ToList()) if (g.Select(m => m.parcel).Distinct().Count() < 2) foreach (var m in g) { m.type = m.roof == "flat" ? "flat" : "detached"; m.tone = Tone(m.type); parcels[m.parcel].type = m.type; }
        foreach (var p in parcels) { if (p.mass == null) p.type = null; Types(p); }
    }
    static int Tone(string ty) => ty == "detached" || ty == "row" ? 0 : ty == "bungalow" || ty == "flat" ? 1 : ty == "narrow3" || ty == "stepped2" ? 2 : 3;
    // 구역: 앞선 칸들의 가장 가까운 길 칸 구역 중 가장 많은 것(갈림목·골목 안쪽은 이웃 길로), 도현 건물 둘레 28 m 는 ridge
    static string ZoneOf(Parcel p)
    {
        if (ridgeR.Any(r => r.Contains(p.ctr))) return "ridge";
        var cnt = new int[5]; foreach (var c in p.cells) { if (!front[c] || src[c] < 0) continue; var z = zc[src[c]]; if (z < 5) cnt[z]++; }
        int best = 4, bn = 0; for (int i = 0; i < 5; i++) if (cnt[i] > bn) { bn = cnt[i]; best = i; }
        if (bn == 0) foreach (var c in p.cells) { if (src[c] >= 0 && zc[src[c]] < 5) { best = zc[src[c]]; break; } }
        return ZoneIds[best];
    }
    // 옆 필지: 이웃 중 앞 방향이 같고(30° 안) 앞선이 거의 같은 줄(앞뒤 6 m 안), 옆으로 2 m 넘게 떨어진 것
    static IEnumerable<Parcel> SideNb(Parcel p, List<Parcel> ps) => p.nb.Select(i => ps[i]).Where(q => Vector2.Angle(q.toRoad, p.toRoad) < 30 && Mathf.Abs(Vector2.Dot(q.ctr - p.ctr, p.toRoad)) < 6 && Mathf.Abs(Vector2.Dot(q.ctr - p.ctr, p.t)) > 2);
    // 이 필지에 X 를 놓으면 X 가 3연속이 되는 유형들: 옆 필지 둘이 X, 또는 옆 필지가 X 이고 그 너머 옆도 X(붙은 묶음은 한 단위)
    static HashSet<string> Avoid(Parcel p, List<Parcel> ps)
    {
        var res = new HashSet<string>(); var sn = SideNb(p, ps).Where(q => q.type != null && !(p.group >= 0 && q.group == p.group)).ToList();
        foreach (var g in sn.GroupBy(q => q.group >= 0 ? -1 - q.group : q.id).Select(g => g.First()).GroupBy(q => q.type)) if (g.Count() >= 2) res.Add(g.Key);
        foreach (var q in sn) if (SideNb(q, ps).Any(r => r != p && r.type == q.type && !(q.group >= 0 && r.group == q.group) && !(p.group >= 0 && r.group == p.group))) res.Add(q.type);
        return res;
    }
    static bool RowRun(Parcel p, List<Parcel> ps) => Avoid(p, ps).Contains("row");
    // 새 묶음(mem 전체)을 한 단위로 볼 때 옆의 묶음 단위가 둘이거나, 옆 묶음 너머에 또 묶음이 있으면 3연속
    static bool GroupRun(List<Parcel> mem, List<Parcel> ps)
    {
        int Key(Parcel q) => q.group >= 0 ? -1 - q.group : q.id;
        var nb = mem.SelectMany(p => SideNb(p, ps)).Where(q => !mem.Contains(q) && q.type == "row" && q.group >= 0).GroupBy(Key).Select(g => g.ToList()).ToList();
        if (nb.Count >= 2) return true;
        foreach (var u0 in nb) { var u = ps.Where(q => q.group == u0[0].group).ToList(); if (u.SelectMany(p => SideNb(p, ps)).Any(r => !mem.Contains(r) && r.type == "row" && r.group >= 0 && r.group != u[0].group)) return true; }
        return false;
    }
    static string PickType(Parcel p, List<Parcel> ps, Zm zm, Rng rng)
    {
        var av = Avoid(p, ps); var opts = new List<(string t, float w)>();
        foreach (var kv in zm.w)
        {
            var ty = kv.Key; float w = kv.Value; if (w <= 0 || av.Contains(ty)) continue;
            if (ty == "shop" && !(p.road == "main" || p.zone == "tomo" && p.road == "village")) continue;   // 가게주택 = 큰길·마을 중심 길만
            if (ty == "barn" && !(p.zone == "outer" || p.zone == "ridge")) continue;
            if (ty == "bungalow" && p.area < 80) continue;
            if (ty == "stepped2" && p.depth < 9) continue;
            opts.Add((ty, w));
        }
        if (opts.Count == 0) return av.Contains("detached") ? "flat" : "detached";
        float s = opts.Sum(o => o.w), u = rng.F() * s; foreach (var o in opts) { if (u < o.w) return o.t; u -= o.w; }
        return opts[opts.Count - 1].t;
    }
    static Dictionary<string, Zm> LoadMix()
    {
        var res = new Dictionary<string, Zm>(); var path = System.IO.Path.GetFullPath(Application.dataPath + MixPath);
        float F(object o) => System.Convert.ToSingle(o, System.Globalization.CultureInfo.InvariantCulture);
        var js = (Dictionary<string, object>)PitCrewPlan.MiniJson.Parse(System.IO.File.ReadAllText(path, Encoding.UTF8));
        foreach (var kv in (Dictionary<string, object>)js["zones"])
        {
            var z = (Dictionary<string, object>)kv.Value; var g = (List<object>)z["sideGap"]; var s = (List<object>)z["setback"];
            var zm = new Zm { row = F(z["rowShare"]), g0 = F(g[0]), g1 = F(g[1]), s0 = F(s[0]), s1 = F(s[1]) };
            foreach (var w in (Dictionary<string, object>)z["w"]) zm.w[w.Key] = F(w.Value);
            res[kv.Key] = zm;
        }
        foreach (var id in ZoneIds) if (!res.ContainsKey(id)) res[id] = res["camo"];
        return res;
    }
    static Vector2 Rot(Vector2 v, float deg) { float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y); }
    static int Floors(Rng r) { float u = r.F(); return u < .2f ? 1 : u < .8f ? 2 : 3; }   // 20/60/20 — house_home 2층 기준
    static float Depth(int fl, Rng r) => fl == 1 ? 5 + r.F() * 2.5f : fl == 2 ? 6 + r.F() * 3 : 7 + r.F() * 3;
    // 직사각형(z0..z1) 안이 이 묶음 필지 칸뿐인 x 구간 중 가장 긴 것
    static bool Fit(HashSet<int> set, Vector2 f, Vector2 t, float z0, float z1, out float x0, out float x1)
    {
        x0 = x1 = 0;
        float best = 0, run0 = float.NaN; var zs = new List<float>(); for (float z = z0; z <= z1 + 1e-3f; z += C * .5f) zs.Add(z); if (zs[zs.Count - 1] < z1 - 1e-3f) zs.Add(z1);
        // 필지 칸의 x 범위를 0.25 m 씩 훑어, z0..z1 세로줄이 전부 이 묶음 필지 칸인 가장 긴 구간
        float xmn = 1e9f, xmx = -1e9f; foreach (var id in set) foreach (var c in Cells(id)) { float x = Vector2.Dot(P(c), t); xmn = Mathf.Min(xmn, x); xmx = Mathf.Max(xmx, x); }
        for (float x = xmn - C; x <= xmx + C + 1e-3f; x += C * .5f)
        {
            // 2판: 주인 없는 땅 칸(뒤 빈터·틈)도 쓴다 — 옆 덩어리와 겹침은 Hit() 가 막는다
            bool ok = true; foreach (var z in zs) { int k = Idx(t * x + f * z); if (k < 0 || (owner[k] < 0 ? type[k] != T.Land || foot[k] : !set.Contains(owner[k]))) { ok = false; break; } }
            if (ok) { if (float.IsNaN(run0)) run0 = x; if (x - run0 > best) { best = x - run0; x0 = run0; x1 = x; } }
            else run0 = float.NaN;
        }
        return best > 0;
    }
    static List<Parcel> cellsOf; static IEnumerable<int> Cells(int pid) => cellsOf[pid].cells;
    // 땅 읽기(2판): 덩어리 밑 1 m 간격 표본 → Δ(최고-최저)·경사(평면 맞춤 기울기, 앞뒤 gz · 옆 gx).
    //  A Δ ≤ 0.68: 바닥 = 최고 + 0.12(노출 기단 ≤ 0.8) · B Δ ≤ 2.1: 바닥 = 최저 + 0.5(노출 ≤ 0.62, 오르막 쪽 벽 묻힘 ≤ 1.6) · C 그보다 큼: 호출자가 둘로 나눔.
    //  밑면 = 가장 낮은 땅 - 0.3(뜸 0) — 패드 뒤 Reseat 가 새 땅으로 다시 맞춘다.
    static string Seat(Mass m)
    {
        var t = new Vector2(m.f.y, -m.f.x); float mn = 1e9f, mx = -1e9f; int n = 0;
        double sx = 0, sz = 0, sy = 0, sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0;
        foreach (var q in Samples(m, 1f))
        {
            float y = TopY(q); if (float.IsNaN(y)) return "땅 못 읽음·바다";
            mn = Mathf.Min(mn, y); mx = Mathf.Max(mx, y); n++;
            double x = Vector2.Dot(q - m.c, t), z = Vector2.Dot(q - m.c, m.f); sx += x; sz += z; sy += y; sxx += x * x; szz += z * z; sxz += x * z; sxy += x * y; szy += z * y;
        }
        if (n == 0 || mn < .3f) return "땅 못 읽음·바다";
        // 평면 y = a + gx·x + gz·z (평균 빼고 2×2 풀기)
        double mx_ = sx / n, mz = sz / n, my = sy / n, cxx = sxx / n - mx_ * mx_, czz = szz / n - mz * mz, cxz = sxz / n - mx_ * mz, cxy = sxy / n - mx_ * my, czy = szy / n - mz * my, det = cxx * czz - cxz * cxz;
        m.gx = det > 1e-6 ? (float)((cxy * czz - czy * cxz) / det) : 0; m.gz = det > 1e-6 ? (float)((czy * cxx - cxy * cxz) / det) : 0;
        m.slope = Mathf.Sqrt(m.gx * m.gx + m.gz * m.gz); m.gMin = mn; m.gMax = mx;
        if (m.slope > MaxSlope) return "경사 60% 넘음(나무 자리)";
        float D = mx - mn;
        if (D <= LowD) { m.seat = "A"; m.y0 = mx + .12f; }
        else { m.y0 = Mathf.Min(mn + PadLift, mx + .12f); float bu = mx - m.y0; m.seat = bu <= BuryMax ? "B" : bu <= HillBury ? "C1" : "C2"; }
        m.bury = Mathf.Max(0, mx - m.y0); m.expo = m.y0 - mn; m.plinth = m.expo > .3f; m.bot = mn - Bury;
        return null;
    }
    // 앉히기 전체: 등고선 방향 맞춤(앞뒤 경사면 얕게) → C 면 경사 쪽으로 둘로 나눠 계단식 2단(각 단 B 규칙, 아래 단 내리막 쪽 면에 차고·창고 문)
    static string SeatAll(Mass m, bool att, bool avoidC2, out Mass m2)
    {
        m2 = null; var why = Seat(m); if (why != null) return why;
        // 긴 변 = 등고선: 경사가 주로 앞뒤인데 깊이가 폭보다 길면 깊이를 폭까지 줄인다(앞면은 그대로 길 쪽). 2단 집은 깊이가 있어야 하므로 제외
        if (!att && m.type != "stepped2" && m.seat != "A" && Mathf.Abs(m.gz) > Mathf.Abs(m.gx) * 1.2f && m.d > m.w + .5f) { m.d = Mathf.Max(4.5f, m.w); m.splitT = true; why = Seat(m); if (why != null) return why; }
        // 옆이 이미 2단 집 둘이면: 깊이를 줄여 반지하 한 채(C1)로 먼저 해 본다
        if (m.seat == "C2" && avoidC2 && !att) while (m.seat == "C2" && m.d > 4.6f) { m.d = Mathf.Max(4.5f, m.d - 1f); why = Seat(m); if (why != null) return why; }
        // 반지하(C1): 오르막 쪽 1층이 땅속 → 단층·헛간은 2층 단독으로(아래층이 내리막 쪽에 드러남)
        if (m.seat == "C1")
        {
            if (m.floors < 2) { m.floors = 2; m.floorH = Mathf.Min(m.floorH, 3.1f); if (m.type == "bungalow" || m.type == "barn") { m.type = "detached"; m.roof = "gable"; m.barnDoor = false; m.pitch = 26; m.tone = Tone(m.type); } }
            // 아래층이 2 m 넘게 드러나면 그게 한 층 몫 — 3층은 2층으로(내리막 쪽에서 4층 탑처럼 안 보이게)
            if (m.floors >= 3 && m.bury > 2f) m.floors = 2;
            Downhill(m); return null;
        }
        bool want2 = m.type == "stepped2" && m.d >= 8f && !att;
        if (m.seat != "C2" && !want2) return null;
        // 묶음 집·좁은 집: 깊이를 줄여(4 m 까지) 한 번 더 — 그래도 C2 면 비움
        bool alongF = want2 && m.seat != "C2" || Mathf.Abs(m.gz) >= Mathf.Abs(m.gx); float L = alongF ? m.d : m.w;
        if (m.seat == "C2" && (att || L < 7f))
        {
            if (m.d > 4.2f) { m.d = Mathf.Max(4f, m.d * .6f); if (m.floors == 3) m.floors = 2; why = Seat(m); if (why != null) return why; if (m.seat == "C1") { if (m.floors < 2) m.floors = 2; Downhill(m); } if (m.seat != "C2") return null; }
            return "가파름(단차 못 넣음)";
        }
        var t = new Vector2(m.f.y, -m.f.x); var a = m.Clone(); var b = m.Clone();
        if (alongF) { a.d = b.d = m.d * .5f; b.c = m.c - m.f * (m.d * .5f); }
        else { a.w = b.w = m.w * .5f; a.c = m.c - t * (m.w * .25f); b.c = m.c + t * (m.w * .25f); a.attR = true; b.attL = true; }
        if (Seat(a) != null || Seat(b) != null || a.seat == "C2" || b.seat == "C2") { if (m.seat == "C2") return "가파름(단차 못 넣음)"; m.type = "detached"; m.tone = Tone(m.type); return null; }
        var lo = a.y0 <= b.y0 ? a : b; var hi = lo == a ? b : a; lo.door = true; lo.doorSide = alongF ? (lo == b ? 1 : 0) : (lo == a ? 2 : 3); lo.doorBack = lo.doorSide == 1; lo.tier = 0; hi.tier = 1;
        // 평지 2단: 앞 단이 한 층 낮다(계단식) · 비탈 2단: 위 단 앞마당 ≈ 아래 단 지붕 높이
        if (Mathf.Abs(a.y0 - b.y0) < .5f && alongF) { a.floors = Mathf.Max(1, m.floors - 1); b.floors = m.floors; }
        a.type = b.type = "stepped2"; a.tone = b.tone = Tone("stepped2");
        if (m.seat == "C2") a.seat = b.seat = "C2"; a.pair = b; b.pair = a; a.ridgeX = b.ridgeX = alongF || m.ridgeX;
        // a 를 m 자리에 복사(부르는 쪽이 m 을 그대로 쓴다)
        foreach (var fi in typeof(Mass).GetFields()) fi.SetValue(m, fi.GetValue(a));
        b.pair = m; m2 = b; return null;
    }
    // 반지하 집 문: 내리막 쪽 면(붙은 쪽이면 앞면)에 아래층 문
    static void Downhill(Mass m)
    {
        int s = Mathf.Abs(m.gz) >= Mathf.Abs(m.gx) ? (m.gz > 0 ? 1 : 0) : (m.gx > 0 ? 2 : 3);
        if (s == 2 && m.attL || s == 3 && m.attR) s = 0;
        m.door = true; m.doorSide = s; m.doorBack = s == 1;
    }
    // ── 꺼진 블록 메우기(1935, D-222 「지형이 내려앉은 곳」) ──────────
    // 원지형(Mesh_TownV6_Terrain)부터 그릇 모양인 땅이 길에 둘러싸이면 블록 전체가 둘레 길보다 수 m 낮은 구덩이가 됐다(예: (115,23) 가운데 5.2 m, 둘레 길 14~30 m).
    // 블록 = 길(T.Road)·축대에 막힌 땅·앞띠·비움 칸 덩어리. 기대 높이 E = 둘레 길 칸 높이의 역거리²(1/d⁴) 가중 평균.
    // 중앙값(땅 − E) ≤ -1 m 인 블록만: 땅이 E − min(0.6, 0.05 + 0.25 × 길까지 거리) 보다 낮은 곳을 그 높이까지 돋운다(깎지 않음 → 길 옆은 1:4 로 내려가 0.6 m 아래에서 평평).
    // 건너뜀: 맵 가장자리·범위 밖·바다(땅 < 0.6 m)에 닿는 블록, 도현 건물 발자국이 든 블록, TerraceBlock 집 블록(중심이 반경 60 m 안).
    // 2147(D-228): 평평한 E 판 → 밋밋하다. 목표 높이를 둘 더한 것으로 바꿈:
    //  ① S = 둘레 길 점에 맞춘 평면 + 평면에서 남은 것(길 높이 − 평면)의 역거리 평균 → 길에선 길 높이, 가운데는 평면 따라 기울어짐(큰 경사 방향 유지).
    //  ② 원지형 작은 기복 = 원래 높이 − 24 m 창 평균(블록 안 칸만; 17 m 창은 블록 안 σ 0.47 m 로 눈에 안 띔 → 24 m), 블록 평균 빼고 ±0.8 m 로 부드럽게 자름(tanh). 큰 그릇(창보다 큰 것)은 빠지고 수 m~20 m 기복만 남음.
    //     길에서 1.5 m 까진 0, 6.5 m 에서 다 더함(길 옆 비탈은 1935 그대로). 여전히 돋우기만 — 원래 땅이 더 높은 곳은 그대로 둔다.
    const float SinkMed = -1f, SinkTol = .6f, SinkUnder = .05f, SinkSlope = .25f, SinkTbR = 60f;
    const float RelHalf = 12f, RelMax = .8f, RelIn = 1.5f, RelRamp = 5f;
    // 2253(D-231 「둘 다 안쪽으로 움푹」): 2147 은 중앙값 ≤ -1 m 블록만, 면보다 0.6 m 아래 + 1:4 비탈 + ±0.8 m 기복 → 길 따라 둑 띠, 안은 접시.
    //  원칙 「블록 안은 둘레 길 가장자리보다 오목하면 안 된다」: 문턱 없이 모든 둘레 블록에서 목표 = S − 0.05 m(길 윗면 바로 밑 = 길 가장자리와 같은 높이)
    //  + 원지형 기복의 위쪽 절반만(아래로 파지 않음). 돋우기만 — 원래 땅이 S 보다 높은 곳(블록이 언덕)은 그대로. 건너뜀 조건(도현 건물·계단 땅·바다·가장자리)은 그대로.
    public static bool OldSink2147;   // 켜면 2147 규칙(전/후 비교용)
    public static List<Vector2> ProbePts; static HashSet<int> Probe; public static StringBuilder ProbeOut = new StringBuilder();   // 검증용: 점 → FillSunk 목표 기록
    static (float a, float b, float c) FitPlane(List<Vector3> pts)
    {
        double mx = 0, mz = 0, my = 0; foreach (var p in pts) { mx += p.x; mz += p.z; my += p.y; } int n = pts.Count; mx /= n; mz /= n; my /= n;
        double sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0;
        foreach (var p in pts) { double dx = p.x - mx, dz = p.z - mz, dy = p.y - my; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; sxy += dx * dy; szy += dz * dy; }
        double det = sxx * szz - sxz * sxz; if (System.Math.Abs(det) < 1e-6) return ((float)my, 0, 0);
        double b = (sxy * szz - szy * sxz) / det, c = (szy * sxx - sxy * sxz) / det;
        return ((float)(my - b * mx - c * mz), (float)b, (float)c);
    }
    // 블록 칸마다 원지형 높이(y0)와 작은 기복(반환). 창 평균은 블록 칸만으로(합계표 2개: 높이 합·칸 수).
    static float[] Relief(List<int> comp, System.Func<Vector2, float> terrY, out float[] y0)
    {
        int n = comp.Count; y0 = new float[n]; var det = new float[n];
        int i0 = int.MaxValue, j0 = int.MaxValue, i1 = 0, j1 = 0; foreach (var c in comp) { int i = c % gw, j = c / gw; i0 = Mathf.Min(i0, i); j0 = Mathf.Min(j0, j); i1 = Mathf.Max(i1, i); j1 = Mathf.Max(j1, j); }
        int W = i1 - i0 + 1, H = j1 - j0 + 1; var sy = new double[(W + 1) * (H + 1)]; var sn = new int[(W + 1) * (H + 1)];
        for (int q = 0; q < n; q++) { y0[q] = terrY(P(comp[q])); if (float.IsNaN(y0[q])) continue; int k = (comp[q] / gw - j0 + 1) * (W + 1) + comp[q] % gw - i0 + 1; sy[k] = y0[q]; sn[k] = 1; }
        for (int j = 1; j <= H; j++) for (int i = 1; i <= W; i++) { int k = j * (W + 1) + i; sy[k] += sy[k - 1] + sy[k - W - 1] - sy[k - W - 2]; sn[k] += sn[k - 1] + sn[k - W - 1] - sn[k - W - 2]; }
        int r = Mathf.RoundToInt(RelHalf / C); double mean = 0; int cnt = 0;
        for (int q = 0; q < n; q++)
        {
            if (float.IsNaN(y0[q])) continue; int i = comp[q] % gw - i0, j = comp[q] / gw - j0;
            int a0 = Mathf.Max(0, i - r), a1 = Mathf.Min(W, i + r + 1), b0 = Mathf.Max(0, j - r), b1 = Mathf.Min(H, j + r + 1);
            int K(int a, int b) => b * (W + 1) + a;
            double s = sy[K(a1, b1)] - sy[K(a0, b1)] - sy[K(a1, b0)] + sy[K(a0, b0)]; int m = sn[K(a1, b1)] - sn[K(a0, b1)] - sn[K(a1, b0)] + sn[K(a0, b0)];
            det[q] = y0[q] - (float)(s / Mathf.Max(1, m)); mean += det[q]; cnt++;
        }
        mean /= Mathf.Max(1, cnt);
        for (int q = 0; q < n; q++) det[q] = float.IsNaN(y0[q]) ? 0 : RelMax * (float)System.Math.Tanh((det[q] - mean) / RelMax);
        return det;
    }
    static string FillSunk(Transform terr, ref Mesh baseMesh, out Mesh filled)
    {
        filled = null; int N = type.Length; var tc = terr.GetComponent<MeshCollider>();
        float TerrY(Vector2 p) => tc.Raycast(new Ray(new Vector3(p.x, 400, p.y), Vector3.down), out var h, 800) ? h.point.y : float.NaN;
        var cells = new List<int>(); for (int k = 0; k < N; k++) if (type[k] == T.Land || type[k] == T.Strip || type[k] == T.Excl) cells.Add(k);
        var del = new float[N]; int nChk = 0, nSunk = 0; var skip = new List<string>(); var done = new List<string>(); float maxD = 0;
        var tbC = new Vector2(TerraceBlock.HouseCenter.x, TerraceBlock.HouseCenter.z);
        Probe = ProbePts == null ? null : new HashSet<int>(ProbePts.Select(Idx)); ProbeOut.Clear();
        if (ProbePts != null) foreach (var pp in ProbePts) { int k = Idx(pp); ProbeOut.Append($"{pp.x:F0},{pp.y:F0} type {(k < 0 ? "?" : type[k].ToString())} dist {(k < 0 ? -1 : dist[k]):F1}\n"); }
        foreach (var comp in Comps(cells))
        {
            float area = comp.Count * C * C; if (area < 150 || area > 40000) continue;
            var ring = new HashSet<int>(); bool edge = false;
            foreach (var c in comp)
            {
                int i = c % gw, j = c / gw; if (i == 0 || j == 0 || i == gw - 1 || j == gh - 1) { edge = true; break; }
                foreach (var q in new[] { c - 1, c + 1, c - gw, c + gw }) { if (type[q] == T.Out) edge = true; else if (type[q] == T.Road) ring.Add(q); }
                if (edge) break;
            }
            if (edge || ring.Count < 40) continue;
            nChk++;
            var rl = ring.ToList(); int st = Mathf.Max(1, rl.Count / 300); var rs = new List<Vector3>();
            for (int q = 0; q < rl.Count; q += st) { var p = P(rl[q]); float y = TopY(p); if (!float.IsNaN(y)) rs.Add(new Vector3(p.x, y, p.y)); }
            if (rs.Count < 10) continue;
            float E(Vector2 p) { double ws = 0, wy = 0; foreach (var r in rs) { float dx = r.x - p.x, dz = r.z - p.y, d2 = dx * dx + dz * dz + .25f; double w = 1.0 / (d2 * d2); ws += w; wy += w * r.y; } return (float)(wy / ws); }
            var defs = new List<float>(); int cs = Mathf.Max(1, comp.Count / 2000); bool sea = false;
            for (int q = 0; q < comp.Count; q += cs) { var p = P(comp[q]); float y = TerrY(p); if (float.IsNaN(y) || y < .6f) { sea = true; break; } defs.Add(y - E(p)); }
            if (sea || defs.Count == 0) continue;
            float med = Median(defs); if (OldSink2147 && med > SinkMed) continue;
            nSunk++; var ctr = Mean(comp); string tg = $"({ctr.x:F0},{ctr.y:F0}) {area:F0}㎡ 중앙 {med:F1} m";
            if (comp.Any(c => foot[c])) { skip.Add(tg + " 도현 건물 있음"); continue; }
            if ((ctr - tbC).magnitude < SinkTbR) { skip.Add(tg + " 계단 땅"); continue; }
            // 2147(D-228 「메운 땅이 밋밋함」): 목표 = 기울어진 면 S + 원지형 작은 기복. 판정(E)은 그대로.
            var (pa, pb, pc) = FitPlane(rs); var res = rs.Select(r => r.y - (pa + pb * r.x + pc * r.z)).ToArray();
            float S(Vector2 p) { double ws = 0, wy = 0; for (int q = 0; q < rs.Count; q++) { float dx = rs[q].x - p.x, dz = rs[q].z - p.y, d2 = dx * dx + dz * dz + .25f; double w = 1.0 / (d2 * d2); ws += w; wy += w * res[q]; } return pa + pb * p.x + pc * p.y + (float)(wy / ws); }
            var det = Relief(comp, TerrY, out var y0);
            float bmax = 0; double dsum = 0; int dn = 0;
            for (int q = 0; q < comp.Count; q++)
            {
                int c = comp[q]; var p = P(c); float y = y0[q]; if (float.IsNaN(y)) continue;
                float wd = Mathf.Clamp01((dist[c] - RelIn) / RelRamp);
                float d = OldSink2147 ? S(p) - Mathf.Min(SinkTol, SinkUnder + SinkSlope * dist[c]) + wd * det[q] - y
                                      : S(p) - SinkUnder + wd * Mathf.Max(0, det[q]) - y;
                if (d > 0) { del[c] = d; bmax = Mathf.Max(bmax, d); }
                if (Probe != null && Probe.Contains(c)) ProbeOut.Append($"{p.x:F0},{p.y:F0} {tg} S {S(p):F2} y {y:F2} det {det[q]:F2} dist {dist[c]:F1} del {d:F2}\n");
                if (wd > .99f) { dsum += det[q] * det[q]; dn++; }
            }
            maxD = Mathf.Max(maxD, bmax); done.Add(tg + $" 돋움 최대 {bmax:F1} m · 기울기 {100 * Mathf.Sqrt(pb * pb + pc * pc):F0}% · 기복 σ {System.Math.Sqrt(dsum / Mathf.Max(1, dn)):F2} m");
        }
        string note = $"꺼진 블록: 검사 {nChk} · 꺼짐 {nSunk} · 메움 {done.Count}" + (done.Count > 0 ? " [" + string.Join("; ", done) + "]" : "") + (skip.Count > 0 ? " · 건너뜀 [" + string.Join("; ", skip) + "]" : "");
        if (done.Count == 0) return note;
        // 0.5 m 칸 돋움 → 지형 정점에 이중선형으로 더함(Pads 와 같은 방식, 정점은 위로만)
        float Bil(float x, float z)
        {
            float fx = (x - g0.x) / C - .5f, fz = (z - g0.y) / C - .5f; int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz); float u = fx - i, v = fz - j;
            float V(int a, int b) => a < 0 || b < 0 || a >= gw || b >= gh ? 0 : del[b * gw + a];
            return Mathf.Lerp(Mathf.Lerp(V(i, j), V(i + 1, j), u), Mathf.Lerp(V(i, j + 1), V(i + 1, j + 1), u), v);
        }
        var vs = baseMesh.vertices; int nv = vs.Length, moved = 0; var chg = new bool[nv];
        for (int i = 0; i < nv; i++) { var w = terr.TransformPoint(vs[i]); float d = Bil(w.x, w.z); if (d < 1e-4f) continue; w.y += d; vs[i] = terr.InverseTransformPoint(w); chg[i] = true; moved++; }
        var mesh = Object.Instantiate(baseMesh); mesh.name = "Mesh_TownV6_Terrain_fill"; mesh.SetVertices(vs);
        var tris = baseMesh.triangles; var mark = new bool[nv];
        for (int i = 0; i < tris.Length; i += 3) if (chg[tris[i]] || chg[tris[i + 1]] || chg[tris[i + 2]]) mark[tris[i]] = mark[tris[i + 1]] = mark[tris[i + 2]] = true;
        var bn = baseMesh.normals; mesh.RecalculateNormals(); var nn = mesh.normals; for (int i = 0; i < nv; i++) if (!mark[i]) nn[i] = bn[i]; mesh.normals = nn; mesh.RecalculateBounds();
        SetTerrain(terr, mesh); baseMesh = mesh; filled = mesh;
        // 2253: 메운 칸 위 Roadside 소품(화분 등)이 묻히지 않게 — 밑면이 새 땅보다 낮으면 땅 위로(다시 돌려도 같은 결과: 이미 땅 위면 그대로)
        int lifted = 0; var rsd = GameObject.Find("Roadside");
        if (rsd != null) foreach (Transform road in rsd.transform) foreach (Transform pr in road)
        {
            var rr = pr.GetComponentsInChildren<Renderer>(); if (rr.Length == 0) continue; var bb = rr[0].bounds; foreach (var r in rr) bb.Encapsulate(r.bounds);
            if (Bil(bb.center.x, bb.center.z) < .01f) continue; float g = TerrY(new Vector2(bb.center.x, bb.center.z));
            if (!float.IsNaN(g) && bb.min.y < g - .02f) { pr.position += Vector3.up * (g - bb.min.y); lifted++; }
        }
        return note + $" · 정점 {moved} 돋움(최대 {maxD:F2} m) · 소품 올림 {lifted}";
    }
    // ── 지형 패드(2판) ─────────────────────────────────────────
    // B·C 덩어리: 바닥 밑은 깎기만(최대 0.65), 앞 1.5 m · 옆 0.8 m(아래 단 뒷문이면 뒤 1.5 m) 마당은 바닥 - 0.15 로 깎기·돋우기(각 0.65 까지).
    // 이 필지 칸·주인 없는 땅 칸만, 길·앞띠·축대·비움·도현 건물에서 1 m 안은 0 으로 줄인다. 깎은 둘레 2 m 는 원지형으로 이어지게(돋운 곳은 0.3 m 넘으면 낮은 축대).
    // 0.5 m 칸 높이차 → 지형 정점에 이중선형으로 더함 = 정점을 위아래로만 옮김(삼각형·순서 그대로 → 구멍·접힘 구조적으로 0).
    static float[] dBlk;
    static string Pads(Transform terr, Mesh baseMesh, List<Mass> masses, Transform root, out string wallNote)
    {
        int N = type.Length; var del = new float[N]; var has = new bool[N]; var padPid = new int[N]; var g = new float[N];
        for (int k = 0; k < N; k++) { g[k] = float.NaN; padPid[k] = -1; }
        float G(int k) { if (float.IsNaN(g[k])) g[k] = TopY(P(k)); return g[k]; }
        var wallSegs = new List<(Vector2 a, Vector2 b, float bot, float top)>();
        // 덩어리 발자국(+0.3 m) 칸 = 그 덩어리 번호 — 남의 발자국 밑은 안 건드린다
        var fp = Enumerable.Repeat(-1, N).ToArray();
        for (int mi = 0; mi < masses.Count; mi++) foreach (var q in Samples(masses[mi], .25f)) { int k = Idx(q); if (k >= 0) fp[k] = mi; }
        for (int mi = 0; mi < masses.Count; mi++) foreach (var q in Corners(masses[mi], .3f)) { int k = Idx(q); if (k >= 0 && fp[k] < 0) fp[k] = mi; }
        for (int mi = 0; mi < masses.Count; mi++)
        {
            var m = masses[mi]; if (m.seat == "A") continue; int pairI = m.pair != null ? masses.IndexOf(m.pair) : -2;
            var t = new Vector2(m.f.y, -m.f.x); float hw = m.w * .5f, tgt = m.y0 - .15f;
            float xL = -hw - (m.attL ? 0 : ApronSide), xR = hw + (m.attR ? 0 : ApronSide), zF = m.doorBack ? 0 : Apron, zB = -m.d - (m.doorBack ? Apron : 0);
            var cs = new[] { m.c + t * xL + m.f * zF, m.c + t * xR + m.f * zF, m.c + t * xR + m.f * zB, m.c + t * xL + m.f * zB };
            int i0 = Mathf.Max(0, Mathf.FloorToInt((cs.Min(c => c.x) - g0.x) / C)), i1 = Mathf.Min(gw - 1, Mathf.FloorToInt((cs.Max(c => c.x) - g0.x) / C));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((cs.Min(c => c.y) - g0.y) / C)), j1 = Mathf.Min(gh - 1, Mathf.FloorToInt((cs.Max(c => c.y) - g0.y) / C));
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
            {
                int k = j * gw + i; var p = P(k); float x = Vector2.Dot(p - m.c, t), z = Vector2.Dot(p - m.c, m.f);
                if (x < xL || x > xR || z < zB || z > zF) continue;
                if (foot[k] || !(owner[k] == m.parcel || (owner[k] < 0 && type[k] == T.Land))) continue;
                bool under = x >= -hw && x <= hw && z >= -m.d && z <= 0;
                if (!under && fp[k] >= 0 && fp[k] != mi) continue;   // 짝 단·옆집 발자국 밑은 그 덩어리 몫
                if (under && fp[k] == pairI) continue;
                float y = G(k); if (float.IsNaN(y)) continue;
                if (!under && tgt - y > FillMax) continue;   // 돋움 한도 넘는 내리막 칸은 마당에서 뺀다(원지형 그대로)
                float dd = under ? Mathf.Clamp(tgt - y, -CutMax, 0) : Mathf.Clamp(tgt - y, -CutMax, FillMax);
                dd *= Mathf.Clamp01((dBlk[k] - C) / Guard);
                if (!has[k] || Mathf.Abs(dd) > Mathf.Abs(del[k])) { del[k] = dd; padPid[k] = m.parcel; }
                has[k] = true;
            }
            // 돋운 마당 바깥 변(앞·옆, 뒷문이면 뒤): 안쪽 칸 돋움 > 0.3 이면 낮은 축대
            var edges = new List<(Vector2 a, Vector2 b, Vector2 inn)>();
            if (!m.doorBack) edges.Add((cs[0], cs[1], -m.f)); else edges.Add((cs[2], cs[3], m.f));
            if (!m.attL) edges.Add((cs[3], cs[0], t)); if (!m.attR) edges.Add((cs[1], cs[2], -t));
            foreach (var (ea, eb, inn) in edges)
            {
                float L = (eb - ea).magnitude; int ns = Mathf.Max(1, Mathf.RoundToInt(L / C)); Vector2? s0 = null; float bot = 1e9f;
                for (int s = 0; s <= ns; s++)
                {
                    var p = Vector2.Lerp(ea, eb, s / (float)ns); int ki = Idx(p + inn * .3f);
                    bool on = ki >= 0 && has[ki] && padPid[ki] == m.parcel && del[ki] > WallMinH;
                    // 바깥이 원래 낭떠러지(기존 단·비탈 끝)라 노출이 0.8 m 넘으면 그 자리는 벽 없음
                    float go = on ? TopY(p - inn * .3f) : float.NaN; if (on && (float.IsNaN(go) || tgt + .12f - go > .8f)) on = false;
                    if (on) { bot = Mathf.Min(bot, go); if (s0 == null) s0 = p; }
                    if ((!on || s == ns) && s0 != null) { var e = on ? p : Vector2.Lerp(ea, eb, (s - 1) / (float)ns); if ((e - s0.Value).magnitude >= C) wallSegs.Add((s0.Value, e, bot - .2f, tgt + .12f)); s0 = null; bot = 1e9f; }
                }
            }
        }
        // 둘레 2 m: 가장 가까운 패드 칸 값에서 0 으로(같은 필지·주인 없는 땅만, 축대 붙는 돋움 칸에서는 안 이음)
        DT(has, out var dP, out var sP); int nRing = 0;
        var fin = (float[])del.Clone();
        for (int k = 0; k < N; k++)
        {
            if (has[k] || sP[k] < 0 || dP[k] > Ring || foot[k] || type[k] != T.Land || fp[k] >= 0) continue;
            int s = sP[k]; if (!(owner[k] < 0 || owner[k] == padPid[s])) continue;
            float ds = del[s]; if (ds > WallMinH) continue;
            fin[k] = ds * (1 - dP[k] / Ring) * Mathf.Clamp01((dBlk[k] - C) / Guard); if (Mathf.Abs(fin[k]) > 1e-4f) nRing++;
        }
        // 지형 정점에 적용
        var vs = baseMesh.vertices; int nv = vs.Length, moved = 0, nan = 0; var chg = new bool[nv]; float maxCut = 0, maxFill = 0;
        float Bil(float x, float z)
        {
            float fx = (x - g0.x) / C - .5f, fz = (z - g0.y) / C - .5f; int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz); float u = fx - i, v = fz - j;
            float V(int a, int b) => a < 0 || b < 0 || a >= gw || b >= gh ? 0 : fin[b * gw + a];
            return Mathf.Lerp(Mathf.Lerp(V(i, j), V(i + 1, j), u), Mathf.Lerp(V(i, j + 1), V(i + 1, j + 1), u), v);
        }
        for (int i = 0; i < nv; i++)
        {
            var w = terr.TransformPoint(vs[i]); float d = Bil(w.x, w.z); if (Mathf.Abs(d) < 1e-4f) continue;
            w.y += d; vs[i] = terr.InverseTransformPoint(w); chg[i] = true; moved++; maxCut = Mathf.Min(maxCut, d); maxFill = Mathf.Max(maxFill, d);
            if (float.IsNaN(vs[i].y)) nan++;
        }
        var mesh = Object.Instantiate(baseMesh); mesh.name = "Mesh_TownV6_Terrain_parcels"; mesh.SetVertices(vs);
        // 법선: 움직인 정점이 닿은 삼각형의 세 정점만 다시 계산한 값으로(나머지는 TerraceBlock 법선 그대로)
        var tris = baseMesh.triangles; var mark = new bool[nv]; int flips = 0; var bv = baseMesh.vertices;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2]; if (!(chg[a] || chg[b] || chg[c])) continue; mark[a] = mark[b] = mark[c] = true;
            float n0 = Vector3.Cross(bv[b] - bv[a], bv[c] - bv[a]).y, n1 = Vector3.Cross(vs[b] - vs[a], vs[c] - vs[a]).y; if (n0 > 1e-6f && n1 <= 0) flips++;
        }
        var bn = baseMesh.normals; mesh.RecalculateNormals(); var nn = mesh.normals; for (int i = 0; i < nv; i++) if (!mark[i]) nn[i] = bn[i]; mesh.normals = nn; mesh.RecalculateBounds();
        var ex = AssetDatabase.LoadAssetAtPath<Mesh>(ParcelTerrain);
        if (ex == null) AssetDatabase.CreateAsset(mesh, ParcelTerrain); else { EditorUtility.CopySerialized(mesh, ex); ex.name = "Mesh_TownV6_Terrain_parcels"; Object.DestroyImmediate(mesh); mesh = ex; }
        AssetDatabase.SaveAssets(); SetTerrain(terr, mesh);
        // 축대 메시(TerraceBlock 재질: 돌 벽 + 갓돌)
        var rm = AssetDatabase.LoadAssetAtPath<Material>(Mats + "TV6_Retain.mat"); var cm = AssetDatabase.LoadAssetAtPath<Material>(Mats + "TV6_RetainCapB.mat");
        float maxWall = 0;
        if (wallSegs.Count > 0)
        {
            var v = new List<Vector3>(); var tw = new List<int>(); var tc = new List<int>();
            foreach (var (a, b, bot, top) in wallSegs)
            {
                var dir = (b - a).normalized; var nrm = new Vector2(-dir.y, dir.x) * .2f; var ctr = new Vector3((a.x + b.x) * .5f, (bot + top) * .5f, (a.y + b.y) * .5f);
                Vector3 V3(Vector2 q, float y) => new Vector3(q.x, y, q.y);
                var p = new[] { a - nrm - dir * .2f, b - nrm + dir * .2f, b + nrm + dir * .2f, a + nrm - dir * .2f };
                for (int s = 0; s < 4; s++) Quad(v, tw, V3(p[s], bot), V3(p[(s + 1) % 4], bot), V3(p[(s + 1) % 4], top - .12f), V3(p[s], top - .12f), ctr);
                var pc = new[] { a - nrm * 1.4f - dir * .26f, b - nrm * 1.4f + dir * .26f, b + nrm * 1.4f + dir * .26f, a + nrm * 1.4f - dir * .26f };
                var cc = new Vector3(ctr.x, top - .06f, ctr.z);
                for (int s = 0; s < 4; s++) Quad(v, tc, V3(pc[s], top - .12f), V3(pc[(s + 1) % 4], top - .12f), V3(pc[(s + 1) % 4], top), V3(pc[s], top), cc);
                Quad(v, tc, V3(pc[0], top), V3(pc[1], top), V3(pc[2], top), V3(pc[3], top), cc);
                maxWall = Mathf.Max(maxWall, top - (bot + .2f));
            }
            var wm = new Mesh { name = "Parcels_PadWalls", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; wm.SetVertices(v); wm.subMeshCount = 2; wm.SetTriangles(tw, 0); wm.SetTriangles(tc, 1);
            wm.SetUVs(0, v.Select(q => new Vector2((q.x + q.z) / 2.5f, q.y / 1.88f)).ToList()); wm.RecalculateNormals(); wm.RecalculateBounds();
            var wg = new GameObject("PadWalls"); wg.transform.SetParent(root, false); wg.AddComponent<MeshFilter>().sharedMesh = wm; wg.AddComponent<MeshRenderer>().sharedMaterials = new[] { rm, cm }; wg.AddComponent<MeshCollider>().sharedMesh = wm;
        }
        padMaxWall = maxWall;
        wallNote = $"패드 축대 {wallSegs.Count}개 · 총 {wallSegs.Sum(s => (s.b - s.a).magnitude):F0} m · 최고 노출 {maxWall:F2} m";
        terrHoles = nan + flips;
        return $"지형 패드: 칸 {has.Count(x => x)}+둘레 {nRing} · 정점 {moved}/{nv} 움직임(깎기 최대 {-maxCut:F2} · 돋움 최대 {maxFill:F2} m) · 삼각형 수 그대로 {tris.Length / 3} · 뒤집힘 {flips} · NaN {nan}";
    }
    static float padMaxWall; static int terrHoles, nLowered; const float ExpoCap = .78f;
    // 패드 뒤 새 땅으로: 밑면 = min(원래 최저, 새 최저) - 0.3 · 노출 기단 = 바닥 - 둘레 새 땅 최저
    static void Reseat(List<Mass> masses)
    {
        nLowered = 0;
        foreach (var m in masses)
        {
            float mn = m.gMin, per = float.PositiveInfinity; var t = new Vector2(m.f.y, -m.f.x);
            foreach (var q in Samples(m, .5f)) { float y = TopY(q); if (!float.IsNaN(y)) mn = Mathf.Min(mn, y); }
            float hw = m.w * .5f;
            for (float s = 0; s <= 1.001f; s += .05f)
                foreach (var q in new[] { m.c + t * Mathf.Lerp(-hw, hw, s), m.c + t * Mathf.Lerp(-hw, hw, s) - m.f * m.d, m.c - t * hw - m.f * (m.d * s), m.c + t * hw - m.f * (m.d * s) })
                { float y = TopY(q); if (!float.IsNaN(y)) per = Mathf.Min(per, y); }
            m.bot = mn - Bury; m.expo = float.IsInfinity(per) ? 0 : Mathf.Max(0, m.y0 - per);
            // 1 m 표본 사이로 더 낮은 땅이 나오면(둘레 0.05 간격) 바닥을 내려 노출 기단 0.78 m 로 묶는다 — 오르막 쪽 묻힘이 그만큼 늘어난다
            if (m.expo > ExpoCap) { float dn = m.expo - ExpoCap; m.y0 -= dn; m.bury += dn; m.expo = ExpoCap; nLowered++; }
            m.plinth = m.expo > .3f || m.seat != "A" && m.expo > .15f;
        }
    }
    // 빈 필지 = 나무·덤불(LofiPilot 대역 프리팹). 넓이 45 ㎡ 당 1그루(1~4), 칸을 seed 로 고르고 2.5 m 띄움, 길 둘레 1.5 m 안 X. 전망 자리면 덤불만.
    static string Greens(List<Parcel> ps, Transform root)
    {
        var tree = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefab); var shrub = AssetDatabase.LoadAssetAtPath<GameObject>(ShrubPrefab);
        if (tree == null || shrub == null) return "나무 프리팹 없음";
        var gr = new GameObject("Greens").transform; gr.SetParent(root, false); int nt = 0, nsb = 0;
        foreach (var p in ps)
        {
            if (p.mass != null) continue; var rng = new Rng(Fnv($"{Seed}|green|{p.id}"));
            bool low = p.empty != null && p.empty.StartsWith("전망"); int want = Mathf.Clamp(Mathf.RoundToInt(p.area / 45f), 1, 4); var picks = new List<Vector2>();
            for (int tr = 0; tr < want * 6 && picks.Count < want; tr++)
            {
                int c = p.cells[(int)(rng.F() * p.cells.Count) % p.cells.Count]; var q = P(c) + new Vector2(rng.F() - .5f, rng.F() - .5f) * C;
                if (dBlk[c] < 1.5f || picks.Any(o => (o - q).sqrMagnitude < 6.25f)) continue;
                float y = TopY(q); if (float.IsNaN(y) || y < .3f) continue; picks.Add(q);
                bool isTree = !low && rng.F() < .6f; var go = (GameObject)PrefabUtility.InstantiatePrefab(isTree ? tree : shrub, gr);
                go.transform.SetPositionAndRotation(new Vector3(q.x, y - .05f, q.y), Quaternion.Euler(0, rng.F() * 360f, 0)); go.transform.localScale *= .85f + rng.F() * .3f;
                go.name = (isTree ? "Tree_P" : "Shrub_P") + p.id; if (isTree) nt++; else nsb++;
            }
        }
        return $"빈 필지 나무 {nt} · 덤불 {nsb}";
    }
    static IEnumerable<Vector2> Samples(Mass m, float step)
    {
        var t = new Vector2(m.f.y, -m.f.x);
        for (float x = -m.w * .5f; x <= m.w * .5f + 1e-3f; x += Mathf.Min(step, m.w * .5f)) for (float z = -m.d; z <= 1e-3f; z += Mathf.Min(step, m.d * .5f)) yield return m.c + t * x + m.f * z;
        foreach (var (u, v) in new[] { (.5f, 0f), (-.5f, 0f), (.5f, -1f), (-.5f, -1f) }) yield return m.c + t * (u * m.w) + m.f * (v * m.d);
    }
    static float TopY(Vector2 p)
    {
        int n = Physics.RaycastNonAlloc(new Ray(new Vector3(p.x, 400, p.y), Vector3.down), buf, 800, ~0, QueryTriggerInteraction.Ignore); float best = float.NaN;
        for (int i = 0; i < n; i++) { if (SkipRoot(buf[i].collider)) continue; if (float.IsNaN(best) || buf[i].point.y > best) best = buf[i].point.y; }
        return best;
    }
    // 0301: 맞은 물체의 맨 위 부모 이름을 매번 문자열로 꺼내던 것(광선 수십만 번) → 맨 위 부모별로 한 번만 묻고 기억. 이름은 실행 중 안 바뀜.
    public static bool OldPath0301;   // 0301 검증용: 켜면 속도 고치기 전 길(병렬 끔·기본 해시·옛 DT·이름 매번). 결과는 같아야 한다.
    static readonly Dictionary<int, bool> skipRoot = new Dictionary<int, bool>();
    static bool SkipRoot(Collider c)
    {
        if (OldPath0301) { var rn0 = c.transform.root.name; return rn0 == RootName || rn0 == "Roadside" || rn0 == "OnFoot_HomeGarage"; }
        var r = c.transform.root; int id = r.GetInstanceID();
        if (!skipRoot.TryGetValue(id, out bool sk)) { var rn = r.name; skipRoot[id] = sk = rn == RootName || rn == "Roadside" || rn == "OnFoot_HomeGarage"; }
        return sk;
    }
    // 높이: 층 × 층고(+평지붕 난간). 이웃(붙은 집·옆 필지) 덩어리와 0.4 m 안 차이면 층고를 바꾸거나 한 층 더/덜 → 줄 맞춘 높이 X
    static void Heights(Mass m, Parcel p, List<Parcel> parcels)
    {
        float Top(Mass x) => x.y0 + x.floors * x.floorH + (x.roof == "flat" ? .35f : 0);
        m.H = m.floors * m.floorH + (m.roof == "flat" ? .35f : 0);
        var nbs = p.nb.Select(i => parcels[i].mass).Where(x => x != null).ToList();
        for (int it = 0; it < 4; it++)
        {
            var clash = nbs.FirstOrDefault(x => Mathf.Abs(Top(x) - (m.y0 + m.H)) < MinStep); if (clash == null) break;
            if (it % 2 == 0) m.floorH = Mathf.Clamp(m.floorH + (m.y0 + m.H >= Top(clash) ? .45f : -.45f), 2.6f, m.type == "barn" ? 4.6f : 3.4f);
            else if (m.type == "row" || m.type == "flat" || m.type == "shop") m.floors = Mathf.Clamp(m.floors + (m.floors <= 2 ? 1 : -1), 2, 3);   // 3판: 유형의 층수는 지킨다
            else m.floorH = Mathf.Clamp(m.floorH + .25f, 2.6f, m.type == "barn" ? 4.6f : 3.4f);
            m.H = m.floors * m.floorH + (m.roof == "flat" ? .35f : 0);
        }
        float span = m.roof == "gable" ? (m.ridgeX ? m.d : m.w) : Mathf.Min(m.w, m.d);
        m.rise = m.roof == "flat" ? 0 : span * .5f * Mathf.Tan(m.pitch * Mathf.Deg2Rad);
    }
    static Vis Vistas()
    {
        var v = new Vis(); var path = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/map/vistas.json"); if (!System.IO.File.Exists(path)) return v;
        var js = (Dictionary<string, object>)PitCrewPlan.MiniJson.Parse(System.IO.File.ReadAllText(path, Encoding.UTF8));
        float F(object o) => System.Convert.ToSingle(o, System.Globalization.CultureInfo.InvariantCulture);
        foreach (Dictionary<string, object> o in (List<object>)js["overlooks"])
        {
            var p = (List<object>)o["pos"]; var q = new Vector2(F(p[0]), F(p[2])); if (!InR(q)) continue;
            v.ov.Add((q, F(o["eye_height_m"]), F(o["open_start_deg"]), F(o["open_end_deg"])));
        }
        foreach (Dictionary<string, object> c in (List<object>)js["corridors"])
        {
            var pl = ((List<object>)c["polyline"]).Cast<List<object>>().Select(a => new Vector3(F(a[0]), F(a[1]), F(a[2]))).ToList();
            for (int i = 0; i + 1 < pl.Count; i += 2)
            {
                var a = new Vector2(pl[i].x, pl[i].z); if (!InR(a)) continue; var d = new Vector2(pl[i + 1].x, pl[i + 1].z) - a; if (d.sqrMagnitude < 1e-4f) continue;
                v.los.Add((a, a + d.normalized * 40f, pl[i].y + 1.6f));
            }
        }
        return v;
    }
    // 3판: 전망대 눈에서 집 꼭대기까지 땅·축대가 먼저 가리면(깎은 단 안쪽 등) 그 집은 시야를 안 막는다
    static bool Hidden(Vector3 eye, Vector3 top)
    {
        var d = top - eye; float L = d.magnitude; if (L < 1f) return false;
        int n = Physics.RaycastNonAlloc(new Ray(eye, d / L), buf, L - .5f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++) { if (buf[i].distance < 3f) continue; if (SkipRoot(buf[i].collider)) continue; return true; }
        return false;
    }
    // 3판: 덩어리 둘레 2.5 m 밖 땅(구덩이 테두리) 최고 높이 — 집 꼭대기가 이보다 0.3 m 안 넘으면 땅이 이미 막는 높이라 전망을 새로 안 막는다
    static float Rim(Mass m)
    {
        var t = new Vector2(m.f.y, -m.f.x); float hw = m.w * .5f + 2.5f, best = float.NegativeInfinity;
        foreach (var (u, v) in new[] { (-1f, .3f), (0f, .3f), (1f, .3f), (-1f, -.5f), (1f, -.5f), (-1f, -1.3f), (0f, -1.3f), (1f, -1.3f) })
        { float y = TopY(m.c + t * (u * hw) + m.f * (v >= 0 ? 2.5f : v < -1 ? -m.d - 2.5f : -m.d * .5f)); if (!float.IsNaN(y)) best = Mathf.Max(best, y); }
        return best;
    }
    static List<(Vector2 c, Vector2 h)> Props()
    {
        var res = new List<(Vector2, Vector2)>(); var r = GameObject.Find("Roadside"); if (r == null) return res;
        foreach (var rd in r.GetComponentsInChildren<Renderer>()) { var b = rd.bounds; res.Add((new Vector2(b.center.x, b.center.z), new Vector2(b.extents.x, b.extents.z))); }
        return res;
    }
    // 시야·소품: 전망대 앞 부채꼴(60 m 안, 5° 내려다보는 선보다 높으면) · 전망 회랑(길 앞 40 m 직선 시선을 끊으면) · 길가 소품과 겹침
    static string Blocked(Mass m, Vis v, List<(Vector2 c, Vector2 h)> props)
    {
        float top = m.y0 + m.H + m.rise; var corners = Corners(m, .0f);
        foreach (var o in v.ov)
            foreach (var q in corners.Append(m.c - m.f * m.d * .5f))
            {
                var d = q - o.p; float dd = d.magnitude; if (dd > 60 || dd < .5f) continue;
                float brg = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; if (brg < 0) brg += 360;
                bool inArc = o.a0 <= o.a1 ? brg >= o.a0 && brg <= o.a1 : brg >= o.a0 || brg <= o.a1;
                if (inArc && top > o.eye - dd * Mathf.Tan(5 * Mathf.Deg2Rad) && !Hidden(new Vector3(o.p.x, o.eye, o.p.y), new Vector3(q.x, top, q.y)) && top > Rim(m) + .3f) return "전망대 앞(vistas overlook)";
            }
        var big = Corners(m, .5f);
        foreach (var l in v.los) if (top > l.eye && SegPoly(l.a, l.b, big)) return "전망 회랑(vistas corridor)";
        var me = Corners(m, .1f);
        foreach (var pr in props) if ((pr.c - m.c).sqrMagnitude < 400 && PolyPoly(me, Box(pr.c, pr.h))) return "길가 소품과 겹침";
        return null;
    }
    static void Types(Parcel p)
    {
        p.types.Clear(); var m = p.mass; if (m == null) { p.types.Add(p.depth > 12 ? "마당·밭" : "빈터"); return; }
        // 3판: 덩어리 유형(D-220 8종) + building-types.md 쪽 이름(아트 연결용 가설)
        p.types.Add(m.type);
        p.types.Add(m.type switch { "shop" => "shop_house", "row" => "town_house", "stepped2" => "terrace_house", "barn" => "farmhouse", "bungalow" => "detached_house", "detached" => "detached_house", _ => m.seat == "C1" ? "terrace_house" : "town_house" });
    }

    // ── 씬에 덩어리 ────────────────────────────────────────────
    static Material[] Materials()
    {
        Material Get(string n, Color c)
        {
            // OSM 회색 건물 벽(TV6_Wall, LofiSurface) 속성 그대로 + 색(_Tint)만 회색. 씬 전용 사본 — 공유 .mat 는 안 건드림
            var path = Mats + n + ".mat"; var src = AssetDatabase.LoadAssetAtPath<Material>(Mats + "TV6_Wall.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = n; AssetDatabase.CreateAsset(m, path); }
            else if (src != null) { m.shader = src.shader; m.CopyPropertiesFromMaterial(src); }
            if (m.HasProperty("_Tint")) m.SetColor("_Tint", c); else m.color = c;
            EditorUtility.SetDirty(m); return m;
        }
        // 3판: 유형 구분용 회색 톤 4개(외관 아님) — 0 단독·연립 · 1 단층·평지붕(밝게) · 2 좁은 3층·2단(차갑고 어둡게) · 3 가게·헛간(따뜻하게)
        var res = new[] { Get("Parcels_Body", new Color(.74f, .74f, .72f)), Get("Parcels_Body1", new Color(.82f, .81f, .78f)), Get("Parcels_Body2", new Color(.64f, .66f, .68f)), Get("Parcels_Body3", new Color(.72f, .68f, .62f)),
            Get("Parcels_Roof", new Color(.46f, .46f, .48f)), Get("Parcels_Plinth", new Color(.55f, .51f, .45f)), Get("Parcels_Door", new Color(.24f, .23f, .22f)),
            Get("Parcels_Window", new Color(.34f, .40f, .48f)) };   // 4판: 창(유리 쪽 푸른 회색 — 문보다 밝게)
        AssetDatabase.SaveAssets(); return res;
    }
    static int nWin, nDoor, nStoop;
    static Transform Spawn(Mass m, Transform root, Material[] mats)
    {
        var go = new GameObject($"P{m.parcel:000}_{m.type}_{m.floors}F_{m.roof}_{m.seat}" + (m.tier >= 0 ? $"_tier{m.tier}" : "") + (m.group >= 0 ? $"_row{m.group}" : ""));
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(new Vector3(m.c.x, m.y0, m.c.y), Quaternion.LookRotation(new Vector3(m.f.x, 0, m.f.y)));
        var v = new List<Vector3>(); var tb = new List<int>(); var tr = new List<int>(); var tp = new List<int>(); var td = new List<int>();
        float hw = m.w * .5f, D = m.d, H = m.H, yb = m.bot - m.y0;
        var dc = new Vector3(0, H * .5f, -D * .5f); var t2 = new Vector2(m.f.y, -m.f.x);
        // 문 판(어두운 판): side 0 앞 · 1 뒤 · 2 왼(-t) · 3 오른(+t). 아래 끝 = 그 면 바깥 땅(묻힌 쪽은 바닥)
        void Door(int side, float dw, float top)
        {
            Vector2 n2 = side == 0 ? m.f : side == 1 ? -m.f : side == 2 ? -t2 : t2; float off = side < 2 ? D * .5f + .6f : hw + .6f;
            float gy = TopY(m.c - m.f * (D * .5f) + n2 * off); float db = float.IsNaN(gy) ? 0 : Mathf.Clamp(gy - m.y0, yb, top - 1.2f);
            float e = .17f;
            if (side == 0) Quad(v, td, new Vector3(-dw * .5f, db, e), new Vector3(dw * .5f, db, e), new Vector3(dw * .5f, top, e), new Vector3(-dw * .5f, top, e), dc);
            else if (side == 1) Quad(v, td, new Vector3(-dw * .5f, db, -D - e), new Vector3(dw * .5f, db, -D - e), new Vector3(dw * .5f, top, -D - e), new Vector3(-dw * .5f, top, -D - e), dc);
            else { float x = side == 2 ? -hw - e : hw + e, zc = -D * .5f; Quad(v, td, new Vector3(x, db, zc - dw * .5f), new Vector3(x, db, zc + dw * .5f), new Vector3(x, top, zc + dw * .5f), new Vector3(x, top, zc - dw * .5f), dc); }
        }
        // 단차·반지하 집: 내리막 쪽 아래층 문 · 가게: 1층 넓은 셔터 · 헛간: 큰 문
        // 4판(판정 결함 1 「창 없는 상자 + 거대한 검은 문 = 창고」): 아래층 문 2.6 → 사람 문 1.1 m · 셔터는 폭 3.2 m 까지
        int lowSide = m.doorSide < 0 ? (m.doorBack ? 1 : 0) : m.doorSide;
        if (m.door) Door(lowSide, 1.1f, 2.2f);
        if (m.shutter) Door(0, Mathf.Clamp(m.w - 1.4f, 1.6f, 3.2f), Mathf.Min(2.4f, m.floorH - .4f));
        if (m.barnDoor) Door(0, Mathf.Min(3.4f, m.w * .45f), Mathf.Min(3.2f, H - .4f));
        var ctr = new Vector3(0, H * .5f, -D * .5f);
        // 4판(결함 2 「밑이 풀에 박힘」): 기단이 없는 집도 밑 0.3 m 까지 기단 띠(돌 색) — 바닥 높이·노출 기단 수치는 그대로
        if (m.plinth) { Box(v, tp, m.attL ? -hw : -hw - .15f, m.attR ? hw : hw + .15f, -D - .15f, .15f, yb, .3f, ctr); Box(v, tb, -hw, hw, -D, 0, 0, H, ctr); }
        else { Box(v, tp, m.attL ? -hw : -hw - .06f, m.attR ? hw : hw + .06f, -D - .06f, .06f, yb, .3f, ctr); Box(v, tb, -hw, hw, -D, 0, 0, H, ctr); }
        // 4판(결함 1): 창 — 층마다 면 길이 2.3 m 에 하나(0.9 × 1.2, 창턱 0.9). 땅에 묻힌 칸·붙은 옆면은 건너뜀. 앞면 1층 한 칸은 현관문(1.0 × 2.15) + 땅이 낮으면 디딤단
        var tw = new List<int>(); var wr = new Rng(Fnv($"{Seed}|win|{m.parcel}|{m.tier}"));
        float LocalGround(int side, float s)
        {
            Vector2 n2 = side == 0 ? m.f : side == 1 ? -m.f : side == 2 ? -t2 : t2; var at = side < 2 ? m.c + t2 * s + m.f * (side == 0 ? 0 : -D) : m.c + t2 * (side == 2 ? -hw : hw) + m.f * s;
            float gy = TopY(at + n2 * .6f); return float.IsNaN(gy) ? 0 : gy - m.y0;
        }
        void Pane(List<int> tl, int side, float s, float half, float y0, float y1, float e)
        {
            if (side == 0) Quad(v, tl, new Vector3(s - half, y0, e), new Vector3(s + half, y0, e), new Vector3(s + half, y1, e), new Vector3(s - half, y1, e), dc);
            else if (side == 1) Quad(v, tl, new Vector3(s - half, y0, -D - e), new Vector3(s + half, y0, -D - e), new Vector3(s + half, y1, -D - e), new Vector3(s - half, y1, -D - e), dc);
            else { float x = side == 2 ? -hw - e : hw + e; Quad(v, tl, new Vector3(x, y0, s - half), new Vector3(x, y0, s + half), new Vector3(x, y1, s + half), new Vector3(x, y1, s - half), dc); }
        }
        bool front = !(m.door && lowSide == 0) && !m.shutter && !m.barnDoor;
        for (int side = 0; side < 4; side++)
        {
            if (side == 2 && m.attL || side == 3 && m.attR) continue;
            float L = side < 2 ? m.w : D; int n = Mathf.Max(L >= 2f ? 1 : 0, Mathf.FloorToInt((L - .6f) / 2.3f)); if (n == 0) continue;
            int doorK = side == 0 && front ? (n >= 2 ? (wr.F() < .5f ? 0 : n - 1) : 0) : -1;
            for (int k = 0; k < n; k++)
            {
                float s = side < 2 ? -L * .5f + L * (k + .5f) / n : -D + D * (k + .5f) / n; float g = LocalGround(side, s);
                for (int fl = 0; fl < m.floors; fl++)
                {
                    float fy = fl * m.floorH;
                    if (fl == 0 && k == doorK)
                    {
                        Pane(td, side, s, .5f, Mathf.Max(0, g), 2.15f, .04f); nDoor++;
                        if (g < -.12f) { Box(v, tp, s - .7f, s + .7f, 0, Mathf.Clamp(m.b - .15f, .4f, .9f), Mathf.Max(g, yb), 0, ctr); nStoop++; }   // 디딤단(현관 앞 땅까지, 물러남 안에서)
                        continue;
                    }
                    if (m.door && fl == 0 && side == lowSide) continue;   // 아래층 문 면 1층은 문만
                    if (m.shutter && fl == 0 && side == 0) continue;
                    if (m.barnDoor) continue;   // 헛간은 창 없이 큰 문
                    float y0w = fy + .9f, y1w = Mathf.Min(fy + 2.1f, H - .3f); if (y1w - y0w < .6f || y0w < g + .25f) continue;
                    Pane(tw, side, s, .45f, y0w, y1w, .04f); nWin++;
                }
            }
        }
        float R = m.rise, o = .25f;
        if (m.roof == "flat")
        {
            Box(v, tr, -hw, hw, -D, 0, H, H + .3f, ctr);
            // 평지붕 집: 옥상 난간(0.9 m) + 뒤 모서리 작은 옥탑(2.4 × 2.4 × 2.3)
            if (m.roofRoom)
            {
                float k = .15f, y1 = H + 1.2f;
                Box(v, tb, -hw, hw, -k, 0, H + .3f, y1, ctr); Box(v, tb, -hw, hw, -D, -D + k, H + .3f, y1, ctr);
                Box(v, tb, -hw, -hw + k, -D + k, -k, H + .3f, y1, ctr); Box(v, tb, hw - k, hw, -D + k, -k, H + .3f, y1, ctr);
                if (m.w >= 4.5f && D >= 5f) { float rx = m.ridgeX ? -hw + .3f : hw - 2.7f; Box(v, tb, rx, rx + 2.4f, -D + .3f, -D + 2.7f, H + .3f, H + 2.6f, ctr); Box(v, tr, rx - .1f, rx + 2.5f, -D + .2f, -D + 2.8f, H + 2.6f, H + 2.8f, ctr); }
            }
        }
        else if (m.roof == "gable" && m.ridgeX)
        {
            var a = new Vector3(-hw, H, o); var b = new Vector3(hw, H, o); var rl = new Vector3(-hw, H + R, -D * .5f); var rr = new Vector3(hw, H + R, -D * .5f); var c = new Vector3(-hw, H, -D - o); var d = new Vector3(hw, H, -D - o);
            float lift = o * Mathf.Tan(m.pitch * Mathf.Deg2Rad); a.y -= lift; b.y -= lift; c.y -= lift; d.y -= lift;
            Quad(v, tr, a, b, rr, rl, ctr); Quad(v, tr, d, c, rl, rr, ctr);
            Tri(v, tr, new Vector3(-hw, H, 0), new Vector3(-hw, H, -D), rl, ctr); Tri(v, tr, new Vector3(hw, H, 0), new Vector3(hw, H, -D), rr, ctr);
        }
        else if (m.roof == "gable")
        {
            var a = new Vector3(-hw - o, H, 0); var b = new Vector3(-hw - o, H, -D); var rf = new Vector3(0, H + R, 0); var rb = new Vector3(0, H + R, -D); var c = new Vector3(hw + o, H, 0); var d = new Vector3(hw + o, H, -D);
            float lift = o * Mathf.Tan(m.pitch * Mathf.Deg2Rad); a.y -= lift; b.y -= lift; c.y -= lift; d.y -= lift;
            Quad(v, tr, a, b, rb, rf, ctr); Quad(v, tr, c, d, rb, rf, ctr);
            Tri(v, tr, new Vector3(-hw, H, 0), new Vector3(hw, H, 0), rf, ctr); Tri(v, tr, new Vector3(-hw, H, -D), new Vector3(hw, H, -D), rb, ctr);
        }
        else
        {
            // 모임지붕: 긴 변 방향 용마루, 양 끝 짧은 변/2 만큼 줄임
            var p00 = new Vector3(-hw, H, 0); var p10 = new Vector3(hw, H, 0); var p01 = new Vector3(-hw, H, -D); var p11 = new Vector3(hw, H, -D);
            if (m.w >= D) { float e = hw - D * .5f; var r0 = new Vector3(-e, H + R, -D * .5f); var r1 = new Vector3(e, H + R, -D * .5f); Quad(v, tr, p00, p10, r1, r0, ctr); Quad(v, tr, p11, p01, r0, r1, ctr); Tri(v, tr, p01, p00, r0, ctr); Tri(v, tr, p10, p11, r1, ctr); }
            else { float e = D * .5f - hw; var r0 = new Vector3(0, H + R, -hw); var r1 = new Vector3(0, H + R, -D + hw); Quad(v, tr, p00, p01, r1, r0, ctr); Quad(v, tr, p11, p10, r0, r1, ctr); Tri(v, tr, p10, p00, r0, ctr); Tri(v, tr, p01, p11, r1, ctr); }
        }
        var mesh = new Mesh { name = go.name }; mesh.SetVertices(v); mesh.SetUVs(0, v.Select(q => new Vector2((q.x + q.z) * .25f, q.y * .25f)).ToList()); mesh.subMeshCount = 5; mesh.SetTriangles(tb, 0); mesh.SetTriangles(tr, 1); mesh.SetTriangles(tp, 2); mesh.SetTriangles(td, 3); mesh.SetTriangles(tw, 4); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats[Mathf.Clamp(m.tone, 0, 3)], mats[4], mats[5], mats[6], mats[7] };
        return go.transform;
    }

    // ── 3판: 같은 유형 3연속 ──────────────────────────────────
    // 한 단위 = 단독 필지 하나 또는 붙은 묶음 하나. 단위 U 의 옆 필지들 중 U 와 같은 유형인 다른 단위가 2개 이상이면 U 가 3연속의 가운데
    static List<List<Parcel>> RunCenters(List<Parcel> ps)
    {
        var res = new List<List<Parcel>>();
        foreach (var u in ps.Where(p => p.mass != null && p.type != null).GroupBy(p => p.group >= 0 ? -1 - p.group : p.id))
        {
            var mem = u.ToList(); var ty = mem[0].type; var ids = new HashSet<int>(mem.Select(p => p.id));
            int n = mem.SelectMany(p => SideNb(p, ps)).Where(q => !ids.Contains(q.id) && q.mass != null && q.type == ty).Select(q => q.group >= 0 ? -1 - q.group : q.id).Distinct().Count();
            if (n >= 2) res.Add(mem);
        }
        return res;
    }
    static int nRepair, nRunLeft;
    // 가운데 단독 집의 유형을 지붕·층만 바꿔 다른 유형으로(자리·바닥은 그대로). 바꾼 뒤 옆집과 겹치면 다음 후보, 다 안 되면 둔다
    static int Repair(List<Parcel> ps, List<Mass> masses)
    {
        int fixedN = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            var cs = RunCenters(ps); if (cs.Count == 0) break;
            foreach (var mem in cs)
            {
                if (mem.Count != 1) continue; var p = mem[0]; var m = p.mass; if (m == null || p.mass2 != null || m.group >= 0) continue;
                var av = Avoid(p, ps); var bak = m.Clone();
                string[] opts = m.type switch { "detached" => new[] { "flat", "narrow3" }, "flat" => new[] { "detached", "narrow3" }, "narrow3" => new[] { "flat", "detached" }, "bungalow" => new[] { "flat", "detached" }, "shop" => new[] { "flat", "detached" }, "barn" => new[] { "bungalow", "detached" }, _ => new[] { "flat", "detached" } };
                bool ok = false;
                foreach (var ty in opts)
                {
                    if (av.Contains(ty) || ty == m.type) continue;
                    m.type = ty; m.shutter = m.barnDoor = m.roofRoom = false; m.floorH = Mathf.Clamp(m.floorH, 2.6f, 3.4f); m.pitch = Mathf.Max(m.pitch, 24);
                    switch (ty)
                    {
                        case "flat": m.roof = "flat"; m.roofRoom = true; if (m.floors < 2 && m.seat != "C1") m.floors = 2; break;
                        case "detached": m.roof = "gable"; m.ridgeX = true; m.floors = Mathf.Max(2, m.floors); if (m.floors > 2 && m.seat != "C1") m.floors = 2; break;
                        case "narrow3": if (m.w > 5.2f) { m.type = bak.type; continue; } m.floors = 3; m.roof = "flat"; break;
                        case "bungalow": if (m.seat == "C1") { m.type = bak.type; continue; } m.floors = 1; m.roof = "hip"; break;
                    }
                    m.tone = Tone(m.type); p.type = m.type; Heights(m, p, ps);
                    bool hit = masses.Any(o => o != m && (o.c - m.c).sqrMagnitude < 900 && PolyPoly(Corners(o, 0), Corners(m, 0)));
                    if (!hit && SideNb(p, ps).Count(q => q.mass != null && q.type == ty) < 2) { ok = true; break; }
                    CopyInto(bak, m); p.type = m.type;
                }
                if (ok) { fixedN++; Types(p); }
            }
        }
        nRunLeft = RunCenters(ps).Count;
        return fixedN;
    }
    static void CopyInto(Mass src, Mass dst) { foreach (var fi in typeof(Mass).GetFields()) fi.SetValue(dst, fi.GetValue(src)); }

    // ── 3판: 집 사이 좁은 돌계단 골목(덩어리만, 충돌체 없음) ──────────
    // 반지하·2단 집(C1·C2) 중 비탈이 앞뒤로 내려가는 집의 빈 옆(붙지 않은 쪽, 벽에서 0.15 m)에 폭 0.9 m · 디딤 0.6 m · 단 높이 0.18 m 로 맞춘 계단
    static string Stairs(List<Parcel> ps, List<Mass> masses, Transform root, Material[] mats)
    {
        var holder = new GameObject("StoneStairs").transform; holder.SetParent(root, false);
        int n = 0; float len = 0; var used = new List<List<Vector2>>();
        foreach (var m in masses)
        {
            if (m.seat == null || !m.seat.StartsWith("C") || m.tier == 1) continue;
            var t = new Vector2(m.f.y, -m.f.x);
            float D = m.pair != null ? m.d + m.pair.d + Mathf.Abs(Vector2.Dot(m.pair.c - m.c, m.f)) - m.pair.d : m.d;   // 2단이면 두 단을 다 따라
            D = Mathf.Max(m.d, Mathf.Min(D, 18f));
            if (Mathf.Abs(m.gz) < Mathf.Abs(m.gx) * 1.2f) continue;   // 비탈이 옆으로 흐르면 계단 골목 의미 없음
            foreach (int side in new[] { 1, -1 })
            {
                if (side < 0 ? m.attL : m.attR) continue;
                const float W = .9f; float x0 = side * (m.w * .5f + .15f), x1 = x0 + side * W, xm = (x0 + x1) * .5f;
                var quad = new List<Vector2> { m.c + t * x0 + m.f * .6f, m.c + t * x1 + m.f * .6f, m.c + t * x1 - m.f * (D + .4f), m.c + t * x0 - m.f * (D + .4f) };
                if (masses.Any(o => o != m && o != m.pair && (o.c - m.c).sqrMagnitude < 900 && PolyPoly(Corners(o, 0), quad))) continue;
                if (used.Any(u => PolyPoly(u, quad))) continue;
                var v = new List<Vector3>(); var tp = new List<int>(); bool bad = false; float prev = float.NaN; int k = 0;
                var ctr = new Vector3(0, -50, 0);
                for (float z = .6f; z > -D - .4f; z -= .6f, k++)
                {
                    var q = m.c + t * xm + m.f * (z - .3f); int c = Idx(q);
                    if (c < 0 || type[c] == T.Road || type[c] == T.Wall || type[c] == T.Excl || foot[c]) { bad = true; break; }
                    float g = TopY(q); if (float.IsNaN(g)) { bad = true; break; }
                    float top = Mathf.Ceil(g / .18f) * .18f + .05f;
                    if (!float.IsNaN(prev) && Mathf.Abs(top - prev) > 1.0f) { bad = true; break; }   // 축대·절벽이면 끊음
                    prev = top;
                    var p0 = m.c + t * x0 + m.f * z; var w3 = new Vector3(p0.x, 0, p0.y);
                    // 월드 좌표로 바로: 디딤판 한 칸 = 옆 W × 앞뒤 0.6 × (땅 - 0.4 ~ top)
                    var a = new Vector3(p0.x, 0, p0.y); var ax = new Vector3(t.x, 0, t.y) * side * W; var az = -new Vector3(m.f.x, 0, m.f.y) * .6f;
                    var lo = Vector3.up * (g - .4f); var hi = Vector3.up * top;
                    void Face(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 cc) => Quad(v, tp, p1, p2, p3, p4, cc);
                    var cc0 = a + ax * .5f + az * .5f + Vector3.up * ((g - .4f + top) * .5f);
                    Face(a + lo, a + ax + lo, a + ax + hi, a + hi, cc0); Face(a + az + lo, a + az + ax + lo, a + az + ax + hi, a + az + hi, cc0);
                    Face(a + lo, a + az + lo, a + az + hi, a + hi, cc0); Face(a + ax + lo, a + ax + az + lo, a + ax + az + hi, a + ax + hi, cc0);
                    Face(a + hi, a + ax + hi, a + ax + az + hi, a + az + hi, cc0);
                }
                if (bad || k < 4) continue;
                var go = new GameObject($"Stair_P{m.parcel:000}_{(side < 0 ? "L" : "R")}"); go.transform.SetParent(holder, false);
                var mesh = new Mesh { name = go.name }; mesh.SetVertices(v); mesh.SetTriangles(tp, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mats[5];
                used.Add(quad); n++; len += k * .6f; break;
            }
        }
        nStairs = n;
        return $"돌계단 골목 {n}줄 · {len:F0} m";
    }
    static int nStairs, nPits;
    // 빈 구덩이: 덩어리 없는 땅 덩이(빈 필지·남은 땅, 40㎡ 이상) 중 둘레의 40% 이상이 축대 칸인 것 = 깎아 놓고 집 없음
    static List<Left> lastLefts;
    // 진단: 마지막 Run 의 3연속 가운데·빈 구덩이 자리
    public static string Diag()
    {
        if (cellsOf == null) return "없음"; var sb = new StringBuilder();
        foreach (var u in RunCenters(cellsOf)) sb.Append($"run {u[0].type} g{u[0].group} n{u.Count} @{u[0].ctr.x:F0},{u[0].ctr.y:F0} zone {u[0].zone} nb " + string.Join(",", u.SelectMany(p => SideNb(p, cellsOf)).Where(q => q.type == u[0].type).Select(q => q.id + "g" + q.group)) + "\n");
        pitWhere.ForEach(s => sb.Append(s).Append('\n'));
        return sb.ToString();
    }
    static List<string> pitWhere = new List<string>();
    static int Pits(List<Parcel> ps, List<Left> lefts)
    {
        int pits = 0; lastLefts = lefts; pitWhere.Clear();
        var regions = ps.Where(p => p.mass == null && p.area >= 40).Select(p => p.cells).Concat(lefts.Where(l => l.area >= 40 && l.cells != null).Select(l => l.cells));
        foreach (var cells in regions)
        {
            var me = new HashSet<int>(cells); int wall = 0, all = 0;
            foreach (var c in cells) foreach (var q in new[] { c - 1, c + 1, c - gw, c + gw }) { if (q < 0 || q >= type.Length || me.Contains(q)) continue; all++; if (type[q] == T.Wall) wall++; }
            if (all > 0 && wall >= .4f * all) { pits++; var pp = ps.FirstOrDefault(p => p.cells == cells); var mc = Mean(cells); pitWhere.Add($"pit @{mc.x:F0},{mc.y:F0} area {cells.Count * C * C:F0} wall {wall}/{all} " + (pp != null ? $"P{pp.id} empty={pp.empty} back={pp.back} slope={pp.slope:F2}" : "남은 땅")); }
        }
        return pits;
    }
    static void Box(List<Vector3> v, List<int> t, float x0, float x1, float z0, float z1, float y0, float y1, Vector3 ctr)
    {
        Vector3 P0(float x, float y, float z) => new Vector3(x, y, z);
        Quad(v, t, P0(x0, y0, z1), P0(x1, y0, z1), P0(x1, y1, z1), P0(x0, y1, z1), ctr); Quad(v, t, P0(x1, y0, z0), P0(x0, y0, z0), P0(x0, y1, z0), P0(x1, y1, z0), ctr);
        Quad(v, t, P0(x0, y0, z0), P0(x0, y0, z1), P0(x0, y1, z1), P0(x0, y1, z0), ctr); Quad(v, t, P0(x1, y0, z1), P0(x1, y0, z0), P0(x1, y1, z0), P0(x1, y1, z1), ctr);
        Quad(v, t, P0(x0, y1, z1), P0(x1, y1, z1), P0(x1, y1, z0), P0(x0, y1, z0), ctr);
    }
    // 바깥을 보게 감음: 면 가운데 - 덩어리 가운데 쪽으로 법선
    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 ctr) { Tri(v, t, a, b, c, ctr, (a + b + c + d) * .25f); Tri(v, t, a, c, d, ctr, (a + b + c + d) * .25f); }
    static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 ctr) => Tri(v, t, a, b, c, ctr, (a + b + c) / 3f);
    static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 ctr, Vector3 fc)
    {
        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-10f) return; int i = v.Count;
        var outv = fc - ctr; if (Mathf.Abs(n.y) > .5f * n.magnitude && fc.y > ctr.y) outv = Vector3.up;   // 지붕·윗면은 위
        v.Add(a); if (Vector3.Dot(n, outv) >= 0) { v.Add(b); v.Add(c); } else { v.Add(c); v.Add(b); }
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    // ── 검사 ───────────────────────────────────────────────────
    class Chk { public string summary, terrain, walls, time; public int overlap, roadHit, stripHit, wallHit, footHit, propHit, floatN; public float worstFloat, expoMax; public string hash; }
    static Chk Check(List<Parcel> parcels, List<Mass> masses, List<(Vector2 c, Vector2 h)> props)
    {
        var k = new Chk(); k.worstFloat = float.NegativeInfinity;
        // 덩어리끼리(붙은 집 이웃은 0.05 m 안쪽으로 줄여서)
        for (int i = 0; i < masses.Count; i++) for (int j = i + 1; j < masses.Count; j++)
        {
            var a = masses[i]; var b = masses[j]; if ((a.c - b.c).sqrMagnitude > 900 || a.pair == b) continue;   // 단차 집 두 단은 한 집
            bool sib = a.group >= 0 && a.group == b.group && Mathf.Abs(a.member - b.member) == 1;
            if (PolyPoly(Corners(a, sib ? -.05f : 0), Corners(b, sib ? -.05f : 0))) k.overlap++;
        }
        var hb = new StringBuilder();
        foreach (var m in masses)
        {
            // 덩어리 밑(기단 포함) 0.25 m 표본 → 길·앞띠·축대·도현 건물 칸
            bool rh = false, sh = false, wh = false, fh = false;
            foreach (var q in Samples(m, .25f)) { int c = Idx(q); if (c < 0) continue; var tt = type[c]; rh |= tt == T.Road; sh |= tt == T.Strip; wh |= tt == T.Wall; fh |= foot[c]; }
            if (rh) k.roadHit++; if (sh) k.stripHit++; if (wh) k.wallHit++; if (fh) k.footHit++;
            if (props.Any(pr => (pr.c - m.c).sqrMagnitude < 400 && PolyPoly(Corners(m, 0), Box(pr.c, pr.h)))) k.propHit++;
            // 뜸: 밑면(바닥 높이 기준 가장 낮은 땅 - 0.3)과 그 자리 땅 높이 차 — 양수면 뜬 것
            float bottom = m.bot; foreach (var q in Samples(m, .5f)) { float y = TopY(q); if (float.IsNaN(y)) continue; float g = bottom - y; k.worstFloat = Mathf.Max(k.worstFloat, g); if (g > .001f) k.floatN++; }
            k.expoMax = Mathf.Max(k.expoMax, m.expo);
            hb.Append(m.seat).Append(m.parcel).Append(':').Append(Mathf.RoundToInt(m.c.x * 100)).Append(',').Append(Mathf.RoundToInt(m.c.y * 100)).Append(',').Append(Mathf.RoundToInt(m.y0 * 100)).Append(',').Append(Mathf.RoundToInt(m.w * 100)).Append(',').Append(Mathf.RoundToInt(m.d * 100)).Append(',').Append(m.floors).Append(m.roof).Append(';');
        }
        foreach (var p in parcels) hb.Append(p.id).Append('#').Append(p.cells.Count).Append(',').Append(p.cells[0]).Append(';');
        k.hash = Fnv(hb.ToString()).ToString("x8");
        var seat = SeatCounts(parcels);
        k.summary = $"앉히기 {seat} · 유형 " + string.Join("/", TypeIds.Select(ty => ty + " " + masses.Count(m => m.type == ty && m.tier != 1))) + $" · 빈 구덩이 {nPits} · 같은 유형 3연속 {nRunLeft}(고침 {nRepair}) ·노출 기단 최대 {k.expoMax:F2} m · 패드 축대 최고 {padMaxWall:F2} m · 지형 구멍·뒤집힘 {terrHoles} · 해시 {k.hash} · 덩어리끼리 겹침 {k.overlap} · 길 {k.roadHit} · 앞띠 {k.stripHit} · 축대 {k.wallHit} · 도현 건물 {k.footHit} · 소품 {k.propHit} · 뜸 {k.floatN}(최대 {k.worstFloat * 100:F1} cm, 음수 = 묻힘)";
        return k;
    }

    // 필지별 처리: A 낮은 기단 · B 패드+축대 · C 단차 · 비움(경사 35%) · 비움(그 밖)
    static string SeatCounts(List<Parcel> ps)
    {
        int a = ps.Count(p => p.mass != null && p.mass.seat == "A"), b = ps.Count(p => p.mass != null && p.mass.seat == "B"), c1 = ps.Count(p => p.mass != null && p.mass.seat == "C1"), c2 = ps.Count(p => p.mass != null && p.mass.seat == "C2");
        int st = ps.Count(p => p.mass == null && p.empty != null && (p.empty.StartsWith("경사") || p.empty.StartsWith("가파름"))), o = ps.Count(p => p.mass == null) - st;
        return $"A 낮은 기단 {a} · B 패드+축대 {b} · C1 반지하 {c1} · C2 2단 {c2} · 비움(경사 60%+) {st} · 비움(그 밖) {o} · 등고선 맞춤 {ps.Count(p => p.mass != null && p.mass.splitT)}";
    }
    // ── 기록 ───────────────────────────────────────────────────
    static string Json(List<Parcel> parcels, List<Left> lefts, List<Mass> masses, Chk k, string gnote, string osm)
    {
        string f2(float x) => x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        string Hist(IEnumerable<float> xs, float[] edges) { var c = new int[edges.Length + 1]; foreach (var x in xs) { int i = 0; while (i < edges.Length && x >= edges[i]) i++; c[i]++; } return "[" + string.Join(",", c) + "]"; }
        var sb = new StringBuilder("{\n");
        sb.Append("  \"note\": \"D-217 길 그리기 3판 — ParcelBuild(PitCrew/필지 다시 나누기, 길 그리기 Enter) 가 쓴다. 손으로 고치지 말 것. 좌표 = Unity 월드 [x,z], yaw = Unity Y(앞면 +Z = 길 쪽), 피벗 = 본체 바닥 앞면 가운데.\",\n");
        sb.Append($"  \"seed\": {Seed}, \"hash\": \"{k.hash}\", \"cellM\": {C}, \"centers\": [{string.Join(",", centers.Select(c => f2(c.x) + "," + f2(c.y)))}], \"radius\": {radius},\n");
        sb.Append($"  \"grid\": \"{gnote}\", \"osm\": \"{osm}\",\n");
        int built0 = parcels.Count(p => p.mass != null);
        sb.Append($"  \"d218\": {{\"area\": \"맵 전체 x {f2(area.xMin)}~{f2(area.xMax)} z {f2(area.yMin)}~{f2(area.yMax)}\", \"fillRate\": {f2((float)built0 / Mathf.Max(1, parcels.Count))}, \"seat\": \"{SeatCounts(parcels)}\", \"expoMaxM\": {f2(k.expoMax)}, \"padWallMaxM\": {f2(padMaxWall)}, \"terrainHolesFlips\": {terrHoles}, \"terrain\": \"{k.terrain}\", \"walls\": \"{k.walls}\", \"time\": \"{k.time}\",");
        sb.Append($" \"rules\": \"경사 > {MaxSlope * 100:F0}% 비움 · A Δ ≤ {LowD} 바닥 = 최고 + 0.12 · B 바닥 = 최저 + {PadLift}(묻힘 ≤ {BuryMax}) · C 경사 쪽 둘로 · 패드 앞 {Apron} 옆 {ApronSide} 깎기·돋움 ≤ {CutMax}/{FillMax} 둘레 {Ring} 가드 {Guard} · 합치기 < {MergeA}㎡ · 묶음 {RowShare}\"}},\n");
        sb.Append($"  \"rules\": {{\"frontW\": \"70% 6~8 · 15% 5~6 · 15% 8~10\", \"maxDepthM\": {MaxDepth}, \"setbackM\": \"1~3 (붙은 집 1.2~2)\", \"floors\": \"1:2:3 = 20:60:20\", \"roof\": \"gable 55 · hip 30 · flat 15\", \"rowShare\": 0.35, \"minStepM\": {MinStep}, \"pads\": {{\"dohyunBuildings\": {FootPad}, \"junction\": {JuncPad}, \"walkMouth\": {MouthPad}}}}},\n");
        // 3판(D-220) 표: 채움 · 유형별 수 · 경사 구간별 처리 · 노출 기단 최대 · 빈 구덩이 · 같은 유형 3연속 · 겹침/뜸/구멍
        {
            int sea = parcels.Count(p => p.mass == null && p.empty == "땅 못 읽음·바다"), landN = parcels.Count - sea;
            string Bin(float lo, float hi) { var g = parcels.Where(p => p.slope >= lo && p.slope < hi).ToList(); return $"{{\"n\": {g.Count}, \"A\": {g.Count(p => p.mass?.seat == "A")}, \"B\": {g.Count(p => p.mass?.seat == "B")}, \"C1\": {g.Count(p => p.mass?.seat == "C1")}, \"C2\": {g.Count(p => p.mass?.seat == "C2")}, \"empty\": {g.Count(p => p.mass == null)}}}"; }
            sb.Append($"  \"d220\": {{\"fillAll\": {f2((float)built0 / Mathf.Max(1, parcels.Count))}, \"fillLand\": {f2((float)built0 / Mathf.Max(1, landN))}, \"built\": {built0}, \"landParcels\": {landN}, \"back\": {parcels.Count(p => p.back)}, \"backBuilt\": {parcels.Count(p => p.back && p.mass != null)},");
            sb.Append(" \"types\": {" + string.Join(", ", TypeIds.Select(ty => $"\"{ty}\": {parcels.Count(p => p.mass != null && p.mass.type == ty)}")) + "},");
            sb.Append(" \"zones\": {" + string.Join(", ", ZoneIds.Select(z => { var g = parcels.Where(p => p.zone == z && p.empty != "땅 못 읽음·바다").ToList(); return $"\"{z}\": [{g.Count(p => p.mass != null)}, {g.Count}]"; })) + "},");
            sb.Append($" \"slopeBins\": {{\"lt15\": {Bin(0, .15f)}, \"15_35\": {Bin(.15f, .35f)}, \"35_60\": {Bin(.35f, .60f)}, \"gt60\": {Bin(.60f, 99f)}, \"unread\": {parcels.Count(p => p.slope < 0)}}},");
            sb.Append($" \"expoMaxM\": {f2(k.expoMax)}, \"pits\": {nPits}, \"sameType3Runs\": {nRunLeft}, \"repaired\": {nRepair}, \"stairs\": {nStairs}, \"overlapPairs\": {k.overlap}, \"floatSamples\": {k.floatN}, \"terrainHolesFlips\": {terrHoles}}},\n");
        }
        // 4판(D-221) 표: 창·현관·디딤단 수 · 옆 필지 집끼리 방향 차(같은 줄이 같은 쪽을 보나)
        {
            var ang = new List<float>(); foreach (var p in parcels) if (p.mass != null) foreach (var q in SideNb(p, parcels)) if (q.id > p.id && q.mass != null) ang.Add(Vector2.Angle(p.mass.f, q.mass.f));
            sb.Append($"  \"d221\": {{\"windows\": {nWin}, \"frontDoors\": {nDoor}, \"stoops\": {nStoop}, \"lowDoorW\": 1.1, \"sidePairs\": {ang.Count}, \"sideAngleMedianDeg\": {f2(Median(ang))}, \"sideAngleLt5\": {f2((float)ang.Count(a => a < 5) / Mathf.Max(1, ang.Count))}, \"sideAngleGt15\": {ang.Count(a => a > 15)}}},\n");
        }
        var built = parcels.Where(p => p.mass != null).ToList();
        float[] wEdges = { 5, 6, 7, 8, 9, 10 };
        sb.Append($"  \"stats\": {{\"parcels\": {parcels.Count}, \"withMass\": {masses.Count}, \"empty\": {parcels.Count - built.Count}, \"rows\": {masses.Where(m => m.group >= 0).Select(m => m.group).Distinct().Count()}, \"rowMasses\": {masses.Count(m => m.group >= 0)},");
        sb.Append($" \"frontW_bins_lt5_5_6_7_8_9_10_ge10\": {Hist(parcels.Select(p => p.frontW), wEdges)}, \"frontW_median\": {f2(Median(parcels.Select(p => p.frontW)))},");
        sb.Append($" \"floors_1_2_3\": [{masses.Count(m => m.floors == 1)},{masses.Count(m => m.floors == 2)},{masses.Count(m => m.floors == 3)}], \"roof_gable_hip_flat\": [{masses.Count(m => m.roof == "gable")},{masses.Count(m => m.roof == "hip")},{masses.Count(m => m.roof == "flat")}], \"plinth\": {masses.Count(m => m.plinth)},");
        sb.Append(" \"emptyReasons\": {" + string.Join(",", parcels.Where(p => p.mass == null).GroupBy(p => p.empty ?? "?").OrderBy(g => g.Key).Select(g => $"\"{g.Key}\": {g.Count()}")) + "},");
        sb.Append(" \"leftovers\": {" + string.Join(",", lefts.GroupBy(l => l.kind).OrderBy(g => g.Key).Select(g => "\"" + g.Key + "\": {\"n\": " + g.Count() + ", \"areaM2\": " + g.Sum(l => l.area).ToString("F0") + "}")) + "}},\n");
        sb.Append($"  \"check\": {{\"overlapPairs\": {k.overlap}, \"roadHits\": {k.roadHit}, \"stripHits\": {k.stripHit}, \"wallHits\": {k.wallHit}, \"dohyunHits\": {k.footHit}, \"propHits\": {k.propHit}, \"floatSamples\": {k.floatN}, \"worstFloatM\": {f2(k.worstFloat)}}},\n");
        sb.Append("  \"parcels\": [\n");
        for (int i = 0; i < parcels.Count; i++)
        {
            var p = parcels[i]; var m = p.mass; float yaw = Mathf.Atan2(p.toRoad.x, p.toRoad.y) * Mathf.Rad2Deg;
            sb.Append($"    {{\"id\": {p.id}, \"center\": [{f2(p.ctr.x)},{f2(p.ctr.y)}], \"frontW\": {f2(p.frontW)}, \"depth\": {f2(p.depth)}, \"areaM2\": {p.area:F0}, \"road\": \"{p.road}\", \"frontYaw\": {yaw:F1}, \"types\": [{string.Join(",", p.types.Select(x => "\"" + x + "\""))}]");
            if (m != null) sb.Append($", \"mass\": {{\"pos\": [{f2(m.c.x)},{f2(m.y0)},{f2(m.c.y)}], \"yaw\": {Mathf.Atan2(m.f.x, m.f.y) * Mathf.Rad2Deg:F1}, \"w\": {f2(m.w)}, \"d\": {f2(m.d)}, \"setback\": {f2(m.b)}, \"floors\": {m.floors}, \"floorH\": {f2(m.floorH)}, \"roof\": \"{m.roof}\", \"ridge\": \"{(m.ridgeX ? "along_road" : "to_road")}\", \"wallH\": {f2(m.H)}, \"ridgeH\": {f2(m.H + m.rise)}, \"seat\": \"{m.seat}\", \"deltaM\": {f2(m.gMax - m.gMin)}, \"slope\": {f2(m.slope)}, \"plinthH\": {f2(m.expo)}, \"buryM\": {f2(m.bury)}, \"tier2\": {(p.mass2 != null ? $"[{f2(p.mass2.c.x)},{f2(p.mass2.y0)},{f2(p.mass2.c.y)},{f2(p.mass2.w)},{f2(p.mass2.d)}]" : "null")}, \"row\": {m.group}, \"attached\": \"{(m.attL ? "L" : "")}{(m.attR ? "R" : "")}\"}}");
            else sb.Append($", \"mass\": null, \"empty\": \"{p.empty}\", \"fitW\": {f2(p.fitW)}");
            sb.Append(i < parcels.Count - 1 ? "},\n" : "}\n");
        }
        sb.Append("  ],\n  \"leftovers\": [\n");
        for (int i = 0; i < lefts.Count; i++) sb.Append($"    {{\"kind\": \"{lefts[i].kind}\", \"areaM2\": {lefts[i].area:F0}, \"center\": [{f2(lefts[i].ctr.x)},{f2(lefts[i].ctr.y)}]}}" + (i < lefts.Count - 1 ? ",\n" : "\n"));
        sb.Append("  ]\n}\n");
        return sb.ToString();
    }
    // 평면도: 1칸 = 2 px, 북(+z) 위. 길 진회색 · 앞띠 연회색 · 축대 갈색 · 비움 분홍 · 필지 = 필지마다 다른 옅은 색 · 덩어리 = 진하게
    static void Png(List<Parcel> parcels, List<Mass> masses, string path)
    {
        const int S = 2; var tex = new Texture2D(gw * S, gh * S, TextureFormat.RGB24, false); var px = new Color32[gw * S * gh * S];
        var massCell = new bool[type.Length]; foreach (var m in masses) foreach (var q in Samples(m, .25f)) { int c = Idx(q); if (c >= 0) massCell[c] = true; }
        for (int k = 0; k < type.Length; k++)
        {
            Color32 col = type[k] switch { T.Out => new Color32(250, 250, 250, 255), T.Road => new Color32(70, 70, 75, 255), T.Strip => new Color32(185, 185, 190, 255), T.Wall => new Color32(130, 95, 60, 255), T.Excl => new Color32(240, 190, 200, 255), _ => new Color32(214, 232, 200, 255) };
            if (foot[k]) col = new Color32(200, 40, 50, 255);
            if (owner != null && owner[k] >= 0) { uint h = Fnv("p" + owner[k]); col = new Color32((byte)(150 + (h & 63)), (byte)(150 + ((h >> 6) & 63)), (byte)(150 + ((h >> 12) & 63)), 255); }
            if (massCell[k]) { var pm = owner != null && owner[k] >= 0 ? parcels[owner[k]].mass : null; col = pm != null && pm.group >= 0 ? new Color32(40, 70, 140, 255) : new Color32(60, 60, 60, 255); }
            int i = k % gw, j = k / gw; for (int a = 0; a < S; a++) for (int b = 0; b < S; b++) px[(j * S + b) * gw * S + i * S + a] = col;
        }
        tex.SetPixels32(px); tex.Apply(); System.IO.File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    // 진단: 마지막 Run 의 필지 하나 둘레를 글자 그림으로(# 필지 · F 앞선 · R 길 · s 앞띠 · W 축대 · x 비움 · o 다른 필지 · . 남은 땅). 위 = 북.
    public static string Ascii(int id)
    {
        if (cellsOf == null || id < 0 || id >= cellsOf.Count) return "없음";
        var p = cellsOf[id]; int i0 = p.cells.Min(c => c % gw) - 6, i1 = p.cells.Max(c => c % gw) + 6, j0 = p.cells.Min(c => c / gw) - 6, j1 = p.cells.Max(c => c / gw) + 6;
        var sb = new StringBuilder($"P{id} front {p.frontW:F1} depth {p.depth:F1} area {p.area:F0} toRoad {p.toRoad} empty {p.empty} fitW {p.fitW:F1}\n");
        for (int j = j1; j >= j0; j--)
        {
            for (int i = i0; i <= i1; i++)
            {
                if (i < 0 || j < 0 || i >= gw || j >= gh) { sb.Append(' '); continue; }
                int k = j * gw + i; char ch = type[k] switch { T.Road => 'R', T.Strip => 's', T.Wall => 'W', T.Excl => 'x', T.Out => ' ', _ => '.' };
                if (owner[k] == id) ch = front[k] ? 'F' : '#'; else if (owner[k] >= 0) ch = 'o';
                sb.Append(ch);
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // ── 격자 도구 ─────────────────────────────────────────────
    static Vector2 P(int k) => g0 + new Vector2((k % gw + .5f) * C, (k / gw + .5f) * C);
    static int Idx(Vector2 p) { int i = Mathf.FloorToInt((p.x - g0.x) / C), j = Mathf.FloorToInt((p.y - g0.y) / C); return i < 0 || j < 0 || i >= gw || j >= gh ? -1 : j * gw + i; }
    static bool InR(Vector2 q) => area.Contains(q);   // 2판(D-218 3): 120 m 원 → 맵 전체
    static Vector2 Mean(List<int> cells) { var s = Vector2.zero; foreach (var c in cells) s += P(c); return s / Mathf.Max(1, cells.Count); }
    static float Median(IEnumerable<float> xs) { var a = xs.OrderBy(x => x).ToList(); return a.Count == 0 ? 0 : a[a.Count / 2]; }
    static void Pca(List<int> cells, out Vector2 mean, out Vector2 u)
    {
        mean = Mean(cells); double xx = 0, xz = 0, zz = 0; foreach (var c in cells) { var d = P(c) - mean; xx += d.x * d.x; xz += d.x * d.y; zz += d.y * d.y; }
        float th = .5f * Mathf.Atan2((float)(2 * xz), (float)(xx - zz)); u = new Vector2(Mathf.Cos(th), Mathf.Sin(th));
    }
    // 이어진 덩어리(4이웃). 입력은 칸 번호 오름차순, 출력도 첫 칸 번호 순 — 결정적
    static List<List<int>> Comps(List<int> cells)
    {
        tagTok++; if (tag == null || tag.Length != type.Length) tag = new int[type.Length]; int tk = tagTok * 2;
        foreach (var c in cells) tag[c] = tk;
        var res = new List<List<int>>(); var q = new Queue<int>();
        foreach (var s in cells)
        {
            if (tag[s] != tk) continue; var comp = new List<int>(); tag[s] = tk + 1; q.Enqueue(s);
            while (q.Count > 0)
            {
                int c = q.Dequeue(); comp.Add(c); int i = c % gw;
                if (i > 0 && tag[c - 1] == tk) { tag[c - 1] = tk + 1; q.Enqueue(c - 1); }
                if (i < gw - 1 && tag[c + 1] == tk) { tag[c + 1] = tk + 1; q.Enqueue(c + 1); }
                if (c - gw >= 0 && tag[c - gw] == tk) { tag[c - gw] = tk + 1; q.Enqueue(c - gw); }
                if (c + gw < tag.Length && tag[c + gw] == tk) { tag[c + gw] = tk + 1; q.Enqueue(c + gw); }
            }
            comp.Sort(); res.Add(comp);
        }
        return res;
    }
    // 거리(벡터 전파 2회 왕복): 가장 가까운 씨앗 칸 번호와 그 유클리드 거리
    // 0301 검증용: 고치기 전 DT 그대로(OldPath0301 일 때만)
    static void DTOld(bool[] seed, out float[] d, out int[] s)
    {
        int N = seed.Length; d = new float[N]; s = new int[N];
        for (int k = 0; k < N; k++) { d[k] = seed[k] ? 0 : float.MaxValue; s[k] = seed[k] ? k : -1; }
        var dd = d; var ss = s;
        void Rel(int k, int q)
        {
            if (q < 0 || q >= N || ss[q] < 0) return; int o = ss[q]; float dx = (k % gw - o % gw) * C, dz = (k / gw - o / gw) * C; float v = Mathf.Sqrt(dx * dx + dz * dz);
            if (v < dd[k]) { dd[k] = v; ss[k] = o; }
        }
        for (int round = 0; round < 2; round++)
        {
            for (int j = 0; j < gh; j++) for (int i = 0; i < gw; i++) { int k = j * gw + i; if (i > 0) { Rel(k, k - 1); if (j > 0) Rel(k, k - gw - 1); } if (j > 0) Rel(k, k - gw); if (i < gw - 1 && j > 0) Rel(k, k - gw + 1); }
            for (int j = gh - 1; j >= 0; j--) for (int i = gw - 1; i >= 0; i--) { int k = j * gw + i; if (i < gw - 1) { Rel(k, k + 1); if (j < gh - 1) Rel(k, k + gw + 1); } if (j < gh - 1) Rel(k, k + gw); if (i > 0 && j < gh - 1) Rel(k, k + gw - 1); }
        }
    }
    static void DT(bool[] seed, out float[] d, out int[] s)
    {
        if (OldPath0301) { DTOld(seed, out d, out s); return; }
        // 0301: 씨앗 칸의 (열, 행)을 같이 들고 다님 — 원래 Rel 마다 나눗셈 4번(k % gw, o / gw …)이 칸 수 × 16번 돌던 것. 같은 정수 → 같은 거리 값, 결과 그대로.
        int N = seed.Length; d = new float[N]; s = new int[N]; var si = new int[N]; var sj = new int[N];
        for (int k = 0; k < N; k++) { d[k] = seed[k] ? 0 : float.MaxValue; s[k] = seed[k] ? k : -1; if (seed[k]) { si[k] = k % gw; sj[k] = k / gw; } }
        var dd = d; var ss = s;
        void Rel(int k, int i, int j, int q)
        {
            int o = ss[q]; if (o < 0) return; float dx = (i - si[q]) * C, dz = (j - sj[q]) * C; float v = Mathf.Sqrt(dx * dx + dz * dz);
            if (v < dd[k]) { dd[k] = v; ss[k] = o; si[k] = si[q]; sj[k] = sj[q]; }
        }
        for (int round = 0; round < 2; round++)
        {
            for (int j = 0; j < gh; j++) for (int i = 0; i < gw; i++) { int k = j * gw + i; if (i > 0) { Rel(k, i, j, k - 1); if (j > 0) Rel(k, i, j, k - gw - 1); } if (j > 0) Rel(k, i, j, k - gw); if (i < gw - 1 && j > 0) Rel(k, i, j, k - gw + 1); }
            for (int j = gh - 1; j >= 0; j--) for (int i = gw - 1; i >= 0; i--) { int k = j * gw + i; if (i < gw - 1) { Rel(k, i, j, k + 1); if (j < gh - 1) Rel(k, i, j, k + gw + 1); } if (j < gh - 1) Rel(k, i, j, k + gw); if (i > 0 && j < gh - 1) Rel(k, i, j, k + gw - 1); }
        }
    }
    static void RasterMesh(Transform tr, bool[] G, float grow, float[] off, float o, byte[] zarr = null, byte zv = 0)
    {
        var mf = tr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return;
        var m = mf.sharedMesh; var v = m.vertices; var w = new Vector2[v.Length]; for (int k = 0; k < v.Length; k++) { var p = tr.TransformPoint(v[k]); w[k] = new Vector2(p.x, p.z); }
        var t = m.triangles;
        for (int k = 0; k < t.Length; k += 3)
        {
            Vector2 a = w[t[k]], b = w[t[k + 1]], c = w[t[k + 2]]; float mg = C * .71f + grow;
            int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - mg - g0.x) / C)), i1 = Mathf.Min(gw - 1, Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + mg - g0.x) / C));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - mg - g0.y) / C)), j1 = Mathf.Min(gh - 1, Mathf.FloorToInt((Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + mg - g0.y) / C));
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
            {
                int kk = j * gw + i; var p = P(kk);
                if (InTri(p, a, b, c) || DistSeg(p, a, b) <= mg - C * .21f || DistSeg(p, b, c) <= mg - C * .21f || DistSeg(p, c, a) <= mg - C * .21f) { G[kk] = true; if (off != null) off[kk] = Mathf.Max(off[kk], o); if (zarr != null && zarr[kk] == 255) zarr[kk] = zv; }
            }
        }
    }
    static List<Vector2> Corners(Mass m, float grow)
    {
        // 붙은 쪽(attL = -t, attR = +t)은 기단·처마가 안 나온다
        var t = new Vector2(m.f.y, -m.f.x); float pd = m.plinth ? .15f : 0, z0 = -m.d - pd - grow, z1 = pd + grow;
        float xl = m.w * .5f + (m.attL ? 0 : pd) + grow, xr = m.w * .5f + (m.attR ? 0 : pd) + grow;
        if (m.roof == "gable" && m.ridgeX) { z0 = Mathf.Min(z0, -m.d - .25f - grow); z1 = Mathf.Max(z1, .25f + grow); }
        if (m.roof == "gable" && !m.ridgeX) { xl = Mathf.Max(xl, m.w * .5f + .25f + grow); xr = Mathf.Max(xr, m.w * .5f + .25f + grow); }
        return new List<Vector2> { m.c + t * -xl + m.f * z1, m.c + t * xr + m.f * z1, m.c + t * xr + m.f * z0, m.c + t * -xl + m.f * z0 };
    }
    static List<Vector2> Box(Vector2 c, Vector2 h) => new List<Vector2> { c + new Vector2(-h.x, -h.y), c + new Vector2(h.x, -h.y), c + new Vector2(h.x, h.y), c + new Vector2(-h.x, h.y) };
    static bool PolyPoly(List<Vector2> a, List<Vector2> b)
    {
        foreach (var poly in new[] { a, b }) for (int i = 0; i < poly.Count; i++)
        {
            var e = poly[(i + 1) % poly.Count] - poly[i]; var n = new Vector2(-e.y, e.x);
            float a0 = a.Min(p => Vector2.Dot(p, n)), a1 = a.Max(p => Vector2.Dot(p, n)), b0 = b.Min(p => Vector2.Dot(p, n)), b1 = b.Max(p => Vector2.Dot(p, n));
            if (a1 <= b0 + 1e-4f || b1 <= a0 + 1e-4f) return false;
        }
        return true;
    }
    static bool SegPoly(Vector2 a, Vector2 b, List<Vector2> poly) => PolyPoly(new List<Vector2> { a, b }, poly);
    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return !(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0)));
    }
    static float DistSeg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float l = ab.sqrMagnitude; float t = l < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l); return (p - (a + ab * t)).magnitude; }
    static uint Fnv(string s) { uint h = 2166136261; foreach (char ch in s) { h ^= ch; h *= 16777619; } return h; }
    class Rng { uint x; public Rng(uint s) { x = s == 0 ? 0x9E3779B9u : s; } public float F() { x ^= x << 13; x ^= x >> 17; x ^= x << 5; return (x & 0xFFFFFF) / 16777216f; } }
}
