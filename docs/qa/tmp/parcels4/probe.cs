// 0236: 봇 이탈 자리(121~122, 8~10) 조사 — 에디트 모드. 반경 10 m 격자 2 m 로 위에서 RaycastAll, 맨 위 두 충돌체 이름·높이. 지형이 길보다 높은 곳만 모아 보고
var sb = new System.Text.StringBuilder(); var cnt = new System.Collections.Generic.Dictionary<string, int>();
for (float x = 108; x <= 134; x += 2) for (float z = -4; z <= 24; z += 2) {
  var hits = Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400); System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
  if (hits.Length == 0) continue;
  float road = float.NaN, terr = float.NaN; string top = hits[0].collider.name;
  foreach (var h in hits) { var n = h.collider.name; if (n.StartsWith("ROAD") && float.IsNaN(road)) road = h.point.y; if (n.Contains("Terrain") && float.IsNaN(terr)) terr = h.point.y; }
  string key = top.Length > 28 ? top.Substring(0, 28) : top; cnt[key] = cnt.TryGetValue(key, out var c) ? c + 1 : 1;
  if (!float.IsNaN(road) && !float.IsNaN(terr) && terr > road + 0.03f) sb.Append($"poke ({x},{z}) terr {terr:F2} > road {road:F2} top={top}\n");
}
foreach (var kv in cnt) sb.Append(kv.Key + " " + kv.Value + "\n");
// 가장 가까운 필지 물체
var root = GameObject.Find("Parcels_Plan"); var near = new System.Collections.Generic.List<string>();
if (root) foreach (Transform t in root.GetComponentsInChildren<Transform>()) { var p = t.position; if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(121.5f, 9f)) < 12 && t.GetComponent<Renderer>()) near.Add(t.name + "@" + p.ToString("F1")); }
sb.Append("parcels<12m: " + near.Count + " " + string.Join(", ", near.Take(10)));
return sb.ToString();
