p='F1/Assets/Editor/RoadNetV2.cs'
s=open(p,encoding='utf-8').read()
def R(a,b,cnt=1):
    global s
    assert s.count(a)==cnt,(a[:80],s.count(a)); s=s.replace(a,b)
R('''    public static void MenuRoadNetV2() { Debug.Log("[RoadNetV2] " + RoadNetV2Build(150, -130, 330, -20, 15)); }''',
'''    public static void MenuRoadNetV2() { Debug.Log("[RoadNetV2] " + RoadNetV2Build(150, -130, 330, -20, 15)); }
    // 2단계(과제 1250, 0c17·4100): 구역 경계에서 v2 띠와 옛 길이 같은 높이로 겹쳐 깊이 싸움·땅 끼임 → 구역을 차길 전부를 담는 사각형으로 넓혀 경계를 없앤다
    [MenuItem("PitCrew/Map/TownV6 — 길 생성 v2 (맵 전체 2단계)")]
    public static void MenuRoadNetV2All() { Debug.Log("[RoadNetV2] " + RoadNetV2BuildAll()); }
    public static string RoadNetV2BuildAll()
    {
        var top = GameObject.Find("TownV6_Gray")?.transform; if (top == null) return "TownV6_Gray 없음";
        var ps = top.Find("Roads").GetComponentsInChildren<RoadSurface>(true).Where(r => r.centerline != null).SelectMany(r => r.centerline).ToList();
        return RoadNetV2Build(ps.Min(q => q.x) - 10f, ps.Min(q => q.z) - 10f, ps.Max(q => q.x) + 10f, ps.Max(q => q.z) + 10f, 15);
    }
    static Vector4 V2BB(List<Vector3> p, float pad) { float a = 1e9f, b = 1e9f, c = -1e9f, d = -1e9f; foreach (var q in p) { a = Mathf.Min(a, q.x); b = Mathf.Min(b, q.z); c = Mathf.Max(c, q.x); d = Mathf.Max(d, q.z); } return new Vector4(a - pad, b - pad, c + pad, d + pad); }
    static bool V2BBX(Vector4 A, Vector4 B) => A.x <= B.z && B.x <= A.z && A.y <= B.w && B.y <= A.w;
    static bool V2BBIn(Vector4 A, Vector3 q) => q.x >= A.x && q.x <= A.z && q.z >= A.y && q.z <= A.w;''')
R('''        var disabled = new List<string>(); var meshes = new List<Mesh>();
        try''','''        var disabled = new List<string>(); var meshes = new List<Mesh>();
        var sw = System.Diagnostics.Stopwatch.StartNew(); void Tk(string st) { Debug.Log($"[RoadNetV2] {sw.Elapsed.TotalSeconds:F1}s {st}"); lg.Add($"⏱ {sw.Elapsed.TotalSeconds:F1}s {st}"); }
        try''')
R('''                L.fix = L.p.Select(q => !V2In(q)).ToList(); cars.Add(L); disabled.Add(rs.name);
            }''','''                L.fix = L.p.Select(q => !V2In(q)).ToList(); cars.Add(L); disabled.Add(rs.name);
            }
            // 2단계: 같은 종류(kind)는 재질 하나 — 길이로 가장 많이 쓰인 옛 재질. 종류가 바뀌는 곳은 갈림목(넓은 길 재질)에서만 바뀐다
            var kindMat = cars.GroupBy(c => c.kind).ToDictionary(g => g.Key, g => g.GroupBy(c => c.mat).OrderByDescending(m => m.Sum(c => V2Len(c.p))).First().Key);
            foreach (var c in cars) c.mat = kindMat[c.kind];
            lg.Add("종류별 재질: " + string.Join(", ", kindMat.Select(kv => kv.Key + "=" + (kv.Value != null ? kv.Value.name : "null"))));
            Tk($"원본 길 {cars.Count}");''')
R('''                var e = cars[k]; bool cut = false;
                foreach (var f in cars)
                {
                    if (f == e || f.w <= e.w) continue;''','''                var e = cars[k]; bool cut = false; var eb = V2BB(e.p, 0);
                foreach (var f in cars)
                {
                    if (f == e || f.w <= e.w || !V2BBX(eb, V2BB(f.p, f.w))) continue;''')
R('''                for (int o = 0; o < cars.Count; o++) { if (o == k) continue; var c = cars[o].p;''','''                for (int o = 0; o < cars.Count; o++) { if (o == k || !V2BBIn(V2BB(cars[o].p, cars[o].w * .5f + 2.5f), q)) continue; var c = cars[o].p;''')
R('''                var pa = cars[a].p; var pb = cars[b].p;
''','''                var pa = cars[a].p; var pb = cars[b].p; if (!V2BBX(V2BB(pa, .5f), V2BB(pb, .5f))) continue;
''')
R('''                    int inn = e.p.Count(q => edges.Any(f => f != e && !f.dead && V2Dist''','''                    var eb = V2BB(e.p, 0); var near = edges.Where(f => f != e && !f.dead && V2BBX(eb, V2BB(f.p, f.w))).ToList();
                    int inn = e.p.Count(q => near.Any(f => V2Dist''')
R('''                var live = edges.Where(x => !x.dead).ToList();
                foreach (var e in live) foreach (var f in live)
                {
                    if (e == f || e.dead || f.dead || V2Len(e.p) > V2Len(f.p)) continue;''','''                var live = edges.Where(x => !x.dead).ToList(); var lbb = live.ToDictionary(x => x, x => V2BB(x.p, x.w));
                foreach (var e in live) foreach (var f in live)
                {
                    if (e == f || e.dead || f.dead || !V2BBX(lbb[e], lbb[f]) || V2Len(e.p) > V2Len(f.p)) continue;''')
R('''                if (e == f || e.w > f.w) continue; int k = 0, i0 = -1, i1 = -1; float dyMax = 0;''','''                if (e == f || e.w > f.w || !V2BBX(V2BB(e.p, 0), V2BB(f.p, f.w))) continue; int k = 0, i0 = -1, i1 = -1; float dyMax = 0;''')
R('''            lg.Add($"그래프: 길 {cars.Count}''','''            Tk("그래프"); lg.Add($"그래프: 길 {cars.Count}''')
R('''            // ── 띠 메시 ────''','''            Tk("평면·높이·끝 자름");
            // ── 띠 메시 ────''')
R('''            // ── 5 지형 ────''','''            Tk("띠·갈림목 메시");
            // ── 5 지형 ────''')
R('''            var pads = V2Pads(top);''','''            var pads = V2Pads(top);
            // 2단계: 맵 전체(정점 15만 × 선분 1만+)는 그대로 돌면 수십 초 → 40 m 칸에 선분·갈림목·건물을 미리 나눠 담는다(원래도 40 m 밖 선분은 건너뜀)
            var sGrid = new Dictionary<(int, int), List<int>>(); const float GC = 40f;
            void GAdd(Dictionary<(int, int), List<int>> g, int idx, float ax0, float az0, float ax1, float az1) { for (int gx = Mathf.FloorToInt(ax0 / GC); gx <= Mathf.FloorToInt(ax1 / GC); gx++) for (int gz = Mathf.FloorToInt(az0 / GC); gz <= Mathf.FloorToInt(az1 / GC); gz++) { if (!g.TryGetValue((gx, gz), out var l)) g[(gx, gz)] = l = new List<int>(); l.Add(idx); } }
            for (int i = 0; i < segs.Count; i++) { var s = segs[i]; GAdd(sGrid, i, Mathf.Min(s.a.x, s.b.x) - 40, Mathf.Min(s.a.z, s.b.z) - 40, Mathf.Max(s.a.x, s.b.x) + 40, Mathf.Max(s.a.z, s.b.z) + 40); }
            var jGrid = new Dictionary<(int, int), List<int>>(); for (int i = 0; i < jPolys.Count; i++) { var bb = V2BB(jPolys[i].poly, 40f); GAdd(jGrid, i, bb.x, bb.y, bb.z, bb.w); }
            var pGrid = new Dictionary<(int, int), List<int>>(); for (int i = 0; i < pads.Count; i++) { var pd = pads[i]; GAdd(pGrid, i, pd.c.x - pd.r - 16f, pd.c.z - pd.r - 16f, pd.c.x + pd.r + 16f, pd.c.z + pd.r + 16f); }
            var none = new List<int>();''')
R('''                foreach (var pd in pads) { var dl''','''                var cell = (Mathf.FloorToInt(p.x / GC), Mathf.FloorToInt(p.z / GC));
                foreach (int pi in pGrid.TryGetValue(cell, out var pl) ? pl : none) { var pd = pads[pi]; var dl''')
R('''                foreach (var s in segs)
                {
                    if (p.x < Mathf.Min''','''                foreach (int si in sGrid.TryGetValue(cell, out var sl) ? sl : none)
                {
                    var s = segs[si];
                    if (p.x < Mathf.Min''')
R('''                for (int jp = 0; jp < jPolys.Count; jp++) { var (poly''','''                foreach (int jp in jGrid.TryGetValue(cell, out var jl) ? jl : none) { var (poly''')
R('''            lg.Add($"지형 정점 {moved}개''','''            Tk("지형"); lg.Add($"지형 정점 {moved}개''')
R('''            lg.Add($"걷는 길 조각 {pathN}''','''            Tk("걷는 길"); lg.Add($"걷는 길 조각 {pathN}''')
R('''            // ── 검사 ────''','''            Tk("옛 길 정리");
            // ── 검사 ────''')
R('''            AssetDatabase.CreateAsset(meshes[0], V2MeshAsset);''','''            Tk("검사"); AssetDatabase.CreateAsset(meshes[0], V2MeshAsset);''')
R('''    static List<(Matrix4x4 w2l, Vector3 mn, Vector3 mx, float H)> V2Pads(Transform top)
    {
        var o = new List<(Matrix4x4, Vector3, Vector3, float)>();''','''    static List<(Matrix4x4 w2l, Vector3 mn, Vector3 mx, float H, Vector3 c, float r)> V2Pads(Transform top)
    {
        var o = new List<(Matrix4x4, Vector3, Vector3, float, Vector3, float)>();''')
R('''            o.Add((w2l, mn, mx, l2w.MultiplyPoint3x4(new Vector3(0, mn.y, 0)).y));''','''            o.Add((w2l, mn, mx, l2w.MultiplyPoint3x4(new Vector3(0, mn.y, 0)).y, cw, Fl(wmx - wmn).magnitude * .5f));''')
open(p,'w',encoding='utf-8').write(s)
print("ok")
