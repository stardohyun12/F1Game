// 언덕 시뮬: (R,h) 마다 지형·OSM 벽·길 메시 사본을 변형해 콜라이더에 끼우고, 발코니(바닥+3.5)에서 바다 점까지 Linecast. 끝나면 원본 복구(씬 변경 없음)
// 변형: r<=R 은 h, R~R+W smoothstep 으로 0. OSM 집은 집 단위 오프셋 = 폴리곤 꼭짓점 D 의 최솟값(뜨지 않게)
float CX = -117.7f, CZ = -11.2f, W = 20f;
string H = "D:/Github/F1Game/docs/qa/tmp/hill/";
var cfg = System.IO.File.ReadAllLines(H + "sim_cfg.txt");          // 1: R 목록, 2: h 목록, 3: 지울 osm id(없으면 -)
var RS = cfg[0].Split(' ').Select(float.Parse).ToArray(); var HS = cfg[1].Split(' ').Select(float.Parse).ToArray();
var DEL = new HashSet<string>(cfg.Length > 2 ? cfg[2].Split(' ').Where(s => s != "-" && s != "") : new string[0]);
if (cfg.Length > 3 && cfg[3].Trim() != "") W = float.Parse(cfg[3]);
// 5: 중심 x z (없으면 D-198 방), 6: 눈 높이 목록(바닥 기준, 없으면 3.5)
if (cfg.Length > 4 && cfg[4].Trim() != "") { var c5 = cfg[4].Split(' '); CX = float.Parse(c5[0]); CZ = float.Parse(c5[1]); }
var EYES = cfg.Length > 5 && cfg[5].Trim() != "" ? cfg[5].Split(' ').Select(float.Parse).ToArray() : new[] { 3.5f };
float D(float x, float z, float h, float R) { float r = Mathf.Sqrt((x - CX) * (x - CX) + (z - CZ) * (z - CZ)); if (r <= R) return h; if (r >= R + W) return 0; float t = (r - R) / W; return h * (1 - t * t * (3 - 2 * t)); }
var polys = System.IO.File.ReadAllLines(H + "polys.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return (id: a[0], pts: a.Skip(2).Select(s => { var q = s.Split(','); return new Vector2(float.Parse(q[0]), float.Parse(q[1])); }).ToArray()); }).ToList();
bool In(Vector2[] p, float x, float z) { bool c = false; for (int i = 0, j = p.Length - 1; i < p.Length; j = i++) if (((p[i].y > z) != (p[j].y > z)) && (x < (p[j].x - p[i].x) * (z - p[i].y) / (p[j].y - p[i].y) + p[i].x)) c = !c; return c; }
int Owner(float x, float z) { for (int b = 0; b < polys.Count; b++) { var p = polys[b].pts; foreach (var q in p) if (Mathf.Abs(q.x - x) < 0.02f && Mathf.Abs(q.y - z) < 0.02f) return b; } for (int b = 0; b < polys.Count; b++) if (In(polys[b].pts, x, z)) return b; return -1; }
var root = GameObject.Find("TownV6_Gray").transform;
var targets = new List<(int yaw, Vector3 p)>(); var eye0 = new Vector3(-117.71f, 9.44f, -11.88f);
if (cfg.Length > 4 && cfg[4].Trim() != "") { Physics.Raycast(new Vector3(CX, 200, CZ), Vector3.down, out var g0, 400, ~0, QueryTriggerInteraction.Ignore); var tc = root.Find("TownV6_Terrain").GetComponent<Collider>(); if (tc.Raycast(new Ray(new Vector3(CX, 200, CZ), Vector3.down), out var gt, 400)) g0 = gt; eye0 = new Vector3(CX, g0.point.y, CZ); }
else eye0.y -= 3.5f;   // eye0 = 바닥, 눈은 EYES 로 더함
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400); return hs.Length == 0 || hs.Max(h => h.point.y) < 0.05f; }
for (int yaw = 100; yaw <= 280; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0; for (float Dd = 100; Dd < 700 && k < 3; Dd += 5) { float x = eye0.x + s * Dd, z = eye0.z + c * Dd; if (SeaAt(x, z)) { targets.Add((yaw, new Vector3(x, 0.5f, z))); k++; Dd += 25; } } }
// 대상 메시
var objs = new List<(MeshFilter mf, MeshCollider mc, Mesh orig, Vector3[] v, int[] own, bool bld)>();
float maxR = RS.Max() + W;
void AddObj(Transform t, bool bld) { var mf = t.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return; var mc = t.GetComponent<MeshCollider>(); var m = mf.sharedMesh; var v = m.vertices; bool near = false; int[] own = null;
  for (int i = 0; i < v.Length && !near; i++) { var w = t.TransformPoint(v[i]); if ((w.x - CX) * (w.x - CX) + (w.z - CZ) * (w.z - CZ) < (maxR + 60) * (maxR + 60)) near = true; }
  if (!near) return;
  if (bld) { own = new int[v.Length]; for (int i = 0; i < v.Length; i++) { var w = t.TransformPoint(v[i]); own[i] = (w.x - CX) * (w.x - CX) + (w.z - CZ) * (w.z - CZ) < 160 * 160 ? Owner(w.x, w.z) : -1; } }
  objs.Add((mf, mc, m, v, own, bld)); }
AddObj(root.Find("TownV6_Terrain"), false);
AddObj(root.Find("Buildings/Walls_0"), true); AddObj(root.Find("Buildings/Roofs_0"), true);
foreach (Transform r in root.Find("Roads")) AddObj(r, false);
var roomCol = GameObject.Find("D198_room").GetComponent<Collider>(); roomCol.enabled = false;
var sb = new System.Text.StringBuilder(); sb.Append("targets " + targets.Count + " objs " + objs.Count + " del " + DEL.Count + "\n");
var lines = new List<string>();
try {
foreach (var R in RS) foreach (var h in HS) {
  var off = polys.Select(p => DEL.Contains(p.id) || (DEL.Contains("ZONE") && p.pts.Max(q => D(q.x, q.y, h, R)) > 0) ? -500f : p.pts.Min(q => D(q.x, q.y, h, R))).ToArray();
  foreach (var o in objs) { var t = o.mf.transform; var nv = new Vector3[o.v.Length];
    for (int i = 0; i < nv.Length; i++) { var w = t.TransformPoint(o.v[i]); float d = o.bld && o.own[i] >= 0 ? off[o.own[i]] : D(w.x, w.z, h, R); nv[i] = t.InverseTransformPoint(w + Vector3.up * d); }
    var m = UnityEngine.Object.Instantiate(o.orig); m.vertices = nv; m.RecalculateBounds(); if (o.mc) o.mc.sharedMesh = m; o.mf.sharedMesh = m; }
  Physics.SyncTransforms();
  foreach (var eo in EYES) {
  var eye = eye0 + Vector3.up * (D(eye0.x, eye0.z, h, R) + eo); int vis = 0; var ys = new SortedSet<int>(); var blk = new Dictionary<string, int>();
  foreach (var tg in targets) { if (!Physics.Linecast(eye, tg.p, out var hit)) { vis++; ys.Add(tg.yaw); } else { string nm = hit.collider.name; if (nm.StartsWith("Walls")) { int b = Owner(hit.point.x, hit.point.z); if (b < 0) { float best = 9; for (int bb = 0; bb < polys.Count; bb++) foreach (var q in polys[bb].pts) { float dd = Vector2.Distance(q, new Vector2(hit.point.x, hit.point.z)); if (dd < best) { best = dd; b = bb; } } } nm = b >= 0 ? polys[b].id : "Walls?"; } if (nm.StartsWith("TownV6_Terrain") && blk.Keys.Count(k => k.StartsWith("PT")) < 4) blk["PT" + tg.yaw + "_eye" + eye.ToString("F1") + "_hit" + hit.point.ToString("F1") + "_D" + D(hit.point.x, hit.point.z, h, R).ToString("F1") + "_" + hit.collider.transform.parent?.name] = 999; nm += "@" + (Mathf.Round(hit.distance / 10f) * 10f); blk[nm] = blk.TryGetValue(nm, out var n0) ? n0 + 1 : 1; } }
  blk = blk.Where(k => !k.Key.StartsWith("PT")).ToDictionary(k => k.Key, k => k.Value);
  lines.Add("R" + R + " h" + h + " eye+" + eo + " floor" + (eye0.y + D(eye0.x, eye0.z, h, R)).ToString("F1") + " vis " + vis + "/" + targets.Count + " yaws " + string.Join(",", ys) + " | " + string.Join(" ", blk.OrderByDescending(k => k.Value).Take(8).Select(k => k.Key + ":" + k.Value)));
  }
  foreach (var o in objs) { var cur = o.mf.sharedMesh; o.mf.sharedMesh = o.orig; if (o.mc) o.mc.sharedMesh = o.orig; UnityEngine.Object.DestroyImmediate(cur); }
}
} finally { foreach (var o in objs) { if (o.mf.sharedMesh != o.orig) { o.mf.sharedMesh = o.orig; if (o.mc) o.mc.sharedMesh = o.orig; } } roomCol.enabled = true; Physics.SyncTransforms(); }
System.IO.File.WriteAllLines(H + "sim_out.txt", lines);
sb.Append(string.Join("\n", lines)); sb.Append("\ndirty=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty);
return sb.ToString();
