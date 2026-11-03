var sb = new System.Text.StringBuilder();
sb.AppendLine("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
var dp = GameObject.Find("DP_tea");
if (dp == null) { foreach (var d in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) sb.AppendLine("DP " + d.name + " " + d.transform.position); }
else {
  sb.AppendLine("DP_tea pos=" + dp.transform.position.ToString("F2") + " rot=" + dp.transform.eulerAngles.y);
  var so = new UnityEditor.SerializedObject(dp.GetComponent<DeliveryPoint>());
  var it = so.GetIterator(); it.NextVisible(true);
  while (it.NextVisible(false)) sb.AppendLine("  " + it.name + "=" + (it.propertyType==UnityEditor.SerializedPropertyType.Float? it.floatValue.ToString(): it.propertyType==UnityEditor.SerializedPropertyType.String? it.stringValue : it.propertyType.ToString()));
  var c = dp.transform.position;
  foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
    var b = r.bounds; var d2 = new Vector2(b.center.x - c.x, b.center.z - c.z).magnitude;
    if (d2 < 22f && b.size.y > 1.5f) sb.AppendLine("  R " + r.name + " par=" + (r.transform.parent? r.transform.parent.name:"-") + " c=" + b.center.ToString("F1") + " s=" + b.size.ToString("F1"));
  }
  foreach (var h in UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None)) if (Vector3.Distance(h.transform.position, c) < 40f) sb.AppendLine("  DOOR " + h.name + " " + h.transform.position);
}
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
sb.AppendLine("dir=" + (dir!=null? dir.name:"-"));
return sb.ToString();
