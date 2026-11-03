// 서쪽 안벽 단면: 방 안(x 269)에서 -x 로 레이, z -32 ~ -26 (0.25 m), 높이 31.9 / 32.55 / 33.2
var sb = new System.Text.StringBuilder();
var board = GameObject.Find("OnFoot_Depot/OrderBoard");
if (board != null) board.SetActive(false);
Physics.SyncTransforms();
foreach (float y in new[] { 31.9f, 32.55f, 33.2f })
{
  sb.Append("y" + y + ": ");
  for (float z = -32f; z <= -26f; z += 0.25f)
  {
    RaycastHit h;
    sb.Append(z.ToString("F2") + "=" + (Physics.Raycast(new Vector3(269f, y, z), Vector3.left, out h, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.point.x.ToString("F2") + (h.collider.name.StartsWith("Q3_building") ? "" : "(" + h.collider.name + ")") : "--") + " ");
  }
  sb.AppendLine();
}
if (board != null) board.SetActive(true);
return sb.ToString();
