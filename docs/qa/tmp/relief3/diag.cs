// 2253(D-231): 오목 블록 측정. 읽기만. 블록 = 길(RoadsV2 + TerraceBlock 진입로)이 아닌 1 m 칸 4-연결.
//  둘레 면 S = 둘레 길 점 최소제곱 평면 + (길 높이 − 평면)의 1/d⁴ 평균 (ParcelBuild.FillSunk 와 같은 식).
//  오목 블록 = 블록 안 칸 중 (지형 < S − 0.3 m) 이 10% 넘는 블록.
//  둑 띠 = 길에서 2~8 m 칸의 (S − 지형) 중앙값(블록마다).
//  건너뜀 표시: 바다(지형 < 0.6)·도현 건물(D198) 든 블록·계단 땅(TerraceBlock 중심 60 m).
//  결과 → docs/qa/tmp/relief3/concave_<tag>.json
Physics.SyncTransforms();
var T = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/relief3/");
string tag = "diag"; var want = new System.Collections.Generic.HashSet<int> { 16, 27 }; var img = new System.Collections.Generic.List<string>();
var top = GameObject.Find("TownV6_Gray").transform; var terr = top.Find("TownV6_Terrain");
var tcol = terr.GetComponent<MeshCollider>();
var rv = top.Find("RoadsV2"); var drive = top.Find("TerraceBlock/Driveway");
var roadCols = new System.Collections.Generic.HashSet<Collider>();
foreach (var c in rv.GetComponentsInChildren<Collider>(false)) roadCols.Add(c);
if (drive) foreach (var c in drive.GetComponentsInChildren<Collider>(false)) roadCols.Add(c);
var d198 = new System.Collections.Generic.List<Rect>();
var dB = top.Find("D198_Buildings"); if (dB) foreach (var r in dB.GetComponentsInChildren<Renderer>()) { var bb = r.bounds; d198.Add(Rect.MinMaxRect(bb.min.x, bb.min.z, bb.max.x, bb.max.z)); }
var tbC = new Vector2(185, -61);
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
var comp = new int[W * H]; for (int k = 0; k < comp.Length; k++) comp[k] = -1;
var stack = new System.Collections.Generic.Stack<int>(); int nb = 0;
var rows = new System.Collections.Generic.List<(float frac, string line)>();
int nMeas = 0, nConc = 0, nConcElig = 0, nElig = 0; var banks = new System.Collections.Generic.List<float>(); var banksElig = new System.Collections.Generic.List<float>();
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
    var rl = new System.Collections.Generic.List<int>(ring); int step = Mathf.Max(1, rl.Count / 240);
    var rs = new System.Collections.Generic.List<Vector3>(); for (int q = 0; q < rl.Count; q += step) { int k = rl[q]; if (!float.IsNaN(ry[k])) rs.Add(new Vector3(x0 + (k % W) * S, ry[k], z0 + (k / W) * S)); }
    if (rs.Count < 10) continue;
    // 평면
    double mx = 0, mz = 0, my = 0; foreach (var p in rs) { mx += p.x; mz += p.z; my += p.y; } mx /= rs.Count; mz /= rs.Count; my /= rs.Count;
    double sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0; foreach (var p in rs) { double dx = p.x - mx, dz = p.z - mz, dy = p.y - my; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; sxy += dx * dy; szy += dz * dy; }
    double det = sxx * szz - sxz * sxz; double pb = 0, pc = 0; if (System.Math.Abs(det) > 1e-6) { pb = (sxy * szz - szy * sxz) / det; pc = (szy * sxx - sxy * sxz) / det; } double pa = my - pb * mx - pc * mz;
    var res = rs.ConvertAll(r => (float)(r.y - (pa + pb * r.x + pc * r.z)));
    bool sea = false, dh = false; float sx = 0, sz = 0; int cstep = Mathf.Max(1, cells.Count / 2500), ns = 0, nLow = 0;
    var bank = new System.Collections.Generic.List<float>(); float worst = 0; Vector2 worstAt = Vector2.zero;
    for (int q = 0; q < cells.Count; q += cstep)
    {
        int k = cells[q]; float x = x0 + (k % W) * S, z = z0 + (k / W) * S; sx += x; sz += z; ns++;
        if (ty[k] < .6f) sea = true;
        var p2 = new Vector2(x, z); if (!dh) foreach (var r in d198) if (r.Contains(p2)) { dh = true; break; }
        double ws = 0, wy = 0; float dmin = 1e9f;
        for (int t = 0; t < rs.Count; t++) { float dx = rs[t].x - x, dz = rs[t].z - z, d2 = dx * dx + dz * dz + .25f; double w = 1.0 / (d2 * d2); ws += w; wy += w * res[t]; dmin = Mathf.Min(dmin, d2); }
        float sv = (float)(pa + pb * x + pc * z + wy / ws); float dd = ty[k] - sv; float dist = Mathf.Sqrt(dmin);
        if (dd < -.3f) { nLow++; if (want.Contains(id)) img.Add($"{id} {x:F0} {z:F0} {dd:F2} {dist:F1}"); } if (dd < worst) { worst = dd; worstAt = p2; }
        if (dist >= 2 && dist <= 8) bank.Add(-dd);
    }
    float frac = nLow / (float)ns; bank.Sort(); float bk = bank.Count > 0 ? bank[bank.Count / 2] : float.NaN;
    var ctr = new Vector2(sx / ns, sz / ns); bool terrace = (ctr - tbC).magnitude < 60;
    bool elig = !sea && !dh && !terrace; nMeas++; if (elig) nElig++;
    bool conc = frac > .10f; if (conc) { nConc++; if (elig) nConcElig++; }
    if (!float.IsNaN(bk)) { banks.Add(bk); if (elig) banksElig.Add(bk); }
    string why = sea ? "sea" : dh ? "dohyun" : terrace ? "terrace" : "";
    rows.Add((frac, $"{{\"id\":{id},\"cx\":{ctr.x:F1},\"cz\":{ctr.y:F1},\"area\":{area:F0},\"lowFrac\":{frac:F3},\"concave\":{(conc ? "true" : "false")},\"bankMed\":{bk:F2},\"worst\":{worst:F2},\"worstAt\":[{worstAt.x:F0},{worstAt.y:F0}],\"skip\":\"{why}\"}}"));
}
rows.Sort((p, q) => q.frac.CompareTo(p.frac));
var sb = new System.Text.StringBuilder(); sb.Append("[\n"); for (int r = 0; r < rows.Count; r++) sb.Append(rows[r].line).Append(r < rows.Count - 1 ? ",\n" : "\n"); sb.Append("]\n");
System.IO.File.WriteAllText(T + "diag_low.txt", string.Join("\n", img));
float Med(System.Collections.Generic.List<float> l) { if (l.Count == 0) return float.NaN; l.Sort(); return l[l.Count / 2]; }
int Over(System.Collections.Generic.List<float> l, float v) => l.FindAll(x => x > v).Count;
return $"{tag}: blocks {nMeas} (eligible {nElig}) concave {nConc} (eligible {nConcElig}) · bank med all {Med(banks):F2} elig {Med(banksElig):F2} · bank>0.3 all {Over(banks, .3f)} elig {Over(banksElig, .3f)} · bank max {(banks.Count > 0 ? banks[banks.Count - 1] : 0):F2}";
