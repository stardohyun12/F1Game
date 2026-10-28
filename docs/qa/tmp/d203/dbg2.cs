// 패드 근처 길 정점의 소속 길 이름·범위
var root = GameObject.Find("TownV6_Gray").transform;
var rects = new Dictionary<string, Rect>();
foreach (var n in new[] { "D198_depot", "D198_garage", "house_home", "D198_postoffice" }) { var t = root.Find("D198_Buildings/" + n); var b = new Bounds(); bool f = true; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); } rects[n] = Rect.MinMaxRect(b.min.x - 1.5f, b.min.z - 1.5f, b.max.x + 1.5f, b.max.z + 1.5f); }
float RD(Rect r, float x, float z) { float dx = Mathf.Max(r.xMin - x, 0, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0, z - r.yMax); return Mathf.Sqrt(dx * dx + dz * dz); }
var sb = new System.Text.StringBuilder();
foreach (Transform r in root.Find("Roads")) { var mf = r.GetComponent<MeshFilter>(); if (!mf) continue; foreach (var kv in rects) { var near = mf.sharedMesh.vertices.Select(v => r.TransformPoint(v)).Where(w => RD(kv.Value, w.x, w.z) < 6f).ToList(); if (near.Count > 0) sb.Append(kv.Key + " <- " + r.name + " n" + near.Count + " minDist " + near.Min(w => RD(kv.Value, w.x, w.z)).ToString("F1") + " ex " + near[0].ToString("F1") + " y " + near.Min(w => w.y).ToString("F1") + ".." + near.Max(w => w.y).ToString("F1") + "\n"); } }
return sb.ToString();
