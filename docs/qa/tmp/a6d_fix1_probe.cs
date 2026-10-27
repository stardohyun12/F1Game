// 과제 1815 탐색: 집 앞 연석 콜라이더·길 벽 재질·집 소품 콜라이더·카트 구조
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6d_Town.unity");
var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var b = c.bounds; var r = c.GetComponent<MeshRenderer>();
  bool hv = b.size.y > 0.5f && b.size.y < 1.3f;
  string p = c.transform.name; var tt = c.transform.parent; for (int i = 0; i < 3 && tt != null; i++) { p = tt.name + "/" + p; tt = tt.parent; }
  if (hv || p.Contains("Curb") || p.Contains("curb") || p.Contains("Wall")) sb.Append(p + " en " + c.enabled + " rend " + (r != null && r.enabled) + " c " + b.center.ToString("F1") + " s " + b.size.ToString("F2") + " rot " + c.transform.eulerAngles.ToString("F0") + "\n");
}
foreach (var n in new[] { "Wall_L", "Wall_R" }) { var w = GameObject.Find(n); if (w != null) { var r = w.GetComponentInChildren<MeshRenderer>(); sb.Append(n + " path? mat " + (r ? r.sharedMaterial.name + " " + AssetDatabase.GetAssetPath(r.sharedMaterial) : "none") + " b " + (r ? r.bounds.size.ToString("F2") : "") + "\n"); } }
foreach (Transform t in GameObject.Find("SceneA4").transform.Find("A4b_Houses")) {
  var inst = t.GetChild(0); var mf = inst.GetComponentInChildren<MeshFilter>(); var cs = mf.GetComponents<BoxCollider>(); int en = 0; foreach (var c in cs) if (c.enabled) en++;
  sb.Append(t.name + " pos " + t.position.ToString("F1") + " rot " + t.eulerAngles.ToString("F0") + " inst " + AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(inst.gameObject)) + " mat " + inst.GetComponentInChildren<MeshRenderer>().sharedMaterial.name + " boxes " + cs.Length + " en " + en + " mfrot " + mf.transform.localEulerAngles.ToString("F0") + "\n");
}
var kv = GameObject.Find("Race").transform.Find("A6_KartVisual");
sb.Append("kart " + kv.localPosition.ToString("F2") + " sc " + kv.localScale.ToString("F2") + " children:");
foreach (Transform c in kv) sb.Append(" " + c.name + "(" + c.localEulerAngles.ToString("F0") + "," + c.childCount + "," + string.Join("/", c.GetComponents<Component>().Select(x => x.GetType().Name)) + ")");
foreach (var kw in kv.GetComponentsInChildren<MonoBehaviour>(true)) sb.Append("\n mb " + kw.GetType().Name + " on " + kw.name);
return sb.ToString();
