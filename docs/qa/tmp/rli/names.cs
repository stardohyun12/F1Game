var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == g.roomName && t.parent != null) { room = t; break; }
var sb = new System.Text.StringBuilder();
sb.Append("room=" + room.name + " parent=" + room.parent.name + "\n");
var heads = new System.Collections.Generic.Dictionary<string,int>();
foreach (var r in room.GetComponentsInChildren<Renderer>(false)) {
  var n = r.name; int k = n.IndexOf('_'); var h = k > 0 ? n.Substring(0, k) : n;
  var col = r.GetComponent<Collider>() != null ? "C" : "-";
  heads[h + col] = heads.TryGetValue(h + col, out var v) ? v + 1 : 1;
}
foreach (var kv in heads) sb.Append(kv.Key + ":" + kv.Value + " ");
sb.Append("\nparentKids: ");
foreach (Transform c in room.parent) sb.Append(c.name + " ");
return sb.ToString();
