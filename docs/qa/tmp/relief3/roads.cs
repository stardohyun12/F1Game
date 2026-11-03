// 2253: RoadsV2 자식 중 콜라이더 없는 것(빌더는 길로 보고, 측정은 못 보는 것) — 이름·위치
var rv = GameObject.Find("TownV6_Gray").transform.Find("RoadsV2"); var sb = new System.Text.StringBuilder(); int n = 0, noc = 0;
foreach (Transform ch in rv)
{
    if (!ch.gameObject.activeInHierarchy) continue; n++;
    if (ch.GetComponentInChildren<Collider>() != null) continue; noc++;
    var r = ch.GetComponentInChildren<Renderer>(); var b = r ? r.bounds : new Bounds(ch.position, Vector3.zero);
    if (b.max.x > -200 && b.min.x < 80 && b.max.z > -60 && b.min.z < 260) sb.Append($"{ch.name} c({b.center.x:F0},{b.center.y:F1},{b.center.z:F0}) size({b.size.x:F0},{b.size.z:F0}) rs {(ch.GetComponent<RoadSurface>() ? ch.GetComponent<RoadSurface>().kind : "-")}\n");
}
return $"active {n} noCollider {noc}\n" + sb;
