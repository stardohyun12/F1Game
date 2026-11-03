// 0338: 끼임 자리 단면 — 진행 방향으로 0.25 m 마다 위에서 레이(RaycastAll), 맞은 면 이름·높이 전부
string r = "";
void Line(Vector3 a, Vector3 b, string tag)
{
    r += "== " + tag + "\n"; var d = b - a; d.y = 0; int n = Mathf.CeilToInt(d.magnitude / 0.25f);
    for (int i = 0; i <= n; i++)
    {
        var p = a + d * (i / (float)n); var hs = Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
        var s = string.Join(" ", hs.Where(h => h.collider.GetComponentInParent<ArcadeCarController>() == null).OrderByDescending(h => h.point.y).Take(3).Select(h => h.collider.name.Replace("TownV6_", "") + "@" + h.point.y.ToString("F3")));
        r += $"{p.x:F2},{p.z:F2} {s}\n";
    }
}
Line(new Vector3(-180.1f, 0, -66f), new Vector3(-179.9f, 0, -74f), "rev -180,-70 (fwd -z)");
Line(new Vector3(142.05f, 0, -82f), new Vector3(141.97f, 0, -92f), "fwd 142,-86 (-z)");
Line(new Vector3(141f, 0, -93.8f), new Vector3(133f, 0, -92.6f), "fwd 136,-93 (-x)");
return r;
