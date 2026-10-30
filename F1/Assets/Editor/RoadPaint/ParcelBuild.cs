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
    const float MaxSlope = .35f, LowD = .68f, PadLift = .5f, BuryMax = 1.6f, DeepBury = .6f;
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

    class Parcel { public int id; public List<int> cells; public Vector2 toRoad, t, ctr; public float frontW, depth, area, fitW; public string road, empty; public Mass mass, mass2; public List<string> types = new List<string>(); public int group = -1; public HashSet<int> nb = new HashSet<int>(); }
    class Mass
    {
        public int parcel, group, member, floors, tier = -1, idx; public Vector2 c, f; public float w, d, b, y0, gMin, gMax, H, floorH, pitch, rise, bot, bury, slope, expo, gx, gz; public string roof, seat; public bool ridgeX, plinth, attL, attR, door, doorBack, splitT; public Mass pair; public Transform go;
        public Mass Clone() => (Mass)MemberwiseClone();
    }
    class Left { public string kind; public float area; public Vector2 ctr; }

    public static string Remove()
    {
        var root = GameObject.Find(RootName); if (root != null) Object.DestroyImmediate(root);
        var b = Top()?.Find("Buildings"); if (b != null) b.gameObject.SetActive(true);
        var terr = Top()?.Find("TownV6_Terrain"); var baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BaseTerrain); if (terr != null && baseMesh != null) SetTerrain(terr, baseMesh);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "필지 지움 · OSM 회색 건물 다시 켬 · 지형 terrace1 로 되돌림";
    }
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tbl = JsonUtility.FromJson<RoadsideBuild.RsFile>(System.IO.File.ReadAllText(RoadsideBuild.TablePath, Encoding.UTF8));
        radius = tbl.radius; centers = new Vector2[tbl.centers.Length / 2]; for (int i = 0; i < centers.Length; i++) centers[i] = new Vector2(tbl.centers[2 * i], tbl.centers[2 * i + 1]);
        var top = Top();
        // 2판: 입력 지형은 매번 TerraceBlock 결과(terrace1) — 패드는 그 위에 새로 깎는다
        var terr = top.Find("TownV6_Terrain"); var baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BaseTerrain);
        if (terr == null || baseMesh == null) return "지형 없음(TownV6_Terrain · terrace1)";
        SetTerrain(terr, baseMesh);
        var tb0 = terr.GetComponent<MeshRenderer>().bounds; area = Rect.MinMaxRect(tb0.min.x + 2, tb0.min.z + 2, tb0.max.x - 2, tb0.max.z - 2);
        var root = GameObject.Find(RootName); if (root != null) Object.DestroyImmediate(root);
        root = new GameObject(RootName);
        var osm = ClipOsm(top, root.transform);
        Physics.SyncTransforms();

        // 1. 격자 분류
        string gnote = Classify(top, tbl);
        // 2. 블록 → 분할
        var land = new List<int>(); for (int i = 0; i < type.Length; i++) if (type[i] == T.Land) land.Add(i);
        var blocks = Comps(land);
        var parcels = new List<Parcel>(); var lefts = new List<Left>();
        int bi = 0; foreach (var b in blocks) Split(b, bi++, parcels, lefts);
        int nSplit = parcels.Count; Merge(parcels);
        cellsOf = parcels;
        foreach (var p in parcels) foreach (var c in p.cells) { int i = c % gw, j = c / gw; foreach (var q in new[] { c - 1, c + 1, c - gw, c + gw }) if (q >= 0 && q < owner.Length && owner[q] >= 0 && owner[q] != p.id && Mathf.Abs(q % gw - i) <= 1) p.nb.Add(owner[q]); }
        var blk = new bool[type.Length]; for (int k = 0; k < blk.Length; k++) blk[k] = type[k] != T.Land || foot[k]; DT(blk, out dBlk, out _);
        double tSplit = sw.Elapsed.TotalSeconds;
        // 3. 덩어리(앉히기 A/B/C 포함)
        var vis = Vistas(); var props = Props();
        var masses = new List<Mass>(); Masses(parcels, masses, vis, props);
        double tMass = sw.Elapsed.TotalSeconds;
        // 4. 지형 패드 + 축대 → 새 땅으로 바닥·밑면 다시 읽기
        var mats = Materials();
        var tnote = Pads(terr, baseMesh, masses, root.transform, out var walls);
        Physics.SyncTransforms(); Reseat(masses);
        double tPad = sw.Elapsed.TotalSeconds;
        // 5. 씬에 놓기 + 빈 필지 나무
        var mroot = new GameObject("Masses").transform; mroot.SetParent(root.transform, false);
        for (int i = 0; i < masses.Count; i++) { masses[i].idx = i; masses[i].go = Spawn(masses[i], mroot, mats); }
        var gnote2 = Greens(parcels, root.transform);
        // 6. 검사 + 기록
        var chk = Check(parcels, masses, props);
        chk.terrain = tnote; chk.walls = walls; chk.time = $"나누기 {tSplit:F1}s · 덩어리 {tMass - tSplit:F1}s · 지형 {tPad - tMass:F1}s · 놓기·검사 {sw.Elapsed.TotalSeconds - tPad:F1}s";
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
        foreach (Transform ch in top.Find("RoadsV2"))
        {
            if (!ch.gameObject.activeInHierarchy) continue;
            float o = .5f; bool j = ch.name.StartsWith("ROAD_V2j");
            var rs = ch.GetComponent<RoadSurface>(); if (rs != null) o = SecOff(rs.kind, rs.width, false); else if (j) o = SecOff("village", 5, false);
            RasterMesh(ch, road, 0, off, o); if (j) RasterMesh(ch, junc, 0, null, 0);
        }
        var tb = top.Find("TerraceBlock");
        if (tb != null)
        {
            var dr = tb.Find("Driveway"); if (dr != null) RasterMesh(dr, road, 0, off, .5f);
            var wk = tb.Find("Walkways"); if (wk != null) RasterMesh(wk, walkw, 0, off, .3f);
            var rw = tb.Find("RetainingWalls"); if (rw != null) RasterMesh(rw, wall, .2f, null, 0);
            var fp = tb.Find("FieldPatch"); if (fp != null) foreach (var mf in fp.GetComponentsInChildren<MeshFilter>()) RasterMesh(mf.transform, foot, 0, null, 0);
            var dp = tb.Find("DirtPath_door_garden"); if (dp != null) RasterMesh(dp, foot, 0, null, 0);
        }
        var d198 = top.Find("D198_Buildings"); if (d198 != null) foreach (var mf in d198.GetComponentsInChildren<MeshFilter>()) RasterMesh(mf.transform, foot, 0, null, 0);
        // 길·보도에서 거리(벡터 전파) — 가장 가까운 길 칸과 그 길의 앞띠 폭
        var seed = new bool[N]; for (int k = 0; k < N; k++) seed[k] = road[k] || walkw[k];
        DT(seed, out dist, out src); soff = off;
        for (int k = 0; k < N; k++)
        {
            if (type[k] == T.Out) continue;
            if (road[k]) type[k] = T.Road; else if (wall[k]) type[k] = T.Wall; else if (walkw[k] || (src[k] >= 0 && dist[k] <= off[src[k]] + C * .5f)) type[k] = T.Strip;
        }
        // 비움: 도현 건물 +3 m · 갈림목 +4 m · 걷는 길 입구 3 m · 집 마당 단
        DT(foot, out var dF, out _); DT(junc, out var dJ, out _);
        var ms = new bool[N]; foreach (var m in Mouths()) { int mk = Idx(m); if (mk >= 0) ms[mk] = true; }
        DT(ms, out var dM, out _); int nEx = 0;
        for (int k = 0; k < N; k++)
        {
            if (type[k] != T.Land) continue;
            if (dF[k] <= FootPad || dJ[k] <= JuncPad || dM[k] < MouthPad) { type[k] = T.Excl; nEx++; }
        }
        string yard = Yard(d198);
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
        var st = new Stack<(List<int> c, int lv)>(); st.Push((block, 0));
        while (st.Count > 0)
        {
            var (cells, lv) = st.Pop(); float area = cells.Count * C * C;
            if (area < MinArea) { lefts.Add(Lf("빈터", cells)); continue; }
            var F = cells.Where(c => front[c]).ToList();
            if (F.Count == 0) { lefts.Add(Lf(area >= 60 ? "밭·빈터" : "빈터", cells)); continue; }
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
                if (kids.Count >= 2 && kids.All(k => k.Count < cells.Count)) { for (int i = kids.Count - 1; i >= 0; i--) st.Push((kids[i], lv + 1)); continue; }
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
                    var keep = cells.Where(c => dist[c] - soff[src[c]] <= MaxDepth).ToList(); var back = cells.Where(c => dist[c] - soff[src[c]] > MaxDepth).ToList();
                    var kc = Comps(keep); var main = kc.OrderByDescending(k => k.Count(c => front[c])).ThenBy(k => k[0]).FirstOrDefault();
                    foreach (var k in kc) if (k != main) back.AddRange(k);
                    if (back.Count > 0) foreach (var k in Comps(back.OrderBy(x => x).ToList())) lefts.Add(Lf(k.Count * C * C >= 60 ? "뒤 마당·밭" : "뒤 빈터", k));
                    if (main == null || Lfr < MinFront || main.Count * C * C < MinArea) { if (main != null) lefts.Add(Lf("좁은 틈", main)); continue; }
                    var p = new Parcel { id = outP.Count, cells = main, toRoad = toRoad, t = t, frontW = Lfr, area = main.Count * C * C };
                    p.depth = main.Max(c => dist[c] - soff[src[c]]); p.ctr = Mean(main); p.road = RoadName(F[F.Count / 2]);
                    outP.Add(p); continue;
                }
            }
            if (kids == null || kids.Count == 0 || kids.Any(k => k.Count == cells.Count)) { lefts.Add(Lf("분할 실패", cells)); continue; }
            for (int i = kids.Count - 1; i >= 0; i--) st.Push((kids[i], lv + 1));
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
            q2.depth = q2.cells.Max(c => src[c] >= 0 ? dist[c] - soff[src[c]] : 0); q2.ctr = Mean(q2.cells);
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
    static Left Lf(string k, List<int> c) => new Left { kind = k, area = c.Count * C * C, ctr = Mean(c) };
    static string RoadName(int c) { return src[c] >= 0 ? (soff[src[c]] > 2.6f ? "main" : soff[src[c]] > 2f ? "village" : soff[src[c]] > .4f ? "lane·walk" : "보도") : "?"; }

    // ── 덩어리 ──────────────────────────────────────────────────
    class Vis { public List<(Vector2 p, float eye, float a0, float a1)> ov = new List<(Vector2, float, float, float)>(); public List<(Vector2 a, Vector2 b, float eye)> los = new List<(Vector2, Vector2, float)>(); }
    static void Masses(List<Parcel> parcels, List<Mass> masses, Vis vis, List<(Vector2 c, Vector2 h)> props)
    {
        // 붙은 집 묶음: 앞 방향이 비슷한(25° 안) 이웃 필지를 +t 쪽으로 2~3채, 35%
        int gid = 0;
        foreach (var p in parcels)
        {
            if (p.group >= 0) continue; var rng = new Rng(Fnv($"{Seed}|grp|{p.id}"));
            if (rng.F() >= RowShare) continue; int want = rng.F() < .6f ? 2 : 3; var mem = new List<Parcel> { p }; var cur = p;
            while (mem.Count < want)
            {
                var nx = cur.nb.Select(i => parcels[i]).Where(q => q.group < 0 && !mem.Contains(q) && Vector2.Angle(q.toRoad, p.toRoad) < 25 && Vector2.Dot(q.ctr - cur.ctr, p.t) > 2f).OrderBy(q => Vector2.Dot(q.ctr - cur.ctr, p.t)).FirstOrDefault();
                if (nx == null) break; mem.Add(nx); cur = nx;
            }
            if (mem.Count < 2) continue; foreach (var q in mem) q.group = gid; gid++;
        }
        var groups = parcels.GroupBy(p => p.group < 0 ? -1 - p.id : p.group).OrderBy(g => g.Min(p => p.id)).Select(g => g.OrderBy(p => Vector2.Dot(p.ctr, g.First().t)).ToList()).ToList();
        // 붙은 집 묶음이 안 들어가면 묶음을 풀어 한 채씩 다시 넣는다(대기열 뒤에)
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var mem = groups[gi]; bool att = mem.Count > 1;
            var f0 = Vector2.zero; foreach (var p in mem) f0 += p.toRoad * p.frontW; f0.Normalize();
            var set = new HashSet<int>(mem.Select(p => p.id));
            var rng = new Rng(Fnv($"{Seed}|mass|{mem[0].id}"));
            var Fc = mem.SelectMany(p => p.cells).Where(c => front[c]).OrderBy(c => Vector2.Dot(P(c), new Vector2(f0.y, -f0.x))).ToList();
            float b = att ? 1.2f + rng.F() * .8f : 1f + rng.F() * 2f;
            int fl0 = Floors(rng); float D = Depth(fl0, rng);
            bool ok = false; float x0 = 0, x1 = 0;
            // 안 들어가면: 깊이 줄임 → 물러남 0.8 m → 앞선을 뒤로(밀어 넣기). 그래도 안 되면 빈 필지.
            var tries = new[] { (0f, 0f, 0f), (-1.5f, 0f, 0f), (-1.5f, -9f, 0f), (-3f, -9f, 0f), (-3f, -9f, -1.5f), (-4f, -9f, -3f), (-9f, -9f, -1f) };
            float gL = .4f + rng.F() * .6f, gR = .4f + rng.F() * .6f;
            float bb = b, DD = D, zz = 0, bestW = 0; Vector2 f = f0, t = new Vector2(f0.y, -f0.x);
            // 방향 후보: 필지 평균 → 앞선 가운데 칸에서 본 길 쪽 → ±12° · ±25°(굽은 길·모퉁이 쐐기 필지)
            var mc = Fc[Fc.Count / 2]; var fm = (P(src[mc]) - P(mc)).sqrMagnitude > 1e-6f ? (P(src[mc]) - P(mc)).normalized : f0;
            var dirs = new List<Vector2> { f0, fm }; foreach (var a in new[] { 12f, -12f, 25f, -25f }) dirs.Add(Rot(f0, a));
            // 둘째 판(한 채만): 옆 틈 0.3 m · 폭 3.2 m 까지 — 얕고 좁은 필지의 작은 집
            for (int pass = 0; pass < (att ? 1 : 2) && !ok; pass++)
            {
                if (pass == 1) { gL = gR = .3f; }
                float need = pass == 0 ? 3.6f * mem.Count : 3.2f;
                foreach (var fd in dirs)
                {
                    f = fd; t = new Vector2(f.y, -f.x);
                    var F = Fc.Select(c => Vector2.Dot(P(c), f)).OrderBy(z => z).ToList(); float zF = F[F.Count / 2];
                    foreach (var (dD, db, dz) in tries)
                    {
                        DD = Mathf.Max(dD < -5 ? (pass == 1 ? 3.6f : 4f) : 4.5f, D + dD); bb = db < -5 ? (pass == 1 ? .5f : .8f) : b; zz = zF + dz;
                        if (Fit(set, f, t, zz - bb - DD, zz - bb, out x0, out x1)) { bestW = Mathf.Max(bestW, x1 - x0); if (x1 - x0 - gL - gR >= need) { ok = true; break; } }
                    }
                    if (ok) break;
                }
            }
            if (!ok)
            {
                if (att) { foreach (var p in mem) { p.group = -1; groups.Add(new List<Parcel> { p }); } continue; }
                foreach (var p in mem) { p.empty = "덩어리 안 들어감(폭·모양)"; p.fitW = bestW; }
                continue;
            }
            x0 += gL; x1 -= gR; float total = x1 - x0;
            // 너무 넓으면(한 채 10 m 넘음) 가운데로 줄인다
            float cap = 10f * mem.Count; if (total > cap) { float mid = (x0 + x1) * .5f; x0 = mid - cap * .5f; x1 = mid + cap * .5f; total = cap; }
            float sumW = mem.Sum(p => p.frontW); float xa = x0;
            for (int k = 0; k < mem.Count; k++)
            {
                var p = mem[k]; float w = k == mem.Count - 1 ? x1 - xa : total * p.frontW / sumW; float xb = xa + w;
                var r2 = new Rng(Fnv($"{Seed}|m|{p.id}"));
                var m = new Mass { parcel = p.id, group = att ? p.group : -1, member = k, f = f, b = bb, d = DD, w = w, attL = att && k > 0, attR = att && k < mem.Count - 1 };
                m.floors = k == 0 ? fl0 : Floors(r2); if (m.floors == 1 && DD > 8) m.floors = 2;
                m.floorH = 2.75f + r2.F() * .35f;
                float u = r2.F(); m.roof = u < .55f ? "gable" : u < .85f ? "hip" : "flat";
                m.ridgeX = att || r2.F() < .7f; m.pitch = 24 + r2.F() * 8;
                // 앞면 가운데(피벗) = x 가운데, z = 앞선 - 물러남
                float xc = (xa + xb) * .5f, zc = zz - bb; m.c = t * xc + f * zc;
                xa = xb;
                // 2판: 비탈 앉히기 — A 낮은 기단 · B 바닥 = 최저 + 0.5(오르막 쪽 묻힘, 패드+축대) · C 둘로 나눈 단차 집 · 35% 넘으면 비움
                string why = SeatAll(m, att, out var m2); if (why != null) { p.empty = why; continue; }
                Heights(m, p, parcels); if (m2 != null) Heights(m2, p, parcels);
                why = Blocked(m, vis, props) ?? (m2 != null ? Blocked(m2, vis, props) : null); if (why != null) { p.empty = why; continue; }
                // 옆 필지 덩어리와 처마·기단이 겹치면: 박공 방향을 길 따라로(옆 처마 없음) → 그래도면 뺀다
                bool Hit(Mass x) => x != null && masses.Any(o => (o.c - x.c).sqrMagnitude < 900 && !(o.group >= 0 && o.group == x.group) && PolyPoly(Corners(o, 0), Corners(x, 0)));
                if ((Hit(m) || Hit(m2)) && m.roof == "gable" && !m.ridgeX && m2 == null) { m.ridgeX = true; Heights(m, p, parcels); }
                if (Hit(m) || Hit(m2)) { p.empty = "옆집과 겹침"; continue; }
                p.mass = m; masses.Add(m); if (m2 != null) { p.mass2 = m2; masses.Add(m2); }
            }
        }
        // 묶음에서 빠진 이웃이 생기면 붙은 쪽 표시를 끈다(뒤에 틈이 생긴 쪽은 사실상 따로 집)
        foreach (var m in masses) if (m.group >= 0)
        {
            m.attL = m.attL && masses.Any(o => o.group == m.group && o.member == m.member - 1);
            m.attR = m.attR && masses.Any(o => o.group == m.group && o.member == m.member + 1);
        }
        foreach (var p in parcels) Types(p);
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
        if (m.slope > MaxSlope) return "경사 35% 넘음(나무 자리)";
        float D = mx - mn;
        if (D <= LowD) { m.seat = "A"; m.y0 = mx + .12f; }
        else { m.y0 = Mathf.Min(mn + PadLift, mx + .12f); m.seat = mx - m.y0 <= BuryMax ? "B" : "C"; }
        m.bury = Mathf.Max(0, mx - m.y0); m.expo = m.y0 - mn; m.plinth = m.expo > .3f; m.bot = mn - Bury;
        return null;
    }
    // 앉히기 전체: 등고선 방향 맞춤(앞뒤 경사면 얕게) → C 면 경사 쪽으로 둘로 나눠 계단식 2단(각 단 B 규칙, 아래 단 내리막 쪽 면에 차고·창고 문)
    static string SeatAll(Mass m, bool att, out Mass m2)
    {
        m2 = null; var why = Seat(m); if (why != null) return why;
        // 긴 변 = 등고선: 경사가 주로 앞뒤인데 깊이가 폭보다 길면 깊이를 폭까지 줄인다(앞면은 그대로 길 쪽)
        if (!att && m.seat != "A" && Mathf.Abs(m.gz) > Mathf.Abs(m.gx) * 1.2f && m.d > m.w + .5f) { m.d = Mathf.Max(4.5f, m.w); m.splitT = true; why = Seat(m); if (why != null) return why; }
        if (m.seat != "C") return null;
        // 묶음 집·좁은 집: 깊이를 줄여(4 m 까지) 한 번 더 — 그래도 C 면 비움
        bool alongF = Mathf.Abs(m.gz) >= Mathf.Abs(m.gx); float L = alongF ? m.d : m.w;
        if (att || L < 7f)
        {
            if (m.d > 4.2f) { m.d = Mathf.Max(4f, m.d * .6f); if (m.floors == 3) m.floors = 2; why = Seat(m); if (why != null) return why; if (m.seat != "C") return null; }
            return "가파름(단차 못 넣음)";
        }
        var t = new Vector2(m.f.y, -m.f.x); var a = m.Clone(); var b = m.Clone();
        if (alongF) { a.d = b.d = m.d * .5f; b.c = m.c - m.f * (m.d * .5f); }
        else { a.w = b.w = m.w * .5f; a.c = m.c - t * (m.w * .25f); b.c = m.c + t * (m.w * .25f); a.attR = true; b.attL = true; }
        if (Seat(a) != null || Seat(b) != null || a.seat == "C" || b.seat == "C") return "가파름(단차 못 넣음)";
        var lo = a.y0 <= b.y0 ? a : b; lo.door = true; lo.doorBack = alongF && lo == b; lo.tier = 0; (lo == a ? b : a).tier = 1;
        a.seat = b.seat = "C"; a.pair = b; b.pair = a; a.ridgeX = b.ridgeX = alongF || m.ridgeX;
        // a 를 m 자리에 복사(부르는 쪽이 m 을 그대로 쓴다)
        foreach (var fi in typeof(Mass).GetFields()) fi.SetValue(m, fi.GetValue(a));
        b.pair = m; m2 = b; return null;
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
        for (int i = 0; i < n; i++) { var rn = buf[i].collider.transform.root.name; if (rn == RootName || rn == "Roadside" || rn == "OnFoot_HomeGarage") continue; if (float.IsNaN(best) || buf[i].point.y > best) best = buf[i].point.y; }
        return best;
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
            if (it % 2 == 0) m.floorH = Mathf.Clamp(m.floorH + (m.y0 + m.H >= Top(clash) ? .45f : -.45f), 2.6f, 3.4f);
            else m.floors = Mathf.Clamp(m.floors + (m.floors <= 2 ? 1 : -1), 1, 3);
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
                if (inArc && top > o.eye - dd * Mathf.Tan(5 * Mathf.Deg2Rad)) return "전망대 앞(vistas overlook)";
            }
        var big = Corners(m, .5f);
        foreach (var l in v.los) if (top > l.eye && SegPoly(l.a, l.b, big)) return "전망 회랑(vistas corridor)";
        var me = Corners(m, .1f);
        foreach (var pr in props) if ((pr.c - m.c).sqrMagnitude < 400 && PolyPoly(me, Box(pr.c, pr.h))) return "길가 소품과 겹침";
        return null;
    }
    static void Types(Parcel p)
    {
        var m = p.mass; if (m == null) { p.types.Add(p.depth > 12 ? "마당·밭" : "빈터"); return; }
        if (m.plinth && m.gMax - m.gMin > 1.2f) p.types.Add("terrace_house");
        if (m.group >= 0) p.types.Add("town_house");
        if (p.road == "main" && m.floors >= 2) p.types.Add("shop_house");
        if (m.group < 0 && p.frontW >= 8 && p.depth >= 12 && m.floors <= 2) p.types.Add("detached_house");
        if (m.floors == 1 && p.depth >= 14) p.types.Add("farmhouse");
        if (p.types.Count == 0) p.types.Add("town_house");
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
        var res = new[] { Get("Parcels_Body", new Color(.74f, .74f, .72f)), Get("Parcels_Roof", new Color(.46f, .46f, .48f)), Get("Parcels_Plinth", new Color(.55f, .51f, .45f)), Get("Parcels_Door", new Color(.24f, .23f, .22f)) };
        AssetDatabase.SaveAssets(); return res;
    }
    static Transform Spawn(Mass m, Transform root, Material[] mats)
    {
        var go = new GameObject($"P{m.parcel:000}_{m.floors}F_{m.roof}_{m.seat}" + (m.tier >= 0 ? $"_tier{m.tier}" : "") + (m.group >= 0 ? $"_row{m.group}" : ""));
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(new Vector3(m.c.x, m.y0, m.c.y), Quaternion.LookRotation(new Vector3(m.f.x, 0, m.f.y)));
        var v = new List<Vector3>(); var tb = new List<int>(); var tr = new List<int>(); var tp = new List<int>(); var td = new List<int>();
        float hw = m.w * .5f, D = m.d, H = m.H, yb = m.bot - m.y0;
        // 단차 집 아래 단: 내리막 쪽 면에 차고·창고 문(어두운 판, 땅까지)
        if (m.door)
        {
            float dw = Mathf.Min(2.6f, m.w * .6f), zd = m.doorBack ? -D - .17f : .17f; var t2 = new Vector2(m.f.y, -m.f.x);
            float gy = TopY(m.c - m.f * (m.doorBack ? D + .6f : -.6f)); float db = float.IsNaN(gy) ? 0 : Mathf.Max(yb, gy - m.y0);
            var dc = new Vector3(0, H * .5f, -D * .5f);
            Quad(v, td, new Vector3(-dw * .5f, db, zd), new Vector3(dw * .5f, db, zd), new Vector3(dw * .5f, 2.3f, zd), new Vector3(-dw * .5f, 2.3f, zd), dc);
        }
        var ctr = new Vector3(0, H * .5f, -D * .5f);
        if (m.plinth) { Box(v, tp, m.attL ? -hw : -hw - .15f, m.attR ? hw : hw + .15f, -D - .15f, .15f, yb, 0, ctr); Box(v, tb, -hw, hw, -D, 0, 0, H, ctr); }
        else Box(v, tb, -hw, hw, -D, 0, yb, H, ctr);
        float R = m.rise, o = .25f;
        if (m.roof == "flat") Box(v, tr, -hw, hw, -D, 0, H, H + .3f, ctr);
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
        var mesh = new Mesh { name = go.name }; mesh.SetVertices(v); mesh.SetUVs(0, v.Select(q => new Vector2((q.x + q.z) * .25f, q.y * .25f)).ToList()); mesh.subMeshCount = 4; mesh.SetTriangles(tb, 0); mesh.SetTriangles(tr, 1); mesh.SetTriangles(tp, 2); mesh.SetTriangles(td, 3); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        return go.transform;
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
        k.summary = $"앉히기 {seat} · 노출 기단 최대 {k.expoMax:F2} m · 패드 축대 최고 {padMaxWall:F2} m · 지형 구멍·뒤집힘 {terrHoles} · 해시 {k.hash} · 덩어리끼리 겹침 {k.overlap} · 길 {k.roadHit} · 앞띠 {k.stripHit} · 축대 {k.wallHit} · 도현 건물 {k.footHit} · 소품 {k.propHit} · 뜸 {k.floatN}(최대 {k.worstFloat * 100:F1} cm, 음수 = 묻힘)";
        return k;
    }

    // 필지별 처리: A 낮은 기단 · B 패드+축대 · C 단차 · 비움(경사 35%) · 비움(그 밖)
    static string SeatCounts(List<Parcel> ps)
    {
        int a = ps.Count(p => p.mass != null && p.mass.seat == "A"), b = ps.Count(p => p.mass != null && p.mass.seat == "B"), c = ps.Count(p => p.mass != null && p.mass.seat == "C");
        int st = ps.Count(p => p.mass == null && p.empty != null && (p.empty.StartsWith("경사") || p.empty.StartsWith("가파름"))), o = ps.Count(p => p.mass == null) - st;
        return $"A 낮은 기단 {a} · B 패드+축대 {b} · C 단차 {c} · 비움(가파름) {st} · 비움(그 밖) {o} · 등고선 맞춤 {ps.Count(p => p.mass != null && p.mass.splitT)}";
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
    static void DT(bool[] seed, out float[] d, out int[] s)
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
    static void RasterMesh(Transform tr, bool[] G, float grow, float[] off, float o)
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
                if (InTri(p, a, b, c) || DistSeg(p, a, b) <= mg - C * .21f || DistSeg(p, b, c) <= mg - C * .21f || DistSeg(p, c, a) <= mg - C * .21f) { G[kk] = true; if (off != null) off[kk] = Mathf.Max(off[kk], o); }
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
