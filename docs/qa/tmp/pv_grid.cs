// 과제 0535: 빈 자리 찾기 — 2m 칸 위에서 레이캐스트(콜라이더 무관하게 렌더러 bounds 로) 키 큰 물체 표시. 행 = z(위가 북), 열 = x
var sb = new System.Text.StringBuilder();
var skip = new[] { "Terrain", "Sky", "Headland", "yard_ground", "sea_v5_combo", "A3_M1", "far_town", "A4_BldgNew" };
var rs = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => { var t = r.transform; while (t.parent && t.parent.name != "SceneA4") t = t.parent; return !skip.Contains(t.name) && r.bounds.size.y > 0.4f && r.bounds.size.x < 40 && r.bounds.size.z < 40; }).ToList();
sb.Append("x -30..34 step2, z 20..-60 step2.  #=물체  .=빈  ~=땅 없음  숫자=땅높이(반올림)\n");
for (float z = 20; z >= -60; z -= 2) { sb.Append(z.ToString("000").PadLeft(4) + " ");
  for (float x = -30; x <= 34; x += 2) { var c = new Vector3(x, 0, z); bool occ = rs.Any(r => { var b = r.bounds; return c.x > b.min.x - 0.5f && c.x < b.max.x + 0.5f && c.z > b.min.z - 0.5f && c.z < b.max.z + 0.5f; });
    if (occ) sb.Append('#'); else if (Physics.Raycast(new Vector3(x, 100, z), Vector3.down, out var h, 300)) { int y = Mathf.RoundToInt(h.point.y); sb.Append(y >= 0 && y < 10 ? (char)('0' + y) : (y < 0 && y > -10 ? (char)('a' - y - 1) : '?')); } else sb.Append('~'); }
  sb.Append("\n"); }
sb.Append("(a=-1 b=-2 ...)\n");
return sb.ToString();
