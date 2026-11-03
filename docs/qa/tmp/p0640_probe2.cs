// 과제 0640 읽기 전용: 남은 두 점에서 ROAD 콜라이더별 맞은 높이
var sb = new System.Text.StringBuilder();
foreach (var p in new[] { new Vector2(285.75f, -65.53f), new Vector2(285.37f, -65.83f), new Vector2(284.72f, -64.68f) }) {
  sb.Append("@" + p + ": ");
  foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)) {
    if (!c.name.StartsWith("ROAD") || !c.enabled || !c.gameObject.activeInHierarchy) continue; RaycastHit h;
    if (c.Raycast(new Ray(new Vector3(p.x, 60, p.y), Vector3.down), out h, 100)) sb.Append(c.name + "(" + c.GetType().Name + ") " + h.point.y.ToString("F2") + "; "); }
  sb.Append("\n"); }
int n = 0, nc = 0; foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) if (mf.name.StartsWith("ROAD")) { n++; if (mf.GetComponent<Collider>() != null) nc++; }
sb.Append("ROAD meshes " + n + " with collider " + nc + "\n");
return sb.ToString();
