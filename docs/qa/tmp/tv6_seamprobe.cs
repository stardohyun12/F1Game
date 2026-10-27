// 과제 0422: 해안 (91,-189) 길 판 이음매 단차 — 고리 중심선 따라 ±1.5m 옆까지 0.25m 간격 최고 표면 높이·콜라이더
var sb = new System.Text.StringBuilder();
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
int c = 0; float bd = 1e9f; for (int i = 0; i < L.Count; i++) { float d = (L[i] - new Vector3(91, 0, -189)).sqrMagnitude; if (d < bd) { bd = d; c = i; } }
var f = L[c + 1] - L[c - 1]; f.y = 0; f.Normalize(); var sd = Vector3.Cross(Vector3.up, f);
sb.Append("idx " + c + " pt " + L[c].ToString("F1") + " fw " + f.ToString("F2") + "\n");
foreach (var o in new[] { -1.5f, -0.75f, 0f, 0.75f, 1.5f })
{
    sb.Append("side " + o + ": "); float prev = float.NaN;
    for (float s = -4f; s <= 4f; s += 0.25f)
    {
        var p = L[c] + f * s + sd * o;
        if (Physics.Raycast(new Vector3(p.x, 50, p.z), Vector3.down, out var h, 100, ~0, QueryTriggerInteraction.Ignore))
        { if (!float.IsNaN(prev) && Mathf.Abs(h.point.y - prev) > 0.03f) sb.Append("[" + s.ToString("F2") + " " + (h.point.y - prev).ToString("+0.00;-0.00") + " " + h.collider.name + "] "); prev = h.point.y; }
    }
    sb.Append("\n");
}
return sb.ToString();
