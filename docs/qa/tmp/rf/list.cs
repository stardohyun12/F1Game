var sb = new System.Text.StringBuilder();
UnityEngine.Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) if (t.name == "ROOM_2F") { room = t; break; }
sb.Append("room parent=" + room.parent.name + " grand=" + (room.parent.parent ? room.parent.parent.name : "-") + "\n");
var house = room.parent;
foreach (var r in house.GetComponentsInChildren<UnityEngine.Renderer>(true)) {
  var mats = r.sharedMaterials; var s = "";
  foreach (var m in mats) s += (m ? m.name + "[" + m.shader.name + "|" + UnityEditor.AssetDatabase.GetAssetPath(m) + "|" + (m.mainTexture ? m.mainTexture.name : "-") + "]" : "null") + " ";
  sb.Append(UnityEditor.AnimationUtility.CalculateTransformPath(r.transform, house) + " :: " + s + "\n");
}
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>(); sb.Append("indoorDot=" + (g ? g.indoorDot.ToString() : "none"));
return sb.ToString();
