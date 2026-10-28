var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform;
var bld = gray.Find("Buildings");
foreach (Transform c in bld) { sb.Append("bld child " + c.name + " n=" + c.childCount + " comps=" + string.Join("/", c.GetComponents<Component>().Select(k => k.GetType().Name)) + "\n"); int i = 0; foreach (Transform d in c) { if (i++ < 3) sb.Append("   " + d.name + " comps=" + string.Join("/", d.GetComponents<Component>().Select(k => k.GetType().Name)) + " p=" + d.position.ToString("F1") + "\n"); } }
var P = new (string id, float x, float z, float yaw, Vector3 mn, Vector3 mx)[] {
 ("room", -117.7f, -11.2f, 0.9f, new Vector3(-1.75f,0,-3.16f), new Vector3(1.75f,0,0)),
 ("garage", -63.2f, -6.7f, -174.8f, new Vector3(-8f,0,-24.91f), new Vector3(8f,0,0)),
 ("depot", -43.7f, -26.8f, 2.7f, new Vector3(-5.01f,0,-7f), new Vector3(5f,0,1.01f)),
 ("postoffice", -57.7f, -25.6f, -5.3f, new Vector3(-5.34f,0,-8.86f), new Vector3(5.1f,0,1.08f)) };
var all = gray.GetComponentsInChildren<Collider>(true).Where(c => c.name != "TownV6_Terrain").ToList();
foreach (var p in P) {
  var q = Quaternion.Euler(0, p.yaw, 0); var o = new Vector3(p.x, 0, p.z);
  var hit = new Dictionary<string,int>();
  for (float u = p.mn.x - 1; u <= p.mx.x + 1.01f; u += 0.5f) for (float v = p.mn.z - 1; v <= p.mx.z + 1.01f; v += 0.5f) {
    var w = o + q * new Vector3(u, 0, v);
    foreach (var c in all) { RaycastHit h; if (c.Raycast(new Ray(w + Vector3.up * 300, Vector3.down), out h, 600)) { var k = c.transform.parent.name + "/" + c.name; hit[k] = hit.TryGetValue(k, out var nn) ? nn + 1 : 1; } }
  }
  sb.Append(p.id + ": " + string.Join(" ", hit.Select(kv => kv.Key + "x" + kv.Value)) + "\n");
}
return sb.ToString();
