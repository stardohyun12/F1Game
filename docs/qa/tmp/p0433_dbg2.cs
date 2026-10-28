// 과제 0433: 보관소·우체국 발자국 3 m 안을 지나는 길 + 그 높이
var sb = new System.Text.StringBuilder();
foreach (var nm in new[] { "D198_depot", "D198_postoffice" }) {
  var t = GameObject.Find("TownV6_Gray/D198_Buildings/" + nm).transform;
  foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c == null) continue;
    float best = 1e9f, y = 0; Vector3 at = Vector3.zero;
    for (int i = 0; i < c.Length; i++) { var lp = t.InverseTransformPoint(c[i]); float dx = Mathf.Max(0, Mathf.Abs(lp.x) - 5.1f), dz = Mathf.Max(0, Mathf.Max(lp.z - 1.1f, -7.1f - lp.z)); if (nm == "D198_postoffice") { dx = Mathf.Max(0, Mathf.Abs(lp.x) - 5.3f); }
      float e = new Vector2(dx, dz).magnitude - r.width * .5f; if (e < best) { best = e; y = c[i].y; at = c[i]; } }
    if (best < 3f) sb.Append(nm + " ← " + r.roadId + " (" + r.kind + ", w" + r.width + ") 가장자리 거리 " + best.ToString("F1") + " m, 길 높이 " + y.ToString("F2") + " at " + at.ToString("F1") + ", 바닥 " + t.position.y.ToString("F2") + "\n"); } }
return sb.ToString();
