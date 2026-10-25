var m = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main");
var c = new Vector3(-38f, 0, 205f); var sb = new System.Text.StringBuilder("main w=" + m.width + "\n");
for (int i = 0; i < m.centerline.Length; i++) { var q = m.centerline[i]; if (new Vector2(q.x - c.x, q.z - c.z).magnitude < 8f && i % 2 == 0) sb.Append(i + ":" + q.ToString("F1") + " "); }
sb.Append("\n");
foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("INTENT_Bollard") && (t.position - new Vector3(-38, t.position.y, 205)).magnitude < 10f)) sb.Append(t.name + t.position.ToString("F1") + " ");
return sb.ToString();
