// 과제 0444-2: 차길 노면 위(0.3~2.5 m 높이 상자, 폭 = 길 폭 − 0.4 m)에 길·지형·범프가 아닌 콜라이더가 있나 — 2 m 간격
var s = new System.Text.StringBuilder(); int n = 0; var seen = new System.Collections.Generic.HashSet<string>(); var per = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
{
  var cl = r.centerline; if (cl == null || cl.Length < 2) continue;
  for (int i = 0; i + 1 < cl.Length; i += 2)
  {
    var a = cl[i]; var b = cl[Mathf.Min(i + 1, cl.Length - 1)]; var d = b - a; d.y = 0; if (d.sqrMagnitude < 1e-4f) continue;
    var rot = Quaternion.LookRotation(d.normalized);
    var hs = Physics.OverlapBox(a + Vector3.up * 1.4f, new Vector3(Mathf.Max(.2f, r.width * .5f - .2f), 1.1f, .5f), rot);
    foreach (var h in hs)
    {
      var nm = h.name; if (nm.StartsWith("ROAD_") || nm.StartsWith("BUMP_") || nm.Contains("Terrain") || h.isTrigger) continue;
      var key = r.roadId + "|" + nm; if (!seen.Add(key)) continue; n++;
      per[r.roadId] = per.TryGetValue(r.roadId, out var c) ? c + 1 : 1;
      if (n <= 60) s.Append($"{r.roadId} @({a.x:F0},{a.y:F1},{a.z:F0}) w{r.width} × {nm} [{h.GetType().Name}]\n");
    }
  }
}
var hp = new System.Text.StringBuilder();
foreach (var kv in per) if (kv.Key.StartsWith("posi_419759968_325")) hp.Append(kv.Key + "=" + kv.Value + " ");
return $"겹침 쌍 {n} · 길 {per.Count}개 | 헤어핀(posi_419759968_325*): {(hp.Length > 0 ? hp.ToString() : "0")}\n" + s;
