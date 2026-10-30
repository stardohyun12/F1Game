using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 과제 2317(길 생성 v2 1단계, 도현 10/29 23:08~23:10 "길 생성이 깔끔하게 되는 알고리즘"): 사각형 구역 안 차길을 그래프로 다시 만든다.
//  원본 = 씬 RoadSurface 중심선(= town-v6b.json knot 1 m 보간 + 빌더 이음) · 걷는 길 = json knot. 손으로 씬을 고치지 않는다 — 다시 돌리면 같은 결과.
//  1 그래프: 끝점 2 m 병합 · 끝점이 다른 길 몸통 옆(반폭+2.5 m)이면 T 로 붙임 · 교차 X · 6 m 미만 군더더기 제거 · 70% 넘게 겹친 길은 하나로 · 차수 2 마디는 이어 한 사슬
//  2 평면: Douglas-Peucker 로 꺾인 점만 남김 → 원호 필렛 R=10 m → 1 m 재표본 → 아직 R 미만인 곳은 꼭짓점을 옮겨(라플라스) 확보
//  3 높이: 지형 표본(폭 3점 평균) → 펴기 → 구배 11%(12% 판정 여유) → 2차 차분 ≤ 0.008/m² → 갈림목은 모든 간선이 마디 높이 공유
//  4 갈림목: 마디마다 다각형(모서리 = 2차 베지어, 띠 경계선에 접함 ≈ R 4 m) — 띠는 그 경계에서 잘리고 끝줄 정점을 다각형이 그대로 씀(틈·겹침 0)
//  5 지형: 길 띠+어깨 2 m = 노면 −5 cm, 바깥 6 m smoothstep 으로 원지형, 2:1 이하. 건물 발자국+2 m 는 바닥 높이(건물 안 움직임). 구역 밖 margin 에서 원지형으로 녹임
//  6 재질 = 옛 길 renderer 의 sharedMaterial 그대로
//  되돌리기 = RoadNetV2Revert(): 끈 옛 길 다시 켜기 + RoadsV2 지우기 + 지형 _0433 다시 꽂기.
public static partial class BuildBayLoop
{
    const float V2R = 10f, V2Rc = 4f, V2Grade = .11f, V2D2 = .008f, V2Under = .05f, V2SH = 2f, V2Blend = 6f, V2Merge = 2f, V2Spur = 6f, V2PathLift = .05f;
    const string V2Folder = "Assets/Scenes/Map_TownV6_Mats/";
    const string V2Src = V2Folder + "Mesh_TownV6_Terrain_0433.asset", V2Dst = V2Folder + "Mesh_TownV6_Terrain_v2.asset", V2MeshAsset = V2Folder + "Mesh_RoadsV2.asset";
    static string V2DisabledFile => System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/roadv2_disabled.txt");
    static float vx0, vz0, vx1, vz1;
    static bool V2In(Vector3 p) => p.x >= vx0 && p.x <= vx1 && p.z >= vz0 && p.z <= vz1;
    static float V2Out(Vector3 p) => new Vector2(Mathf.Max(0, Mathf.Max(vx0 - p.x, p.x - vx1)), Mathf.Max(0, Mathf.Max(vz0 - p.z, p.z - vz1))).magnitude;

    class V2L { public string id, kind; public float w, rough; public List<Vector3> p; public List<bool> fix; public Material mat; public int n0 = -1, n1 = -1; public bool dead; public float d0, d1; }

    [MenuItem("PitCrew/Map/TownV6 — 길 생성 v2 (능선 구역 1단계)")]
    public static void MenuRoadNetV2() { Debug.Log("[RoadNetV2] " + RoadNetV2Build(150, -130, 330, -20, 15)); }
    [MenuItem("PitCrew/Map/TownV6 — 길 생성 v2 되돌리기")]
    public static void MenuRoadNetV2Revert() { Debug.Log("[RoadNetV2] " + RoadNetV2Revert(true)); }

    public static string RoadNetV2Revert(bool save)
    {
        var top = GameObject.Find("TownV6_Gray")?.transform; if (top == null) return "TownV6_Gray 없음";
        var roadsT = top.Find("Roads"); int on = 0;
        if (System.IO.File.Exists(V2DisabledFile))
            foreach (var nm in System.IO.File.ReadAllLines(V2DisabledFile)) { var t = roadsT.Find(nm.Trim()); if (t != null && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); on++; } }
        var v2 = top.Find("RoadsV2"); if (v2 != null) Object.DestroyImmediate(v2.gameObject);
        var terr = top.Find("TownV6_Terrain"); var src = AssetDatabase.LoadAssetAtPath<Mesh>(V2Src);
        terr.GetComponent<MeshFilter>().sharedMesh = src; var mc = terr.GetComponent<MeshCollider>(); mc.sharedMesh = null; mc.sharedMesh = src;
        if (save) { var sc = EditorSceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc); }
        return $"되돌림: 옛 길 {on}개 켬, 지형 = _0433";
    }

    public static string RoadNetV2Build(float x0, float z0, float x1, float z1, float margin)
    {
        var scene = EditorSceneManager.GetActiveScene(); if (scene.path != TownV6Scene) return "Map_TownV6 를 먼저 연다: " + scene.path;
        vx0 = x0; vz0 = z0; vx1 = x1; vz1 = z1;
        var lg = new List<string>(); lg.Add(RoadNetV2Revert(false));
        var top = GameObject.Find("TownV6_Gray").transform; var roadsT = top.Find("Roads"); var terrGo = top.Find("TownV6_Terrain").gameObject;
        var srcMesh = AssetDatabase.LoadAssetAtPath<Mesh>(V2Src); if (srcMesh == null) return "없음 " + V2Src;
        var tmpGo = new GameObject("v2_tmp_col") { hideFlags = HideFlags.HideAndDontSave };
        tmpGo.transform.SetPositionAndRotation(terrGo.transform.position, terrGo.transform.rotation); tmpGo.transform.localScale = terrGo.transform.lossyScale;
        var tmc = tmpGo.AddComponent<MeshCollider>(); tmc.sharedMesh = srcMesh;
        System.Func<float, float, float> G = (x, z) => tmc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600) ? h.point.y : float.NaN;
        var disabled = new List<string>(); var meshes = new List<Mesh>();
        try
        {
            // ── 1 그래프 ─────────────────────────────────────────
            var cars = new List<V2L>();
            foreach (var rs in roadsT.GetComponentsInChildren<RoadSurface>(false))
            {
                if (rs.centerline == null || rs.centerline.Length < 2 || !rs.centerline.Any(V2In)) continue;
                var L = new V2L { id = rs.roadId, kind = rs.kind, w = rs.width, rough = rs.roughness, mat = rs.GetComponent<MeshRenderer>().sharedMaterial, p = rs.centerline.Select(q => q - Vector3.up * RoadColLift).ToList() };
                L.fix = L.p.Select(q => !V2In(q)).ToList(); cars.Add(L); disabled.Add(rs.name);
            }
            // 3차 실행: camo_874876277_276 이 큰길 165_4 띠 안을 가운데 구간(19점, 높이차 ≤0.94)만 나란히 달려 70% 중복 기준에 안 걸림 → 겹침 188쌍.
            // 좁은 길이 넓은 길 띠 안을 4 m 넘게 달리면 그 구간을 잘라 내고, 남은 두 토막의 끝은 아래 T 붙이기가 큰길에 붙인다.
            for (int k = 0; k < cars.Count; k++)
            {
                var e = cars[k]; bool cut = false;
                foreach (var f in cars)
                {
                    if (f == e || f.w <= e.w) continue;
                    var ins = e.p.Select(q => V2In(q) && V2Dist(f.p, q, out var nq) < f.w * .5f && Mathf.Abs(nq.y - q.y) < 1.5f).ToArray();
                    var keep = new List<List<int>>(); var cur = new List<int>(); int i = 0;
                    while (i < e.p.Count)
                    {
                        if (!ins[i]) { cur.Add(i); i++; continue; }
                        int j = i; while (j + 1 < e.p.Count && ins[j + 1]) j++;
                        if (V2Len(e.p.GetRange(i, j - i + 1)) >= 4f) { cur.Add(i); keep.Add(cur); cur = new List<int> { j }; cut = true; } else for (int t = i; t <= j; t++) cur.Add(t);
                        i = j + 1;
                    }
                    keep.Add(cur);
                    if (!cut) continue;
                    cars.RemoveAt(k); int add = 0;
                    foreach (var g in keep) { var p = g.Select(t => e.p[t]).ToList(); if (p.Count < 2 || V2Len(p) < V2Spur) continue; cars.Insert(k + add, new V2L { id = e.id + "_s" + add, kind = e.kind, w = e.w, rough = e.rough, mat = e.mat, p = p, fix = p.Select(q => !V2In(q)).ToList() }); add++; }
                    lg.Add($"띠 안 구간 잘라냄: {e.id} ⊂ {f.id} → 토막 {add}");
                    k += add - 1; break;
                }
            }
            var nP = new List<Vector3>(); var par = new List<int>();
            int NewN(Vector3 q) { nP.Add(q); par.Add(par.Count); return par.Count - 1; }
            int Fd(int a) { while (par[a] != a) a = par[a] = par[par[a]]; return a; }
            var splits = cars.Select(_ => new SortedDictionary<int, int>()).ToList();
            for (int k = 0; k < cars.Count; k++) foreach (int e in new[] { 0, cars[k].p.Count - 1 })
            {
                var q = cars[k].p[e]; int hit = -1;
                for (int n = 0; n < nP.Count; n++) if (Fl(nP[n] - q).magnitude < V2Merge && Mathf.Abs(nP[n].y - q.y) < 1.5f) { hit = n; break; }
                if (hit < 0) hit = NewN(q); else if (V2In(q)) cars[k].p[e] = new Vector3(nP[hit].x, q.y, nP[hit].z);
                splits[k][e] = hit;
            }
            var endCnt = new Dictionary<int, int>(); foreach (var s in splits) foreach (var kv in s) endCnt[kv.Value] = endCnt.GetValueOrDefault(kv.Value) + 1;
            int nT = 0, nX = 0;
            for (int k = 0; k < cars.Count; k++) foreach (int e in new[] { 0, cars[k].p.Count - 1 })
            {
                int nd = splits[k][e]; var q = cars[k].p[e]; if (endCnt[nd] != 1 || !V2In(q)) continue;
                int bk = -1, bj = -1; float bd = 1e9f;
                for (int o = 0; o < cars.Count; o++) { if (o == k) continue; var c = cars[o].p; for (int j = 0; j < c.Count; j++) { float d = Fl(c[j] - q).magnitude; if (d < cars[o].w * .5f + 2.5f && d < bd && Mathf.Abs(c[j].y - q.y) < 1.5f) { bd = d; bk = o; bj = j; } } }
                if (bk < 0) continue;
                int tn = -1; foreach (var kv in splits[bk]) if (Mathf.Abs(kv.Key - bj) <= 3) { tn = kv.Value; break; }
                if (tn < 0) { tn = NewN(cars[bk].p[bj]); splits[bk][bj] = tn; }
                if (Fd(tn) == Fd(nd)) continue;
                par[Fd(nd)] = Fd(tn); var np = nP[Fd(tn)]; cars[k].p[e] = new Vector3(np.x, q.y, np.z); nT++;
            }
            for (int a = 0; a < cars.Count; a++) for (int b = a + 1; b < cars.Count; b++)
            {
                var pa = cars[a].p; var pb = cars[b].p;
                for (int i = 0; i + 1 < pa.Count; i++)
                {
                    if (!V2In(pa[i])) continue;
                    for (int j = 0; j + 1 < pb.Count; j++)
                    {
                        if (!V2SegX(pa[i], pa[i + 1], pb[j], pb[j + 1], out float t, out float u)) continue;
                        var X = Vector3.Lerp(pa[i], pa[i + 1], t); float yb = Mathf.Lerp(pb[j].y, pb[j + 1].y, u); if (Mathf.Abs(X.y - yb) > 1.5f) continue;
                        int ia = t < .5f ? i : i + 1, jb = u < .5f ? j : j + 1;
                        if (splits[a].Keys.Any(x => Mathf.Abs(x - ia) <= 3) || splits[b].Keys.Any(x => Mathf.Abs(x - jb) <= 3)) continue;
                        int n = NewN(X); pa[ia] = new Vector3(X.x, pa[ia].y, X.z); pb[jb] = new Vector3(X.x, pb[jb].y, X.z); splits[a][ia] = n; splits[b][jb] = n; nX++;
                    }
                }
            }
            var edges = new List<V2L>();
            for (int k = 0; k < cars.Count; k++)
            {
                var keys = splits[k].Keys.ToList();
                for (int m = 0; m + 1 < keys.Count; m++)
                {
                    int s0 = keys[m], s1 = keys[m + 1]; if (s1 - s0 < 1) continue;
                    var c = cars[k]; var e = new V2L { id = c.id + (keys.Count > 2 ? "_" + m : ""), kind = c.kind, w = c.w, rough = c.rough, mat = c.mat, p = c.p.GetRange(s0, s1 - s0 + 1), fix = c.fix.GetRange(s0, s1 - s0 + 1), n0 = Fd(splits[k][s0]), n1 = Fd(splits[k][s1]) };
                    edges.Add(e);
                }
            }
            int Deg(int n) => edges.Count(e => !e.dead && e.n0 == n) + edges.Count(e => !e.dead && e.n1 == n);
            int nSpur = 0, nDup = 0;
            for (bool ch = true; ch;)
            {
                ch = false;
                foreach (var e in edges.Where(x => !x.dead).ToList())
                {
                    int d0 = Deg(e.n0), d1 = Deg(e.n1);
                    if (V2Len(e.p) < V2Spur && e.p.Any(V2In) && ((d0 == 1 && d1 >= 3) || (d1 == 1 && d0 >= 3))) { e.dead = true; ch = true; nSpur++; lg.Add($"군더더기 {e.id} {V2Len(e.p):F1}m"); }
                }
                // 1e: ridge_up_b_s0_2(9 m, 막다른 끝)가 점 10/24 를 큰길 띠 위에 깔아 진회색 길 위 밝은 회색 판으로 보임 → 15 m 미만 막다른 꼬리가 35% 넘게 다른 띠에 묻혔으면 지운다
                foreach (var e in edges.Where(x => !x.dead).ToList())
                {
                    // 끝마다: 구역 안 막다른 끝(차수 1) 이거나 지워도 남는 길이 2개 이상(차수 ≥3). 구역 밖 끝(차수 1)은 바깥 길과 이어지니 건드리지 않는다
                    //  (1차: 차수 1 만 봐서 구역 경계에 걸친 tomo_723378509_33 8.3 m 를 지움 / ridge_up_b_s0_2 는 양 끝이 갈림목이라 안 걸림)
                    int d0 = Deg(e.n0), d1 = Deg(e.n1); if (V2Len(e.p) >= 15f) continue;
                    bool Ok(int d, Vector3 q) => d >= 3 || (d == 1 && V2In(q));
                    if (!Ok(d0, e.p[0]) || !Ok(d1, e.p[e.p.Count - 1])) continue;
                    int inn = e.p.Count(q => edges.Any(f => f != e && !f.dead && V2Dist(f.p, q, out var nq) < f.w * .5f && Mathf.Abs(nq.y - q.y) < 2.5f));
                    if (inn > e.p.Count * .35f) { e.dead = true; ch = true; nSpur++; lg.Add($"띠에 묻힌 꼬리 {e.id} {V2Len(e.p):F1}m ({inn}/{e.p.Count})"); }
                }
                var live = edges.Where(x => !x.dead).ToList();
                foreach (var e in live) foreach (var f in live)
                {
                    if (e == f || e.dead || f.dead || V2Len(e.p) > V2Len(f.p)) continue;   // e = 짧은 쪽
                    int inn = e.p.Count(q => V2Dist(f.p, q, out var nq) < Mathf.Max(e.w, f.w) * .5f && Mathf.Abs(nq.y - q.y) < 2.5f);   // 1 m → 2.5 m: camo_874876277 이 큰길 띠 안을 1~2.5 m 높이차로 25 m 나란히 달려 겹침 188쌍
                    if (inn > e.p.Count * .7f && e.p.Any(V2In)) { e.dead = true; ch = true; nDup++; lg.Add($"겹친 길 하나로: {e.id} → {f.id} ({inn}/{e.p.Count})"); }
                }
                // 차수 2 마디 = 이어 붙임
                foreach (int n in edges.Where(x => !x.dead).SelectMany(x => new[] { x.n0, x.n1 }).Distinct().ToList())
                {
                    var inc = edges.Where(x => !x.dead && (x.n0 == n || x.n1 == n)).ToList();
                    if (inc.Count != 2 || inc[0] == inc[1] || (inc[0].n0 == n && inc[0].n1 == n)) continue;
                    var a = inc[0]; var b = inc[1]; if (a.n0 == n) V2Rev(a); if (b.n1 == n) V2Rev(b);
                    a.p.AddRange(b.p.Skip(1)); a.fix.AddRange(b.fix.Skip(1)); a.n1 = b.n1; if (b.w > a.w) { a.w = b.w; a.kind = b.kind; a.mat = b.mat; a.rough = b.rough; }
                    a.id += "+" + b.id; b.dead = true; ch = true;
                }
            }
            var chains = edges.Where(x => !x.dead).ToList();
            foreach (var e in chains) foreach (var f in chains)   // 진단: 좁은 길이 넓은 길 띠 안을 달리는 구간
            {
                if (e == f || e.w > f.w) continue; int k = 0, i0 = -1, i1 = -1; float dyMax = 0;
                for (int i = 0; i < e.p.Count; i++) if (V2In(e.p[i]) && V2Dist(f.p, e.p[i], out var nq) < f.w * .5f) { k++; if (i0 < 0) i0 = i; i1 = i; dyMax = Mathf.Max(dyMax, Mathf.Abs(nq.y - e.p[i].y)); }
                if (k >= 4) lg.Add($"띠 안 진단: {V2Short(e.id)} 점 {k}/{e.p.Count} [{i0}..{i1}] 이 {V2Short(f.id)} 띠 안, 높이차 최대 {dyMax:F2}");
            }
            lg.Add($"그래프: 길 {cars.Count} → 사슬 {chains.Count}, 마디 {nP.Count(_ => true)}(T {nT}·X {nX}), 군더더기 {nSpur}, 겹침합침 {nDup}");

            // ── 2 평면 곡선 ─────────────────────────────────────
            foreach (var c in chains) { V2Fillet(c); V2SmoothXZ(c); }
            // ── 3 높이 ──────────────────────────────────────────
            var nodes = chains.SelectMany(x => new[] { x.n0, x.n1 }).Distinct().ToList();
            var nodeY = new Dictionary<int, float>();
            foreach (int n in nodes) { var q = nP[n]; float g = G(q.x, q.z); nodeY[n] = V2In(q) && !float.IsNaN(g) ? g : chains.Where(x => x.n0 == n).Select(x => x.p[0].y).Concat(chains.Where(x => x.n1 == n).Select(x => x.p[x.p.Count - 1].y)).Average(); }
            // ── 4 갈림목: 자를 거리(평면만 보고 정한다 → 높이 단계에서 그 구간을 마디 높이로 고정) ──
            var juncs = new List<(int n, List<(V2L c, bool st, Vector3 u)> inc)>();
            foreach (int n in nodes)
            {
                var inc = new List<(V2L c, bool st, Vector3 u)>();
                foreach (var c in chains) { if (c.n0 == n) inc.Add((c, true, V2Dir(c.p, true))); if (c.n1 == n) inc.Add((c, false, V2Dir(c.p, false))); }
                if (inc.Count < 3 || !V2In(nP[n])) continue;
                inc = inc.OrderBy(x => Mathf.Atan2(x.u.z, x.u.x)).ToList();
                var d = inc.Select(x => Mathf.Max(x.c.w * .5f, 2f)).ToArray();
                for (int i = 0; i < inc.Count; i++)
                {
                    int j = (i + 1) % inc.Count; var ui = inc[i].u; var uj = inc[j].u;
                    float th = Mathf.Atan2(ui.x * uj.z - ui.z * uj.x, Vector3.Dot(ui, uj)); if (th < 0) th += Mathf.PI * 2f;
                    if (th >= Mathf.PI * .97f) continue;
                    float hi = inc[i].c.w * .5f, hj = inc[j].c.w * .5f, sn = Mathf.Sin(th), cs = Mathf.Cos(th), tc = V2Rc / Mathf.Tan(th * .5f);
                    d[i] = Mathf.Max(d[i], (hj + hi * cs) / sn + tc); d[j] = Mathf.Max(d[j], (hi + hj * cs) / sn + tc);
                }
                for (int i = 0; i < inc.Count; i++) { var c = inc[i].c; float L = V2Len(c.p); bool both = c.n0 != n ? false : c.n1 != n ? false : true; float cap = Mathf.Min(14f, L * (both ? .4f : .45f)); if (inc[i].st) c.d0 = Mathf.Min(d[i], cap); else c.d1 = Mathf.Min(d[i], cap); }
                juncs.Add((n, inc));
            }
            // 1차 실행: 높이를 먼저 펴고 갈림목 d 구간을 나중에 마디 높이로 덮었더니 d(최대 14 m)×11% 만큼의 차가 6 m 에 몰려 구배 262% → 이제 d 구간을 고정점으로 넣고 편다
            int DegE(int n) => chains.Count(x => x.n0 == n) + chains.Count(x => x.n1 == n);
            foreach (var c in chains) { V2Profile(c, nodeY[c.n0], nodeY[c.n1], G); V2Trim(c); V2FinalGrade(c, DegE(c.n0) == 1, DegE(c.n1) == 1); }
            // 1f: 갈림목 없는 끝(d=0)이 넓은 길 띠 안에 놓이면 두 띠가 1~4 cm 차로 겹쳐 깊이 싸움(진회색 위 밝은 가시) — 874_s1 끝은 구역 밖(x 331)이라 T 붙이기를 못 받음
            //  → 그 끝을 넓은 띠 가장자리에서 자르고 높이를 넓은 길에 맞춘다
            int nEndTrim = 0;
            foreach (var c in chains) for (int side = 0; side < 2; side++)
            {
                if ((side == 0 ? c.d0 : c.d1) > 0f) continue;
                if (side == 1) { c.p.Reverse(); c.fix.Reverse(); }
                var f = chains.FirstOrDefault(o => o != c && o.w > c.w && V2Dist(o.p, c.p[0], out var nq0) < o.w * .5f && Mathf.Abs(nq0.y - c.p[0].y) < 1.5f);
                if (f != null)
                {
                    float hw = f.w * .5f; int i = 0; while (i + 1 < c.p.Count && V2Dist(f.p, c.p[i + 1], out _) < hw) i++;
                    if (i + 2 < c.p.Count)
                    {
                        Vector3 a = c.p[i], b = c.p[i + 1]; float lo = 0, hi = 1;
                        for (int it = 0; it < 24; it++) { float mid = (lo + hi) * .5f; if (V2Dist(f.p, Vector3.Lerp(a, b, mid), out _) < hw) lo = mid; else hi = mid; }
                        var X = Vector3.Lerp(a, b, hi); V2Dist(f.p, X, out var nqX); X.y = nqX.y;
                        c.p.RemoveRange(0, i + 1); c.fix.RemoveRange(0, i + 1); c.p.Insert(0, X); c.fix.Insert(0, false);
                        if (c.p.Count > 2 && Fl(c.p[1] - c.p[0]).magnitude < .3f) { c.p.RemoveAt(1); c.fix.RemoveAt(1); }
                        nEndTrim++; lg.Add($"띠 안 끝 자름: {V2Short(c.id)} ⊂ {V2Short(f.id)} 점 {i + 1}개");
                    }
                }
                if (side == 1) { c.p.Reverse(); c.fix.Reverse(); }
            }

            // ── 띠 메시 ────────────────────────────────────────
            var v2T = new GameObject("RoadsV2").transform; v2T.SetParent(top, false);
            var bands = new Dictionary<V2L, List<Vector3>>(); var topTris = new List<(int obj, Vector3 a, Vector3 b, Vector3 c)>(); int objN = 0;
            var report = new List<string>();
            foreach (var c in chains)
            {
                var v = V2BandVerts(c.p, c.w * .5f, RoadColLift); bands[c] = v;
                var go = V2MakeRoad(v2T, "ROAD_V2e_" + V2Short(c.id), v, null, c.mat, meshes);
                var rsf = go.AddComponent<RoadSurface>(); rsf.roadId = "v2_" + V2Short(c.id); rsf.kind = c.kind; rsf.roughness = c.rough; rsf.width = c.w;
                rsf.centerline = c.p.Select(q => q + Vector3.up * RoadColLift).ToArray(); rsf.closed = false;
                for (int i = 1; i < c.p.Count; i++) { int a = (i - 1) * 2; topTris.Add((objN, v[a], v[a + 2], v[a + 1])); topTris.Add((objN, v[a + 1], v[a + 2], v[a + 3])); }
                objN++;
                report.Add(V2Stats(c));
            }
            var jPolys = new List<(List<Vector3> poly, float y)>(); var jCen = new List<Vector3>(); int gapN = 0;
            foreach (var (n, inc) in juncs)
            {
                var poly = new List<Vector3>(); var cut = new List<bool>(); float ny = nodeY[n] + RoadColLift;
                var AB = inc.Select(x => { var v = bands[x.c]; int m = v.Count; return x.st ? (A: v[0], B: v[1]) : (A: v[m - 1], B: v[m - 2]); }).ToList();
                var U = inc.Select(x => x.st ? Fl(x.c.p[1] - x.c.p[0]).normalized : Fl(x.c.p[x.c.p.Count - 2] - x.c.p[x.c.p.Count - 1]).normalized).ToList();
                for (int i = 0; i < inc.Count; i++)
                {
                    int j = (i + 1) % inc.Count; var A = AB[i].A; var B = AB[j].B;
                    poly.Add(AB[i].B); cut.Add(true); poly.Add(A); cut.Add(false);
                    Vector3 X = (A + B) * .5f;
                    if (V2LineX(A, U[i], B, U[j], out float t, out float s) && t <= .01f && s <= .01f && t > -20f && s > -20f) X = A + U[i] * t;
                    for (int k = 1; k < 8; k++) { float q = k / 8f; var P = (1 - q) * (1 - q) * A + 2 * (1 - q) * q * X + q * q * B; P.y = Mathf.Lerp(A.y, B.y, q); poly.Add(P); cut.Add(false); }
                }
                var center = new Vector3(nP[n].x, ny, nP[n].z);
                V2MakeJunction(v2T, "ROAD_V2j_" + n, center, poly, cut, inc.OrderByDescending(x => x.c.w).First().c, meshes, topTris, objN++);
                jPolys.Add((poly, ny)); jCen.Add(center);
                for (int i = 0; i < inc.Count; i++) foreach (var q in new[] { AB[i].A, AB[i].B }) if (poly.Min(pp => (pp - q).magnitude) > .01f) gapN++;
            }
            // ── 5 지형 ──────────────────────────────────────────
            var segs = new List<(Vector3 a, Vector3 b, float hw, bool endA, bool endB)>();
            foreach (var c in chains) for (int i = 1; i < c.p.Count; i++) segs.Add((c.p[i - 1] + Vector3.up * RoadColLift, c.p[i] + Vector3.up * RoadColLift, c.w * .5f, i == 1 && c.d0 == 0f, i == c.p.Count - 1 && c.d1 == 0f));
            // 1b-1: 막힌 끝(갈림목 없는 사슬 끝) — 2 m 격자 정점 사이 삼각형이 어깨 밖(e 2~2.8 m) 정점으로 끌려 캡 변을 6~9 cm 덮음 → 캡 둘레는 어깨 +1 m, 깎기 −10 cm 더
            var deadEnds = new List<(Vector3 p, float hw, Vector3 o)>();
            foreach (var c in chains) { int m = c.p.Count; if (c.d0 == 0f && V2In(c.p[0])) deadEnds.Add((c.p[0], c.w * .5f, Fl(c.p[0] - c.p[1]).normalized)); if (c.d1 == 0f && V2In(c.p[m - 1])) deadEnds.Add((c.p[m - 1], c.w * .5f, Fl(c.p[m - 1] - c.p[m - 2]).normalized)); }
            var pads = V2Pads(top);
            var tr = terrGo.transform; var sv = srcMesh.vertices; var w = new Vector3[sv.Length]; for (int i = 0; i < sv.Length; i++) w[i] = tr.TransformPoint(sv[i]);
            int moved = 0, padN = 0; float maxD = 0;
            for (int k = 0; k < w.Length; k++)
            {
                var p = w[k]; float out0 = V2Out(p); if (out0 >= margin) continue;
                float orig = p.y, y = orig; float pe = 1e9f, pH = 0;
                foreach (var pd in pads) { var dl = pd.w2l.MultiplyPoint3x4(p); float lx = Mathf.Max(pd.mn.x - dl.x, dl.x - pd.mx.x), lz = Mathf.Max(pd.mn.z - dl.z, dl.z - pd.mx.z); float e = Mathf.Max(lx, lz) > 0 ? new Vector2(Mathf.Max(lx, 0), Mathf.Max(lz, 0)).magnitude : Mathf.Max(lx, lz); if (e < pe) { pe = e; pH = pd.H; } }
                if (pe <= 2f) { y = pH; padN++; } else if (pe < 14f) y = Mathf.Clamp(y, pH - (pe - 2f) / 3f, pH + (pe - 2f) / 2f);
                float eO = 1e9f, yO = 0, lo = -1e9f, hi = 1e9f;
                foreach (var s in segs)
                {
                    if (p.x < Mathf.Min(s.a.x, s.b.x) - 40 || p.x > Mathf.Max(s.a.x, s.b.x) + 40 || p.z < Mathf.Min(s.a.z, s.b.z) - 40 || p.z > Mathf.Max(s.a.z, s.b.z) + 40) continue;
                    var ab = Fl(s.b - s.a); float t0 = Vector3.Dot(Fl(p - s.a), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude), t = Mathf.Clamp01(t0);
                    var q = Vector3.Lerp(s.a, s.b, t); var dd = Fl(p - q); float e = dd.magnitude - s.hw;
                    if ((s.endA && t0 < 0) || (s.endB && t0 > 1)) { float over = (t0 < 0 ? -t0 : t0 - 1) * ab.magnitude; e = Mathf.Max(Mathf.Sqrt(Mathf.Max(0, dd.sqrMagnitude - over * over)) - s.hw, over); }
                    V2Acc(e, q.y, ref eO, ref yO, ref lo, ref hi);
                }
                // 3차 실행: 안쪽을 마디 높이 하나로 깎았더니 갈림목 메시(가운데→띠 끝 부채꼴)와 6~20 cm 어긋나 띠 끝이 묻히거나 뜸 → 부채꼴 삼각형 높이를 그대로 따름
                for (int jp = 0; jp < jPolys.Count; jp++) { var (poly, jy) = jPolys[jp]; float e = V2PolyE(poly, p, out float yb); float fy = e <= 0 ? V2FanY(jCen[jp], poly, p, jy) : yb; V2Acc(e, fy, ref eO, ref yO, ref lo, ref hi); }
                float sh = V2SH, under = V2Under;
                foreach (var de in deadEnds) if (Fl(p - de.p).magnitude < de.hw + V2SH + 2f && Vector3.Dot(Fl(p - de.p), de.o) > 0f) {   // 1c: 캡 선 너머만(옆변까지 −10 cm 면 착지 띠가 뜸)
                    sh = V2SH + 1f; under = V2Under + .10f; break; }
                if (eO < sh + 30f && !(pe <= 2f && eO > 0f) && !(orig - yO > 3f && eO <= sh))
                {
                    float S = yO - under;
                    if (eO <= sh) y = S;
                    else
                    {
                        float x = Mathf.Clamp01((eO - sh) / V2Blend); x = x * x * (3 - 2 * x); y = Mathf.Lerp(S, y, x);
                        if (lo <= hi) y = Mathf.Clamp(y, lo, hi); else y = Mathf.Clamp(y, S - (eO - sh) * .5f, S + (eO - sh) * .5f);
                    }
                }
                if (out0 > 0) y = Mathf.Lerp(y, orig, out0 / margin);
                // 1d: 구역 밖 여유띠의 원래 높이 되돌림이 874_s1 끝(구역 밖 1.4 m) 캡 너머 0.6 m 정점을 24.54 로 남겨, 2 m 삼각형이 띠 끝을 2 cm 덮음
                //  → 띠 가장자리 1 m 안은 되돌림보다 띠−깎기가 우선
                if (out0 > 0 && eO <= 1f) y = Mathf.Min(y, yO - V2Under);   // 구역 안까지 걸면 D198_postoffice 판 모서리를 0.31 깎음
                if (Mathf.Abs(y - orig) > .001f) { w[k].y = y; moved++; maxD = Mathf.Max(maxD, Mathf.Abs(y - orig)); }
            }
            var m2 = Object.Instantiate(srcMesh); m2.name = "TownV6_Terrain_v2"; var v2v = new Vector3[w.Length]; for (int i = 0; i < w.Length; i++) v2v[i] = tr.InverseTransformPoint(w[i]);
            m2.vertices = v2v; m2.RecalculateNormals(); m2.RecalculateBounds();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(V2Dst) != null) AssetDatabase.DeleteAsset(V2Dst);
            AssetDatabase.CreateAsset(m2, V2Dst); terrGo.GetComponent<MeshFilter>().sharedMesh = m2; var mc = terrGo.GetComponent<MeshCollider>(); mc.sharedMesh = null; mc.sharedMesh = m2;
            lg.Add($"지형 정점 {moved}개(최대 {maxD:F2} m), 건물 판 정점 {padN}, 건물 {pads.Count}");

            // ── 걷는 길: 차길 띠와 겹친 부분 지우고 새 지형에 깐다 ──────────
            tmc.sharedMesh = m2;
            var plan = (Dictionary<string, object>)Json.Parse(System.IO.File.ReadAllText(TownV6Json, Encoding.UTF8));
            int pathN = 0, pathCut = 0;
            foreach (Dictionary<string, object> r in (List<object>)plan["roads"])
            {
                string kind = (string)r["kind"]; if (kind == "main" || kind == "village") continue;
                var ks = ((List<object>)r["knots"]).Cast<List<object>>().Select(k => new Vector3(F(k[0]), F(k[2]), F(k[1]))).ToList();
                if (!ks.Any(q => V2Out(q) < margin)) continue;
                var old = roadsT.Find("ROAD_" + (string)r["id"]); if (old == null || !old.gameObject.activeSelf) continue;
                var mat = old.GetComponent<MeshRenderer>().sharedMaterial; disabled.Add(old.name);
                var pts = new List<Vector3>(); for (int i = 0; i + 1 < ks.Count; i++) { int nn = Mathf.Max(1, Mathf.CeilToInt(Fl(ks[i + 1] - ks[i]).magnitude)); for (int s = 0; s < nn; s++) pts.Add(Vector3.Lerp(ks[i], ks[i + 1], s / (float)nn)); }
                pts.Add(ks[ks.Count - 1]); float hw = F(r["width"]) * .5f;
                var keep = pts.Select(q => segs.All(s => V2SegE(s.a, s.b, q) - s.hw > hw + .3f) && jPolys.All(j => V2PolyE(j.poly, q, out _) > hw + .3f)).ToList();
                int piece = 0;
                for (int i = 0; i < pts.Count;)
                {
                    if (!keep[i]) { i++; pathCut++; continue; }
                    int j = i; while (j < pts.Count && keep[j]) j++;
                    if (j - i >= 3)
                    {
                        var v = V2BandVerts(pts.GetRange(i, j - i), hw, V2PathLift);
                        // 1d: 정점 높이만 재면 2 m 격자 삼각형이 볼록한 곳에서 1 m 간격 정점 사이를 3 cm 덮음(295.7,−99.5) → 이웃(±1 m 4방향)과의 가운데가 직선보다 솟은 만큼만 올림
                        //  (둘레 최고점을 쓰면 비탈 35% 에서 +0.18 뜸 — 평면이면 솟음 0 이라 비탈 무관)
                        for (int q = 0; q < v.Count; q++)
                        {
                            float g = G(v[q].x, v[q].z); if (float.IsNaN(g)) continue; float bump = 0;
                            foreach (var o in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                            { float gm = G(v[q].x + o.x * .5f, v[q].z + o.y * .5f), gf = G(v[q].x + o.x, v[q].z + o.y); if (!float.IsNaN(gm) && !float.IsNaN(gf)) bump = Mathf.Max(bump, gm - (g + gf) * .5f); }
                            v[q] = new Vector3(v[q].x, g + V2PathLift + Mathf.Min(bump, .1f), v[q].z);
                        }
                        var go = V2MakeRoad(v2T, "ROAD_V2p_" + (string)r["id"] + "_" + piece++, v, null, mat, meshes, false); pathN++;
                    }
                    i = j;
                }
            }
            lg.Add($"걷는 길 조각 {pathN}, 차길 띠와 겹쳐 지운 점 {pathCut}");
            // 1f: v2 는 구역에 닿은 길 전체(165_6 은 x 608 까지)를 새로 까는데, 구역에 안 닿은 옛 곁길(camo_794408003_237 @371,−95)은 그대로라
            //  그 끝이 새 띠와 0 cm 차로 겹쳐 깊이 싸움 → 띠 안 삼각형을 뺀 사본을 RoadsV2 에 두고 옛 것은 끈다(되돌리기 = 다시 켬)
            //  띠 경계에 걸친 삼각형은 띠 안 정점만 0.10 m 내려 띠 밑으로 숨긴다
            int oldClip = 0;
            bool InBand(Vector3 q) => chains.Any(c => V2Dist(c.p, q, out var nq) < c.w * .5f && Mathf.Abs(nq.y + RoadColLift - q.y) < 1.5f) || jPolys.Any(j => V2PolyE(j.poly, q, out _) <= 0f && Mathf.Abs(j.y - q.y) < 1.5f);
            foreach (var mr in roadsT.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!mr.name.StartsWith("ROAD_") || disabled.Contains(mr.name)) continue;
                var b = mr.bounds; b.Expand(1f);
                if (!chains.Any(c => c.p.Any(q => b.Contains(new Vector3(q.x, b.center.y, q.z))))) continue;
                var mf = mr.GetComponent<MeshFilter>(); var src = mf.sharedMesh; var otr = mr.transform;
                var wv = src.vertices.Select(q => otr.TransformPoint(q)).ToArray(); var inV = wv.Select(InBand).ToArray(); if (!inV.Any(x => x)) continue;
                var cm = Object.Instantiate(src); cm.name = "ROAD_V2o_" + mr.name; int cut = 0;
                for (int sm = 0; sm < src.subMeshCount; sm++)
                {
                    var t = src.GetTriangles(sm); var keep = new List<int>();
                    for (int i = 0; i < t.Length; i += 3) { if (InBand((wv[t[i]] + wv[t[i + 1]] + wv[t[i + 2]]) / 3f)) { cut++; continue; } keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]); }
                    cm.SetTriangles(keep, sm);
                }
                cm.vertices = wv.Select((q, i) => otr.InverseTransformPoint(inV[i] ? q - Vector3.up * .10f : q)).ToArray(); cm.RecalculateBounds(); meshes.Add(cm);
                var cl = Object.Instantiate(mr.gameObject, v2T, true); cl.name = "ROAD_V2o_" + mr.name; cl.GetComponent<MeshFilter>().sharedMesh = cm;
                var cc = cl.GetComponent<MeshCollider>(); if (cc != null) { cc.sharedMesh = null; cc.sharedMesh = cm; }
                disabled.Add(mr.name); oldClip++; lg.Add($"옛 길 띠 안 잘라냄: {mr.name} 삼각형 {cut}, 정점 내림 {inV.Count(x => x)}");
            }
            lg.Add($"띠 안 끝 자름 {nEndTrim}, 옛 길 사본 {oldClip}");
            foreach (var nm in disabled.Distinct()) roadsT.Find(nm)?.gameObject.SetActive(false);
            System.IO.File.WriteAllLines(V2DisabledFile, disabled.Distinct());

            // ── 검사 ─────────────────────────────────────────────
            int fold = topTris.Count(t => Vector3.Cross(t.b - t.a, t.c - t.a).y <= 0f);
            int overN = V2Overlaps(topTris, out int overTris);
            int unconnected = 0;
            foreach (var c in chains) foreach (var (q, n) in new[] { (c.p[0], c.n0), (c.p[c.p.Count - 1], c.n1) })
                if (Deg2(chains, n) == 1 && V2In(q) && chains.Any(o => o != c && V2Dist(o.p, q, out _) - o.w * .5f < 4f)) unconnected++;
            lg.Add($"검사: 접힌 삼각형 {fold}, 겹친 삼각형 쌍 {overN}(삼각형 {overTris}), 갈림목 이음 틈 {gapN}, 다른 길 4 m 안 끊긴 끝 {unconnected}, 갈림목 {juncs.Count}");
            lg.Add("겹침 위치(5 m 칸): " + string.Join(", ", v2OverAt.OrderByDescending(x => x.Value).Take(10).Select(x => x.Key + "=" + x.Value)));
            lg.Add("물체 번호: " + string.Join(", ", v2T.Cast<Transform>().Where(x => x.name.StartsWith("ROAD_V2e") || x.name.StartsWith("ROAD_V2j")).Select((x, i) => i + ":" + x.name.Substring(9, Mathf.Min(18, x.name.Length - 9)))));
            lg.AddRange(report);

            if (AssetDatabase.LoadAssetAtPath<Object>(V2MeshAsset) != null) AssetDatabase.DeleteAsset(V2MeshAsset);
            AssetDatabase.CreateAsset(meshes[0], V2MeshAsset); for (int i = 1; i < meshes.Count; i++) AssetDatabase.AddObjectToAsset(meshes[i], V2MeshAsset);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return string.Join("\n", lg);
        }
        finally { Object.DestroyImmediate(tmpGo); }
    }

    static int Deg2(List<V2L> cs, int n) => cs.Count(c => c.n0 == n) + cs.Count(c => c.n1 == n);
    static string V2Short(string id) => id.Length > 60 ? id.Substring(0, 40) + "~" + (id.GetHashCode() & 0xffff).ToString("x") : id;

    static void V2Acc(float e, float yS, ref float eO, ref float yO, ref float lo, ref float hi)
    {
        if (e < eO) { eO = e; yO = yS; }
        if (e > V2SH && e < V2SH + 30f) { lo = Mathf.Max(lo, yS - V2Under - (e - V2SH) * .5f); hi = Mathf.Min(hi, yS - V2Under + (e - V2SH) * .5f); }
    }

    // ── 기하 도우미 ──────────────────────────────────────────
    static float V2Len(List<Vector3> p) { float L = 0; for (int i = 1; i < p.Count; i++) L += Fl(p[i] - p[i - 1]).magnitude; return L; }
    static void V2Rev(V2L c) { c.p.Reverse(); c.fix.Reverse(); (c.n0, c.n1) = (c.n1, c.n0); (c.d0, c.d1) = (c.d1, c.d0); }
    static float V2SegE(Vector3 a, Vector3 b, Vector3 p) { var ab = Fl(b - a); float t = Mathf.Clamp01(Vector3.Dot(Fl(p - a), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return Fl(p - Vector3.Lerp(a, b, t)).magnitude; }
    static float V2Dist(List<Vector3> pl, Vector3 p, out Vector3 nq)
    {
        float bd = 1e9f; nq = pl[0];
        for (int i = 1; i < pl.Count; i++) { var ab = Fl(pl[i] - pl[i - 1]); float t = Mathf.Clamp01(Vector3.Dot(Fl(p - pl[i - 1]), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); var q = Vector3.Lerp(pl[i - 1], pl[i], t); float d = Fl(p - q).magnitude; if (d < bd) { bd = d; nq = q; } }
        return bd;
    }
    static bool V2SegX(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float t, out float u)
    {
        float rx = b.x - a.x, rz = b.z - a.z, sx = d.x - c.x, sz = d.z - c.z, den = rx * sz - rz * sx; t = u = 0;
        if (Mathf.Abs(den) < 1e-6f) return false;
        t = ((c.x - a.x) * sz - (c.z - a.z) * sx) / den; u = ((c.x - a.x) * rz - (c.z - a.z) * rx) / den;
        return t > 0 && t < 1 && u > 0 && u < 1;
    }
    static bool V2LineX(Vector3 a, Vector3 ua, Vector3 b, Vector3 ub, out float t, out float s)
    {
        float den = ua.x * ub.z - ua.z * ub.x; t = s = 0; if (Mathf.Abs(den) < .05f) return false;
        t = ((b.x - a.x) * ub.z - (b.z - a.z) * ub.x) / den; s = ((b.x - a.x) * ua.z - (b.z - a.z) * ua.x) / den; return true;
    }
    static Vector3 V2Dir(List<Vector3> p, bool start)
    {
        float acc = 0; int n = p.Count;
        for (int i = 1; i < n; i++) { int a = start ? i : n - 1 - i, b = start ? 0 : n - 1; acc += Fl(p[start ? i : n - 1 - i] - p[start ? i - 1 : n - i]).magnitude; if (acc >= 4f || i == n - 1) return Fl(p[a] - p[b]).normalized; }
        return Vector3.forward;
    }
    static List<Vector3> V2Resample(List<Vector3> p, float step)
    {
        float L = V2Len(p); int n = Mathf.Max(1, Mathf.RoundToInt(L / step)); float st = L / n; var o = new List<Vector3> { p[0] };
        int i = 1; float acc = 0, segStart = 0;
        for (int k = 1; k < n; k++)
        {
            float tgt = k * st;
            while (i < p.Count - 1 && segStart + Fl(p[i] - p[i - 1]).magnitude < tgt) { segStart += Fl(p[i] - p[i - 1]).magnitude; i++; }
            float sl = Fl(p[i] - p[i - 1]).magnitude; o.Add(Vector3.Lerp(p[i - 1], p[i], sl < 1e-6f ? 0 : (tgt - segStart) / sl));
        }
        o.Add(p[p.Count - 1]); _ = acc; return o;
    }
    static List<Vector3> V2DP(List<Vector3> p, float eps)
    {
        var keep = new bool[p.Count]; keep[0] = keep[p.Count - 1] = true; var st = new Stack<(int, int)>(); st.Push((0, p.Count - 1));
        while (st.Count > 0)
        {
            var (a, b) = st.Pop(); float bd = 0; int bi = -1;
            for (int i = a + 1; i < b; i++) { float d = V2SegE(p[a], p[b], p[i]); if (d > bd) { bd = d; bi = i; } }
            if (bi >= 0 && bd > eps) { keep[bi] = true; st.Push((a, bi)); st.Push((bi, b)); }
        }
        return p.Where((_, i) => keep[i]).ToList();
    }
    // 꺾인 점마다 원호(반경 R, 이웃 선분이 짧으면 두 접선 길이 합 ≤ 선분 길이가 되게 줄임 — 그 몫은 V2SmoothXZ 가 꼭짓점을 옮겨 채움)
    static List<Vector3> V2Arc(List<Vector3> K, float R)
    {
        int n = K.Count; if (n < 3) return K;
        var T = new float[n]; var phi = new float[n];
        for (int i = 1; i < n - 1; i++) { var a = Fl(K[i] - K[i - 1]).normalized; var b = Fl(K[i + 1] - K[i]).normalized; phi[i] = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1, 1)); T[i] = R * Mathf.Tan(Mathf.Min(phi[i], 2.9f) * .5f); }
        var sc = Enumerable.Repeat(1f, n).ToArray();
        for (int s = 0; s + 1 < n; s++) { float L = Fl(K[s + 1] - K[s]).magnitude, sum = T[s] + T[s + 1]; if (sum > L) { float f = L / sum; sc[s] = Mathf.Min(sc[s], f); sc[s + 1] = Mathf.Min(sc[s + 1], f); } }
        var o = new List<Vector3> { K[0] };
        for (int i = 1; i < n - 1; i++)
        {
            float Ti = T[i] * sc[i]; if (phi[i] < 1e-3f || Ti < .05f) { o.Add(K[i]); continue; }
            var a = Fl(K[i] - K[i - 1]).normalized; var b = Fl(K[i + 1] - K[i]).normalized; float cr = a.x * b.z - a.z * b.x;
            float r = Ti / Mathf.Tan(phi[i] * .5f); var S = K[i] - a * Ti; var nL = new Vector3(-a.z, 0, a.x); var C = S + nL * (cr > 0 ? r : -r);
            int steps = Mathf.Max(2, Mathf.CeilToInt(phi[i] * r / .5f));
            for (int k = 0; k <= steps; k++)
            {
                float ang = phi[i] * k / steps * (cr > 0 ? 1 : -1), cs = Mathf.Cos(ang), sn = Mathf.Sin(ang); var v = S - C;
                o.Add(new Vector3(C.x + v.x * cs - v.z * sn, K[i].y, C.z + v.x * sn + v.z * cs));
            }
        }
        o.Add(K[n - 1]); return o;
    }
    static void V2Fillet(V2L c)
    {
        var p = c.p; var fx = c.fix; fx[0] = fx[fx.Count - 1] = true;
        var np = new List<Vector3> { p[0] }; var nf = new List<bool> { true };
        for (int i = 0; i < p.Count - 1;)
        {
            if (fx[i + 1]) { np.Add(p[i + 1]); nf.Add(true); i++; continue; }
            int j = i + 1; while (!fx[j]) j++;
            var rs = V2Resample(V2Arc(V2DP(p.GetRange(i, j - i + 1), .4f), V2R), 1f);
            for (int m = 1; m < rs.Count; m++) { np.Add(rs[m]); nf.Add(m == rs.Count - 1); }
            i = j;
        }
        c.p = np; c.fix = nf;
    }
    static float[] V2Turn(List<Vector3> p)   // 점마다 1 m 당 방향 변화(rad/m)
    {
        var r = new float[p.Count];
        for (int i = 1; i < p.Count - 1; i++) { var d1 = Fl(p[i] - p[i - 1]); var d2 = Fl(p[i + 1] - p[i]); float l = (d1.magnitude + d2.magnitude) * .5f; if (l < 1e-4f) continue; r[i] = Vector3.Angle(d1, d2) * Mathf.Deg2Rad / l; }
        return r;
    }
    static void V2SmoothXZ(V2L c)
    {
        for (int it = 0; it < 600; it++)
        {
            var tr = V2Turn(c.p); var mark = new bool[c.p.Count]; bool any = false;
            for (int i = 1; i < c.p.Count - 1; i++) if (tr[i] > 1f / V2R) for (int k = Mathf.Max(1, i - 4); k <= Mathf.Min(c.p.Count - 2, i + 4); k++) if (!c.fix[k]) { mark[k] = true; any = true; }
            if (!any) break;
            var q = c.p.ToList();
            for (int i = 1; i < c.p.Count - 1; i++) if (mark[i]) { var m = (q[i - 1] + q[i + 1]) * .5f; c.p[i] = new Vector3(Mathf.Lerp(q[i].x, m.x, .5f), q[i].y, Mathf.Lerp(q[i].z, m.z, .5f)); }
            if (it % 10 == 9)
            {
                var np = new List<Vector3> { c.p[0] }; var nf = new List<bool> { true };
                for (int i = 0; i < c.p.Count - 1;)
                {
                    if (c.fix[i + 1]) { np.Add(c.p[i + 1]); nf.Add(true); i++; continue; }
                    int j = i + 1; while (!c.fix[j]) j++;
                    var rs = V2Resample(c.p.GetRange(i, j - i + 1), 1f); for (int m = 1; m < rs.Count; m++) { np.Add(rs[m]); nf.Add(m == rs.Count - 1); }
                    i = j;
                }
                c.p = np; c.fix = nf;
            }
        }
    }
    static bool V2YPin(V2L c, int i) => c.fix[i] && !V2In(c.p[i]) && i > 0 && i < c.p.Count - 1;
    static void V2Profile(V2L c, float y0, float y1, System.Func<float, float, float> G)
    {
        int n = c.p.Count; var y = new float[n]; var flat = new bool[n]; float L = V2Len(c.p), acc = 0;
        for (int i = 0; i < n; i++) { if (i > 0) acc += Fl(c.p[i] - c.p[i - 1]).magnitude; flat[i] = (c.d0 > 0 && acc <= c.d0 + .5f) || (c.d1 > 0 && L - acc <= c.d1 + .5f); }
        for (int i = 0; i < n; i++)
        {
            if (i == 0) { y[i] = y0; continue; } if (i == n - 1) { y[i] = y1; continue; }
            if (flat[i]) { y[i] = (c.d0 > 0 && acc >= 0 && i < n / 2) ? y0 : (c.d1 > 0 ? y1 : y0); continue; }
            if (V2YPin(c, i)) { y[i] = c.p[i].y; continue; }
            var f = Fl(c.p[i + 1] - c.p[i - 1]).normalized; var rt = new Vector3(f.z, 0, -f.x) * c.w * .4f; float s = 0; int k = 0;
            foreach (var q in new[] { c.p[i], c.p[i] + rt, c.p[i] - rt }) { float g = G(q.x, q.z); if (!float.IsNaN(g)) { s += g; k++; } }
            y[i] = k > 0 ? s / k : c.p[i].y;
        }
        bool Free(int i) => i > 0 && i < n - 1 && !V2YPin(c, i) && !flat[i];
        void Grade() { for (int pass = 0; pass < 3; pass++) { for (int i = 1; i < n; i++) if (Free(i)) { float d = Fl(c.p[i] - c.p[i - 1]).magnitude * V2Grade; y[i] = Mathf.Clamp(y[i], y[i - 1] - d, y[i - 1] + d); } for (int i = n - 2; i >= 0; i--) if (Free(i)) { float d = Fl(c.p[i] - c.p[i + 1]).magnitude * V2Grade; y[i] = Mathf.Clamp(y[i], y[i + 1] - d, y[i + 1] + d); } } }
        for (int pass = 0; pass < 50; pass++) { var o = (float[])y.Clone(); for (int i = 1; i < n - 1; i++) if (Free(i)) y[i] = (o[i - 1] + 2 * o[i] + o[i + 1]) * .25f; }
        Grade();
        for (int it = 0; it < 300; it++)
        {
            bool any = false; var o = (float[])y.Clone();
            for (int i = 1; i < n - 1; i++) if (Free(i) && Mathf.Abs(o[i - 1] - 2 * o[i] + o[i + 1]) > V2D2) { y[i] = Mathf.Lerp(o[i], (o[i - 1] + o[i + 1]) * .5f, .7f); any = true; }
            if (!any) break; if (it % 20 == 19) Grade();
        }
        Grade();
        for (int i = 0; i < n; i++) c.p[i] = new Vector3(c.p[i].x, y[i], c.p[i].z);
    }
    // 2차 실행: 고정점(마디·평탄·구역 밖)끼리 맞붙은 곳에서 228~339% 가 남음 → 자른 뒤 끝점·구역 밖만 고정하고 구배를 한 번 더 끼운다(평탄 구간도 따라 움직임)
    static void V2FinalGrade(V2L c, bool dead0 = false, bool dead1 = false)
    {
        // 1c: 막힌 끝 직전까지 17~24% 로 오르다 끝에서 멈추면, 끝을 사이에 둔 2 m 격자 정점 두 줄(끝 1 m 앞 = −0.24, 끝 너머 = 평평)의
        //  선형 보간이 길보다 ~0.2 m 낮아 띠 끝이 뜸(ridge_up_b_s0_2 +0.20·ridge_up_c_1 +0.24) → 막힌 끝 2.5 m 는 끝 높이로 평탄(착지)·고정
        float L = V2Len(c.p), F = Mathf.Min(V2Land, L * .3f);
        void Land(bool st)
        {
            if (F < .5f || !V2In(st ? c.p[0] : c.p[c.p.Count - 1])) return;
            if (!st) c.p.Reverse();
            float acc = 0, y0 = c.p[0].y;
            for (int i = 1; i < c.p.Count; i++)
            {
                float l = Fl(c.p[i] - c.p[i - 1]).magnitude;
                if (acc + l > F) { if (acc + l - F > .3f && F - acc > .3f) c.p.Insert(i, Vector3.Lerp(c.p[i - 1], c.p[i], (F - acc) / l)); break; }
                acc += l;
            }
            acc = 0; for (int i = 1; i < c.p.Count; i++) { acc += Fl(c.p[i] - c.p[i - 1]).magnitude; if (acc > F + .01f) break; c.p[i] = new Vector3(c.p[i].x, y0, c.p[i].z); }
            if (!st) c.p.Reverse();
        }
        if (dead0) Land(true); if (dead1) Land(false); c.fix = c.p.Select(q => !V2In(q)).ToList();
        int n = c.p.Count; var land = new bool[n]; { float a = 0; for (int i = 0; i < n; i++) { if (i > 0) a += Fl(c.p[i] - c.p[i - 1]).magnitude; land[i] = (dead0 && a <= F + .01f) || (dead1 && L - a <= F + .01f); } }
        bool Fx(int i) => i == 0 || i == n - 1 || land[i] || !V2In(c.p[i]);
        // 3차 실행: 양 끝 고정 높이차가 길이×11% 를 넘으면(능선 오르막 64 m 에 15 m) 해가 없어 마지막 패스 쪽 끝에 계단 339% → 고정점 사이 구간은 필요한 구배로 고르게
        var gl = Enumerable.Repeat(V2Grade, n).ToArray(); int a0 = 0; float run = 0;
        for (int i = 1; i < n; i++)
        {
            run += Fl(c.p[i] - c.p[i - 1]).magnitude;
            if (!Fx(i)) continue;
            float req = Mathf.Abs(c.p[i].y - c.p[a0].y) / Mathf.Max(.3f, run) * 1.02f;
            for (int k = a0 + 1; k <= i; k++) gl[k] = Mathf.Max(V2Grade, req);
            a0 = i; run = 0;
        }
        void Pass()
        {
            for (int i = 1; i < n; i++) if (!Fx(i)) { float d = Fl(c.p[i] - c.p[i - 1]).magnitude * gl[i]; var q = c.p[i]; q.y = Mathf.Clamp(q.y, c.p[i - 1].y - d, c.p[i - 1].y + d); c.p[i] = q; }
            for (int i = n - 2; i >= 0; i--) if (!Fx(i)) { float d = Fl(c.p[i] - c.p[i + 1]).magnitude * gl[i + 1]; var q = c.p[i]; q.y = Mathf.Clamp(q.y, c.p[i + 1].y - d, c.p[i + 1].y + d); c.p[i] = q; }
        }
        for (int pass = 0; pass < 4; pass++) Pass();
        for (int it = 0; it < 40; it++) { var o = c.p.Select(q => q.y).ToArray(); for (int i = 1; i < n - 1; i++) if (!Fx(i)) c.p[i] = new Vector3(c.p[i].x, (o[i - 1] + 2 * o[i] + o[i + 1]) * .25f, c.p[i].z); }
        for (int pass = 0; pass < 4; pass++) Pass();
    }
    const float V2Land = 2.5f;   // 막힌 끝 착지(평탄) 길이 m
    static void V2FlatEnd(V2L c, bool start, float ny)
    {
        float d = start ? c.d0 : c.d1; if (d <= 0) return; float acc = 0; int n = c.p.Count;
        for (int k = 0; k < n; k++)
        {
            int i = start ? k : n - 1 - k; if (k > 0) acc += Fl(c.p[i] - c.p[start ? i - 1 : i + 1]).magnitude;
            if (acc > d + 6f) break; float t = Mathf.Clamp01((acc - d) / 6f); t = t * t * (3 - 2 * t);
            c.p[i] = new Vector3(c.p[i].x, Mathf.Lerp(ny, c.p[i].y, t), c.p[i].z);
        }
    }
    static void V2Trim(V2L c)
    {
        List<Vector3> Cut(List<Vector3> p, float d)
        {
            if (d <= 0) return p; float acc = 0;
            for (int i = 1; i < p.Count; i++) { float l = Fl(p[i] - p[i - 1]).magnitude; if (acc + l >= d) { var q = Vector3.Lerp(p[i - 1], p[i], (d - acc) / Mathf.Max(1e-6f, l)); var o = new List<Vector3> { q }; if ((p[i] - q).magnitude > .3f) o.Add(p[i]); o.AddRange(p.Skip(i + 1)); return o; } acc += l; }
            return p;
        }
        float L = V2Len(c.p); if (c.d0 + c.d1 > L - 1f) { float f = (L - 1f) / Mathf.Max(1e-3f, c.d0 + c.d1); c.d0 *= f; c.d1 *= f; }
        var p1 = Cut(c.p, c.d0); p1.Reverse(); p1 = Cut(p1, c.d1); p1.Reverse();
        if (p1.Count < 2) p1 = new List<Vector3> { c.p[0], c.p[c.p.Count - 1] };
        c.p = p1; c.fix = c.p.Select(q => !V2In(q)).ToList();
    }
    static List<Vector3> V2BandVerts(List<Vector3> pts, float h, float lift)   // Ribbon 의 Band 와 같은 식(끝줄 정점을 갈림목이 그대로 씀)
    {
        var v = new List<Vector3>(); int n = pts.Count;
        for (int i = 0; i < n; i++) { var f = Fl(pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized; var rt = new Vector3(f.z, 0, -f.x); var c = pts[i] + Vector3.up * lift; v.Add(c - rt * h); v.Add(c + rt * h); }
        return v;
    }
    static GameObject V2MakeRoad(Transform parent, string name, List<Vector3> v, object _, Material mat, List<Mesh> meshes, bool collider = true)
    {
        int n = v.Count / 2; var t = new List<int>();
        for (int i = 1; i < n; i++) { int a = (i - 1) * 2; t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
        var vis = new List<Vector3>(v); var tv = new List<int>(t); int b0 = vis.Count;
        for (int i = 0; i < n; i++) { vis.Add(v[i * 2]); vis.Add(v[i * 2] - Vector3.up * .6f); vis.Add(v[i * 2 + 1]); vis.Add(v[i * 2 + 1] - Vector3.up * .6f); }
        for (int i = 1; i < n; i++) { int a = b0 + (i - 1) * 4, b = b0 + i * 4; foreach (int s0 in new[] { 0, 2 }) tv.AddRange(new[] { a + s0, a + s0 + 1, b + s0, b + s0, a + s0 + 1, b + s0 + 1, a + s0, b + s0, a + s0 + 1, a + s0 + 1, b + s0, b + s0 + 1 }); }
        return V2Go(parent, name, vis, tv, collider ? v : null, collider ? t : null, mat, meshes);
    }
    static GameObject V2Go(Transform parent, string name, List<Vector3> vis, List<int> tv, List<Vector3> cv, List<int> ct, Material mat, List<Mesh> meshes)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.isStatic = true;
        var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(vis); m.SetTriangles(tv, 0); m.RecalculateNormals(); m.RecalculateBounds(); meshes.Add(m);
        go.AddComponent<MeshFilter>().sharedMesh = m; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (cv != null) { var cm = new Mesh { name = name + "_col", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; cm.SetVertices(cv); cm.SetTriangles(ct, 0); cm.RecalculateNormals(); cm.RecalculateBounds(); meshes.Add(cm); go.AddComponent<MeshCollider>().sharedMesh = cm; }
        return go;
    }
    static void V2MakeJunction(Transform parent, string name, Vector3 center, List<Vector3> poly, List<bool> cut, V2L main, List<Mesh> meshes, List<(int obj, Vector3 a, Vector3 b, Vector3 c)> topTris, int obj)
    {
        var v = new List<Vector3> { center }; v.AddRange(poly); var t = new List<int>(); int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % n;
            if (Vector3.Cross(v[a] - center, v[b] - center).y > 0) { t.AddRange(new[] { 0, a, b }); topTris.Add((obj, center, v[a], v[b])); }
            else { t.AddRange(new[] { 0, b, a }); topTris.Add((obj, center, v[b], v[a])); }
        }
        var vis = new List<Vector3>(v); var tv = new List<int>(t);
        for (int i = 0; i < n; i++)
        {
            if (cut[i]) continue;   // 띠와 맞닿은 자른 변(B→A)에는 치마 없음
            var a = poly[i]; var b = poly[(i + 1) % n]; int k = vis.Count; vis.AddRange(new[] { a, a - Vector3.up * .6f, b, b - Vector3.up * .6f });
            tv.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3, k, k + 2, k + 1, k + 1, k + 2, k + 3 });
        }
        var go = V2Go(parent, name, vis, tv, v, t, main.mat, meshes);
        var rsf = go.AddComponent<RoadSurface>(); rsf.roadId = "v2_" + name; rsf.kind = main.kind; rsf.roughness = main.rough; rsf.width = main.w; rsf.centerline = new[] { center, center + Vector3.forward * .1f }; rsf.closed = false;
    }
    static float V2FanY(Vector3 c, List<Vector3> poly, Vector3 p, float dflt)   // 가운데 c 와 경계 변 (a,b) 로 된 부채꼴 삼각형 중 p 를 담은 것의 높이
    {
        for (int k = 0; k < poly.Count; k++)
        {
            var a = poly[k]; var b = poly[(k + 1) % poly.Count];
            float d = (a.x - c.x) * (b.z - c.z) - (b.x - c.x) * (a.z - c.z); if (Mathf.Abs(d) < 1e-6f) continue;
            float u = ((p.x - c.x) * (b.z - c.z) - (b.x - c.x) * (p.z - c.z)) / d, v = ((a.x - c.x) * (p.z - c.z) - (p.x - c.x) * (a.z - c.z)) / d;
            if (u >= -1e-4f && v >= -1e-4f && u + v <= 1.0001f) return c.y + u * (a.y - c.y) + v * (b.y - c.y);
        }
        return dflt;
    }
    static float V2PolyE(List<Vector3> poly, Vector3 p, out float yb)   // 안 = −(경계까지 거리), 밖 = +거리. yb = 가장 가까운 경계점 높이
    {
        bool inside = false; float bd = 1e9f; yb = poly[0].y; int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var a = poly[j]; var b = poly[i];
            if (((b.z > p.z) != (a.z > p.z)) && (p.x < (a.x - b.x) * (p.z - b.z) / (a.z - b.z + 1e-9f) + b.x)) inside = !inside;
            var ab = Fl(b - a); float t = Mathf.Clamp01(Vector3.Dot(Fl(p - a), ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); var q = Vector3.Lerp(a, b, t); float d = Fl(p - q).magnitude;
            if (d < bd) { bd = d; yb = q.y; }
        }
        return inside ? -bd : bd;
    }
    static List<(Matrix4x4 w2l, Vector3 mn, Vector3 mx, float H)> V2Pads(Transform top)
    {
        var o = new List<(Matrix4x4, Vector3, Vector3, float)>(); var scn = top.gameObject.scene;
        foreach (var r in scn.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(false))
        {
            if (t.name.StartsWith("ROAD")) continue;
            bool inGroup = t.parent != null && t.parent.name.EndsWith("Buildings");
            bool pref = PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && t.root.name.StartsWith("TownV6") && (t.parent == null || !t.parent.name.EndsWith("Buildings"));
            if (!inGroup && !pref) continue;
            var l2w = Matrix4x4.TRS(t.position, t.rotation, Vector3.one); var w2l = l2w.inverse; var mn = Vector3.one * 1e9f; var mx = -mn; var wmn = mn; var wmx = mx; bool any = false;
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(false))
            {
                var rr = mf.GetComponent<Renderer>(); if (mf.sharedMesh == null || rr == null || !rr.enabled) continue; var b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++) { var w = mf.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)); var l = w2l.MultiplyPoint3x4(w); mn = Vector3.Min(mn, l); mx = Vector3.Max(mx, l); wmn = Vector3.Min(wmn, w); wmx = Vector3.Max(wmx, w); any = true; }
            }
            if (!any || wmx.x - wmn.x > 60f || wmx.z - wmn.z > 60f) continue;
            var cw = (wmn + wmx) * .5f; if (V2Out(cw) > 20f) continue;
            o.Add((w2l, mn, mx, l2w.MultiplyPoint3x4(new Vector3(0, mn.y, 0)).y));
        }
        return o;
    }
    static string V2Stats(V2L c)
    {
        var tr = V2Turn(c.p); float mt = 0; for (int i = 2; i < c.p.Count - 2; i++) if (V2In(c.p[i])) mt = Mathf.Max(mt, tr[i]);
        float g = 0, d2 = 0; for (int i = 1; i < c.p.Count; i++) { float l = Fl(c.p[i] - c.p[i - 1]).magnitude; if (l > .3f && V2In(c.p[i])) g = Mathf.Max(g, Mathf.Abs(c.p[i].y - c.p[i - 1].y) / l); if (i + 1 < c.p.Count && V2In(c.p[i])) d2 = Mathf.Max(d2, Mathf.Abs(c.p[i - 1].y - 2 * c.p[i].y + c.p[i + 1].y)); }
        return $"  {V2Short(c.id)}: {V2Len(c.p):F0}m 폭{c.w} 최소반경 {(mt > 0 ? 1f / mt : 999f):F1}m 1m당 최대 {mt * Mathf.Rad2Deg:F1}° 구배 {g * 100:F1}% Δ² {d2:F3}";
    }
    static Dictionary<string, int> v2OverAt = new Dictionary<string, int>();
    static int V2Overlaps(List<(int obj, Vector3 a, Vector3 b, Vector3 c)> tris, out int triN)
    {
        v2OverAt.Clear();
        var grid = new Dictionary<(int, int), List<int>>(); var involved = new HashSet<int>(); int pairs = 0; var seen = new HashSet<long>();
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i]; float x0 = Mathf.Min(t.a.x, Mathf.Min(t.b.x, t.c.x)), x1 = Mathf.Max(t.a.x, Mathf.Max(t.b.x, t.c.x)), z0 = Mathf.Min(t.a.z, Mathf.Min(t.b.z, t.c.z)), z1 = Mathf.Max(t.a.z, Mathf.Max(t.b.z, t.c.z));
            for (int gx = Mathf.FloorToInt(x0 / 4); gx <= Mathf.FloorToInt(x1 / 4); gx++) for (int gz = Mathf.FloorToInt(z0 / 4); gz <= Mathf.FloorToInt(z1 / 4); gz++) { if (!grid.TryGetValue((gx, gz), out var l)) grid[(gx, gz)] = l = new List<int>(); l.Add(i); }
        }
        Vector2[] Sh(Vector3 a, Vector3 b, Vector3 c) { var g = (a + b + c) / 3f; Vector2 S(Vector3 p) { var d = Fl(p - g); float m = d.magnitude; var q = m > .03f ? p - d / m * .03f : g; return new Vector2(q.x, q.z); } return new[] { S(a), S(b), S(c) }; }
        bool Sep(Vector2[] A, Vector2[] B) { foreach (var P in new[] { A, B }) for (int i = 0; i < 3; i++) { var e = P[(i + 1) % 3] - P[i]; var nn = new Vector2(-e.y, e.x); float a0 = 1e9f, a1 = -1e9f, b0 = 1e9f, b1 = -1e9f; foreach (var q in A) { float d = Vector2.Dot(q, nn); a0 = Mathf.Min(a0, d); a1 = Mathf.Max(a1, d); } foreach (var q in B) { float d = Vector2.Dot(q, nn); b0 = Mathf.Min(b0, d); b1 = Mathf.Max(b1, d); } if (a1 < b0 || b1 < a0) return true; } return false; }
        foreach (var l in grid.Values) for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++)
        {
            var A = tris[l[i]]; var B = tris[l[j]]; if (A.obj == B.obj) continue; long key = (long)Mathf.Min(l[i], l[j]) * 1000000 + Mathf.Max(l[i], l[j]); if (!seen.Add(key)) continue;
            if (Mathf.Abs((A.a.y + A.b.y + A.c.y) - (B.a.y + B.b.y + B.c.y)) / 3f > 3f) continue;   // 3 m 넘게 다른 층
            if (!Sep(Sh(A.a, A.b, A.c), Sh(B.a, B.b, B.c))) { pairs++; involved.Add(l[i]); involved.Add(l[j]); var g = (A.a + A.b + A.c) / 3f; string key2 = $"({Mathf.Round(g.x / 5) * 5},{Mathf.Round(g.z / 5) * 5}) obj{A.obj}×{B.obj}"; v2OverAt[key2] = v2OverAt.GetValueOrDefault(key2) + 1; }
        }
        triN = involved.Count; return pairs;
    }
}
