// 1514: 2.png 자리(O_breakwater 시작 바다 쪽) 단면 — 길에서 바다 쪽으로 1m 마다 RaycastAll + 그 점을 덮는 렌더러(콜라이더 없는 것 포함)
var p0 = new Vector3(-96, 5, -111); var d = new Vector3(-0.7f, 0, -0.7f).normalized;
var rends = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.bounds.Intersects(new Bounds(p0 + d * 12, new Vector3(30, 30, 30)))).ToList();
var sb = new System.Text.StringBuilder("rends near: " + string.Join(",", rends.Select(r => r.name).Distinct().Take(40)) + "\n");
for (int k = 0; k <= 24; k += 2)
{
    var q = p0 + d * k;
    var hits = Physics.RaycastAll(q + Vector3.up * 30, Vector3.down, 60f).OrderBy(h => h.distance);
    sb.Append(k + "m H=" + BuildBayLoop.H(q.x, q.z).ToString("F2") + " : " + string.Join(", ", hits.Select(h => h.collider.name + "@" + h.point.y.ToString("F2"))) + "\n");
}
return sb.ToString();
