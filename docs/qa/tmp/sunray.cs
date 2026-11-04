var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == g.roomName && t.parent != null) { room = t; break; }
Matrix4x4 box; A7ShadeGlobals.TryRoomBox(room, out box); var inv = box.inverse;
var sun = RenderSettings.sun; Vector3 toSun = -sun.transform.forward;
int lit = 0, n = 0; var sb = new System.Text.StringBuilder();
var hits = new System.Collections.Generic.Dictionary<string,int>();
for (float x = -0.9f; x <= 0.91f; x += 0.15f) for (float z = -0.9f; z <= 0.91f; z += 0.15f) {
  Vector3 p = inv.MultiplyPoint3x4(new Vector3(x, -0.95f, z)); n++;
  RaycastHit h;
  if (!Physics.Raycast(p, toSun, out h, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { lit++; sb.Append("(" + x.ToString("F1") + "," + z.ToString("F1") + ")"); }
  else { int c; hits[h.collider.name] = hits.TryGetValue(h.collider.name, out c) ? c + 1 : 1; }
}
string hs = ""; foreach (var kv in hits) hs += kv.Key + "x" + kv.Value + " ";
// wall facing: outward normal of each box face in world vs light
string faces = "";
foreach (var f in new[]{ Vector3.right, Vector3.left, Vector3.forward, Vector3.back }) { Vector3 wn = inv.MultiplyVector(f).normalized; faces += f + ":" + Vector3.Dot(wn, toSun).ToString("F2") + " "; }
return "toSun " + toSun.ToString("F2") + " floor lit " + lit + "/" + n + " " + sb + " | blockers " + hs + " | faces·toSun " + faces;
