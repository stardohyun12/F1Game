// 과제 0248: tv6_loop_pts.txt / tv6_go_pts.txt 의 점(JSON 매듭 기준)을 씬 RoadSurface 중심선(촘촘히 0.5m)에 붙여 *_snap.txt 로. 이동량 통계 반환.
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length >= 2 && (r.kind.ToString() == "main" || r.kind.ToString() == "village" || r.roadId.Contains("seam"))).ToList();
var C = new List<Vector3>();
foreach (var r in roads) for (int i = 1; i < r.centerline.Length; i++) { var a = r.centerline[i - 1]; var b = r.centerline[i]; int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / .5f)); for (int k = 0; k < n; k++) C.Add(Vector3.Lerp(a, b, k / (float)n)); }
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var sb = new System.Text.StringBuilder("centerPts " + C.Count + " kinds " + string.Join(",", roads.Select(r => r.kind.ToString()).Distinct()) + "; ");
foreach (var f in new[] { "tv6_loop_pts", "tv6_go_pts" })
{
    var P = System.IO.File.ReadAllLines(dir + f + ".txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
    var O = new List<Vector3>(); float mx = 0; int moved = 0;
    foreach (var p in P)
    {
        Vector3 best = p; float bd = 36f;
        foreach (var c in C) { float d = (c.x - p.x) * (c.x - p.x) + (c.z - p.z) * (c.z - p.z); if (d < bd) { bd = d; best = c; } }
        float m = Mathf.Sqrt(Mathf.Min(bd, 36f)); if (best != p) { mx = Mathf.Max(mx, m); if (m > 1f) moved++; }
        if (O.Count == 0 || new Vector2(best.x - O[O.Count - 1].x, best.z - O[O.Count - 1].z).magnitude > .8f) O.Add(best);
    }
    System.IO.File.WriteAllLines(dir + f + "_snap.txt", O.Select(q => q.x.ToString("F2") + " " + q.z.ToString("F2")));
    sb.Append(f + " " + P.Count + "→" + O.Count + " maxMove " + mx.ToString("F1") + " moved>1m " + moved + "; ");
}
return sb.ToString();
