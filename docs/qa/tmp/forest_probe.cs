// artlive 20261031-1926: 비탈 왼쪽이 검게 나오는 원인 찾기(읽기만) — 씬 루트·해 방향·비탈 법선
var sb = new System.Text.StringBuilder();
foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
  var b = new Bounds(g.transform.position, Vector3.zero); foreach (var r in g.GetComponentsInChildren<Renderer>(false)) b.Encapsulate(r.bounds);
  sb.Append("root " + g.name + " active " + g.activeSelf + " " + b.min.ToString("F0") + ".." + b.max.ToString("F0") + "\n"); }
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sb.Append("sun " + l.name + " fwd " + l.transform.forward.ToString("F2") + " shadows " + l.shadows + "\n");
sb.Append("shadowDist " + QualitySettings.shadowDistance + "\n");
var sl = GameObject.Find("STUDIO/STUDIO_Slot/forest/FOREST_slope"); var me = sl.GetComponent<MeshFilter>().sharedMesh;
for (int i = 0; i < me.vertexCount; i += 61) sb.Append("v " + me.vertices[i].ToString("F1") + " n " + me.normals[i].ToString("F2") + "\n");
foreach (Transform t in GameObject.Find("STUDIO").transform) { var r = t.GetComponent<Renderer>(); if (r) sb.Append(t.name + " cast " + r.shadowCastingMode + " mat " + r.sharedMaterial.name + " sh " + r.sharedMaterial.shader.name + "\n"); }
return sb.ToString();
