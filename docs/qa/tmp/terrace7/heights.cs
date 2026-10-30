// terrace7: 차고 동쪽 높이 격자(가장 높은 콜라이더). R=차길 W=보도 G=차고 T=지형 P=보행 리본
var cols=new System.Collections.Generic.List<(Collider c,char k)>();
cols.Add((GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>(),'T'));
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add((wk.GetComponent<MeshCollider>(),'W'));
var dw=GameObject.Find("TownV6_Gray/TerraceBlock/Driveway");if(dw)cols.Add((dw.GetComponent<MeshCollider>(),'D'));
var rw=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls");if(rw)cols.Add((rw.GetComponent<MeshCollider>(),'X'));
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){if(!ch.gameObject.activeInHierarchy)continue;var c=ch.GetComponent<Collider>();if(c)cols.Add((c,ch.name.StartsWith("ROAD_V2p")?'P':'R'));}
foreach(var c in GameObject.Find("D198_Buildings/D198_garage").GetComponentsInChildren<Collider>())cols.Add((c,'G'));
var sb=new System.Text.StringBuilder();sb.Append("      ");
for(float x=220;x<=264.1f;x+=2f)sb.Append(x.ToString("F0").PadLeft(6));sb.Append("\n");
for(float z=-38;z>=-76.1f;z-=2f){sb.Append(z.ToString("F0").PadLeft(6));
 for(float x=220;x<=264.1f;x+=2f){float y=float.NegativeInfinity;char k='?';foreach(var (c,kk) in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)&&h.point.y>y+.01f){y=h.point.y;k=kk;}}sb.Append((y.ToString("F1")+k).PadLeft(6));}
 sb.Append("\n");}
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){var rs=ch.GetComponent<RoadSurface>();if(rs==null||rs.centerline==null)continue;if(!(ch.name.Contains("ridge_up_b")||ch.name.Contains("ridge_up_c")||ch.name.Contains("165_1")||ch.name.Contains("245")))continue;
 sb.Append(ch.name+" w"+rs.width+" n"+rs.centerline.Length+": ");int st=Mathf.Max(1,rs.centerline.Length/12);for(int i=0;i<rs.centerline.Length;i+=st)sb.Append(rs.centerline[i].ToString("F1")+" ");sb.Append(rs.centerline[rs.centerline.Length-1].ToString("F1")+"\n");}
return sb.ToString();
