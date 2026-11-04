var sb = new System.Text.StringBuilder();
UnityEngine.Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) if (t.name == "ROOM_2F") { room = t; break; }
foreach (var r in room.GetComponentsInChildren<UnityEngine.Renderer>(true)) {
  var s = "";
  foreach (var m in r.sharedMaterials) s += (m ? m.name + "|" + UnityEditor.AssetDatabase.GetAssetPath(m) : "null") + " ";
  sb.Append(UnityEditor.AnimationUtility.CalculateTransformPath(r.transform, room) + " :: " + s + "\n");
}
var hm = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Library/Buildings/house_home/M_house_home_house_home.mat");
foreach (var n in hm.GetTexturePropertyNames()) { var tx = hm.GetTexture(n); sb.Append("tex " + n + "=" + (tx ? tx.name + " " + tx.width + "x" + tx.height + " " + UnityEditor.AssetDatabase.GetAssetPath(tx) : "-") + "\n"); }
var shell = room.parent.Find("house_home/HOUSE_shell");
sb.Append("room children=" + room.childCount + " renderers=" + room.GetComponentsInChildren<UnityEngine.Renderer>(true).Length + "\n");
var mf = shell.GetComponent<UnityEngine.MeshFilter>(); sb.Append("shell mesh=" + mf.sharedMesh.name + " path=" + UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh) + " sub=" + mf.sharedMesh.subMeshCount + " v=" + mf.sharedMesh.vertexCount);
return sb.ToString();
