// 1514: 모든 차길 양쪽 차로 끝~+3m(인도 포함) 띠에 Coast_* / Viewpoint 콜라이더가 겹치는지 — 바위·옹벽이 길을 막지 않나
int hits = 0, probes = 0; var sb = new System.Text.StringBuilder();
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0))
{
    var P = rs.centerline; float lh = rs.width * .5f;
    for (int i = 2; i + 2 < P.Length; i += 2)
    {
        var f = P[i + 2] - P[i - 2]; f.y = 0; if (f.sqrMagnitude < 1e-4f) continue; f.Normalize(); var nr = new Vector3(f.z, 0, -f.x);
        foreach (int sd in new[] { 1, -1 })
            for (float d = 0f; d <= 3f; d += 1f)
            {
                var c = P[i] + nr * sd * (lh + d) + Vector3.up * 0.6f; probes++;
                foreach (var col in Physics.OverlapBox(c, new Vector3(0.4f, 0.5f, 0.4f)))
                    if (col.name.StartsWith("Coast_")) { hits++; if (hits <= 8) sb.Append(rs.roadId + "#" + i + " side" + sd + " +" + d + "m " + col.name + "\n"); }
            }
    }
}
return "probes " + probes + " coastHits " + hits + "\n" + sb;
