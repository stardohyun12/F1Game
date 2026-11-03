var sb = new System.Text.StringBuilder();
var house = GameObject.Find("house_home");
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
  if (t.name.StartsWith("DOOR_")) { var r=t.GetComponent<Renderer>(); sb.AppendLine(t.name+" "+(r!=null?r.bounds.center:t.position).ToString("F1")); }
}
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(UnityEngine.FindObjectsSortMode.None)) {
  if (rs.centerline==null||rs.centerline.Length<2) continue;
  float len=0; for(int i=1;i<rs.centerline.Length;i++) len+=Vector3.Distance(rs.centerline[i-1],rs.centerline[i]);
  if (len<150) continue;
  var m=rs.centerline[rs.centerline.Length/2];
  sb.AppendLine("ROAD "+rs.roadId+" kind="+rs.kind+" len="+len.ToString("F0")+" mid="+m.ToString("F1")+" n="+rs.centerline.Length);
}
foreach (var p in new Vector2[]{new Vector2(120,18),new Vector2(110,30),new Vector2(100,38),new Vector2(130,10),new Vector2(120,0)}) {
  RaycastHit h; bool ok=Physics.Raycast(new Vector3(p.x,200,p.y),Vector3.down,out h,400f);
  sb.AppendLine("ground "+p+" -> "+(ok?h.point.ToString("F1")+" "+h.collider.name:"none"));
}
return sb.ToString();
