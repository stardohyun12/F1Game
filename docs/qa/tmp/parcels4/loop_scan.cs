// 0236: 봇 고리 중심선을 따라 1 m 마다 좌우 0·±2·±3.5 m 에서 위→아래 Raycast. 맨 위가 길이 아니면(지형·PadWalls 등) 길 표면보다 몇 m 위인지 모아 보고
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/d198/loop_pts.txt"));
var pts = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector2(float.Parse(a[0]), float.Parse(a[1])); }).ToList();
var sb = new System.Text.StringBuilder(); int bad = 0, samples = 0; var near = new Vector2(121.5f, 9f); float best = 1e9f; int bestI = -1;
for (int i = 0; i + 1 < pts.Count; i++) {
  var a = pts[i]; var b = pts[i + 1]; var dir = (b - a).normalized; var nrm = new Vector2(dir.y, -dir.x); float L = Vector2.Distance(a, b);
  if (Vector2.Distance(a, near) < best) { best = Vector2.Distance(a, near); bestI = i; }
  for (float s = 0; s < L; s += 1f) foreach (var off in new[] { 0f, -2f, 2f, -3.5f, 3.5f }) {
    var p = a + dir * s + nrm * off; samples++;
    var hits = Physics.RaycastAll(new Vector3(p.x, 200, p.y), Vector3.down, 400); if (hits.Length == 0) continue; System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));
    float road = float.NaN; foreach (var h in hits) if (h.collider.name.StartsWith("ROAD")) { road = h.point.y; break; }
    var top = hits[0]; if (top.collider.name.StartsWith("ROAD") || float.IsNaN(road)) continue;
    float up = top.point.y - road; if (up < .05f) continue;
    if (top.collider.isTrigger) continue;
    bad++; if (bad <= 40) sb.Append($"i{i} off{off} ({p.x:F1},{p.y:F1}) {top.collider.name} +{up:F2}m\n");
  }
}
return $"samples {samples} blocked {bad}; loop pt nearest (121.5,9): i{bestI} d{best:F1} = {pts[bestI]}\n" + sb;
