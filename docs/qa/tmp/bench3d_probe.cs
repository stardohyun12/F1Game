var spot = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(UnityEngine.FindObjectsInactive.Include);
var sb = new System.Text.StringBuilder();
var t2 = UnityEngine.Object.FindFirstObjectByType<PackTable2>(UnityEngine.FindObjectsInactive.Include);
if (t2 != null) {
  var ws = t2.workSpot; sb.Append("workSpot " + ws.position.ToString("F3") + " fwd " + ws.forward.ToString("F2") + "\n");
  var r = t2.GetComponent<Renderer>(); if (r != null) sb.Append("table " + t2.name + " bounds " + r.bounds.center.ToString("F3") + " size " + r.bounds.size.ToString("F3") + " mat " + r.sharedMaterial.name + "\n");
  var p = t2.transform.parent; sb.Append("parent " + (p ? p.name : "-") + "\n");
  if (p != null) foreach (var rr in p.GetComponentsInChildren<Renderer>(true)) { if (rr.bounds.size.x > 0.8f || rr.bounds.size.z > 0.8f) sb.Append("  r " + rr.name + " " + rr.bounds.center.ToString("F2") + " " + rr.bounds.size.ToString("F2") + " " + (rr.sharedMaterial ? rr.sharedMaterial.name : "-") + " en=" + rr.enabled + "\n"); }
}
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
  float dd = Vector3.Distance(l.transform.position, new Vector3(274.4f, 32.2f, -29.5f));
  if (l.type == LightType.Directional || dd < 15f) sb.Append("light " + l.name + " " + l.type + " d=" + dd.ToString("F1") + " int=" + l.intensity + " shadows=" + l.shadows + " mode=" + l.lightmapBakeType + " act=" + l.gameObject.activeInHierarchy + " pos " + l.transform.position.ToString("F1") + " rng " + l.range + "\n");
}
foreach (var c in Camera.allCameras) sb.Append("cam " + c.name + " fov " + c.fieldOfView + " aspect " + c.aspect + " tex " + (c.targetTexture ? c.targetTexture.width + "x" + c.targetTexture.height : "-") + "\n");
var ph = System.Type.GetType("PackHands, Assembly-CSharp"); sb.Append("PackHands type " + (ph != null) + "\n");
sb.Append("shadowDist " + QualitySettings.shadowDistance);
return sb.ToString();
