var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform;
foreach (Transform c in gray) sb.Append("gray child " + c.name + " n=" + c.childCount + " comps=" + string.Join("/", c.GetComponents<Component>().Select(k => k.GetType().Name)) + "\n");
var terr = GameObject.Find("TownV6_Terrain"); var mf = terr.GetComponent<MeshFilter>();
sb.Append("terrain mesh=" + (mf ? mf.sharedMesh.name + " v=" + mf.sharedMesh.vertexCount + " asset=" + UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh) : "none") + " par=" + terr.transform.parent.name + "\n");
var tcol = terr.GetComponent<Collider>();
var P = new (string id, float x, float z, float yaw, Vector3 mn, Vector3 mx)[] {
 ("room", -117.7f, -11.2f, 0.9f, new Vector3(-1.75f,0,-3.16f), new Vector3(1.75f,0,0)),
 ("garage", -63.2f, -6.7f, -174.8f, new Vector3(-8f,0,-24.91f), new Vector3(8f,0,0)),
 ("depot", -43.7f, -26.8f, 2.7f, new Vector3(-5.01f,0,-7f), new Vector3(5f,0,1.01f)),
 ("postoffice", -57.7f, -25.6f, -5.3f, new Vector3(-5.34f,0,-8.86f), new Vector3(5.1f,0,1.08f)) };
var bs = gray.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("B_")).ToList();
foreach (var p in P) {
  var q = Quaternion.Euler(0, p.yaw, 0); var o = new Vector3(p.x, 0, p.z);
  float lo = 1e9f, hi = -1e9f, fc = 0; int n = 0;
  for (float u = p.mn.x; u <= p.mx.x + 0.01f; u += 1f) for (float v = p.mn.z; v <= p.mx.z + 0.01f; v += 1f) {
    var w = o + q * new Vector3(u, 0, v); RaycastHit h;
    if (tcol.Raycast(new Ray(w + Vector3.up * 300, Vector3.down), out h, 600)) { lo = Mathf.Min(lo, h.point.y); hi = Mathf.Max(hi, h.point.y); n++; }
  }
  RaycastHit hf; if (tcol.Raycast(new Ray(o + Vector3.up * 300, Vector3.down), out hf, 600)) fc = hf.point.y;
  sb.Append(p.id + " terrain lo=" + lo.ToString("F2") + " hi=" + hi.ToString("F2") + " frontCenter=" + fc.ToString("F2") + " n=" + n + "\n");
  // overlap: B_ renderer bounds xz vs footprint (expanded 0.5)
  var inv = Quaternion.Inverse(q); var hitB = new List<string>();
  foreach (var b in bs) {
    var r = b.GetComponent<Renderer>(); if (!r) continue; var bb = r.bounds;
    bool ov = false;
    for (float u = bb.min.x; u <= bb.max.x && !ov; u += 0.5f) for (float v = bb.min.z; v <= bb.max.z && !ov; v += 0.5f) {
      var l = inv * (new Vector3(u, 0, v) - o);
      if (l.x > p.mn.x - 1 && l.x < p.mx.x + 1 && l.z > p.mn.z - 1 && l.z < p.mx.z + 1) {
        // confirm point is inside building mesh via raycast down on its collider
        var bc = b.GetComponent<Collider>(); RaycastHit bh;
        if (bc == null || bc.Raycast(new Ray(new Vector3(u, 300, v), Vector3.down), out bh, 600)) ov = true;
      }
    }
    if (ov) hitB.Add(b.name + "@" + bb.center.ToString("F0"));
  }
  sb.Append("   overlapB=" + hitB.Count + " " + string.Join(" ", hitB) + "\n");
}
return sb.ToString();
