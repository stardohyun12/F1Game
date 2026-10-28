// D-203: 집 자리 바닥+3.5m 에서 yaw 별 바다 점 가시 수(지형 변형 없음) + 주변 B_* + 정밀 높이
var sb = new System.Text.StringBuilder();
var root = GameObject.Find("TownV6_Gray").transform;
var tc = root.Find("TownV6_Terrain").GetComponent<Collider>();
float CX = 185f, CZ = -61f;
tc.Raycast(new Ray(new Vector3(CX, 300, CZ), Vector3.down), out var g0, 600);
var home = GameObject.Find("house_home"); bool NOOSM = System.IO.File.Exists("D:/Github/F1Game/docs/qa/tmp/d203/noosm.flag");
var homeCols = home.GetComponentsInChildren<Collider>().Concat(NOOSM ? root.Find("Buildings").GetComponentsInChildren<Collider>() : new Collider[0]).Where(c => c.enabled).ToList(); foreach (var c in homeCols) c.enabled = false;
Physics.SyncTransforms();
var eye = new Vector3(CX, g0.point.y + 3.5f, CZ);
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600); return hs.Length == 0 || hs.Max(h => h.point.y) < 0.05f; }
// 바다 점: 360도 5도 간격, 거리 60~900, 방향마다 최대 3점
var tg = new List<(int yaw, Vector3 p)>();
for (int yaw = 0; yaw < 360; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0; for (float d = 60; d < 900 && k < 3; d += 5) { float x = CX + s * d, z = CZ + c * d; if (SeaAt(x, z)) { tg.Add((yaw, new Vector3(x, 0.5f, z))); k++; d += 25; } } }
var visY = new Dictionary<int, int>(); var tgY = new Dictionary<int, int>();
foreach (var t in tg) { tgY[t.yaw] = tgY.TryGetValue(t.yaw, out var a) ? a + 1 : 1; if (!Physics.Linecast(eye, t.p, out var hit, ~0, QueryTriggerInteraction.Ignore)) visY[t.yaw] = visY.TryGetValue(t.yaw, out var b) ? b + 1 : 1; }
foreach (var c in homeCols) c.enabled = true;
sb.Append("floor " + g0.point.y.ToString("F2") + " seaTargets " + tg.Count + " visible " + visY.Values.Sum() + "\n");
sb.Append("target yaws: " + string.Join(" ", tgY.Select(k => k.Key + ":" + k.Value)) + "\n");
sb.Append("visible yaws: " + string.Join(" ", visY.Select(k => k.Key + ":" + k.Value)) + "\n");
// yaw 후보별 정면 ±60° 안 보이는 바다 점 수
var best = new List<string>();
for (int f = 0; f < 360; f += 5) { int n = visY.Where(k => Mathf.Abs(Mathf.DeltaAngle(k.Key, f)) <= 60).Sum(k => k.Value); best.Add(f + ":" + n); }
sb.Append("cone60 per facing: " + string.Join(" ", best) + "\n");
// B_* 오브젝트
var bs = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("B_")).ToList();
sb.Append("B_* total " + bs.Count + "\n");
foreach (var t in bs) { var r = t.GetComponentInChildren<Renderer>(); var c = r ? r.bounds.center : t.position; if (new Vector2(c.x - CX, c.z - CZ).magnitude < 70) sb.Append("  " + t.name + " parent " + t.parent?.name + " c " + c.ToString("F1") + " size " + (r ? r.bounds.size.ToString("F1") : "-") + " active " + t.gameObject.activeInHierarchy + "\n"); }
sb.Append("h grid x150..235 s5 / z-35..-95 s5:\n");
for (float z = -35; z >= -95; z -= 5) { sb.Append("z" + z + ": "); for (float x = 150; x <= 235; x += 5) { tc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600); sb.Append(h.point.y.ToString("F1") + " "); } sb.Append("\n"); }
return sb.ToString();
