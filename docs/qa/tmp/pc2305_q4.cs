var cam=Camera.main; var sb=new System.Text.StringBuilder();
sb.AppendLine("cam "+cam.transform.position.ToString("F1")+" fwd "+cam.transform.forward.ToString("F2")+" fov "+cam.fieldOfView);
// 화면 중앙선 기준 좌우 -2..+2도, 위 0..9도 — 큰길 파란 상자는 중앙 약간 왼쪽·수평선 아래쪽
var seen=new System.Collections.Generic.HashSet<string>();
for(int iy=-3;iy<=10;iy++) for(int ix=-12;ix<=6;ix++){
 var d=Quaternion.AngleAxis(ix*0.5f,cam.transform.up)*Quaternion.AngleAxis(-iy*0.5f,cam.transform.right)*cam.transform.forward;
 foreach(var h in Physics.RaycastAll(cam.transform.position,d,500f)){
  string key=h.collider.name+"|"+(h.collider.transform.parent!=null?h.collider.transform.parent.name:"-");
  if(key.StartsWith("TownV6_Terrain")||key.StartsWith("ROAD")||key.StartsWith("Walkways")||key.StartsWith("RetainingWalls")) continue;
  if(seen.Add(key)){ var r=h.collider.GetComponent<Renderer>(); sb.AppendLine(key+" d="+h.distance.ToString("F0")+" p="+h.point.ToString("F1")+" mat="+(r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name+"/"+r.sharedMaterial.shader.name:"-")); }
 }
}
return sb.ToString();
