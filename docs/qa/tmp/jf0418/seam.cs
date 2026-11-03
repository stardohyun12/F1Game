// 갈림목 판(ROAD_V2j_*) 경계 변을 0.5 m 마다 표본 → 그 자리 V2e 띠 표면 높이와의 차(판 − 띠). 에디트 모드, 읽기만.
var top = GameObject.Find("TownV6_Gray").transform; var v2 = top.Find("RoadsV2");
var bandCols = new System.Collections.Generic.List<MeshCollider>();
foreach (Transform c in v2) if (c.name.StartsWith("ROAD_V2e_")) { var mc = c.GetComponent<MeshCollider>(); if (mc != null) bandCols.Add(mc); }
var sb = new System.Text.StringBuilder(); int nJ = 0, nS = 0; float gMax = 0, gMin = 0; string gAt = "";
var rows = new System.Collections.Generic.List<(float mx, string line)>();
int over5 = 0, over5J = 0;
foreach (Transform c in v2)
{
    if (!c.name.StartsWith("ROAD_V2j_")) continue; nJ++;
    var mf = c.GetComponent<MeshFilter>(); var m = mf.sharedMesh; var vs = m.vertices; var ts = m.triangles; var tr = c.transform;
    var ec = new System.Collections.Generic.Dictionary<(int, int), int>();
    for (int i = 0; i < ts.Length; i += 3) for (int e = 0; e < 3; e++) { int a = ts[i + e], b = ts[i + (e + 1) % 3]; var k = a < b ? (a, b) : (b, a); ec[k] = ec.GetValueOrDefault(k) + 1; }
    float jMax = 0, jMin = 0; Vector3 jAt = Vector3.zero; int jS = 0, jOver = 0;
    foreach (var kv in ec)
    {
        if (kv.Value != 1) continue;
        var A = tr.TransformPoint(vs[kv.Key.Item1]); var B = tr.TransformPoint(vs[kv.Key.Item2]);
        int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(A, B) / .5f));
        for (int s = 0; s <= n; s++)
        {
            var p = Vector3.Lerp(A, B, s / (float)n); float best = 1e9f;
            foreach (var bc in bandCols)
            {
                var bb = bc.bounds; if (p.x < bb.min.x - .01f || p.x > bb.max.x + .01f || p.z < bb.min.z - .01f || p.z > bb.max.z + .01f) continue;
                if (bc.Raycast(new Ray(new Vector3(p.x, p.y + 1f, p.z), Vector3.down), out var h, 2f)) { float d = p.y - h.point.y; if (Mathf.Abs(d) < Mathf.Abs(best)) best = d; }
            }
            if (best > 1e8f) continue; jS++; nS++;
            if (best > jMax) { jMax = best; jAt = p; } if (best < jMin) jMin = best;
            if (Mathf.Abs(best) > .005f) jOver++;
        }
    }
    if (jOver > 0) { over5J++; over5 += jOver; }
    if (jMax > gMax) { gMax = jMax; gAt = c.name + " @(" + jAt.x.ToString("F1") + "," + jAt.y.ToString("F2") + "," + jAt.z.ToString("F1") + ")"; }
    gMin = Mathf.Min(gMin, jMin);
    rows.Add((Mathf.Max(jMax, -jMin), $"{c.name}\tsamples {jS}\tmax+ {jMax * 100:F1}cm\tmax- {jMin * 100:F1}cm\t>5mm {jOver}\t@({jAt.x:F1},{jAt.y:F2},{jAt.z:F1})"));
}
rows.Sort((a, b) => b.mx.CompareTo(a.mx));
sb.AppendLine($"plates {nJ}, band-touching samples {nS}, samples |step|>5mm {over5} in {over5J} plates, max plate-above-band {gMax * 100:F1}cm at {gAt}, max plate-below-band {gMin * 100:F1}cm");
foreach (var r in rows) sb.AppendLine(r.line);
System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/seam_" + (System.Environment.GetEnvironmentVariable("JF_TAG") ?? "now") + ".txt"), sb.ToString());
var lines = sb.ToString().Split('\n'); return string.Join("\n", lines, 0, Mathf.Min(25, lines.Length));
