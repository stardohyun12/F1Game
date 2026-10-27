// 과제 0535: Scene_A7_Light 구조 조사 — 정비소·집·카메라·길 위치
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A7_Light.unity");
var sb = new System.Text.StringBuilder();
var root = GameObject.Find("SceneA4").transform;
foreach (Transform t in root) {
  var b = new Bounds(); bool f = false; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); }
  sb.Append(t.name + " act " + t.gameObject.activeSelf + " ch " + t.childCount + (f ? " c " + b.center.ToString("F1") + " s " + b.size.ToString("F1") : "") + "\n"); }
var g = root.Find("garage_v5_combo");
sb.Append("garage pos " + g.position.ToString("F2") + " rot " + g.eulerAngles.ToString("F0") + " sc " + g.lossyScale.ToString("F2") + "\n");
foreach (var r in g.GetComponentsInChildren<Renderer>()) sb.Append("  " + r.name + " [" + string.Join(",", r.sharedMaterials.Select(m => m ? m.name + "/" + (m.GetTexture("_BaseMap") ? m.GetTexture("_BaseMap").name : "-") : "null")) + "] cols " + r.GetComponents<Collider>().Length + "\n");
foreach (Transform t in root.Find("A4b_Houses")) sb.Append("house " + t.name + " " + t.position.ToString("F1") + " rot " + t.eulerAngles.y.ToString("F0") + " child0 " + t.GetChild(0).localPosition.ToString("F1") + " " + t.GetChild(0).localEulerAngles.ToString("F0") + "\n");
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("cam " + c.name + " act " + c.gameObject.activeInHierarchy + " " + c.transform.position.ToString("F1") + " e " + c.transform.eulerAngles.ToString("F0") + " fov " + c.fieldOfView + " lr " + (c.GetComponent<LowResRenderTest>() != null) + "\n");
var gm = g.GetComponentInChildren<Renderer>().sharedMaterial; sb.Append("garage mat shader " + gm.shader.name + " path " + AssetDatabase.GetAssetPath(gm) + "\n");
return sb.ToString();
