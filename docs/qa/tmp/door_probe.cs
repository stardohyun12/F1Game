RenderSettings.sun.shadows = LightShadows.Soft;
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
  if (!(r.name.StartsWith("DOOR_") || r.name.StartsWith("HOUSE_shell") || r.name.StartsWith("WINDOW") || r.name.StartsWith("FRAME"))) continue;
  if ((r.bounds.center - new Vector3(187.16f, 29.8f, -62.57f)).magnitude > 8f) continue;
  sb.Append(r.name + " b " + r.bounds.center.ToString("F1") + " sz " + r.bounds.size.ToString("F1") + " cast " + r.shadowCastingMode + " mats:");
  foreach (var m in r.sharedMaterials) sb.Append(" " + (m ? m.name + "/" + m.shader.name + (m.HasProperty("_Cutoff") ? " cut" + m.GetFloat("_Cutoff").ToString("F2") : "") : "null"));
  var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) sb.Append(" sub " + mf.sharedMesh.subMeshCount + " v " + mf.sharedMesh.vertexCount);
  sb.AppendLine();
}
return sb.ToString();
