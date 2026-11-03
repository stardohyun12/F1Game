// 2147(D-228): 메운 블록 A(115,23)·B(112,-47) 높이 기복 측정. 1 m 격자, 길에서 3 m 넘게 떨어진 블록 칸만.
//  raw = 높이 표준편차, plane = 평면 맞춤 뺀 나머지 표준편차, hp = 높이 − 17 m 창 평균(작은 기복) 표준편차. 현재 지형 vs terrace1(원지형).
//  tag 는 맨 아래 파일 이름에만 쓰인다 — tag.txt 에서 읽는다.
Physics.SyncTransforms();
string tdir = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/relief2147/");
string tag = System.IO.File.ReadAllText(tdir + "tag.txt").Trim();
var top = GameObject.Find("TownV6_Gray").transform; var terr = top.Find("TownV6_Terrain"); var tcol = terr.GetComponent<MeshCollider>();
var m0 = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_terrace1.asset");
var go = new GameObject("_relief0") { hideFlags = HideFlags.HideAndDontSave }; go.transform.SetPositionAndRotation(terr.position, terr.rotation); go.transform.localScale = terr.lossyScale;
var oc = go.AddComponent<MeshCollider>(); oc.sharedMesh = m0; Physics.SyncTransforms();
var rv = top.Find("RoadsV2"); var roadCols = new System.Collections.Generic.HashSet<Collider>(rv.GetComponentsInChildren<Collider>(false));
var hits = new RaycastHit[32]; var sb = new System.Text.StringBuilder(); sb.Append("{ \"tag\": \"").Append(tag).Append("\", \"blocks\": [");
var names = new[] { "A", "B" }; var rects = new[] { new Rect(80, -10, 75, 70), new Rect(75, -85, 75, 75) }; var seeds = new[] { new Vector2(115, 23), new Vector2(112, -47) };
try {
for (int bi = 0; bi < 2; bi++)
{
    var R = rects[bi]; var seed = seeds[bi]; int W = (int)R.width, H = (int)R.height;
    var road = new bool[W * H]; var yc = new float[W * H]; var yo = new float[W * H];
    for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
    {
        int k = j * W + i; var ray = new Ray(new Vector3(R.x + i, 300, R.y + j), Vector3.down);
        yc[k] = tcol.Raycast(ray, out var h1, 600) ? h1.point.y : float.NaN; yo[k] = oc.Raycast(ray, out var h2, 600) ? h2.point.y : float.NaN;
        int n = Physics.RaycastNonAlloc(ray, hits, 600); for (int q = 0; q < n; q++) if (roadCols.Contains(hits[q].collider)) road[k] = true;
    }
    var dr = new int[W * H]; for (int k = 0; k < dr.Length; k++) dr[k] = road[k] ? 0 : 999;
    for (int it = 0; it < 4; it++) for (int j = 0; j < H; j++) for (int i = 0; i < W; i++) { int k = j * W + i; for (int dj = -1; dj <= 1; dj++) for (int di = -1; di <= 1; di++) { int a = i + di, c = j + dj; if (a < 0 || c < 0 || a >= W || c >= H) continue; dr[k] = Mathf.Min(dr[k], dr[c * W + a] + 1); } }
    var inb = new bool[W * H]; var st = new System.Collections.Generic.Stack<int>(); int s0 = (int)(seed.y - R.y) * W + (int)(seed.x - R.x); st.Push(s0); inb[s0] = true;
    var nb4 = new[] { 1, 0, -1, 0, 0, 1, 0, -1 };
    while (st.Count > 0) { int k = st.Pop(); int i = k % W, j = k / W; for (int e = 0; e < 4; e++) { int a = i + nb4[2 * e], c = j + nb4[2 * e + 1]; if (a < 0 || c < 0 || a >= W || c >= H) continue; int q = c * W + a; if (inb[q] || road[q]) continue; inb[q] = true; st.Push(q); } }
    System.Func<float[], string> Stat = (y) =>
    {
        var px = new System.Collections.Generic.List<float>(); var pz = new System.Collections.Generic.List<float>(); var py = new System.Collections.Generic.List<float>(); var ph = new System.Collections.Generic.List<float>();
        for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
        {
            int k = j * W + i; if (!inb[k] || dr[k] <= 3 || float.IsNaN(y[k])) continue;
            double s = 0; int c = 0; for (int dj = -8; dj <= 8; dj++) for (int di = -8; di <= 8; di++) { int a = i + di, b = j + dj; if (a < 0 || b < 0 || a >= W || b >= H) continue; int q = b * W + a; if (!inb[q] || float.IsNaN(y[q])) continue; s += y[q]; c++; }
            px.Add(i); pz.Add(j); py.Add(y[k]); ph.Add(y[k] - (float)(s / c));
        }
        int N = px.Count; double mx = 0, mz = 0, my = 0, hm = 0; for (int t = 0; t < N; t++) { mx += px[t]; mz += pz[t]; my += py[t]; hm += ph[t]; } mx /= N; mz /= N; my /= N; hm /= N;
        double sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0, syy = 0, hh = 0;
        for (int t = 0; t < N; t++) { double dx = px[t] - mx, dz = pz[t] - mz, dy = py[t] - my; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; sxy += dx * dy; szy += dz * dy; syy += dy * dy; hh += (ph[t] - hm) * (ph[t] - hm); }
        double det = sxx * szz - sxz * sxz, a1 = (sxy * szz - szy * sxz) / det, a2 = (szy * sxx - sxy * sxz) / det;
        double rr = 0; for (int t = 0; t < N; t++) { double r = (py[t] - my) - a1 * (px[t] - mx) - a2 * (pz[t] - mz); rr += r * r; }
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{ \"n\": {0}, \"mean\": {1:F2}, \"raw\": {2:F2}, \"plane\": {3:F2}, \"hp\": {4:F2}, \"slopePct\": {5:F1} }}", N, my, System.Math.Sqrt(syy / N), System.Math.Sqrt(rr / N), System.Math.Sqrt(hh / N), 100 * System.Math.Sqrt(a1 * a1 + a2 * a2));
    };
    sb.Append(bi > 0 ? ", " : "").Append("{ \"block\": \"").Append(names[bi]).Append("\", \"now\": ").Append(Stat(yc)).Append(", \"orig\": ").Append(Stat(yo)).Append(" }");
}
} finally { UnityEngine.Object.DestroyImmediate(go); }
sb.Append("] }");
System.IO.File.WriteAllText(tdir + "relief_" + tag + ".json", sb.ToString()); return sb.ToString();
