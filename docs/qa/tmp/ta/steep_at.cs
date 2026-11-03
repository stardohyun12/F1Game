// 칼날 자리 4 m 안 지형 삼각형 중 가파른 것(면 법선 y < 0.7) — 가는 세운 삼각형이면 경사 셰이더가 절벽 색으로 칠한다
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var m = t.GetComponent<MeshFilter>().sharedMesh; var tr = t.transform;
var V = m.vertices; var T = m.triangles; var sb = new System.Text.StringBuilder(); int all = 0, steep = 0;
for (int i = 0; i < T.Length; i += 3)
{
    Vector3 a = tr.TransformPoint(V[T[i]]), b = tr.TransformPoint(V[T[i + 1]]), c = tr.TransformPoint(V[T[i + 2]]);
    var g = (a + b + c) / 3f; if (Mathf.Abs(g.x - 279f) > 4f || Mathf.Abs(g.z + 25f) > 4f) continue; all++;
    var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * .5f; if (area < 1e-6f) continue; n /= (2f * area);
    if (n.y > 0.7f) continue; steep++;
    if (steep <= 12) sb.Append($"tri {i / 3} 법선y {n.y:F2} 넓이 {area:F3} 높이차 {Mathf.Max(a.y, Mathf.Max(b.y, c.y)) - Mathf.Min(a.y, Mathf.Min(b.y, c.y)):F2} 꼭짓점 {a:F2} {b:F2} {c:F2} 번호 {T[i]},{T[i + 1]},{T[i + 2]}\n");
}
var mat = t.GetComponent<Renderer>().sharedMaterial; var sh = mat.shader; var props = new System.Text.StringBuilder();
for (int p = 0; p < sh.GetPropertyCount(); p++) props.Append(sh.GetPropertyName(p)).Append(' ');
return $"삼각형 {all} · 가파른 {steep}\n" + sb + "props: " + props;
