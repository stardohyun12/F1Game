// D-203 마무리: 옛 방 비활성, 건물·OSM 뜸/묻힘 확인, 발코니 눈높이별 바다, 씬 저장
var sb = new System.Text.StringBuilder();
var root = GameObject.Find("TownV6_Gray").transform; var terMC = root.Find("TownV6_Terrain").GetComponent<MeshCollider>();
float G(float x, float z) { terMC.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600); return h.point.y; }
var room = root.Find("D198_Buildings/D198_room"); room.gameObject.SetActive(false); sb.Append("D198_room active " + room.gameObject.activeSelf + "\n");
// 건물: 발자국 1m 격자에서 지형-바닥 차
foreach (var n in new[] { "house_home", "D198_postoffice", "D198_depot", "D198_garage" }) { var t = root.Find("D198_Buildings/" + n); var b = new Bounds(); bool f = true; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); } float lo = 99, hi = -99; for (float x = b.min.x; x <= b.max.x; x += 1) for (float z = b.min.z; z <= b.max.z; z += 1) { float d = G(x, z) - t.position.y; lo = Mathf.Min(lo, d); hi = Mathf.Max(hi, d); } int over5 = 0; for (float x = b.min.x; x <= b.max.x; x += 1) for (float z = b.min.z; z <= b.max.z; z += 1) if (Mathf.Abs(G(x, z) - t.position.y) > 0.05f) over5++; sb.Append(n + " pos " + t.position.ToString("F1") + " yaw " + t.eulerAngles.y.ToString("F0") + " ground-floor " + lo.ToString("F2") + ".." + hi.ToString("F2") + " cells>5cm " + over5 + " distHomeCenter " + Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(185, -61)).ToString("F1") + "\n"); }
// OSM 벽: 패드 30m 안 남은 집의 벽 아래끝 정점 vs 지형
var walls = root.Find("Buildings/Walls_0"); var wm = walls.GetComponent<MeshFilter>().sharedMesh; var wv = wm.vertices;
var cells = new Dictionary<Vector2Int, float>();
foreach (var lv in wv) { var w = walls.TransformPoint(lv); if (w.x < 160 || w.x > 270 || w.z < -95 || w.z > -25) continue; var k = new Vector2Int(Mathf.RoundToInt(w.x * 2), Mathf.RoundToInt(w.z * 2)); cells[k] = cells.TryGetValue(k, out var y) ? Mathf.Min(y, w.y) : w.y; }
int flo = 0, bur = 0; float wf = 0, wb = 0; foreach (var kv in cells) { float d = kv.Value - G(kv.Key.x / 2f, kv.Key.y / 2f); if (d > 0.05f) { flo++; wf = Mathf.Max(wf, d); } if (d < -0.5f) { bur++; wb = Mathf.Min(wb, d); } }
sb.Append("OSM wall base cols " + cells.Count + " floating>5cm " + flo + " (max " + wf.ToString("F2") + ") sunk>50cm " + bur + " (min " + wb.ToString("F2") + ")\n");
// 바다: 눈높이 3.5/5/7
var hc = root.Find("D198_Buildings/house_home").GetComponentsInChildren<Collider>().Where(c => c.enabled).ToList(); foreach (var c in hc) c.enabled = false; Physics.SyncTransforms();
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600); return hs.Length == 0 || hs.Max(h => h.point.y) < 0.05f; }
var tg = new List<Vector3>(); for (int yaw = 130; yaw <= 250; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0; for (float d = 60; d < 900 && k < 3; d += 5) { float x = 185 + s * d, z = -61 + c * d; if (SeaAt(x, z)) { tg.Add(new Vector3(x, 0.5f, z)); k++; d += 25; } } }
foreach (var e in new[] { 3.5f, 5f, 7f, 10f }) { var eye = new Vector3(185, 24.9f + e, -61); sb.Append("sea eye+" + e + " vis " + tg.Count(p => !Physics.Linecast(eye, p)) + "/" + tg.Count + "\n"); }
foreach (var c in hc) c.enabled = true; Physics.SyncTransforms();
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
sb.Append("saved " + ok + " dirty " + sc.isDirty + "\n");
return sb.ToString();
