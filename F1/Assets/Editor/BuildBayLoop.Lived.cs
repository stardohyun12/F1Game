using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// v5 (D-141 사람이 사는 길) — 빌더 본체(BuildBayLoop.cs)에 붙는 규칙들. 모든 수치는 가설(보고서 programmer-20261025-2202).
///  1) 지면: 밀집·보통 구역 필지 둘레·길가 = 포장(돌·콘크리트·자갈·짙은 돌 섞임, 6m 칸마다), 두 차길 사이 섬 = 포장. 잔디는 밭·풀밭·벼랑·성긴 구역만.
///  2) 건물 후퇴·높이·방향 들쭉날쭉 + 30~60m 마다 골목 틈(Buildings 안에서 RowSt 로).
///  3) 노면: 땜질·맨홀·배수구·기름 얼룩, 갈림목 정지선·횡단보도, 골목 입구 반사경.
///  4) 생활 소품: 틈 안 주차·빨래줄·화분, 가게 앞 차양+진열.
///  5) 성긴 구역 집 = 담장+대문+진입로+텃밭+나무, 이웃 집끼리 오솔길.
/// </summary>
public static partial class BuildBayLoop
{
    // ------------------------------------------------------------------ 구역 조회(필지 사각형)
    struct PRect { public Vector2 c, ax, az; public float hw, hd; public string dens, dist; }
    static List<PRect> pRects;
    static Dictionary<(int, int), List<int>> pGrid;
    const float PG = 16f;

    static void InitParcelRects()
    {
        pRects = new List<PRect>(); pGrid = new Dictionary<(int, int), List<int>>();
        var dists = ((List<object>)J["districts"]).Cast<Dictionary<string, object>>().ToDictionary(dd => (string)dd["id"]);
        foreach (Dictionary<string, object> p in (List<object>)J["parcels"])
        {
            float x = F(p["x"]), z = F(p["z"]), w = F(p["w"]), d = F(p["d"]);
            var fm = (List<object>)p["frontMid"]; var dir = new Vector2(F(fm[0]) - x, F(fm[1]) - z).normalized;
            string di = p.TryGetValue("district", out var o) ? o as string : null;
            string dens = di != null && dists.TryGetValue(di, out var dd) ? (string)dd["density"] : "medium";
            var r = new PRect { c = new Vector2(x, z), az = dir, ax = new Vector2(dir.y, -dir.x), hw = w * .5f, hd = d * .5f, dens = dens, dist = di };
            int id = pRects.Count; pRects.Add(r);
            float ext = Mathf.Max(w, d) * .5f + 8f;
            for (int gx = Mathf.FloorToInt((x - ext) / PG); gx <= Mathf.FloorToInt((x + ext) / PG); gx++)
                for (int gz = Mathf.FloorToInt((z - ext) / PG); gz <= Mathf.FloorToInt((z + ext) / PG); gz++)
                {
                    if (!pGrid.TryGetValue((gx, gz), out var l)) pGrid[(gx, gz)] = l = new List<int>();
                    l.Add(id);
                }
        }
    }

    /// <summary>(x,z) 둘레에서 가장 "짙은" 구역(밀집 2 &gt; 보통 1 &gt; 성김 0) 중 사각형까지 거리 ≤ margin 인 것. 없으면 -1.</summary>
    static int DensAt(float x, float z, float margin)
    {
        if (!pGrid.TryGetValue((Mathf.FloorToInt(x / PG), Mathf.FloorToInt(z / PG)), out var l)) return -1;
        int best = -1; var q = new Vector2(x, z);
        foreach (int id in l)
        {
            var r = pRects[id]; var v = q - r.c;
            float u = Mathf.Abs(Vector2.Dot(v, r.ax)) - r.hw, w = Mathf.Abs(Vector2.Dot(v, r.az)) - r.hd;
            float dd = new Vector2(Mathf.Max(0, u), Mathf.Max(0, w)).magnitude;
            if (dd > margin) continue;
            int k = r.dens == "dense" ? 2 : r.dens == "medium" ? 1 : 0; if (k > best) best = k;
        }
        return best;
    }

    // 차길 점 격자(도로 근접·교통섬 판정)
    static Dictionary<(int, int), List<(Vector3 p, float reach, int road)>> cGrid;
    static void InitCarGrid()
    {
        cGrid = new Dictionary<(int, int), List<(Vector3, float, int)>>();
        var car = roads.Where(q => q.Car && !q.overWater).ToList();
        for (int ri = 0; ri < car.Count; ri++)
        {
            var r = car[ri];
            for (int i = 0; i < r.pts.Count; i++)
            {
                var p = r.pts[i]; float reach = r.LaneHalf(i) + Mathf.Max(r.Walk(i, 1), r.Walk(i, -1));
                var k = (Mathf.FloorToInt(p.x / PG), Mathf.FloorToInt(p.z / PG));
                if (!cGrid.TryGetValue(k, out var l)) cGrid[k] = l = new List<(Vector3, float, int)>();
                l.Add((p, reach, ri));
            }
        }
    }
    /// <summary>이 점에서 길 바깥선(차로+인도)까지 거리 ≤ m 인 서로 다른 차길 수, 그중 최소 거리.</summary>
    static int RoadsNear(float x, float z, float m, out float minD)
    {
        minD = float.MaxValue; var hit = new HashSet<int>();
        int gx = Mathf.FloorToInt(x / PG), gz = Mathf.FloorToInt(z / PG);
        for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
            {
                if (!cGrid.TryGetValue((gx + a, gz + b), out var l)) continue;
                foreach (var (p, reach, ri) in l)
                {
                    float d = new Vector2(p.x - x, p.z - z).magnitude - reach;
                    if (d < minD) minD = d;
                    if (d <= m) hit.Add(ri);
                }
            }
        return hit.Count;
    }

    // ------------------------------------------------------------------ 1) 지면 포장
    static readonly string[] LotMats = { "LotStone", "LotConcrete", "LotGravel", "LotDark" };
    static int pavedCells, islandCells;

    /// <summary>지형 칸 하나의 겉: -1 = 풀(Ground), 0..3 = 포장 변형.</summary>
    static int GroundKind(float cx, float cz, float hMin, float hMax, List<(Dictionary<string, object> g, List<Vector2> poly)> green)
    {
        if (hMax - hMin > 1.5f || hMin < 0.6f) return -1;                    // 벼랑·물가는 풀/흙
        foreach (var (g, poly) in green) if (InP(poly, cx, cz)) return -1;    // 밭·풀밭은 풀
        bool paved = false;
        int dz = DensAt(cx, cz, 3.5f);
        if (dz >= 1) paved = true;                                            // 밀집·보통 필지 둘레 3.5m
        int nRoads = RoadsNear(cx, cz, 6f, out float rd);
        if (!paved && nRoads >= 2 && rd <= 5f) { paved = true; islandCells++; }   // 두 차길 사이 섬 → 포장 삼각지
        if (!paved && rd <= 4f && DensAt(cx, cz, 14f) >= 1) paved = true;    // 밀집·보통 구역 길가
        if (!paved) return -1;
        pavedCells++;
        // 6m 덩어리마다 재질 한 가지(돌 45 · 콘크리트 30 · 자갈 15 · 짙은 돌 10)
        uint hsh = (uint)(Mathf.FloorToInt(cx / 6f) * 73856093) ^ (uint)(Mathf.FloorToInt(cz / 6f) * 19349663);
        hsh ^= hsh >> 13; hsh *= 0x5bd1e995; hsh ^= hsh >> 15;
        int u = (int)(hsh % 100);
        return u < 45 ? 0 : u < 75 ? 1 : u < 90 ? 2 : 3;
    }

    /// <summary>
    /// 과제 1943: GroundKind 의 연속판 — 이 점에서 "포장 구역" 경계까지 부호 거리(m, 음수 = 포장 안).
    /// 격자점에서 재고 셰이더가 쌍선형으로 읽으면 경계가 2m 칸 계단 대신 매끈한 선이 된다.
    /// 규칙은 GroundKind 와 같다: 밀집·보통 필지 둘레 3.5m / 두 차길 사이 섬(길 5m 안) / 밀집·보통 구역(14m) 길가 4m.
    /// </summary>
    static float PaveDist(float x, float z, float hMin, float hMax, List<(Dictionary<string, object> g, List<Vector2> poly)> green)
    {
        if (hMax - hMin > 3f || hMin < 0.6f) return 4f;                       // 벼랑·물가(3×3 격자점 = 4m 폭이라 문턱을 칸 기준 1.5 의 두 배로)
        foreach (var (g, poly) in green) if (InP(poly, x, z)) return 4f;
        float best = 8f;
        if (pGrid.TryGetValue((Mathf.FloorToInt(x / PG), Mathf.FloorToInt(z / PG)), out var l))
        {
            var q = new Vector2(x, z);
            foreach (int id in l)
            {
                var r = pRects[id]; if (r.dens != "dense" && r.dens != "medium") continue;
                var v = q - r.c;
                float u = Mathf.Abs(Vector2.Dot(v, r.ax)) - r.hw, w = Mathf.Abs(Vector2.Dot(v, r.az)) - r.hd;
                float dd = new Vector2(Mathf.Max(0, u), Mathf.Max(0, w)).magnitude + Mathf.Min(0, Mathf.Max(u, w));
                best = Mathf.Min(best, dd - 3.5f);
            }
        }
        int nRoads = RoadsNear(x, z, 6f, out float rd);
        if (nRoads >= 2 && rd - 5f < best) { if (rd <= 5f && best >= 0) islandCells++; best = rd - 5f; }
        if (rd - 4f < best && DensAt(x, z, 14f) >= 1) best = rd - 4f;
        return best;
    }

    // ------------------------------------------------------------------ 2) 건물 줄 상태(Buildings 에서 씀)
    class RowSt { public float prevEnd = -1e9f, lastGap = float.NaN, next; public List<float> sbs = new List<float>(); public float lastH = -1f; public Vector3 prevEdge; public float prevW; }
    struct Gap { public Vector3 edge, dir; public float w, d; public int dens; public string road; }
    static readonly List<Gap> gaps = new List<Gap>();
    struct Yard { public Vector3 face, fdir; public Quaternion rot; public Vector3 c; public float w, d, fo; public string type; public bool ownWall; }
    static readonly List<Yard> sparseYards = new List<Yard>();
    static readonly Dictionary<string, float> pfH = new Dictionary<string, float>();
    static int sameSb3, hPairs, hPairsDiff, yawJit, natGaps, cutGaps;

    static float PfH(string path)
    {
        if (path == null) return 0f;
        if (pfH.TryGetValue(path, out var h)) return h;
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path); h = 0f;
        if (pf != null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(pf); go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); h = Bounds(go).size.y; Object.DestroyImmediate(go); }
        pfH[path] = h; return h;
    }

    /// <summary>후퇴(인도 바깥선 → 앞면): 밀집 0/0.25/0.5, 보통 0~3m. 같은 줄 앞 두 채와 셋이 같으면(밀집 ±0.1, 보통 ±0.4) 다른 값으로.</summary>
    static float PickSetback(RowSt st, int dens, System.Random rnd)
    {
        float tol = dens == 2 ? 0.1f : 0.4f;
        float sb = dens == 2 ? rnd.Next(3) * 0.25f : (float)rnd.NextDouble() * 3f;
        int k = st.sbs.Count;
        if (k >= 2 && Mathf.Abs(st.sbs[k - 1] - sb) < tol && Mathf.Abs(st.sbs[k - 2] - sb) < tol)
            sb = dens == 2 ? (sb + 0.25f) % 0.75f : (sb + 1.5f) % 3f;
        if (k >= 2 && Mathf.Abs(st.sbs[k - 1] - sb) < tol && Mathf.Abs(st.sbs[k - 2] - sb) < tol) sameSb3++;
        st.sbs.Add(sb); return sb;
    }

    // ------------------------------------------------------------------ 3)~5) 생활 층
    static GameObject Decal(Transform parent, string id, Vector3 pos, float yaw, float scale = 1f)
    {
        var pf = PropPf(id); if (pf == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent); go.name = "DECAL_" + id;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }
    static void Flat2(Transform parent, string name, Vector3 c, Vector2 size, Quaternion rot, string mat)
    {
        var b = Box(parent, name, new Vector3(size.x, 0.012f, size.y), c, rot, M(mat), false);
        b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static string LivedIn(Transform parent)
    {
        var rnd = new System.Random(141);
        var gD = new GameObject("RoadWear").transform; gD.SetParent(parent);
        var gM = new GameObject("Markings").transform; gM.SetParent(parent);
        var gL = new GameObject("LifeProps").transform; gL.SetParent(parent);
        var gY = new GameObject("SparseYards").transform; gY.SetParent(parent);
        int scoot = 0, garage = 0, display = 0, cafe = 0, mailb = 0, slotTry = 0, wear = 0, stopL = 0, cross = 0, mirrors = 0, parked = 0, lines = 0, awn = 0, walls = 0, gates = 0, drives = 0, gardens = 0, paths = 0, signs = 0;

        // 3a) 노면 땜질·맨홀·배수구·기름 얼룩 — 밀집·보통 구역 차길, 7~15m 마다 하나
        foreach (var r in roads.Where(q => q.Car && !q.overWater))
        {
            float s = 4f + (float)rnd.NextDouble() * 6f;
            while (s < r.len - 3f)
            {
                At(r, s, out var c, out var f, out var rt);
                int i = Mathf.Clamp(Mathf.RoundToInt(s), 0, r.pts.Count - 1);
                float lh = r.LaneHalf(i);
                int dz = DensAt(c.x, c.z, 16f);
                float step = dz == 2 ? 7f : dz == 1 ? 10f : 26f;
                double u = rnd.NextDouble();
                float lift = r.kind == "main" ? 0f : -0.03f;
                float yaw = Quaternion.LookRotation(Flat(f)).eulerAngles.y;
                if (dz >= 1 || u < 0.25)
                {
                    string id; float lat;
                    if (u < 0.40) { id = rnd.Next(2) == 0 ? "road_patch_01" : "road_patch_02"; lat = ((float)rnd.NextDouble() * 2f - 1f) * (lh - 0.8f); }
                    else if (u < 0.60) { id = "Manhole"; lat = ((float)rnd.NextDouble() - .5f) * 1.6f; }
                    else if (u < 0.80) { id = "Drain"; lat = (rnd.Next(2) == 0 ? -1 : 1) * (lh - 0.25f); }
                    else { id = "OilStain"; lat = ((float)rnd.NextDouble() * 2f - 1f) * (lh - 1f); }
                    var pos = c + rt * lat; pos.y = c.y + lift + 0.004f;
                    bool patch = id.StartsWith("road_patch");
                    float sc = patch ? 0.7f + (float)rnd.NextDouble() * 0.6f : 1f;
                    if (Decal(gD, id, pos, yaw + (patch || id == "OilStain" ? (float)rnd.NextDouble() * 40f - 20f : 0f), sc) != null) wear++;
                    // 땜질 옆 색 다른 긴 포장 띠(가끔, 집 앞 포장 이음 자국)
                    if (patch && dz >= 1 && rnd.NextDouble() < 0.35)
                    {
                        float len = 3f + (float)rnd.NextDouble() * 5f, side = rnd.Next(2) == 0 ? -1 : 1;
                        var q = c + rt * side * (lh - 0.55f); q.y = c.y + lift + 0.006f;
                        Flat2(gD, "PatchStrip", q, new Vector2(0.9f, len), Quaternion.LookRotation(Flat(f)), rnd.Next(2) == 0 ? "PatchDark" : "PatchLight"); wear++;
                    }
                }
                s += step * (0.7f + (float)rnd.NextDouble() * 0.8f);
            }
        }

        // 3b) 갈림목: 아래 위계 차길 입구에 정지선(+ 止まれ 글씨 자리 = 흰 띠 둘), 밀집·보통이면 윗 길에 횡단보도, 반사경
        foreach (var r in roads.Where(q => q.Car && q.kind != "main" && !q.overWater))
            foreach (bool atStart in new[] { true, false })
            {
                if (atStart ? !r.startJoin : !r.endJoin) continue;
                string pid = atStart ? r.startJoinId : r.endJoinId; var par = roads.FirstOrDefault(q => q.id == pid);
                if (par == null || !par.Car) continue;
                float parHalf = par.Half + 2.5f;
                // 윗 길 띠 밖으로 나올 때까지 안쪽으로
                float s0 = atStart ? 0f : r.len, dirS = atStart ? 1f : -1f, sq = s0;
                for (int k = 0; k < 30; k++)
                {
                    At(r, sq, out var cc, out _, out _);
                    if (ParentSurface(par, cc.x, cc.z, out _) > parHalf) break;
                    sq += dirS;
                }
                if (Mathf.Abs(sq - s0) > 28f || sq < 1f || sq > r.len - 1f) continue;
                At(r, sq, out var c, out var f, out var rt);
                int i = Mathf.Clamp(Mathf.RoundToInt(sq), 0, r.pts.Count - 1); float lh = r.LaneHalf(i);
                var inward = Flat(f) * dirS; var yrot = Quaternion.LookRotation(inward);
                // 정지선: 나가는 쪽 차로(진행 방향 왼쪽 — 일본 좌측통행) 반폭
                var outSide = atStart ? rt : -rt;
                var stopC = c + outSide * (lh * .5f); stopC.y = c.y - 0.03f + 0.008f;
                Flat2(gM, "StopLine", stopC, new Vector2(lh - 0.2f, 0.35f), yrot, "PaintWhite"); stopL++;
                int dz = DensAt(c.x, c.z, 16f);
                // 止まれ 표지(빨간 역삼각): 길가 바깥 0.6m
                var sp = c + outSide * (lh + Mathf.Max(0.6f, r.Walk(i, atStart ? -1 : 1) - 0.3f)); sp.y = H(sp.x, sp.z);
                if (!NearCar(sp, 0.2f) && !NearBuilding(sp, 0.1f))
                {
                    if (Prop(gL, "street_sign_0" + (1 + rnd.Next(3)), sp, Quaternion.LookRotation(inward)) != null)
                    signs++;
                }
                // 반사경(커브 미러): 밀집·보통 갈림목 맞은편 길가
                if (dz >= 1)
                {
                    var mp = c - outSide * (lh + 0.7f) - inward * 1.5f; mp.y = H(mp.x, mp.z);
                    if (!NearCar(mp, 0.2f) && !NearBuilding(mp, 0.1f))
                    {
                        if (Prop(gL, "traffic_mirror_01", mp, Quaternion.LookRotation(inward)) != null)
                        mirrors++;
                    }
                    // 횡단보도: 윗 길 위, 갈림목 옆 6m(갈래 입구 앞을 지나는 사람)
                    if (rnd.NextDouble() < 0.6)
                    {
                        float ps = Project(par, c.x, c.z) + (rnd.Next(2) == 0 ? -1 : 1) * (r.Half + 4f);
                        if (par.closed) ps = (ps + par.len) % par.len;
                        if (ps > 2f && ps < par.len - 2f) { CrosswalkAt(gM, par, ps); cross++; }
                    }
                }
            }
        // 밀집 구역 본선: 140m 마다 횡단보도 하나 더(가게 줄 앞)
        {
            var mainR = roads.First(q => q.id == "main"); float last = -999f;
            for (float s = 10f; s < mainR.len - 10f; s += 5f)
            {
                At(mainR, s, out var c, out _, out _);
                if (s - last < 140f || DensAt(c.x, c.z, 10f) < 2) continue;
                CrosswalkAt(gM, mainR, s); cross++; last = s;
            }
        }

        // 4a) 골목 틈: 밀집 = 화분·빨래줄·반사경, 보통 = 차고 진입(주차) 또는 낮은 담+대문
        foreach (var gp in gaps)
        {
            var lat = new Vector3(gp.dir.z, 0, -gp.dir.x); var inRot = Quaternion.LookRotation(-gp.dir);
            if (gp.dens == 2 && gp.w <= 4.5f)   // 화분·빨래줄은 양옆 벽이 있는 좁은 골목에서만 읽힌다(넓은 공터에 세우면 표지판처럼 보였다)
            {
                foreach (float sd in new[] { -1f, 1f })
                {
                    var pp = gp.edge - gp.dir * 0.6f + lat * sd * (gp.w * .5f - 0.5f); pp.y = H(pp.x, pp.z) + 0.02f;
                    if (!NearCar(pp, 0.3f)) Prop(gL, rnd.NextDouble() < 0.5 ? "Pots" : "planter_row", pp, inRot);
                }
                if (gp.w >= 2.6f && rnd.NextDouble() < 0.7)
                {
                    // 빨래줄: 골목을 가로질러(프리팹 길이 2.43m, -Z 로 뻗음)
                    var cl = gp.edge - gp.dir * (2.5f + (float)rnd.NextDouble() * 2f) + lat * (Mathf.Min(gp.w, 2.43f) * .5f); cl.y = H(cl.x, cl.z);
                    var go = Prop(gL, "Clothesline", cl, Quaternion.LookRotation(lat));
                    if (go != null) { foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); lines++; }
                }
            }
            else
            {
                if (rnd.NextDouble() < 0.55 && gp.w >= 2.6f && gp.d >= 4.5f)
                {
                    // 차는 아트 보류(D-141 v5b) — 자리는 비우고 스쿠터만
                    var cp = gp.edge - gp.dir * (0.6f + 1.2f) + lat * ((float)rnd.NextDouble() - .5f) * (gp.w - 1.2f); cp.y = H(cp.x, cp.z);
                    if (Scooter(gL, cp, Quaternion.LookRotation(gp.dir * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;
                }
                else
                {
                    // 낮은 담 두 토막 + 대문 기둥(가운데 2.6m 열림)
                    float half = gp.w * .5f, open = Mathf.Min(2.6f, gp.w - 1f);
                    foreach (float sd in new[] { -1f, 1f })
                    {
                        float segL = half - open * .5f; if (segL < 0.4f) continue;
                        var mid = gp.edge - gp.dir * 0.3f + lat * sd * (open * .5f + segL * .5f);
                        LowWall(gL, mid, lat, segL, 0.9f); walls++;
                        var post = gp.edge - gp.dir * 0.3f + lat * sd * (open * .5f + 0.15f); post.y = H(post.x, post.z);
                        Box(gL, "GatePost", new Vector3(0.3f, 1.3f, 0.3f), post + Vector3.up * 0.55f, Quaternion.LookRotation(gp.dir), M("Stone"), true);
                    }
                    gates++;
                }
            }
        }

        // 4b) 길가 주차: 밀집·보통 구역 인도 2m 이상 길에서 가끔 인도 바깥 앞마당 자리(필지 앞 공간 2.4m 이상)
        foreach (var b in frontSlots)
        {
            if (rnd.NextDouble() > 0.5) continue;
            slotTry++;
            var blat = new Vector3(b.dir.z, 0, -b.dir.x); float gs = rnd.Next(2) == 0 ? -1f : 1f;
            // 차고 입구: 집 앞벽에 붙여(보통 구역 집), 앞 공간은 비워 둔다(차 자리 — 아트 보류)
            if (b.w >= 5.5f && rnd.NextDouble() < 0.6)
            {
                var gpz = b.edge - b.dir * b.space + blat * gs * (b.w * .5f - 1.7f); gpz.y = H(gpz.x, gpz.z);
                var gd = Prop(gL, "garage_door_inset_01", gpz, Quaternion.LookRotation(b.dir));
                if (gd != null) { foreach (var c in gd.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); garage++; }
            }
            // 스쿠터: 차고 반대쪽 앞마당
            var sp2 = b.edge - b.dir * (b.space * .5f) - blat * gs * Mathf.Min(1.4f, b.w * .25f); sp2.y = H(sp2.x, sp2.z);
            if (rnd.NextDouble() < 0.7 && Scooter(gL, sp2, Quaternion.LookRotation(b.dir * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;
            // 우편함: 인도 바깥선 대문 자리
            if (rnd.NextDouble() < 0.4)
            {
                var mp2 = b.edge - b.dir * 0.35f - blat * gs * 0.9f; mp2.y = H(mp2.x, mp2.z);
                if (!NearCar(mp2, 0.2f) && Prop(gL, "mailbox_cluster_01", mp2, Quaternion.LookRotation(b.dir)) != null) mailb++;
            }
        }

        // 4c) 가게 앞 차양 + 진열(상자·화분·벤치)
        foreach (var sh in shopFronts)
        {
            if (rnd.NextDouble() > 0.55) continue;
            var lat = new Vector3(sh.dir.z, 0, -sh.dir.x);
            var a = sh.face + Vector3.up * 2.55f;
            var aw = Prop(gL, "Awning", a, Quaternion.LookRotation(sh.dir));
            if (aw != null) { aw.transform.localScale = new Vector3(Mathf.Min(sh.w * 0.7f, 4.5f) / 1.6f, 1f, 1.6f); awn++; }
            // 가게 앞 진열대(한쪽) + 상자(다른 쪽)
            float ds = rnd.Next(2) == 0 ? -1f : 1f;
            var dp = sh.face + sh.dir * 0.5f + lat * ds * 0.9f; dp.y = H(dp.x, dp.z) + 0.02f;
            if (!NearCar(dp, 0.5f) && Prop(gL, "shop_display_01", dp, Quaternion.LookRotation(sh.dir)) != null) display++;
            var pp = sh.face + sh.dir * 0.55f - lat * ds * 1.0f; pp.y = H(pp.x, pp.z) + 0.02f;
            if (!NearCar(pp, 0.4f)) Prop(gL, rnd.NextDouble() < 0.5 ? "CrateStack" : "Crate", pp, Quaternion.LookRotation(sh.dir));
            // 광장·상점가(밀집): 카페 세트 + 파라솔 — 차양보다 바깥, 귀가 차길에 안 닿을 때만
            if (DensAt(sh.face.x, sh.face.z, 10f) == 2 && rnd.NextDouble() < 0.35)
            {
                var cc = sh.face + sh.dir * 1.9f; cc.y = H(cc.x, cc.z) + 0.02f;
                bool ok = true; foreach (var q in new[] { cc + lat * 1.1f + sh.dir * 0.6f, cc - lat * 1.1f + sh.dir * 0.6f, cc + sh.dir * 1.2f }) if (NearCar(q, 0.2f)) ok = false;
                if (ok && Prop(gL, "cafe_set_01", cc, Quaternion.LookRotation(lat)) != null)
                {
                    cafe++;
                    var pa = Prop(gL, "parasol_01", cc, Quaternion.Euler(0, rnd.Next(360), 0));
                    if (pa != null) foreach (var c in pa.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                }
            }
            // 가게 앞 스쿠터(배달·손님) 25%
            if (rnd.NextDouble() < 0.25)
            {
                var scp = sh.face + sh.dir * 1.3f + lat * ds * (Mathf.Min(sh.w * .5f, 3f)); scp.y = H(scp.x, scp.z);
                if (Scooter(gL, scp, Quaternion.LookRotation(lat * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;
            }
        }

        // 5) 성긴 구역 집 = 담장(앞 대문 3m 열림) + 진입로 + 텃밭 + 나무, 이웃 집끼리 오솔길
        var treePf = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + "CoastalTree.prefab");
        for (int k = 0; k < sparseYards.Count; k++)
        {
            var y = sparseYards[k]; var lat = y.rot * Vector3.right; var fwd = y.rot * Vector3.forward;   // fwd = 길 쪽
            float W = y.w + 3f, D = y.d + 3f;   // 집 둘레 1.5m 마당
            var yc = y.c + fwd * 0.5f;
            if (!y.ownWall)
            {
                // 담장: 뒤·양옆 + 앞은 대문 자리(가운데 3m) 빼고
                foreach (var (a0, a1) in new[] { (new Vector2(-1, -1), new Vector2(1, -1)), (new Vector2(-1, -1), new Vector2(-1, 1)), (new Vector2(1, -1), new Vector2(1, 1)) })
                    { WallLine(gY, yc, lat, fwd, W, D, a0, a1); walls++; }
                float fl = W * .5f - 1.5f;
                if (fl > 0.5f)
                    foreach (float sd in new[] { -1f, 1f })
                    { WallLine(gY, yc, lat, fwd, W, D, new Vector2(sd * 1.5f / (W * .5f), 1), new Vector2(sd, 1)); walls++; }
                foreach (float sd in new[] { -1f, 1f })
                {
                    var post = yc + fwd * (D * .5f) + lat * sd * 1.6f; post.y = H(post.x, post.z);
                    Box(gY, "GatePost", new Vector3(0.35f, 1.4f, 0.35f), post + Vector3.up * 0.6f, y.rot, M("Stone"), true);
                }
                gates++;
            }
            // 진입로: 대문에서 길(인도 바깥선)까지 자갈 띠
            var gate = yc + fwd * (D * .5f);
            float toRoad = 0f;
            for (float t = 0.5f; t < 9f; t += 0.5f) { var q = gate + fwd * t; if (NearCar(q, 0.4f)) break; toRoad = t; }
            var dStart = y.face - fwd * 0.2f;
            Strip(gY, "Driveway", dStart, gate + fwd * toRoad, 2.6f, "LotGravel", 0.05f); drives++;
            // 텃밭(집 옆, 3 줄) + 나무 하나
            float gs = rnd.Next(2) == 0 ? -1f : 1f;
            var gc = y.c + lat * gs * (y.w * .5f + 2.2f) - fwd * (y.d * 0.15f);
            if (!NearCar(gc, 2.5f) && !NearBuilding(gc, 0.3f))
            {
                for (int b = 0; b < 3; b++)
                {
                    var bc = gc + lat * gs * (b - 1) * 0.9f;
                    Strip(gY, "Garden", bc - fwd * 1.6f, bc + fwd * 1.6f, 0.6f, b % 2 == 0 ? "Soil" : "FieldA", 0.08f);
                }
                gardens++;
            }
            var tp = y.c - lat * gs * (y.w * .5f + 1.8f) - fwd * (y.d * .5f + 0.8f);
            if (treePf != null && !NearCar(tp, 2f) && !NearBuilding(tp, 0.5f))
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(treePf, gY); t.name = "YARD_Tree";
                t.transform.SetPositionAndRotation(new Vector3(tp.x, H(tp.x, tp.z) - 0.05f, tp.z), Quaternion.Euler(0, rnd.Next(360), 0)); t.transform.localScale = Vector3.one * (0.8f + (float)rnd.NextDouble() * 0.4f);
            }
            // 오솔길: 다음 성긴 집이 70m 안이면 마당 옆구리끼리 흙길(차길을 건너면 생략)
            if (k + 1 < sparseYards.Count)
            {
                var n = sparseYards[k + 1]; var nl = n.rot * Vector3.right;
                // 옆구리(담 바깥 0.8m)끼리 — 뒤쪽은 담에 막힌다(1차 0개 원인)
                var a = y.c + lat * Mathf.Sign(Vector3.Dot(n.c - y.c, lat)) * (W * .5f + 0.8f);
                var b = n.c + nl * Mathf.Sign(Vector3.Dot(y.c - n.c, nl)) * ((n.w + 3f) * .5f + 0.8f);
                float L = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
                if (L > 6f && L < 70f)
                {
                    bool blocked = false; for (float t = 0; t <= 1f; t += 1f / L) { var q = Vector3.Lerp(a, b, t); if (NearCar(q, 0.6f) || (t > 0.08f && t < 0.92f && NearBuilding(q, 0f))) { blocked = true; break; } }
                    if (!blocked) { Strip(gY, "Footpath", a, b, 1.1f, "PathDirt", 0.05f); paths++; }
                }
            }
        }
        return $"v5 생활 층: 노면 흔적 {wear}, 정지선 {stopL}, 止まれ {signs}, 반사경 {mirrors}, 횡단보도 {cross}, 스쿠터 {scoot}·차고 {garage}·우편함 {mailb}(앞마당 시도 {slotTry}/{frontSlots.Count}·틈 {gaps.Count}, 차 보류), 진열 {display}, 카페 {cafe}, 빨래줄 {lines}, 차양 {awn}, 담 {walls}, 대문 {gates}, 진입로 {drives}, 텃밭 {gardens}, 오솔길 {paths}"
            + $" | 골목 틈 {gaps.Count}(자연 {natGaps}·새로 뺀 필지 {cutGaps}) | 같은 후퇴 3연속 {sameSb3} | 이웃 높이차≥1층 {hPairsDiff}/{hPairs} | 방향 흔듦 {yawJit} | 포장 칸 {pavedCells}(섬 {islandCells})";
    }

    struct Slot { public Vector3 edge, dir; public float space, w; }
    static readonly List<Slot> frontSlots = new List<Slot>();
    struct ShopF { public Vector3 face, dir; public float w; }
    static readonly List<ShopF> shopFronts = new List<ShopF>();

    static void CrosswalkAt(Transform parent, Road r, float s)
    {
        At(r, s, out var c, out var f, out var rt);
        int i = Mathf.Clamp(Mathf.RoundToInt(s), 0, r.pts.Count - 1); float lh = r.LaneHalf(i);
        float lift = r.kind == "main" ? 0f : -0.03f; var rot = Quaternion.LookRotation(Flat(f));
        var cpos = c; cpos.y = c.y + lift + 0.010f;
        var cw = Decal(parent, "crosswalk_paint_01", cpos, rot.eulerAngles.y, 1f);
        if (cw != null) { cw.transform.localScale = new Vector3(lh * 2f / 4.78f, 1f, 1f); return; }
        for (float x = -lh + 0.45f; x <= lh - 0.3f; x += 0.9f)
        {
            var q = c + rt * x; q.y = c.y + lift + 0.008f;
            Flat2(parent, "Zebra", q, new Vector2(0.45f, 2.6f), rot, "PaintWhite");
        }
    }

    /// <summary>스쿠터(아트 2차). 길이 2m·폭 0.8m 덩치가 차길·건물에 닿으면 생략. 차(kenney 폴백)는 쓰지 않는다 — 아트 보류.</summary>
    static bool Scooter(Transform parent, Vector3 pos, Quaternion rot, System.Random rnd)
    {
        var fwd = rot * Vector3.forward; var lat = rot * Vector3.right;
        foreach (var q in new[] { pos + fwd * 1.0f, pos - fwd * 1.0f, pos + lat * 0.4f, pos - lat * 0.4f })
            if (NearCar(q, 0.25f) || NearBuilding(q, 0.05f)) return false;
        return Prop(parent, rnd.Next(2) == 0 ? "scooter_01" : "scooter_02", pos + Vector3.up * 0.01f, rot) != null;
    }

    /// <summary>땅을 따라 가는 낮은 담(1m 토막, 바닥 0.4m 묻힘). 콜라이더 있음.</summary>
    static void LowWall(Transform parent, Vector3 mid, Vector3 along, float len, float hgt)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 1f)); float seg = len / n;
        for (int k = 0; k < n; k++)
        {
            var c = mid + along * ((k + .5f) * seg - len * .5f); float g = H(c.x, c.z);
            Box(parent, "LowWall", new Vector3(seg + 0.02f, hgt + 0.4f, 0.22f), new Vector3(c.x, g + (hgt - 0.4f) * .5f, c.z), Quaternion.LookRotation(Vector3.Cross(along, Vector3.up)), M("Stone"), true);
        }
    }
    static void WallLine(Transform parent, Vector3 yc, Vector3 lat, Vector3 fwd, float W, float D, Vector2 a, Vector2 b)
    {
        var pa = yc + lat * (a.x * W * .5f) + fwd * (a.y * D * .5f); var pb = yc + lat * (b.x * W * .5f) + fwd * (b.y * D * .5f);
        var dv = pb - pa; dv.y = 0; float L = dv.magnitude; if (L < 0.3f) return;
        // 차길 위는 건너뛴다(토막 단위)
        int n = Mathf.CeilToInt(L); var along = dv / L;
        for (int k = 0; k < n; k++)
        {
            var c = pa + along * ((k + .5f) * L / n);
            if (NearCar(c, 0.5f)) continue;
            float g = H(c.x, c.z);
            Box(parent, "YardWall", new Vector3(L / n + 0.02f, 1.3f, 0.22f), new Vector3(c.x, g + 0.25f, c.z), Quaternion.LookRotation(Vector3.Cross(along, Vector3.up)), M("Stone"), true);
        }
    }
    /// <summary>땅 위 띠(진입로·텃밭·오솔길): 1m 마다 지형 높이 + lift, 보기만.</summary>
    static void Strip(Transform parent, string name, Vector3 a, Vector3 b, float width, string mat, float lift)
    {
        var dv = b - a; dv.y = 0; float L = dv.magnitude; if (L < 0.3f) return;
        var f = dv / L; var rt = new Vector3(f.z, 0, -f.x); int n = Mathf.Max(1, Mathf.CeilToInt(L));
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int k = 0; k <= n; k++)
        {
            var c = a + f * (L * k / n);
            foreach (float sd in new[] { -1f, 1f }) { var q = c + rt * sd * width * .5f; verts.Add(new Vector3(q.x, H(q.x, q.z) + lift, q.z)); uvs.Add(new Vector2(q.x, q.z)); }
            if (k > 0) { int bb = verts.Count - 4; tris.Add(bb); tris.Add(bb + 2); tris.Add(bb + 1); tris.Add(bb + 1); tris.Add(bb + 2); tris.Add(bb + 3); }
        }
        var go = new GameObject(name); go.transform.SetParent(parent);
        var mesh = new Mesh { name = name }; mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // 작은 띠는 씬에 직렬화(에셋 수 폭증 방지)
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = M(mat);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
