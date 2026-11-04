var sb = new System.Text.StringBuilder();
var center = new Vector3(187.16f, 29.8f, -62.57f);
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
  if (!(r.name == "GLASS_17" || r.name == "GLASS_13" || r.name == "GLASS_18")) continue;
  Vector3 p = r.bounds.center; Vector3 dir = p - center; dir.y = 0; dir.Normalize();
  // 방 안 0.5 m 에서 유리를 지나 바깥 3 m 까지 — 렌더 메시 기준으로 보려고 RaycastAll
  var o = p - dir * 0.6f;
  var hs = Physics.RaycastAll(o, dir, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
  sb.Append(r.name + " p " + p.ToString("F2") + " dir " + dir.ToString("F2") + " hits:");
  foreach (var h in hs) sb.Append(" " + h.collider.name + "@" + h.distance.ToString("F2"));
  sb.AppendLine();
}
var shell = GameObject.Find("HOUSE_shell");
if (shell) { var mc = shell.GetComponent<MeshCollider>(); sb.AppendLine("shell collider " + (shell.GetComponent<Collider>() ? shell.GetComponent<Collider>().GetType().Name : "none") + " mc mesh same " + (mc && mc.sharedMesh == shell.GetComponent<MeshFilter>().sharedMesh)); }
return sb.ToString();
