// BayLoop v4 경계 턱 검사 (에디트 모드, 빌드된 씬). 차길 중심선 8점마다 양옆으로 0.2m 간격 레이(차로 끝~+3.5m 인도·앞마당),
// 바닥(Road_/Band_/OG_/BayTerrain)만 보고 이웃 칸 높이차를 잰다. 오르막 턱: ≤0.12 넘김 / 0.12~0.25 걸림 / >0.25 벽.
// 내리막 >0.5m 인데 난간(INTENT_Guard) 없으면 낭떠러지로 센다.
Physics.SyncTransforms();
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.kind == "main" || r.kind == "village" || r.kind == "shortcut").ToList();
int samples = 0, ok = 0, catchN = 0, wallN = 0, dropN = 0; var worst = new List<(float d, string s)>(); var drops = new List<string>();
foreach (var rs in roads)
{
    var P = rs.centerline; int n = P.Length;
    for (int i = 4; i < n - 4; i += 8)
    {
        var f = P[i + 2] - P[i - 2]; f.y = 0; f.Normalize(); var rt = Vector3.Cross(Vector3.up, f);
        float half = (rs.widths.Length > i ? rs.widths[i] : rs.width) * 0.5f;
        foreach (var sd in new[] { -1f, 1f })
        {
            samples++; float prev = float.NaN, maxUp = 0, maxDown = 0; Vector3 at = P[i]; bool guard = false, blocked = false;
            for (float o = half - 0.4f; o < half + 3.5f; o += 0.2f)
            {
                var q = P[i] + rt * sd * o;
                var hits = Physics.RaycastAll(q + Vector3.up * 6f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToList();
                if (hits.Any(h => h.collider.name.StartsWith("INTENT_Guard"))) guard = true;
                var top = hits.FirstOrDefault(h => !h.collider.name.StartsWith("INTENT_"));
                if (top.collider == null) break;
                string nm = top.collider.name;
                if (!(nm.StartsWith("Road_") || nm.StartsWith("Band_") || nm.StartsWith("OG_") || nm.StartsWith("BayTerrain"))) { blocked = true; break; }   // 건물·소품·가구에 닿으면 거기서 끝
                float y = top.point.y;
                if (!float.IsNaN(prev)) { float d = y - prev; if (d > maxUp) { maxUp = d; at = q; } if (-d > maxDown) maxDown = -d; }
                prev = y;
            }
            if (maxUp <= 0.12f) ok++; else if (maxUp <= 0.25f) catchN++; else wallN++;
            if (maxUp > 0.12f) worst.Add((maxUp, rs.roadId + "(" + at.x.ToString("F0") + "," + at.z.ToString("F0") + ")+" + maxUp.ToString("F2")));
            if (maxDown > 0.5f && !guard) { dropN++; if (drops.Count < 12) drops.Add(rs.roadId + "(" + at.x.ToString("F0") + "," + at.z.ToString("F0") + ")-" + maxDown.ToString("F1")); }
        }
    }
}
return "edge samples=" + samples + " cross(≤12cm)=" + ok + " catch(12~25cm)=" + catchN + " wall(>25cm)=" + wallN + " dropNoGuard(>0.5m)=" + dropN
    + " | worst " + string.Join(" ", worst.OrderByDescending(w => w.d).Take(12).Select(w => w.s)) + " | drops " + string.Join(" ", drops);
