// 1448: 자리 #40L·#77L 단면 — 차로 끝에서 바깥으로 0.1m 마다 아래로 레이(맞은 콜라이더·높이)
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var P = main.centerline; int n = P.Length;
var sb = new System.Text.StringBuilder();
foreach (int i in new[] { 40, 77, 1187 })
{
    var f = P[(i + 3) % n] - P[(i - 3 + n) % n]; f.y = 0; f.Normalize(); var nrm = new Vector3(-f.z, 0, f.x);
    sb.Append("#" + i + " y0=" + P[i].y.ToString("F2") + ": ");
    string lastName = ""; float lastY = 0;
    for (float d = 3.0f; d <= 9f; d += 0.1f)
    {
        if (Physics.Raycast(P[i] + nrm * d + Vector3.up * 6f, Vector3.down, out var h, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            float y = h.point.y - P[i].y;
            if (h.collider.name != lastName || Mathf.Abs(y - lastY) > 0.03f) { sb.Append(d.ToString("F1") + ":" + (h.collider.name.Length > 22 ? h.collider.name.Substring(0, 22) : h.collider.name) + "(" + y.ToString("+0.00;-0.00") + ") "); lastName = h.collider.name; lastY = y; }
        }
    }
    sb.Append("\n");
}
return sb.ToString();
