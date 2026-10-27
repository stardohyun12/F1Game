// 과제 0422: (75,-147) 근처 고리 단면 — 중심선 앞뒤 3줄, 옆 -7..7m 0.5m 간격 최고 표면 높이·이름
var sb = new System.Text.StringBuilder();
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
int c = 0; float bd = 1e9f; for (int i = 0; i < L.Count; i++) { float d = (L[i] - new Vector3(75, 0, -147)).sqrMagnitude; if (d < bd) { bd = d; c = i; } }
sb.Append("idx " + c + " d " + Mathf.Sqrt(bd).ToString("F1") + "\n");
foreach (var ii in new[] { c - 4, c, c + 4 })
{
    var f = L[ii + 1] - L[ii - 1]; f.y = 0; f.Normalize(); var sd = Vector3.Cross(Vector3.up, f);
    sb.Append(ii + " " + L[ii].ToString("F0") + ": ");
    for (float o = -7f; o <= 7f; o += 0.5f) { var p = L[ii] + sd * o; if (Physics.Raycast(new Vector3(p.x, 200, p.z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore)) sb.Append(o.ToString("F1") + "=" + h.point.y.ToString("F2") + (h.collider.name.StartsWith("ROAD") ? h.collider.name.Substring(h.collider.name.Length - 3) : "~" + h.collider.name.Substring(0, 4)) + " "); }
    sb.Append("\n");
}
return sb.ToString();
