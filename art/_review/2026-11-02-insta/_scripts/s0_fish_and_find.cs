var r = Still("01_fishing_sunset", null, null, 0f);
var sb = new System.Text.StringBuilder(r + "\n");
string[] keys = { "lighthouse", "등대", "light", "tower", "cart", "Cart", "Cargo", "cargo", "garage", "Garage", "depot", "Depot", "Q3_", "window", "Window", "alley", "골목", "Pier", "Quay" };
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
  if (t.parent != null && t.parent.parent != null && t.parent.parent.parent != null) continue;   // 깊은 건 건너뜀(상위 3단계까지)
  string n = t.name; bool hit = false; foreach (var k in keys) if (n.Contains(k)) hit = true;
  if (hit && seen.Add(n)) sb.Append(n + " @ " + t.position.ToString("F1") + "\n");
}
return sb.ToString();
