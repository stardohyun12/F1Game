var sb = new System.Text.StringBuilder();
Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == "ROOM_2F" && t.parent != null) { room = t; break; }
Matrix4x4 box; A7ShadeGlobals.TryRoomBox(room, out box);
var inv = box.inverse;
sb.AppendLine("room " + room.parent.name + "/" + room.name + " center " + inv.MultiplyPoint3x4(Vector3.zero) + " ax " + inv.MultiplyVector(Vector3.right) + " " + inv.MultiplyVector(Vector3.up) + " " + inv.MultiplyVector(Vector3.forward));
foreach (var r in room.parent.GetComponentsInChildren<Renderer>(true)) {
  if (!r.name.StartsWith("GLASS_")) continue;
  var q = box.MultiplyPoint3x4(r.bounds.center);
  sb.AppendLine("glass " + r.name + " q " + q.ToString("F2") + " size " + r.bounds.size.ToString("F2") + " min.y " + r.bounds.min.y.ToString("F2") + " mat " + (r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "-") + " en " + r.enabled + " act " + r.gameObject.activeInHierarchy);
}
foreach (var r in room.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("DESK_") || r.name.StartsWith("FLOOR") || r.name.StartsWith("WALL")) sb.AppendLine("room r " + r.name + " " + r.bounds.center.ToString("F1") + " mat " + (r.sharedMaterial? r.sharedMaterial.shader.name:"-"));
var sun = RenderSettings.sun; sb.AppendLine("sun " + (sun? sun.name + " int " + sun.intensity + " sh " + sun.shadows + " rot " + sun.transform.eulerAngles : "null"));
sb.AppendLine("terrains " + Terrain.activeTerrains.Length + " skybox " + (RenderSettings.skybox? RenderSettings.skybox.name + "/" + RenderSettings.skybox.shader.name : "null") + " fog " + RenderSettings.fog);
var shaders = new System.Collections.Generic.Dictionary<string,int>();
var wb = new Bounds(inv.MultiplyPoint3x4(Vector3.zero), Vector3.one * 30f);
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) { if (!r.bounds.Intersects(wb)) continue; string k = r.sharedMaterial && r.sharedMaterial.shader ? r.sharedMaterial.shader.name : "-"; int c; shaders[k] = shaders.TryGetValue(k, out c) ? c + 1 : 1; if (k != "PitCrew/SceneA7/Lit") sb.AppendLine("  nonA7 near " + r.name + " " + k + " " + r.bounds.center.ToString("F0") + " sz " + r.bounds.size.ToString("F0")); }
foreach (var kv in shaders) sb.AppendLine("shader " + kv.Key + " x" + kv.Value);
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) sb.AppendLine("cam " + c.name + " clear " + c.clearFlags + " bg " + c.backgroundColor);
return sb.ToString();
