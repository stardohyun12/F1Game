// 보관소 앞 높이 지도: x 258~286, z -44~-31 (1 m), 값 = 땅 y (소수 1자리), 길이면 r 붙임
var sb = new System.Text.StringBuilder("x→ 258..286\n");
for (float z = -31f; z >= -44f; z -= 1f)
{
  sb.Append("z" + z.ToString("F0") + ": ");
  for (float x = 258f; x <= 286f; x += 1f)
  {
    RaycastHit h;
    if (Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out h, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
      sb.Append(h.point.y.ToString("F1") + (h.collider.name.StartsWith("ROAD") ? "r" : h.collider.name.StartsWith("TownV6_Terrain") ? "" : "b") + " ");
    else sb.Append("--- ");
  }
  sb.AppendLine();
}
var cc = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().Player.Controller;
sb.AppendLine("player stepOffset=" + cc.stepOffset + " slopeLimit=" + cc.slopeLimit + " h=" + cc.height + " r=" + cc.radius);
return sb.ToString();
