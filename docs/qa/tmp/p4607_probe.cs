// 과제 0622(fb 4607): 길 가장자리 뜸 지점 주변 지형 정점(현재·_ridge 원본)과 가까운 길 — 읽기만
var sb = new System.Text.StringBuilder();
var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var tr = terrGo.transform; var mc = terrGo.GetComponent<MeshCollider>();
var cur = terrGo.GetComponent<MeshFilter>().sharedMesh; var org = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_ridge.asset");
var vc = cur.vertices; var vo = org.vertices; sb.Append(UnityEditor.AssetDatabase.GetAssetPath(cur) + " n=" + vc.Length + "\n");
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None);
foreach (var P in new[] { new Vector2(250.4f, -63.1f), new Vector2(287.5f, -64.7f), new Vector2(284.7f, -64.7f) }) {
  RaycastHit h; float ty = mc.Raycast(new Ray(new Vector3(P.x, 200, P.y), Vector3.down), out h, 400) ? h.point.y : float.NaN;
  sb.Append("== P " + P + " terrain " + ty.ToString("F2") + "\n");
  foreach (var r in roads) { var c = r.centerline; if (c == null) continue; float bd = 1e9f; Vector3 bq = Vector3.zero;
    for (int i = 1; i < c.Length; i++) { var ab = c[i] - c[i - 1]; ab.y = 0; var ap = new Vector3(P.x, 0, P.y) - c[i - 1]; ap.y = 0; float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); var q = Vector3.Lerp(c[i - 1], c[i], t); var d = new Vector2(P.x - q.x, P.y - q.z).magnitude; if (d < bd) { bd = d; bq = q; } }
    if (bd - r.width * .5f < 3f) sb.Append("  road " + r.name + " hw " + (r.width * .5f).ToString("F2") + " e " + (bd - r.width * .5f).ToString("F2") + " q.y " + bq.y.ToString("F2") + " ends " + c[0].ToString("F1") + " / " + c[c.Length - 1].ToString("F1") + "\n"); }
  for (int k = 0; k < vc.Length; k++) { var w = tr.TransformPoint(vc[k]); if (Mathf.Abs(w.x - P.x) < 2.1f && Mathf.Abs(w.z - P.y) < 2.1f) sb.Append("   v" + k + " (" + w.x.ToString("F1") + "," + w.z.ToString("F1") + ") y " + w.y.ToString("F2") + " org " + tr.TransformPoint(vo[k]).y.ToString("F2") + "\n"); }
}
return sb.ToString();
