var sb = new System.Text.StringBuilder();
var lh = GameObject.Find("STOP_lighthouse");
sb.Append("STOP_lighthouse children=" + lh.transform.childCount + " pos=" + lh.transform.position + "\n");
foreach (Transform c in lh.transform) sb.Append("  " + c.name + " " + c.position.ToString("F1") + "\n");
// 이름에 lighthouse/등대 가 든 모든 오브젝트의 렌더러 경계
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
  string n = t.name.ToLower(); if (!(n.Contains("lighthouse") || n.Contains("등대"))) continue;
  var rr = t.GetComponentsInChildren<Renderer>(); if (rr.Length == 0) continue;
  var b = rr[0].bounds; foreach (var r in rr) b.Encapsulate(r.bounds);
  sb.Append(t.name + " renderers=" + rr.Length + " bounds center=" + b.center.ToString("F1") + " size=" + b.size.ToString("F1") + "\n");
}
// 해안 바닥 높이 몇 점
foreach (var p in new[] { new Vector2(-340, -340), new Vector2(-300, -330), new Vector2(-250, -320), new Vector2(-200, -300), new Vector2(-100, -290), new Vector2(-378, -361) }) {
  RaycastHit h; bool ok = Physics.Raycast(new Vector3(p.x, 100, p.y), Vector3.down, out h, 300f);
  sb.Append(p + " ground " + (ok ? h.point.y.ToString("F1") + " " + h.collider.name : "none") + "\n");
}
return sb.ToString();
