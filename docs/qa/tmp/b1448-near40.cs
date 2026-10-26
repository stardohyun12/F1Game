// 1448: #40L 바깥(차로 끝 +2.5~+4.5m, 높이 0~1.2m) 에 있는 콜라이더
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var P = main.centerline; int n = P.Length; int i = 40;
var f = P[i + 3] - P[i - 3]; f.y = 0; f.Normalize(); var nrm = new Vector3(-f.z, 0, f.x);
var c = P[i] + nrm * 7f + Vector3.up * 0.7f;
var hits = Physics.OverlapBox(c, new Vector3(1.0f, 0.5f, 2.5f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore);
return string.Join(" | ", hits.Select(h => h.name + " " + h.GetType().Name + " b=" + h.bounds.min.ToString("F1") + "~" + h.bounds.max.ToString("F1")));
