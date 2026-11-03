var sb=new System.Text.StringBuilder();
foreach(var nm in new[]{"ROAD_V2e_camo_309378634_165_0","ROAD_V2e_camo_309378634_165_1"}){var rs=GameObject.Find("TownV6_Gray/RoadsV2/"+nm).GetComponent<RoadSurface>();sb.Append(nm+": ");for(int i=0;i<rs.centerline.Length;i+=3)sb.Append(rs.centerline[i].ToString("F1")+" ");sb.AppendLine();}
var col=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
float G(float x,float z){RaycastHit h;return col.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)?h.point.y:float.NaN;}
foreach(var nm in new[]{"D198_Buildings/house_home","TV6_Loop/AssetSlots/Q_MART","D198_Buildings/D198_garage"}){var t=GameObject.Find(nm).transform;var b=new Bounds(t.position,Vector3.zero);foreach(var r in t.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);sb.AppendLine(nm+" bounds min="+b.min.ToString("F1")+" max="+b.max.ToString("F1")+" fwd="+t.forward.ToString("F2"));}
sb.Append("terrain z-line x=185: ");for(float z=-55;z>=-85;z-=2)sb.Append(z+":"+G(185,z).ToString("F1")+" ");sb.AppendLine();
sb.Append("terrain z-line x=200: ");for(float z=-50;z>=-85;z-=2)sb.Append(z+":"+G(200,z).ToString("F1")+" ");sb.AppendLine();
sb.Append("terrain z-line x=215: ");for(float z=-45;z>=-85;z-=2)sb.Append(z+":"+G(215,z).ToString("F1")+" ");sb.AppendLine();
sb.Append("terrain x-line z=-72: ");for(float x=170;x<=230;x+=3)sb.Append(x+":"+G(x,-72).ToString("F1")+" ");sb.AppendLine();
return sb.ToString();
