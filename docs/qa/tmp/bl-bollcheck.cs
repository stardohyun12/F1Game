var cps = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.kind == "main" || r.kind == "village" || r.kind == "shortcut").ToList();
var bs = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("INTENT_Bollard")).ToList();
int on = 0; var ex = new System.Collections.Generic.List<string>();
foreach (var b in bs)
  foreach (var r in cps)
  {
    float best = 1e9f; foreach (var q in r.centerline) { float d = new Vector2(q.x - b.position.x, q.z - b.position.z).magnitude; if (d < best) best = d; }
    if (best < r.width * .5f + 0.3f) { on++; if (ex.Count < 6) ex.Add(b.name + "@" + r.roadId + b.position.ToString("F0")); break; }
  }
return "bollards " + bs.Count + " onCar " + on + " " + string.Join(" ", ex);
