// 과제 1935(D-222 지형 내려앉음): 길(RoadsV2 + TerraceBlock 진입로)로 둘러싸인 땅 블록마다
//  둘레 길 높이(IDW)로 기대 높이를 만들고, 지형(지금 메시 + 0433·v2·terrace1 단계)과 비교한다. 읽기만.
//  꺼진 블록 = 중앙값 결손 ≤ -1 m. 결과 → docs/qa/tmp/sink1935/blocks_<tag>.json
Physics.SyncTransforms();
string tag = "relief";
var top = GameObject.Find("TownV6_Gray").transform; var terr = top.Find("TownV6_Terrain");
var tcol = terr.GetComponent<MeshCollider>();
string F = "Assets/Scenes/Map_TownV6_Mats/";
var stageNames = new[] { "0433", "v2", "terrace1" };
var stageCols = new System.Collections.Generic.List<MeshCollider>();
var tmp = new System.Collections.Generic.List<GameObject>();
foreach (var sn in stageNames)
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(F + "Mesh_TownV6_Terrain_" + sn + ".asset");
    var go = new GameObject("_sink_" + sn) { hideFlags = HideFlags.HideAndDontSave }; go.transform.SetPositionAndRotation(terr.position, terr.rotation); go.transform.localScale = terr.lossyScale;
    var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = m; stageCols.Add(mc); tmp.Add(go);
}
Physics.SyncTransforms();
try
{
    var rv = top.Find("RoadsV2"); var drive = top.Find("TerraceBlock/Driveway");
    var roadCols = new System.Collections.Generic.HashSet<Collider>();
    foreach (var c in rv.GetComponentsInChildren<Collider>(false)) roadCols.Add(c);
    if (drive) foreach (var c in drive.GetComponentsInChildren<Collider>(false)) roadCols.Add(c);
    var b = terr.GetComponent<MeshRenderer>().bounds;
    const float S = 1f; float x0 = b.min.x + 3, z0 = b.min.z + 3; int W = (int)((b.size.x - 6) / S), H = (int)((b.size.z - 6) / S);
    var ty = new float[W * H]; var ry = new float[W * H]; var isRoad = new bool[W * H];
    var hits = new RaycastHit[32];
    for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
    {
        int k = j * W + i; float x = x0 + i * S, z = z0 + j * S; var ray = new Ray(new Vector3(x, 300, z), Vector3.down);
        ty[k] = tcol.Raycast(ray, out var th, 600) ? th.point.y : float.NaN; ry[k] = float.NaN;
        int n = Physics.RaycastNonAlloc(ray, hits, 600);
        for (int q = 0; q < n; q++) if (roadCols.Contains(hits[q].collider)) { if (float.IsNaN(ry[k]) || hits[q].point.y > ry[k]) ry[k] = hits[q].point.y; isRoad[k] = true; }
    }
    // 블록 = 길 아닌 칸 4-연결
    var comp = new int[W * H]; for (int k = 0; k < comp.Length; k++) comp[k] = -1;
    var sb = new System.Text.StringBuilder(); sb.Append("[\n"); int nb = 0, nSunk = 0; var stack = new System.Collections.Generic.Stack<int>();
    var rows = new System.Collections.Generic.List<(float med, string line)>();
    for (int s0 = 0; s0 < comp.Length; s0++)
    {
        if (isRoad[s0] || comp[s0] >= 0 || float.IsNaN(ty[s0])) continue;
        var cells = new System.Collections.Generic.List<int>(); bool edge = false; var ring = new System.Collections.Generic.HashSet<int>();
        comp[s0] = nb; stack.Push(s0);
        while (stack.Count > 0)
        {
            int k = stack.Pop(); cells.Add(k); int i = k % W, j = k / W;
            if (i == 0 || j == 0 || i == W - 1 || j == H - 1) edge = true;
            foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int a = i + di, c = j + dj; if (a < 0 || c < 0 || a >= W || c >= H) continue; int q = c * W + a;
                if (isRoad[q]) { ring.Add(q); continue; }
                if (comp[q] >= 0 || float.IsNaN(ty[q])) { if (float.IsNaN(ty[q])) edge = true; continue; }
                comp[q] = nb; stack.Push(q);
            }
        }
        int id = nb++;
        float area = cells.Count * S * S;
        if (edge || area < 150 || area > 40000 || ring.Count < 20) continue;
        // 둘레 표본 ≤ 240
        var rl = new System.Collections.Generic.List<int>(ring); int step = Mathf.Max(1, rl.Count / 240);
        var rs = new System.Collections.Generic.List<Vector3>(); for (int q = 0; q < rl.Count; q += step) { int k = rl[q]; rs.Add(new Vector3(x0 + (k % W) * S, ry[k], z0 + (k / W) * S)); }
        var def = new System.Collections.Generic.List<float>(); var defS = new System.Collections.Generic.List<float>[3]; for (int s = 0; s < 3; s++) defS[s] = new System.Collections.Generic.List<float>();
        float sx = 0, sz = 0; int cstep = Mathf.Max(1, cells.Count / 1500); float worst = 0; Vector2 worstAt = Vector2.zero;
        for (int q = 0; q < cells.Count; q += cstep)
        {
            int k = cells[q]; float x = x0 + (k % W) * S, z = z0 + (k / W) * S; sx += x; sz += z;
            double ws = 0, wy = 0; foreach (var r in rs) { float d2 = (r.x - x) * (r.x - x) + (r.z - z) * (r.z - z) + .25f; double w = 1.0 / (d2 * d2); ws += w; wy += w * r.y; }
            float exp = (float)(wy / ws); float d = ty[k] - exp; def.Add(d); if (d < worst) { worst = d; worstAt = new Vector2(x, z); }
            var ray = new Ray(new Vector3(x, 300, z), Vector3.down);
            for (int s = 0; s < 3; s++) defS[s].Add(stageCols[s].Raycast(ray, out var sh, 600) ? sh.point.y - exp : float.NaN);
        }
        float Med(System.Collections.Generic.List<float> l) { var a = l.FindAll(v => !float.IsNaN(v)); if (a.Count == 0) return float.NaN; a.Sort(); return a[a.Count / 2]; }
        int ns = def.Count; float med = Med(def); float frac = def.FindAll(v => v < -1f).Count / (float)ns;
        var ryl = rs.ConvertAll(r => r.y); ryl.Sort();
        if (med <= -1f) nSunk++;
        rows.Add((med, $"{{\"id\":{id},\"cx\":{sx / ns:F1},\"cz\":{sz / ns:F1},\"area\":{area:F0},\"medDef\":{med:F2},\"fracBelow1\":{frac:F2},\"worst\":{worst:F2},\"worstAt\":[{worstAt.x:F0},{worstAt.y:F0}],\"ringMed\":{ryl[ryl.Count / 2]:F2},\"ringMin\":{ryl[0]:F2},\"ringMax\":{ryl[ryl.Count - 1]:F2},\"def0433\":{Med(defS[0]):F2},\"defV2\":{Med(defS[1]):F2},\"defTerrace1\":{Med(defS[2]):F2} }}"));
    }
    rows.Sort((p, q) => p.med.CompareTo(q.med));
    for (int r = 0; r < rows.Count; r++) sb.Append(rows[r].line).Append(r < rows.Count - 1 ? ",\n" : "\n");
    sb.Append("]\n");
    var path = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/relief2147/blocks_" + tag + ".json");
    System.IO.File.WriteAllText(path, sb.ToString());
    return $"grid {W}x{H} blocks(all) {nb} measured {rows.Count} sunk(med<=-1m) {nSunk} -> {path}";
}
finally { foreach (var g in tmp) UnityEngine.Object.DestroyImmediate(g); }
