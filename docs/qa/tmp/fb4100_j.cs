var sb = new System.Text.StringBuilder(); var inv = System.Globalization.CultureInfo.InvariantCulture;
var terrGo = GameObject.Find("TownV6_Gray").transform.Find("TownV6_Terrain"); var tc = terrGo.GetComponent<MeshCollider>();
System.Func<float, float, float> G = (x, z) => tc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600) ? h.point.y : float.NaN;
var dep = GameObject.Find("D198_depot"); if (dep != null) { var b = new Bounds(dep.transform.position, Vector3.zero); foreach (var r in dep.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds); sb.AppendLine("depot pos " + dep.transform.position.ToString("F2") + " bounds " + b.min.ToString("F1") + " " + b.max.ToString("F1")); }
foreach (var nm in new[] { "ROAD_V2j_29", "ROAD_V2e_ridge_up_b_s0_2" })
{
    var go = GameObject.Find(nm); var mc = go.GetComponent<MeshCollider>(); var m = mc.sharedMesh; var v = m.vertices;
    sb.AppendLine(nm + " colverts " + v.Length);
    int step = nm.Contains("V2j") ? 1 : 1;
    for (int i = 0; i < v.Length; i += step) { var w = go.transform.TransformPoint(v[i]); float g = G(w.x, w.z); sb.AppendLine(i + "\t" + w.x.ToString("F2", inv) + "\t" + w.z.ToString("F2", inv) + "\troad " + w.y.ToString("F3", inv) + "\tgnd " + g.ToString("F3", inv) + "\td " + (w.y - g).ToString("F3", inv)); }
}
return sb.ToString();
