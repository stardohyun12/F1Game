// terrace5: 마당 남쪽 끝 → 큰길 보도까지 직각 거리(직선 계단이 들어갈 자리가 있나). 지금 땅 + 보도 콜라이더 기준.
var cols=new System.Collections.Generic.List<Collider>();cols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add(wk.GetComponent<MeshCollider>());
var rw=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls");if(rw)cols.Add(rw.GetComponent<MeshCollider>());
float Y(float x,float z){float y=float.NegativeInfinity;foreach(var c in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}return y;}
var sb=new System.Text.StringBuilder();
foreach(var x in new[]{186f,190f,194f,198f}){
 float zTop=float.NaN,zBot=float.NaN;string prof="";
 for(float z=-64;z>=-82;z-=.25f){float y=Y(x,z);if(Mathf.Abs(z-Mathf.Round(z))<.01f)prof+=z.ToString("F0")+":"+y.ToString("F1")+" ";
  if(float.IsNaN(zTop)&&y<24.2f)zTop=z;if(float.IsNaN(zBot)&&y<20.8f)zBot=z;}
 sb.AppendLine("x="+x+" yardEdge z="+zTop+" walk(<20.8) z="+zBot+" run="+(zTop-zBot).ToString("F1")+" | "+prof);}
return sb.ToString();
