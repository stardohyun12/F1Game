DebugWindow.SetDusk(0f);
var sb = new System.Text.StringBuilder();
var eye = new Vector3(187.1f, 29.9f, -62.7f);
foreach (var yaw in new[] { 130f, 220f, 310f }) {
  var dir = Quaternion.Euler(-4f, yaw, 0) * Vector3.forward;
  sb.Append(Still("cand_room" + (int)yaw, eye, eye + dir * 5f, 52f));
}
sb.Append(Still("cand_garage0", new Vector3(219f, 28.2f, -68f), new Vector3(234f, 28.5f, -54f), 52f));
sb.Append(Still("cand_garage1", new Vector3(224f, 29.5f, -42f), new Vector3(234f, 28.5f, -54f), 52f));
// 짐칸 상자 위치 탐색
var boxes = UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
sb.Append("\nCargoBox x" + boxes.Length + ": ");
foreach (var b in boxes) sb.Append(b.name + "@" + b.transform.position.ToString("F1") + (b.gameObject.activeInHierarchy ? "(on)" : "(off)") + " ");
return sb.ToString();
