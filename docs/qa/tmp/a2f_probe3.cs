var cam = GameObject.Find("Cam_B_Game").GetComponent<Camera>(); var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
  var sp = cam.WorldToViewportPoint(r.bounds.center); float dd = Vector3.Distance(cam.transform.position, r.bounds.center);
  if (sp.z > 0 && sp.x > 0 && sp.x < 1 && sp.y > 0 && sp.y < 0.55f && dd < 25) sb.AppendLine(r.name + " | " + r.sharedMaterial.name + " | vp " + sp.x.ToString("F2") + "," + sp.y.ToString("F2") + " d " + dd.ToString("F1"));
}
return sb.ToString();
