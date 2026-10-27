// 과제 1124 조사: 재질별 텍셀 밀도(텍셀/m), 셰이더, 하늘, 카메라 B
var sb = new System.Text.StringBuilder();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A2f_Town.unity");
var acc = new System.Collections.Generic.Dictionary<Material, double[]>();
var cnt = new System.Collections.Generic.Dictionary<Material, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; var m = mf.sharedMesh;
  var v = m.vertices; var uv = m.uv; if (uv == null || uv.Length != v.Length) continue; var L = r.transform.localToWorldMatrix;
  for (int s = 0; s < m.subMeshCount && s < r.sharedMaterials.Length; s++) {
    var mat = r.sharedMaterials[s]; if (!mat) continue; var tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null; if (!tex) continue;
    var st = mat.GetVector("_BaseMap_ST"); var t = m.GetTriangles(s); double wa = 0, ua = 0;
    for (int i = 0; i < t.Length; i += 3) {
      Vector3 a = L.MultiplyPoint3x4(v[t[i]]), b = L.MultiplyPoint3x4(v[t[i+1]]), c = L.MultiplyPoint3x4(v[t[i+2]]);
      wa += Vector3.Cross(b - a, c - a).magnitude * 0.5;
      Vector2 p = uv[t[i]], q = uv[t[i+1]], w = uv[t[i+2]];
      ua += Mathf.Abs((q.x - p.x) * (w.y - p.y) - (w.x - p.x) * (q.y - p.y)) * 0.5 * tex.width * tex.height * Mathf.Abs(st.x * st.y);
    }
    if (!acc.ContainsKey(mat)) { acc[mat] = new double[2]; cnt[mat] = 0; } acc[mat][0] += wa; acc[mat][1] += ua; cnt[mat]++;
  }
}
foreach (var kv in acc) { var tx = kv.Key.GetTexture("_BaseMap"); sb.AppendLine(kv.Key.name + " | " + kv.Key.shader.name + " | " + UnityEditor.AssetDatabase.GetAssetPath(tx) + " " + tx.width + "x" + tx.height + " | n" + cnt[kv.Key] + " | area " + kv.Value[0].ToString("F0") + " | tex/m " + System.Math.Sqrt(kv.Value[1] / System.Math.Max(1e-6, kv.Value[0])).ToString("F1")); }
sb.AppendLine("sky: " + RenderSettings.skybox + " | " + (RenderSettings.skybox ? RenderSettings.skybox.shader.name : ""));
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (r.name.ToLower().Contains("sky") || r.name.ToLower().Contains("cloud") || r.name.ToLower().Contains("sea")) sb.AppendLine("obj " + r.name + " " + r.GetType().Name + " " + (r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "") + " " + r.bounds.size);
var cam = GameObject.Find("Cam_B_Game").GetComponent<Camera>(); sb.AppendLine("camB " + cam.transform.position + " fwd " + cam.transform.forward + " fov " + cam.fieldOfView + " clear " + cam.clearFlags);
var road = GameObject.Find("Road"); sb.AppendLine("road " + road.transform.position + " " + road.transform.lossyScale + " parent " + road.transform.parent.name);
foreach (Transform ch in road.transform.parent) sb.Append(ch.name + ", ");
return sb.ToString();
