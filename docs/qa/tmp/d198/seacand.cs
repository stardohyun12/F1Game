// D-198 2차: (1) 방 눈높이(바닥+3.5)에서 바다 쪽을 가리는 벽 맞은 점 (2) 근처 80 m 안에서 바닥+3.5 m 로 바다가 보이는 자리 찾기
// 바다 점 = 방향 yaw 로 D m 나간 곳 중 땅 높이 < 0.05 (아래로 쏴서 아무것도 없거나 y<0.05) — 그 위 0.5 m 를 겨냥, Linecast 가 안 막히면 보임
var eye0 = new Vector3(-117.71f, 9.44f, -11.88f);
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400); return hs.Length == 0 || hs.Max(h => h.point.y) < 0.05f; }
var seaPts = new Dictionary<int, List<Vector3>>();
float GroundY(float x, float z, out string nm) { nm = ""; if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var h, 400)) return float.NaN; nm = h.collider.name; return h.point.y; }
// yaw 별로 eye 기준이 아니라 "바다 목표"를 고정: 방에서 yaw 방향으로 처음 바다가 되는 D 부터 +60 m 까지 3점
var targets = new List<(int yaw, Vector3 p)>();
for (int yaw = 100; yaw <= 280; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0;
  for (float D = 40; D < 700 && k < 3; D += 5) { float x = eye0.x + s * D, z = eye0.z + c * D; if (SeaAt(x, z)) { targets.Add((yaw, new Vector3(x, 0.5f, z))); k++; D += 25; } } }
var sb = new System.Text.StringBuilder();
sb.Append("targets " + targets.Count + "\n");
// (1) 지금 방에서: 목표마다 막는 첫 콜라이더와 맞은 점
var block = new List<string>();
foreach (var t in targets) { if (Physics.Linecast(eye0, t.p, out var h)) block.Add(t.yaw + " " + h.collider.name + " " + h.point.x.ToString("F1") + " " + h.point.z.ToString("F1") + " " + h.point.y.ToString("F1") + " d" + h.distance.ToString("F0")); else block.Add(t.yaw + " VISIBLE"); }
System.IO.File.WriteAllLines("D:/Github/F1Game/docs/qa/tmp/d198/sea_block.txt", block);
// (2) 후보: 80 m 반경 4 m 격자, 땅(TownV6_Terrain)이 맨 위, 경사 < 12°, 근처 3 m 안에 벽 없음
var cand = new List<string>();
// 2회째: 80 m 안 0곳 → 200 m, 5 m 격자, 벽(Walls_0)만 3 m 안 금지(길 옆은 허용)
for (float dx = -200; dx <= 200; dx += 5) for (float dz = -200; dz <= 200; dz += 5) {
  float x = eye0.x + dx, z = eye0.z + dz; if (dx * dx + dz * dz > 40000) continue;
  if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var g, 400)) continue;
  if (!g.collider.name.Contains("Terrain") || Vector3.Angle(g.normal, Vector3.up) > 12 || g.point.y < 2f) continue;
  if (Physics.OverlapSphere(g.point + Vector3.up * 1.5f, 3f).Any(cl => cl.name.StartsWith("Walls") || cl.name.StartsWith("Roofs") || cl.name.StartsWith("D198"))) continue;
  var eye = g.point + Vector3.up * 3.5f; int vis = 0; var ys = new List<int>();
  foreach (var t in targets) if (!Physics.Linecast(eye, t.p)) { vis++; if (!ys.Contains(t.yaw)) ys.Add(t.yaw); }
  if (vis > 0) cand.Add(vis + " " + x.ToString("F1") + " " + z.ToString("F1") + " " + g.point.y.ToString("F2") + " d" + Mathf.Sqrt(dx * dx + dz * dz).ToString("F0") + " yaws " + string.Join(",", ys));
}
System.IO.File.WriteAllLines("D:/Github/F1Game/docs/qa/tmp/d198/sea_cand.txt", cand.OrderByDescending(s => int.Parse(s.Split(' ')[0])));
return "targets " + targets.Count + " blocked " + block.Count(b => !b.Contains("VISIBLE")) + " cand " + cand.Count;
