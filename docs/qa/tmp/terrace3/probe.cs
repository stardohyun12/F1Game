var c=new Vector2(185,-61);var sb=new System.Text.StringBuilder();
sb.AppendLine("terrain="+GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshFilter>().sharedMesh.name+" dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){
 if(!ch.gameObject.activeInHierarchy)continue;var mr=ch.GetComponent<MeshRenderer>();if(!mr)continue;var b=mr.bounds;
 float d=Vector2.Distance(c,new Vector2(Mathf.Clamp(c.x,b.min.x,b.max.x),Mathf.Clamp(c.y,b.min.z,b.max.z)));if(d>62)continue;
 var rs=ch.GetComponent<RoadSurface>();string extra="";
 if(rs!=null&&rs.centerline!=null){int inside=0;foreach(var p in rs.centerline)if(Vector2.Distance(c,new Vector2(p.x,p.z))<62)inside++;extra=" kind="+rs.kind+" w="+rs.width+" pts="+rs.centerline.Length+" in="+inside+" p0="+rs.centerline[0].ToString("F0")+" pN="+rs.centerline[rs.centerline.Length-1].ToString("F0");}
 sb.AppendLine(ch.name+" b=("+b.min.x.ToString("F0")+","+b.min.z.ToString("F0")+")-("+b.max.x.ToString("F0")+","+b.max.z.ToString("F0")+") mat="+mr.sharedMaterial.name+" col="+(ch.GetComponent<Collider>()!=null)+" layer="+ch.gameObject.layer+extra);
}
foreach(var nm in new[]{"D198_Buildings","TV6_Loop/AssetSlots"}){var g=GameObject.Find(nm);if(!g)continue;foreach(Transform s in g.transform){if(!s.gameObject.activeInHierarchy)continue;var p=s.position;if(Vector2.Distance(c,new Vector2(p.x,p.z))>75)continue;sb.AppendLine(nm+"/"+s.name+" pos="+p.ToString("F1")+" yaw="+s.eulerAngles.y.ToString("F0"));}}
var tb=GameObject.Find("TownV6_Gray/TerraceBlock");sb.AppendLine("TerraceBlock="+(tb!=null));
return sb.ToString();
