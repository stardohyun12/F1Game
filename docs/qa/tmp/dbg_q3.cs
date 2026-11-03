var sb = new System.Text.StringBuilder();
int n=0;
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(UnityEngine.FindObjectsSortMode.None)) {
  if (rs.centerline==null||rs.centerline.Length<2||rs.kind!="main") continue;
  for (int i=0;i<rs.centerline.Length;i+=25) {
    var p = rs.transform.TransformPoint(rs.centerline[i]);
    if (Mathf.Abs(p.x-150)<250 && Mathf.Abs(p.z+100)<250 && n<14) { sb.AppendLine("MAIN "+rs.roadId.Substring(0,Mathf.Min(14,rs.roadId.Length))+" "+p.ToString("F1")+" rsPos="+rs.transform.position); n++; }
  }
}
int k=0;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
  if (t.name!="Pier") continue;
  RaycastHit h; bool ok=Physics.Raycast(new Vector3(t.position.x,40,t.position.z),Vector3.down,out h,80f);
  if (ok && k<8) { sb.AppendLine("PIERHIT "+t.position.ToString("F1")+" -> "+h.point.ToString("F2")+" "+h.collider.name+" parent="+(t.parent!=null?t.parent.name:"-")); k++; }
}
return sb.ToString();
