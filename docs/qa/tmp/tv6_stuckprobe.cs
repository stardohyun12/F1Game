// 과제 0311: 튜토리얼 귀로 막힌 자리 주변 콜라이더(에디트 모드)
var sb = new System.Text.StringBuilder();
foreach (var p in new[] { new Vector3(178, 27, 7), new Vector3(150, 23, 16), new Vector3(138, 20, 3), new Vector3(60, 6, -118) })
{
    sb.Append(p.ToString("F0") + ": ");
    foreach (var c in Physics.OverlapSphere(p + Vector3.up, 3.5f, ~0, QueryTriggerInteraction.Collide)) sb.Append(c.name + (c.isTrigger ? "(T)" : "") + "<" + (c.transform.parent ? c.transform.parent.name : "") + "> ");
    sb.Append("\n");
}
return sb.ToString();
