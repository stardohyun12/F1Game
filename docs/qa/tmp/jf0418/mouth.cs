// 띠(V2e) 끝마다: 끝 1 m·3 m 구배, 끝 너머 0.5 m 가 갈림목 판인지. 판은 평면(구배 0)이라 입구 꺾임 각 = 띠 끝 구배.
Physics.SyncTransforms();
var top = GameObject.Find("TownV6_Gray").transform; var v2 = top.Find("RoadsV2");
var rows = new System.Collections.Generic.List<(float g, string s)>(); int mouths = 0, g3 = 0, g6 = 0;
var spots = new[] { new Vector3(142, 13, -86), new Vector3(136, 0, -93), new Vector3(-180, 4, -70), new Vector3(86.16f, 3.49f, -192.19f) };
var near = new string[spots.Length]; var nearD = new float[spots.Length]; for (int i = 0; i < spots.Length; i++) nearD[i] = 1e9f;
foreach (Transform c in v2)
{
    if (!c.name.StartsWith("ROAD_V2e_")) continue; var rs = c.GetComponent<RoadSurface>(); var p = rs.centerline; if (p == null || p.Length < 3) continue;
    foreach (bool st in new[] { true, false })
    {
        int e = st ? 0 : p.Length - 1, s1 = st ? 1 : p.Length - 2;
        var dirOut = (p[e] - p[s1]); dirOut.y = 0; dirOut.Normalize();
        var q = p[e] + dirOut * .5f; string pl = null;
        foreach (var h in Physics.RaycastAll(new Vector3(q.x, p[e].y + .5f, q.z), Vector3.down, 1f)) if (h.collider.name.StartsWith("ROAD_V2j_")) pl = h.collider.name;
        if (pl == null) continue; mouths++;
        float L1 = 0; int k = e; float y0 = p[e].y; int stp = st ? 1 : -1; int j = e;
        while (j + stp >= 0 && j + stp < p.Length && L1 < 1f) { var d = p[j + stp] - p[j]; d.y = 0; L1 += d.magnitude; j += stp; }
        float g1 = (p[j].y - y0) / Mathf.Max(.01f, L1);   // 판에서 멀어질수록 높이 변화 / 거리 (+ = 판에서 멀어지며 오름)
        float L3 = L1; while (j + stp >= 0 && j + stp < p.Length && L3 < 3f) { var d = p[j + stp] - p[j]; d.y = 0; L3 += d.magnitude; j += stp; }
        float g3v = (p[j].y - y0) / Mathf.Max(.01f, L3);
        float deg = Mathf.Atan(Mathf.Abs(g1)) * Mathf.Rad2Deg; if (deg > 3) g3++; if (deg > 6) g6++;
        rows.Add((deg, $"{pl}\t{c.name.Substring(9, Mathf.Min(24, c.name.Length - 9))}\tgrade1m {g1 * 100:F1}%\tgrade3m {g3v * 100:F1}%\tcrease {deg:F1}deg\t@({p[e].x:F1},{p[e].y:F2},{p[e].z:F1})"));
        for (int i = 0; i < spots.Length; i++) { var dd = p[e] - spots[i]; dd.y = 0; if (dd.magnitude < nearD[i]) { nearD[i] = dd.magnitude; near[i] = $"{pl} {c.name.Substring(9, Mathf.Min(24, c.name.Length - 9))} crease {deg:F1}deg d{dd.magnitude:F1}m"; } }
    }
}
rows.Sort((a, b) => b.g.CompareTo(a.g));
var sb = new System.Text.StringBuilder(); sb.AppendLine($"band mouths at plates {mouths}, crease >3deg {g3}, >6deg {g6}");
for (int i = 0; i < spots.Length; i++) sb.AppendLine($"stuck spot {spots[i]} nearest mouth: {near[i]}");
foreach (var r in rows) sb.AppendLine(r.s);
System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/mouth_" + (System.Environment.GetEnvironmentVariable("JF_TAG") ?? "now") + ".txt"), sb.ToString());
var lines = sb.ToString().Split('\n'); return string.Join("\n", lines, 0, Mathf.Min(22, lines.Length));
