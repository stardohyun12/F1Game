// 캡처 자리 찾기: 크레인·랜드마크 위치, 본선 시작부 방향, 바다 쪽, terrace_house 가 몰린 본선 인덱스
var m = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main");
var P = m.centerline; var sb = new System.Text.StringBuilder();
foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.parent != null && (t.parent.name == "Features" || t.parent.name == "Landmarks")))
  sb.Append(t.name + t.position.ToString("F0") + " ");
sb.Append("\nsea: "); var sea = GameObject.Find("Sea"); if (sea) sb.Append(sea.transform.position.ToString("F1"));
sb.Append("\nmain 0..80: "); for (int i = 0; i <= 80; i += 10) sb.Append(i + ":" + P[i].ToString("F0") + " ");
// 본선 각 인덱스(10m)마다 20m 안 terrace_house 수
var th = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("BLD_") && t.name.Contains("terrace_house")).Select(t => t.position).ToList();
sb.Append("\nterrace " + th.Count + " | main idx with most terrace within 25m: ");
var best = Enumerable.Range(0, P.Length / 10).Select(k => k * 10).Select(i => (i, c: th.Count(q => new Vector2(q.x - P[i].x, q.z - P[i].z).magnitude < 25f))).OrderByDescending(x => x.c).Take(8);
foreach (var b in best) sb.Append(b.i + "(" + b.c + ")" + P[b.i].ToString("F0") + " ");
return sb.ToString();
