// 집 발코니 눈높이(바닥+3.5)에서 남쪽 바다 점까지 막는 것: OSM 폴리곤 id 별 집계
var root = GameObject.Find("TownV6_Gray").transform;
var home = root.Find("D198_Buildings/house_home"); var hc = home.GetComponentsInChildren<Collider>().Where(c => c.enabled).ToList(); foreach (var c in hc) c.enabled = false; Physics.SyncTransforms();
var polys = System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/d203/polys.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return (id: a[0], pts: a.Skip(2).Select(q => { var p = q.Split(','); return new Vector2(float.Parse(p[0]), float.Parse(p[1])); }).ToArray()); }).ToList();
string Near(Vector3 h) { string best = "?"; float bd = 4; foreach (var p in polys) foreach (var q in p.pts) { float d = Vector2.Distance(q, new Vector2(h.x, h.z)); if (d < bd) { bd = d; best = p.id; } } return best; }
var eye = new Vector3(185, 24.9f + 3.5f, -61);
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600); return hs.Length == 0 || hs.Max(h => h.point.y) < 0.05f; }
var blk = new Dictionary<string, int>(); int tot = 0, vis = 0;
for (int yaw = 130; yaw <= 250; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0; for (float d = 60; d < 900 && k < 3; d += 5) { float x = 185 + s * d, z = -61 + c * d; if (!SeaAt(x, z)) continue; k++; d += 25; tot++; var tp = new Vector3(x, 0.5f, z);
  if (!Physics.Linecast(eye, tp, out var hit)) { vis++; continue; }
  string key = hit.collider.name.StartsWith("Walls") || hit.collider.name.StartsWith("Roofs") ? "osm" + Near(hit.point) : hit.collider.name;
  key += "@" + Mathf.Round(hit.distance) + "m(" + hit.point.x.ToString("F0") + "," + hit.point.z.ToString("F0") + ")";
  blk[key] = blk.TryGetValue(key, out var n) ? n + 1 : 1; } }
foreach (var c in hc) c.enabled = true; Physics.SyncTransforms();
return "vis " + vis + "/" + tot + "\n" + string.Join("\n", blk.OrderByDescending(k => k.Value).Take(25).Select(k => k.Key + " x" + k.Value));
