// 과제 1704 탐색(저장 안 함): 소품 상자(life x,y) vs 실제 몸체 밖 정점 위치 — cafe·shop·warehouse·narrow_b
var g = GameObject.Find("SceneA4").transform.Find("A4b_Houses"); var sb = new System.Text.StringBuilder();
foreach (var hn in new[] { "house_cafe", "house_shop", "house_warehouse", "house_narrow_b" }) {
  var mf = g.Find(hn).GetComponentInChildren<MeshFilter>(); var cols = mf.GetComponents<BoxCollider>(); var bodyB = new Bounds(cols[0].center, cols[0].size + Vector3.one * 0.05f);
  sb.Append(hn + " body c " + cols[0].center.ToString("F2") + " s " + cols[0].size.ToString("F2") + "\n");
  for (int i = 1; i < cols.Length; i++) sb.Append("  box" + i + " c " + cols[i].center.ToString("F2") + " s " + cols[i].size.ToString("F2") + " en " + cols[i].enabled + "\n");
  // 몸체 밖 정점을 1m 칸으로 묶어 많은 칸 순
  var cnt = new System.Collections.Generic.Dictionary<Vector3Int, int>();
  foreach (var v in mf.sharedMesh.vertices) { if (bodyB.Contains(v)) continue; var k = new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z)); cnt.TryGetValue(k, out int n); cnt[k] = n + 1; }
  var l = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Vector3Int, int>>(cnt); l.Sort((a, b) => b.Value - a.Value);
  sb.Append("  out-of-body cells(x,y,z floor m:n): "); for (int i = 0; i < Mathf.Min(14, l.Count); i++) sb.Append(l[i].Key + ":" + l[i].Value + " "); sb.Append("\n"); }
return sb.ToString();
