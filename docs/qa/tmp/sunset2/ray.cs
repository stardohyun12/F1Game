// 자리 앞 바다 몇 점에서 아래로 레이 → 물 밑에 바닥(콜라이더/메시)이 있나
var sea = GameObject.Find("Sea"); var sb = new System.Text.StringBuilder();
if (sea) { var r = sea.GetComponent<Renderer>(); sb.Append("Sea pos=" + sea.transform.position + " bounds=" + r.bounds + " mat=" + r.sharedMaterial.name + "\n"); }
Vector3[] pts = { new Vector3(-9, 5, -296), new Vector3(-12, 5, -300), new Vector3(-20, 5, -310), new Vector3(-3, 5, -300), new Vector3(-30, 5, -295), new Vector3(-60, 5, -330) };
foreach (var p in pts)
{
    var hits = Physics.RaycastAll(p, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
    sb.Append(p + ": ");
    foreach (var h in hits) sb.Append(h.collider.name + "@" + h.point.y.ToString("F2") + " ");
    sb.Append("\n");
}
return sb.ToString();
