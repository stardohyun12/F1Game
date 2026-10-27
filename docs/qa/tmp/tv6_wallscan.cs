// 과제 0311: 고리 점마다 차 높이(길+0.8m)에서 반경 1.6m 구에 걸리는 비트리거 콜라이더(길·지형 제외) → 막는 물체 목록
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/" + UnityEditor.SessionState.GetString("tv6.pts", "tv6_loop_pts.txt")));
var P = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
var sb = new System.Text.StringBuilder(); int n = 0;
for (int i = 0; i < P.Count; i++)
{
    var p = P[i]; if (!Physics.Raycast(new Vector3(p.x, 200, p.z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore)) continue;
    var hits = Physics.OverlapSphere(h.point + Vector3.up * 0.9f, 1.6f, ~0, QueryTriggerInteraction.Ignore).Where(c => !c.name.StartsWith("ROAD") && !(c.name.Contains("Terrain")) && c.GetComponentInParent<ArcadeCarController>() == null).Select(c => c.name).Distinct().ToList();
    if (hits.Count > 0) { n++; sb.Append(i + "(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + ") " + string.Join("/", hits) + "; "); }
}
return "blocked " + n + " / " + P.Count + " : " + sb;
