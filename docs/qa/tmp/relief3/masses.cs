// 2253: 낮은 칸이 집 패드(깎기) 때문인지 — diag_low.txt 칸마다 가장 가까운 Parcels_Plan 덩어리 바닥 상자까지 거리
var T = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/relief3/");
var root = GameObject.Find("Parcels_Plan"); if (root == null) return "no Parcels_Plan";
var rects = new System.Collections.Generic.List<Rect>();
var ms = root.transform.Find("Masses"); foreach (Transform t in ms) { var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); rects.Add(Rect.MinMaxRect(bb.min.x, bb.min.z, bb.max.x, bb.max.z)); }
int n = 0, near = 0; var inv = System.Globalization.CultureInfo.InvariantCulture; var far = new System.Text.StringBuilder();
foreach (var ln in System.IO.File.ReadAllLines(T + "diag_low.txt"))
{
    var a = ln.Split(' '); if (a.Length < 5) continue; var p = new Vector2(float.Parse(a[1], inv), float.Parse(a[2], inv)); n++;
    float best = 1e9f; foreach (var r in rects) { float dx = Mathf.Max(r.xMin - p.x, 0, p.x - r.xMax), dz = Mathf.Max(r.yMin - p.y, 0, p.y - r.yMax); best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz)); }
    if (best <= 3f) near++; else far.Append(ln + " d=" + best.ToString("F0") + "\n");
}
System.IO.File.WriteAllText(T + "diag_far.txt", far.ToString());
return $"masses {rects.Count} low {n} within3m {near} far {n - near}";
