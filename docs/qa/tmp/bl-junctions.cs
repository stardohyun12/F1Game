var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-cam"));
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 3).ToArray();
var roadCols = new HashSet<Collider>(); foreach (var r in roads) foreach (var c in r.GetComponentsInChildren<Collider>()) roadCols.Add(c);
System.Func<Collider, string> Cls = c => roadCols.Contains(c) || c.GetComponentInParent<RoadSurface>() != null ? "road" : (c is TerrainCollider || c.name.ToLower().Contains("terrain") || c.name.ToLower().Contains("ground")) ? "terrain" : "solid";
System.Func<RoadSurface, int, float> W = (r, i) => r.widths != null && r.widths.Length == r.centerline.Length ? r.widths[i] : r.width;
var rows = new List<string>(); var issues = new List<(float sev, string line)>();
System.Action<string, string, Vector3, float, string> Add = (road, kind, p, sev, det) => { issues.Add((sev, road + "," + kind + "," + p.x.ToString("F1") + "," + p.y.ToString("F1") + "," + p.z.ToString("F1") + "," + sev.ToString("F2") + "," + det)); };
foreach (var r in roads)
{
    var P = r.centerline; int n = P.Length;
    if (!r.closed)
    {
        for (int e = 0; e < 2; e++)
        {
            int i0 = e == 0 ? 0 : n - 1, i1 = e == 0 ? Mathf.Min(4, n - 1) : n - 5, i2 = e == 0 ? Mathf.Min(10, n - 1) : n - 11;
            var E = P[i0]; var outDir2 = E - P[i1]; outDir2.y = 0; outDir2.Normalize();
            var d1 = P[i1] - P[i0]; d1.y = 0; var d2 = P[i2] - P[i1]; d2.y = 0;
            float bend = Vector3.Angle(d1, d2);
            RoadSurface hr = null; int hj = 0; float hd = 1e9f;
            foreach (var o in roads) { if (o == r) continue; var Q = o.centerline; for (int j = 0; j < Q.Length; j++) { float d = (new Vector2(Q[j].x - E.x, Q[j].z - E.z)).magnitude; if (d < hd) { hd = d; hr = o; hj = j; } } }
            string tag = r.roadId + (e == 0 ? ".start" : ".end");
            float hw = hr != null ? W(hr, hj) * 0.5f : 0f;
            if (hr != null && hd <= hw + 1.5f)
            {
                var Q = hr.centerline; var hf = Q[Mathf.Min(hj + 2, Q.Length - 1)] - Q[Mathf.Max(hj - 2, 0)]; hf.y = 0;
                float cross = Vector3.Angle(outDir2, hf); cross = Mathf.Min(cross, 180f - cross);
                float dy = Mathf.Abs(E.y - Q[hj].y);
                float grade = Mathf.Abs(P[i1].y - P[i0].y) / Mathf.Max(0.5f, (P[i1] - P[i0]).magnitude);
                float wr = W(r, i0) / Mathf.Max(0.5f, W(hr, hj));
                rows.Add(tag + "," + hr.roadId + "#" + hj + ",join," + E.x.ToString("F0") + "," + E.z.ToString("F0") + ",dy" + dy.ToString("F2") + ",cross" + cross.ToString("F0") + ",bend" + bend.ToString("F0"));
                if (dy > 0.25f) Add(tag, "높이단차", E, Mathf.Clamp01(dy / 1.0f) * 3f, "dy=" + dy.ToString("F2") + "m vs " + hr.roadId + "#" + hj);
                if (bend > 30f) Add(tag, "급꺾임", E, Mathf.Clamp01((bend - 30f) / 60f) * 2f + 0.5f, "첫10m 꺾임 " + bend.ToString("F0") + "도");
                if (cross < 20f && hd > 0.5f) Add(tag, "얕은합류각", E, 1f, "접속각 " + cross.ToString("F0") + "도 (" + hr.roadId + ")");
                if (grade > 0.2f) Add(tag, "합류경사", E, 1.5f, "끝 4m 경사 " + (grade * 100f).ToString("F0") + "%");
                if (wr < 0.45f && r.kind != "stairs" && r.kind != "alley") Add(tag, "폭급변", E, 0.8f, "폭 " + W(r, i0).ToString("F1") + " vs " + W(hr, hj).ToString("F1"));
            }
            else
            {
                string kind = hd < 12f ? "틈" : "막다른끝";
                RaycastHit h; string wall = "";
                if (Physics.Raycast(E + Vector3.up * 1f, outDir2, out h, 6f, ~0, QueryTriggerInteraction.Ignore) && Cls(h.collider) != "road") wall = h.collider.name;
                rows.Add(tag + "," + (hr ? hr.roadId + "#" + hj : "-") + "," + kind + "," + E.x.ToString("F0") + "," + E.z.ToString("F0") + ",gap" + hd.ToString("F1") + ",wall=" + wall);
                if (kind == "틈") Add(tag, "틈", E, 2.5f, "가장 가까운 길 " + hr.roadId + "#" + hj + " 까지 " + (hd - hw).ToString("F1") + "m");
                if (wall != "") Add(tag, "벽으로끝남", E, 1.2f, "앞 6m 안에 " + wall);
                else if (kind == "막다른끝") Add(tag, "막다른끝", E, 0.3f, "벽 없이 끝남 (가장 가까운 길 " + hd.ToString("F0") + "m)");
            }
        }
    }
    for (int i = 0; i < n; i += 2)
    {
        var p = P[i];
        foreach (var h in Physics.RaycastAll(p + Vector3.up * 6f, Vector3.down, 6.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.point.y > p.y + 0.25f && h.point.y < p.y + 3f && Cls(h.collider) == "terrain") { Add(r.roadId + "#" + i, "지형에묻힘", p, Mathf.Clamp01((h.point.y - p.y) / 1.5f) * 2f + 0.5f, "지면이 노면보다 " + (h.point.y - p.y).ToString("F2") + "m 위 (" + h.collider.name + ")"); i += 10; break; }
        }
        if (i + 1 < n) { float dw = Mathf.Abs(W(r, Mathf.Min(i + 1, n - 1)) - W(r, i)); if (dw > 0.6f) Add(r.roadId + "#" + i, "폭급변", p, 1f, "1m 에 폭 " + dw.ToString("F1") + "m 변함"); }
        if (i >= 2 && i + 2 < n) { float dy2 = Mathf.Abs((P[i + 2].y - P[i].y) - (P[i].y - P[i - 2].y)); if (dy2 > 0.35f) Add(r.roadId + "#" + i, "노면턱", p, Mathf.Clamp01(dy2 / 0.8f) * 2f, "2m 간 경사 꺾임 " + dy2.ToString("F2") + "m"); }
    }
}
for (int a = 0; a < roads.Length; a++) for (int b = a + 1; b < roads.Length; b++)
{
    var A = roads[a].centerline; var B = roads[b].centerline;
    for (int i = 0; i < A.Length; i += 3) for (int j = 0; j < B.Length; j += 3)
    {
        var d = A[i] - B[j]; float hz = new Vector2(d.x, d.z).magnitude;
        if (hz < (W(roads[a], i) + W(roads[b], j)) * 0.5f && Mathf.Abs(d.y) > 0.6f && Mathf.Abs(d.y) < 4.5f) { Add(roads[a].roadId + "#" + i + "x" + roads[b].roadId + "#" + j, "겹침높이차", A[i], Mathf.Clamp01(Mathf.Abs(d.y) / 2f) * 3f, "겹치는데 높이차 " + Mathf.Abs(d.y).ToString("F2") + "m"); i += 15; break; }
    }
}
var sorted = issues.OrderByDescending(x => x.sev).ToList();
System.IO.Directory.CreateDirectory(outDir);
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "junction_audit.csv"), "where,kind,x,y,z,severity,detail\n" + string.Join("\n", sorted.Select(x => x.line)));
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "junction_endpoints.csv"), "end,host,type,x,z,a,b,c\n" + string.Join("\n", rows));
var byKind = sorted.GroupBy(x => x.line.Split(',')[1]).Select(g => g.Key + "=" + g.Count());
return "issues " + sorted.Count + " endpoints " + rows.Count + " | " + string.Join(" ", byKind) + "\nTOP:\n" + string.Join("\n", sorted.Take(25).Select(x => x.line));
