// 과제 0320-1: 노면 품질 측정 (에디트 모드, ucli exec @tools/qa/road_feel_measure.cs). 결과 = docs/qa/tmp/road-feel.tsv → tools/qa/road_feel.py
// 차길(RoadSurface)마다 중심선을 1 m 로 다시 깔고, 아래로 레이를 쏴 "차가 실제로 밟는 면"을 잰다.
//  bump  : 1 m 간격 경사 변화 > 3 %/m (값 = %/m)
//  step  : 0.25 m 간격 세 줄(가운데·±w/4)에서 높이가 앞뒤 기울기로 설명 안 되게 튀는 것 > 2 cm (값 = cm)
//  float : 길 가장자리(안쪽 0.1 m) 노면 − 바깥 0.5 m 지형 > 5 cm (값 = cm)      buried : 그 반대 > 5 cm
//  gap   : 가장자리 노면 − 바로 아래 지형 > 5 cm (길 아래 빈 공간)
//  cross : 가로 기울기 > 6 % (가장자리 안쪽 0.3 m 두 점, 급굽이 ±5 m 45° 이상은 'hp' 로 따로)
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length >= 2).OrderBy(r => r.roadId).ToList();
var terr = GameObject.Find("TownV6_Terrain")?.GetComponent<Collider>();
Physics.SyncTransforms();
var sb = new System.Text.StringBuilder(); sb.AppendLine("road\tkind\trough\ttype\tx\tz\tvalue");
var sum = new System.Text.StringBuilder(); sum.AppendLine("road\tkind\trough\tlen");
System.Func<Vector3, float, (float road, float terr, bool other)> Probe = (p, yRef) =>
{
  float hr = float.NaN, ht = float.NaN; bool other = false;
  foreach (var h in Physics.RaycastAll(p + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
  {
    if (h.collider == terr) { ht = h.point.y; continue; }
    if (h.collider.name.StartsWith("ROAD_") || h.collider.transform.parent != null && h.collider.transform.parent.name == "DeadEnds")
    { if (Mathf.Abs(h.point.y - yRef) < 1.5f && (float.IsNaN(hr) || h.point.y > hr)) hr = h.point.y; }
    else if (h.collider.GetComponentInParent<ArcadeCarController>() == null && h.point.y > yRef - .3f) other = true;
  }
  return (hr, ht, other);
};
int nPts = 0;
foreach (var r in roads)
{
  var c = r.centerline; var P = new List<Vector3> { c[0] }; float acc = 0f, len = 0f;
  for (int i = 1; i < c.Length; i++)
  {
    var a = c[i - 1]; var b = c[i]; float d = new Vector2(b.x - a.x, b.z - a.z).magnitude; len += d;
    float t = 0f; while (acc + (d - t) >= 1f && d > 1e-4f) { t += 1f - acc; acc = 0f; P.Add(Vector3.Lerp(a, b, t / d)); }
    acc += d - t;
  }
  sum.AppendLine($"{r.roadId}\t{r.kind}\t{r.roughness:F2}\t{len:F0}");
  if (P.Count < 4) continue;
  string pre = $"{r.roadId}\t{r.kind}\t{r.roughness:F2}";
  float hw = r.width * .5f;
  var F = new Vector3[P.Count]; for (int i = 0; i < P.Count; i++) { var f = P[Mathf.Min(i + 1, P.Count - 1)] - P[Mathf.Max(i - 1, 0)]; f.y = 0; F[i] = f.normalized; }
  // (a) 중심 높이
  var H = new float[P.Count]; for (int i = 0; i < P.Count; i++) { H[i] = Probe(P[i], P[i].y).road; nPts++; }
  for (int i = 1; i + 1 < P.Count; i++)
  {
    if (float.IsNaN(H[i - 1]) || float.IsNaN(H[i]) || float.IsNaN(H[i + 1])) continue;
    float d0 = new Vector2(P[i].x - P[i - 1].x, P[i].z - P[i - 1].z).magnitude, d1 = new Vector2(P[i + 1].x - P[i].x, P[i + 1].z - P[i].z).magnitude;
    if (d0 < .5f || d1 < .5f) continue;
    float dg = ((H[i + 1] - H[i]) / d1 - (H[i] - H[i - 1]) / d0) / ((d0 + d1) * .5f);
    if (Mathf.Abs(dg) > .03f) sb.AppendLine($"{pre}\tbump\t{P[i].x:F1}\t{P[i].z:F1}\t{dg * 100:F1}");
  }
  // (b) 단차: 세 줄 × 0.25 m
  foreach (float off in new[] { 0f, -hw * .5f, hw * .5f })
  {
    var Q = new List<(Vector3 p, float h)>();
    for (int i = 0; i + 1 < P.Count; i++) for (int s = 0; s < 4; s++)
    {
      var p = Vector3.Lerp(P[i], P[i + 1], s / 4f); var f = Vector3.Lerp(F[i], F[i + 1], s / 4f).normalized; var rt = new Vector3(f.z, 0, -f.x);
      var q = p + rt * off; Q.Add((q, Probe(q, p.y).road));
    }
    for (int i = 2; i + 3 < Q.Count; i++)
    {
      float dh = Q[i + 1].h - Q[i].h, ref0 = (Q[i].h - Q[i - 2].h) * .5f, ref1 = (Q[i + 3].h - Q[i + 1].h) * .5f;
      if (float.IsNaN(dh) || float.IsNaN(ref0) || float.IsNaN(ref1)) continue;
      float st = Mathf.Abs(dh - (ref0 + ref1) * .5f);
      if (st > .02f) sb.AppendLine($"{pre}\tstep\t{Q[i].p.x:F1}\t{Q[i].p.z:F1}\t{st * 100:F1}");
    }
  }
  // (c)(d) 가장자리
  for (int i = 0; i < P.Count; i++)
  {
    var rt = new Vector3(F[i].z, 0, -F[i].x);
    float turn = Vector3.Angle(P[Mathf.Min(i + 5, P.Count - 1)] - P[i] - Vector3.up * (P[Mathf.Min(i + 5, P.Count - 1)].y - P[i].y), P[i] - P[Mathf.Max(i - 5, 0)] - Vector3.up * (P[i].y - P[Mathf.Max(i - 5, 0)].y));
    float[] he = new float[2];
    for (int sd = 0; sd < 2; sd++)
    {
      float sg = sd == 0 ? -1f : 1f;
      var e = Probe(P[i] + rt * sg * (hw - .1f), P[i].y); var o = Probe(P[i] + rt * sg * (hw + .5f), P[i].y);
      var ei = Probe(P[i] + rt * sg * (hw - .3f), P[i].y); he[sd] = ei.road;
      if (float.IsNaN(e.road)) continue;
      if (!float.IsNaN(e.terr) && e.road - e.terr > .05f) sb.AppendLine($"{pre}\tgap\t{P[i].x:F1}\t{P[i].z:F1}\t{(e.road - e.terr) * 100:F0}");
      if (!float.IsNaN(o.road) || o.other || float.IsNaN(o.terr)) continue;   // 바깥이 다른 길·건물·담이면 건넘
      float df = e.road - o.terr;
      if (df > .05f) sb.AppendLine($"{pre}\tfloat\t{P[i].x:F1}\t{P[i].z:F1}\t{df * 100:F0}");
      else if (df < -.05f) sb.AppendLine($"{pre}\tburied\t{P[i].x:F1}\t{P[i].z:F1}\t{-df * 100:F0}");
    }
    if (!float.IsNaN(he[0]) && !float.IsNaN(he[1]))
    {
      float cs = Mathf.Abs(he[1] - he[0]) / Mathf.Max(.5f, r.width - .6f);
      if (cs > .06f) sb.AppendLine($"{pre}\t{(turn > 45f ? "cross_hp" : "cross")}\t{P[i].x:F1}\t{P[i].z:F1}\t{cs * 100:F1}");
    }
  }
}
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
System.IO.File.WriteAllText(dir + "road-feel.tsv", sb.ToString());
System.IO.File.WriteAllText(dir + "road-feel-roads.tsv", sum.ToString());
return $"roads {roads.Count} pts {nPts} lines {sb.Length} terr {(terr != null)}";
