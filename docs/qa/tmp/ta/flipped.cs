// 지형 삼각형 중 아래를 보는 것(면 법선 y < 0) — 렌더는 뒷면이라 안 그려 구멍(배경색 칼날), 레이는 맞을 수 있다. 칼날 자리 4 m 안 + 맵 전체.
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var m = t.GetComponent<MeshFilter>().sharedMesh; var tr = t.transform;
var V = m.vertices; var T = m.triangles; int near = 0, all = 0, allBig = 0; var sb = new System.Text.StringBuilder(); float areaSum = 0;
var cells = new System.Collections.Generic.Dictionary<(int, int), int>();
for (int i = 0; i < T.Length; i += 3)
{
    Vector3 a = tr.TransformPoint(V[T[i]]), b = tr.TransformPoint(V[T[i + 1]]), c = tr.TransformPoint(V[T[i + 2]]);
    var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * .5f; if (area < 1e-5f || n.y >= 0f) continue;
    var g = (a + b + c) / 3f; if (g.y <= 0.3f) continue;
    all++; areaSum += area; if (area > 0.01f) allBig++;
    var k = (Mathf.FloorToInt(g.x / 12f), Mathf.FloorToInt(g.z / 12f)); cells[k] = cells.TryGetValue(k, out int q) ? q + 1 : 1;
    if (Mathf.Abs(g.x - 279f) <= 4f && Mathf.Abs(g.z + 25f) <= 4f) { near++; if (near <= 6) sb.Append($"tri {i / 3} 법선y {n.y / (2 * area):F2} 넓이 {area:F3} {a:F2} {b:F2} {c:F2}\n"); }
}
var top = System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(cells, kv => kv.Value), 8);
sb.Append("12 m 칸 상위: "); foreach (var kv in top) sb.Append($"({kv.Key.Item1 * 12 + 6},{kv.Key.Item2 * 12 + 6})×{kv.Value} ");
return $"아래 보는 삼각형: 맵 {all}(넓이>0.01㎡ {allBig}, 합 {areaSum:F1}㎡) · 칼날 자리 {near}\n" + sb;
