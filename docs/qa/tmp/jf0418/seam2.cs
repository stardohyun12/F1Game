// 갈림목 판 경계 변을 0.5 m 마다 표본 → 판 바깥 3 cm 지점의 가장 높은 표면(이 판·형제 판 제외, ±0.3 m 안)과 높이차.
// 양수 = 판이 바깥 표면보다 높음(바깥에서 들어올 때 턱). 바깥 표면 종류별로 나눔. 에디트·Play 둘 다 읽기만.
Physics.SyncTransforms();
var top = GameObject.Find("TownV6_Gray").transform; var v2 = top.Find("RoadsV2");
var sb = new System.Text.StringBuilder();
var stat = new System.Collections.Generic.Dictionary<string, (int n, int over, float mx, string at)>();
var rows = new System.Collections.Generic.List<(float mx, string line)>();
string Kind(string nm) => nm.StartsWith("ROAD_V2e") ? "band" : nm.StartsWith("ROAD_V2j") ? "plate" : nm.StartsWith("ROAD_V2p") ? "walk" : nm.Contains("Terrain") ? "terrain" : nm.StartsWith("ROAD_") ? "oldroad" : "other";
foreach (Transform c in v2)
{
    if (!c.name.StartsWith("ROAD_V2j_")) continue;
    string baseName = c.name.EndsWith("_l") ? c.name.Substring(0, c.name.Length - 2) : c.name;
    var m = c.GetComponent<MeshFilter>().sharedMesh; var vs = m.vertices; var ts = m.triangles; var tr = c.transform;
    var ec = new System.Collections.Generic.Dictionary<(int, int), int>(); var dir = new System.Collections.Generic.Dictionary<(int, int), (int, int)>();
    for (int i = 0; i < ts.Length; i += 3) for (int e = 0; e < 3; e++) { int a = ts[i + e], b = ts[i + (e + 1) % 3]; var k = a < b ? (a, b) : (b, a); ec[k] = ec.GetValueOrDefault(k) + 1; dir[k] = (a, b); }
    float jMax = -1, jMin = 1; Vector3 jAt = Vector3.zero; string jK = ""; int jOver = 0;
    foreach (var kv in ec)
    {
        if (kv.Value != 1) continue; var (ia, ib) = dir[kv.Key];
        var A = tr.TransformPoint(vs[ia]); var B = tr.TransformPoint(vs[ib]);
        var d = B - A; d.y = 0; var outN = new Vector3(d.z, 0, -d.x).normalized;   // 시계/반시계: 삼각형 위쪽 법선(y+) 기준 a→b 오른쪽 = 바깥
        int n = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / .5f));
        for (int s = 0; s <= n; s++)
        {
            var p = Vector3.Lerp(A, B, s / (float)n); var q = p + outN * .03f;
            var hs = Physics.RaycastAll(new Vector3(q.x, p.y + .3f, q.z), Vector3.down, .6f);
            float best = float.NaN; string bk = "";
            foreach (var h in hs)
            {
                var nm = h.collider.name; if (nm == c.name || nm == baseName || nm == baseName + "_l") continue;
                if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
                if (float.IsNaN(best) || h.point.y > best) { best = h.point.y; bk = Kind(nm); }
            }
            if (float.IsNaN(best)) continue;
            float step = p.y - best;
            var st = stat.GetValueOrDefault(bk); st.n++; if (step > .005f) st.over++; if (step > st.mx) { st.mx = step; st.at = $"{c.name}@({p.x:F1},{p.y:F2},{p.z:F1})"; } stat[bk] = st;
            if (bk == "band" || bk == "plate" || bk == "oldroad" || bk == "walk") { if (step > jMax) { jMax = step; jAt = p; jK = bk; } jMin = Mathf.Min(jMin, step); if (Mathf.Abs(step) > .005f) jOver++; }
        }
    }
    rows.Add((jMax, $"{c.name}\tmax+ {jMax * 100:F1}cm ({jK})\tmax- {jMin * 100:F1}cm\t|>5mm| {jOver}\t@({jAt.x:F1},{jAt.y:F2},{jAt.z:F1})"));
}
rows.Sort((a, b) => b.mx.CompareTo(a.mx));
foreach (var kv in stat) sb.AppendLine($"outside={kv.Key}: samples {kv.Value.n}, plate higher >5mm {kv.Value.over}, max {kv.Value.mx * 100:F1}cm at {kv.Value.at}");
sb.AppendLine("-- per plate (road-type neighbours only) --");
foreach (var r in rows) sb.AppendLine(r.line);
System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/seam2_out.txt"), sb.ToString());
var lines = sb.ToString().Split('\n'); return string.Join("\n", lines, 0, Mathf.Min(30, lines.Length));
