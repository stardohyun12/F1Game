// 봇 실패 지점 진단: 지면 레이 히트 이름·도로면 대비 높이, 차 크기 상자 안 막는 콜라이더, 중심선 국소 경사·꺾임
if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.Contains("Map_TownV6")) UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity");
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).ToDictionary(r => r.roadId);
var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/townv6-bot-cover.txt"));
var sb = new System.Text.StringBuilder();
foreach (var ln in lines.Skip(2))
{
  var c = ln.Split('\t'); if (c.Length < 8 || c[4] == "ok") continue;
  var a = c[7].Split(','); var at = new Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
  if (!rs.TryGetValue(c[0], out var r)) { sb.AppendLine(c[0] + " 없음"); continue; }
  var P = c[1] == "+" ? r.centerline.ToList() : Enumerable.Reverse(r.centerline).ToList();
  int bi = 0; float bd = 1e9f; for (int i = 0; i < P.Count; i++) { float d = new Vector2(P[i].x - at.x, P[i].z - at.z).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
  // 앞 12 m 국소 경사 최대(2 m 현), 꺾임
  float gmax = 0, step = 0; for (int i = bi; i + 2 < P.Count && i < bi + 14; i++) { var d = P[i + 2] - P[i]; float h = new Vector2(d.x, d.z).magnitude; if (h > .3f) gmax = Mathf.Max(gmax, d.y / h); }
  for (int i = Mathf.Max(1, bi - 3); i < Mathf.Min(P.Count, bi + 12); i++) step = Mathf.Max(step, Mathf.Abs(P[i].y - P[i - 1].y));
  // 지면 레이: 중심선 점 위 6 m 에서 아래로, 첫 히트
  var hits = new System.Collections.Generic.List<string>();
  for (int i = bi; i < Mathf.Min(P.Count, bi + 10); i += 2)
  {
    if (Physics.Raycast(P[i] + Vector3.up * 6f, Vector3.down, out var h, 12f)) hits.Add($"{h.collider.name}:{h.point.y - P[i].y:+0.00;-0.00}");
  }
  // 차 상자(2×0.6×4.5) 를 도로 위 0.9 m 에 두고 겹치는 것
  var fwd = (P[Mathf.Min(bi + 3, P.Count - 1)] - P[bi]); fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
  var q = Quaternion.LookRotation(fwd.normalized);
  var ov = Physics.OverlapBox(P[bi] + Vector3.up * .9f + fwd.normalized * 2f, new Vector3(1f, .3f, 2.25f), q).Where(x => x.GetComponentInParent<ArcadeCarController>() == null).Select(x => x.name).Distinct().Take(4);
  sb.AppendLine($"{c[0]}{c[1]} {c[4]} i{bi}/{P.Count} w{r.width:F1} g12={gmax * 100:F0}% dy1={step:F2} ray[{string.Join(" ", hits)}] box[{string.Join(",", ov)}]");
}
System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/v6d-diag.txt"), sb.ToString());
return sb.ToString();
