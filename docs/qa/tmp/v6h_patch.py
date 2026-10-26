# 과제 0640 — BuildBayLoop.Gray.cs 를 v6h 로: 입력 json · profileFinal(높이 그대로) · 갈림목 기울어진 판 · 판 옆 띠 bank
import json, sys
P = 'F1/Assets/Editor/BuildBayLoop.Gray.cs'
s = open(P, encoding='utf-8').read()
print('profileFinal =', json.load(open('docs/map/town-v6h.json', encoding='utf8'))['v6e'].get('profileFinal'))

def rep(old, new, n=1):
    global s
    assert s.count(old) == n, (old[:60], s.count(old))
    s = s.replace(old, new)

rep('docs/map/town-v6b.json"));', 'docs/map/town-v6h.json"));   // 0640: v6b → v6h(높이 최종 · 갈림목 기울어진 판 · 판 옆 띠 bank)')

rep('        lg.Add(SmoothProfiles(rs));\n',
'''        // v6h(과제 0640): 합성기가 높이를 최종으로 풀었다(profileFinal) → 평면 완화·빌더 갈림목 판·경사 완화를 건너뛰고 json 판(junctions[])을 그대로 깐다
        var v6e = plan.TryGetValue("v6e", out var vo) ? vo as Dictionary<string, object> : null;
        bool final = v6e != null && v6e.TryGetValue("profileFinal", out var pf) && pf is bool pfb && pfb;
        lg.Add(SmoothProfiles(rs, final));
        plates.Clear(); banks.Clear();
        if (final && plan.TryGetValue("junctions", out var jo)) lg.Add(ReadPlates((List<object>)jo, rs));
''')

# 지형: 판 안 격자는 판 평면(노면 −4.5 cm)으로
rep('        // 걷는 길(골목·계단)은 지형 위로 묻히지 않게만\n',
'''        // v6h(과제 0640): 판 안 격자 = 판 평면(어깨와 같은 −4.5 cm)
        foreach (var pl in plates)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt((pl.minX - GX0) / GStep)), i1 = Mathf.Min(GN - 1, Mathf.CeilToInt((pl.maxX - GX0) / GStep));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((pl.minZ - GZ0) / GStep)), j1 = Mathf.Min(GN - 1, Mathf.CeilToInt((pl.maxZ - GZ0) / GStep));
            for (int i = i0; i <= i1; i++) for (int j = j0; j <= j1; j++)
            {
                float x = GX0 + i * GStep, z = GZ0 + j * GStep; if (!pl.In(x, z)) continue;
                gH[i, j] = pl.Y(x, z) + RoadColLift - .045f; best[i, j] = 0f;
            }
        }
        // 걷는 길(골목·계단)은 지형 위로 묻히지 않게만
''')

# 띠: 판 안 꼭짓점은 판 평면, 판 옆 bankFade 안은 bank
rep('            var go = Ribbon(roadsT, "ROAD_" + r.id, r.pts, r.width, lift, GM(key, c), r.Car);',
    '            var go = Ribbon(roadsT, "ROAD_" + r.id, r.pts, r.width, lift, GM(key, c), r.Car, r);')
rep('''        lg.Add($"길 {rs.Count}(차길 {carN}개 {carM / 1000f:F1}km)");
''', '''        lg.Add($"길 {rs.Count}(차길 {carN}개 {carM / 1000f:F1}km)");
        PlateMeshes(roadsT, lg);
''')
rep('static GameObject Ribbon(Transform parent, string name, List<Vector3> pts, float width, float lift, Material mat, bool collider)',
    'static GameObject Ribbon(Transform parent, string name, List<Vector3> pts, float width, float lift, Material mat, bool collider, GRoad road = null)')
rep('                v.Add(c - rt * h); v.Add(c + rt * h);',
    '                var vl = c - rt * h; var vr = c + rt * h; vl.y = VY(road, i, vl, h, lf); vr.y = VY(road, i, vr, -h, lf); v.Add(vl); v.Add(vr);   // 0640: 왼쪽 법선(−dz,dx) 쪽 = +')

# SmoothProfiles(final)
rep('    static string SmoothProfiles(List<GRoad> rs)\n', '    static string SmoothProfiles(List<GRoad> rs, bool final = false)\n')
rep('''        var car = rs.Where(r => r.Car && r.pts.Count >= 3).ToList();
        foreach (var r in car)
        {''', '''        var car = rs.Where(r => r.Car && r.pts.Count >= 3).ToList();
        int dropped = 0;
        foreach (var r in car)
        {''')
rep('''            q.Add(r.pts[r.pts.Count - 1]); r.pts = q;
''', '''            q.Add(r.pts[r.pts.Count - 1]); dropped += r.pts.Count - q.Count; r.pts = q;
            if (final) continue;   // v6h(과제 0640): 높이·평면은 합성기 값 그대로 — 0.3 m 보다 촘촘한 점(리본 꼬임)만 지운다
''')
rep('''        System.Func<List<Vector3>, float> brk = p =>''', '''        if (final) return $"높이 그대로(profileFinal) · 0.3 m 미만 촘촘한 점 {dropped}개 지움";
        System.Func<List<Vector3>, float> brk = p =>''')

# 새 코드: 판 읽기 · 높이 함수 · 판 메시
rep('''    static void DeadEndMarks(Transform top, List<DeadEnd> ends)''', '''    // ------------------------------------------------------------------ v6h(과제 0640): 갈림목 = json 의 기울어진 판
    /// 판 높이 y(x,z) = y + grad·(xz − pos). 판 안 차길 knots 는 이 평면 위에 있다(mapgen 0540).
    /// 팔 끝 edge 에서 띠 가로 높이 = 중심 + bank·오프셋(왼쪽 법선 +), outward 쪽으로 bankFade 동안 0 까지.
    class Plate
    {
        public string id; public Vector3 c; public Vector2 g; public List<Vector3> poly; public HashSet<string> roads; public float minX, maxX, minZ, maxZ;
        public float Y(float x, float z) => c.y + g.x * (x - c.x) + g.y * (z - c.z);
        public bool In(float x, float z)
        {
            if (x < minX || x > maxX || z < minZ || z > maxZ) return false;
            bool ins = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].z > z) != (poly[j].z > z) && x < (poly[j].x - poly[i].x) * (z - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x) ins = !ins;
            return ins;
        }
    }
    class Bank { public int i0, dir; public float bank, fade; }
    static readonly List<Plate> plates = new List<Plate>();
    static readonly Dictionary<GRoad, List<Bank>> banks = new Dictionary<GRoad, List<Bank>>();
    static readonly Dictionary<GRoad, float[]> cumLen = new Dictionary<GRoad, float[]>();

    static string ReadPlates(List<object> js, List<GRoad> rs)
    {
        var byId = rs.GroupBy(r => r.id).ToDictionary(g => g.Key, g => g.First()); cumLen.Clear();
        int nArm = 0, miss = 0;
        foreach (Dictionary<string, object> j in js)
        {
            var pos = (List<object>)j["pos"]; var gr = (List<object>)j["grad"];
            var pl = new Plate { id = (string)j["id"], c = new Vector3(F(pos[0]), F(j["y"]), F(pos[1])), g = new Vector2(F(gr[0]), F(gr[1])),
                poly = ((List<object>)j["poly"]).Cast<List<object>>().Select(p => new Vector3(F(p[0]), F(p[2]), F(p[1]))).ToList(),
                roads = new HashSet<string>(((List<object>)j["roads"]).Cast<string>()) };
            pl.minX = pl.poly.Min(p => p.x); pl.maxX = pl.poly.Max(p => p.x); pl.minZ = pl.poly.Min(p => p.z); pl.maxZ = pl.poly.Max(p => p.z);
            plates.Add(pl);
            float fade = j.TryGetValue("bankFade", out var bf) ? F(bf) : 8f;
            foreach (Dictionary<string, object> a in (List<object>)j["arms"])
            {
                nArm++;
                if (!byId.TryGetValue((string)a["road"], out var r)) { miss++; continue; }
                var e = (List<object>)a["edge"]; var ep = new Vector2(F(e[0]), F(e[1]));
                int bi = -1; float bd = 2f;   // edgeIndex 는 knots 번호 — 1 m 로 다시 깐 pts 와 안 맞아 edge 좌표로 찾는다
                for (int i = 0; i < r.pts.Count; i++) { float d = (new Vector2(r.pts[i].x, r.pts[i].z) - ep).magnitude; if (d < bd) { bd = d; bi = i; } }
                if (bi < 0) { miss++; continue; }
                if (!banks.TryGetValue(r, out var bl)) banks[r] = bl = new List<Bank>();
                bl.Add(new Bank { i0 = bi, dir = System.Convert.ToInt32(a["outward"]) >= 0 ? 1 : -1, bank = F(a["bank"]), fade = fade });
                if (!cumLen.ContainsKey(r)) { var cl = new float[r.pts.Count]; for (int i = 1; i < cl.Length; i++) cl[i] = cl[i - 1] + Fl(r.pts[i] - r.pts[i - 1]).magnitude; cumLen[r] = cl; }
            }
        }
        return $"갈림목 판 {plates.Count}(json) · 팔 {nArm} 중 bank 못 붙임 {miss}";
    }

    /// 띠 꼭짓점 높이: 그 길이 속한 판 안이면 판 평면, 판 옆 bankFade 안이면 중심 + bank·오프셋·(1 − s/fade)
    static float VY(GRoad r, int i, Vector3 v, float off, float lf)
    {
        if (r == null || plates.Count == 0) return v.y;
        float cy = r.pts[i].y;
        foreach (var p in plates) if (p.roads.Contains(r.id) && p.In(v.x, v.z)) { float py = p.Y(v.x, v.z); if (Mathf.Abs(py - cy) < 1.2f) return py + lf; }
        if (!banks.TryGetValue(r, out var bl)) return v.y;
        var cl = cumLen[r]; float bestS = 1e9f, add = 0f;
        foreach (var b in bl)
        {
            if ((i - b.i0) * b.dir < 0) continue;
            float s = Mathf.Abs(cl[i] - cl[b.i0]); if (s >= b.fade || s >= bestS) continue;
            bestS = s; add = b.bank * off * (1f - s / b.fade);
        }
        return v.y + add;
    }

    static void PlateMeshes(Transform parent, List<string> lg)
    {
        if (plates.Count == 0) return;
        var mat = GM("Village", new Color(.72f, .72f, .70f)); int n = 0;
        foreach (var pl in plates)
        {
            var poly = pl.poly.ToList(); if (poly.Count < 3) continue;
            var p2 = poly.Select(p => new Vector2(p.x, p.z)).ToList();
            if (Area(p2) < 0) { poly.Reverse(); p2.Reverse(); }
            var tris = EarClip(p2).ToList();
            Mesh M(float lf)
            {
                var m = new Mesh(); m.SetVertices(poly.Select(p => new Vector3(p.x, pl.Y(p.x, p.z) + lf, p.z)).ToList());
                var t = new List<int>(); foreach (var tr in tris) { t.Add(tr.x); t.Add(tr.z); t.Add(tr.y); }
                m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
            }
            var go = MeshGo(parent, "ROAD_PLATE_" + pl.id, M(.05f), mat, false);   // 이름 ROAD_ = 측정 도구가 노면으로 센다
            var cm = M(RoadColLift); cm.name = go.name + "_col"; string mp = grayMatDir + "/Mesh_" + cm.name + ".asset"; AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(cm, mp);
            go.AddComponent<MeshCollider>().sharedMesh = cm; n++;
        }
        lg.Add($"판 메시 {n}");
    }

    static void DeadEndMarks(Transform top, List<DeadEnd> ends)''')
open(P, 'w', encoding='utf-8').write(s)
print('patched')
