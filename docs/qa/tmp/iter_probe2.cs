// artlive 1509: Q3_Depot 둘레에서 길 찾기(읽기만). 열려 있는 Map_TownV6 기준
var log = new System.Text.StringBuilder();
var c = new Vector3(12.84f, 0f, -90.29f); var yaw = Quaternion.Euler(0, 7.71f, 0); var fw = yaw * Vector3.forward; var rt = yaw * Vector3.right;
foreach (var d in new[] { -8f, 4f, 7f, 10f, 14f, 20f }) { log.Append("fw " + d + ": "); foreach (var s in new[] { -30f, -20f, -10f, 0f, 10f, 20f, 30f }) { var p = c + fw * d + rt * s + Vector3.up * 60f; string nm = "-"; float y = 0; foreach (var h in Physics.RaycastAll(p, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance)) { nm = h.collider.name; y = h.point.y; break; } log.Append(s + ":" + nm + "@" + y.ToString("F1") + "  "); } log.Append("\n"); }
var mc = Camera.main; log.Append("main fov " + mc.fieldOfView + " lr h " + mc.GetComponent<LowResRenderTest>().targetHeight + "\n");
foreach (Transform t in GameObject.Find("TV6_Loop").transform) log.Append("loop kid " + t.name + " " + t.childCount + "\n");
foreach (Transform t in GameObject.Find("TownV6_Gray").transform) log.Append("gray kid " + t.name + " " + t.childCount + "\n");
return log.ToString();
