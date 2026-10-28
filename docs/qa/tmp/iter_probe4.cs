// artlive 1532: 보관소 주변 이웃 건물 조사(읽기만)
var log = new System.Text.StringBuilder();
var root = GameObject.Find("ITER_Depot").transform; var c = root.TransformPoint(new Vector3(0, 2, -3.5f));
foreach (Transform t in GameObject.Find("TownV6_Gray").transform.Find("Buildings")) { log.Append("Buildings/" + t.name + " kids " + t.childCount + "\n"); int k = 0; foreach (Transform u in t) { if (k++ < 6) log.Append("   " + u.name + "\n"); } }
foreach (var col in Physics.OverlapSphere(c, 14f, ~0, QueryTriggerInteraction.Ignore)) { if (col.name.Contains("Terrain") || col.name.StartsWith("ROAD")) continue; var mf = col.GetComponent<MeshFilter>(); var r = col.GetComponent<Renderer>();
  log.Append("COL " + AnimationUtility.CalculateTransformPath(col.transform, null) + " " + col.GetType().Name + " verts " + (mf && mf.sharedMesh ? mf.sharedMesh.vertexCount : -1) + " rbounds " + (r ? r.bounds.size.ToString("F1") : "-") + " mat " + (r ? r.sharedMaterial.name : "-") + "\n"); }
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) { if (r.transform.IsChildOf(root)) continue; var b = r.bounds; if (b.SqrDistance(c) > 14f * 14f || b.size.magnitude > 80f) continue; log.Append("REN " + AnimationUtility.CalculateTransformPath(r.transform, null) + " b " + b.center.ToString("F1") + " s " + b.size.ToString("F1") + " col " + (r.GetComponent<Collider>() ? "Y" : "N") + "\n"); }
return log.ToString();
