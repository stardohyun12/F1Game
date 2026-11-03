// 0236: Parcels_Plan 안 충돌체(나무·덤불 등) 중 봇 고리(d198/loop_pts) 중심선에서 가까운 것 — 경계상자 가장자리까지 수평 거리
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/d198/loop_pts.txt"));
var pts = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector2(float.Parse(a[0]), float.Parse(a[1])); }).ToList();
float SegD(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return Vector2.Distance(p, a + ab * t); }
var root = GameObject.Find("Parcels_Plan"); var sb = new System.Text.StringBuilder(); int n = 0, total = 0;
foreach (var col in root.GetComponentsInChildren<Collider>()) {
  total++; var b = col.bounds; var c = new Vector2(b.center.x, b.center.z); float r = Mathf.Max(b.extents.x, b.extents.z);
  float d = float.MaxValue; for (int i = 0; i + 1 < pts.Count; i++) d = Mathf.Min(d, SegD(c, pts[i], pts[i + 1]));
  if (d - r < 4.5f) { n++; sb.Append($"{col.name} ({b.center.x:F1},{b.center.z:F1}) r{r:F1} dCenterline {d:F1} edge {d - r:F1}\n"); }
}
return $"colliders {total}, edge<4.5m from loop centerline: {n}\n" + sb;
