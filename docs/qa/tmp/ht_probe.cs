var sb = new System.Text.StringBuilder();
foreach (Transform t in GameObject.Find("STUDIO").transform) {
  var b = new Bounds(t.position, Vector3.zero); bool f = false;
  foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); }
  sb.Append(t.name + " " + b.min.ToString("F1") + ".." + b.max.ToString("F1") + " | ");
  if (t.name == "STUDIO_Slot") foreach (Transform c in t) { var bb = new Bounds(c.position, Vector3.zero); bool g = false; foreach (var r in c.GetComponentsInChildren<Renderer>(true)) { if (!g) { bb = r.bounds; g = true; } else bb.Encapsulate(r.bounds); } sb.Append("  [" + c.name + (c.gameObject.activeSelf ? "+" : "-") + " " + bb.min.ToString("F1") + ".." + bb.max.ToString("F1") + "]"); }
}
var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
foreach (var l in sun) if (l.type == LightType.Directional) sb.Append(" SUN " + l.transform.forward.ToString("F2"));
return sb.ToString();
