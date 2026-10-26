// 1514: 차길 점 중 지형 사각형(-208..192, -240..288) 가장자리 25m 안/밖 + 해안 쪽 길 아래 콜라이더 이름 통계
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 5).ToList();
var sb = new System.Text.StringBuilder();
foreach (var r in rs)
{
    int near = 0, outside = 0; Vector3 ex = Vector3.zero;
    foreach (var p in r.centerline)
    {
        float m = Mathf.Min(Mathf.Min(p.x + 208, 192 - p.x), Mathf.Min(p.z + 240, 288 - p.z));
        if (m < 0) { outside++; ex = p; } else if (m < 25) { near++; if (ex == Vector3.zero) ex = p; }
    }
    if (near + outside > 0) sb.Append(r.roadId + " near" + near + " out" + outside + " e.g." + ex.ToString("F0") + " | ");
}
var sea = GameObject.Find("Sea"); sb.Append("seaY=" + (sea ? sea.transform.position.y.ToString("F2") : "?"));
var far = GameObject.Find("FarTerrain"); if (far) sb.Append(" far bounds " + far.GetComponent<Renderer>().bounds);
return sb.ToString();
