UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A2d_Town.unity");
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var m = r.sharedMaterial; if (m == null) continue; string n = m.name;
  if (!(n.Contains("asphalt") || n.Contains("grass") || n.Contains("yard") || n.Contains("drum") || n.Contains("far_town") || n.Contains("tree_01") || n.Contains("tires"))) continue;
  var mf = r.GetComponent<MeshFilter>(); string uv = "";
  if (mf && mf.sharedMesh) { var u = mf.sharedMesh.uv; if (u.Length > 0) { Vector2 lo = u[0], hi = u[0]; foreach (var q in u) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); } uv = "uv " + lo + "-" + hi + " v" + u.Length + " sub" + mf.sharedMesh.subMeshCount; } }
  sb.AppendLine(r.name + " | " + n + " | " + uv + " | st " + m.GetTextureScale("_BaseMap") + " | mats " + r.sharedMaterials.Length + " | size " + r.bounds.size);
}
return sb.ToString();
