var sb=new System.Text.StringBuilder();
var c=new Vector3(185,0,-61);float R=68;
var top=GameObject.Find("TownV6_Gray").transform;
var terr=top.Find("TownV6_Terrain");
var src=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_0433.asset");
var tg=new GameObject("_tmpcol"){hideFlags=HideFlags.HideAndDontSave};var tmc=tg.AddComponent<MeshCollider>();tmc.sharedMesh=src;
var cmc=terr.GetComponent<MeshCollider>();
float G(Collider col,float x,float z){RaycastHit h;return col.Raycast(new Ray(new Vector3(x,300,z),Vector3.down),out h,600)?h.point.y:float.NaN;}
try{
foreach(Transform ch in top.Find("RoadsV2")){var mr=ch.GetComponent<MeshRenderer>();if(!mr||!ch.gameObject.activeInHierarchy)continue;var b=mr.bounds;
 float dx=Mathf.Max(0,Mathf.Max(b.min.x-c.x,c.x-b.max.x)),dz=Mathf.Max(0,Mathf.Max(b.min.z-c.z,c.z-b.max.z));if(new Vector2(dx,dz).magnitude>R)continue;
 sb.AppendLine("RD "+ch.name+" v="+ch.GetComponent<MeshFilter>().sharedMesh.vertexCount+" min="+b.min.ToString("F1")+" max="+b.max.ToString("F1")+" mat="+mr.sharedMaterial.name+" col="+(ch.GetComponent<Collider>()!=null));}
sb.AppendLine("grid cur/0433 (x 155..215 step 5, z -31..-91 step 5)");
for(float z=-31;z>=-91;z-=5){sb.Append("z"+z+":");for(float x=155;x<=215;x+=5){sb.Append(" "+G(cmc,x,z).ToString("F1")+"/"+G(tmc,x,z).ToString("F1"));}sb.AppendLine();}
var hs=GameObject.Find("D198_Buildings/house_home/house_home/HOUSE_shell");var mf=hs.GetComponent<MeshFilter>();var lo=new Bounds();bool f=false;int n=0;
foreach(var v in mf.sharedMesh.vertices){var w=hs.transform.TransformPoint(v);if(w.y<25.2f){n++;if(!f){lo=new Bounds(w,Vector3.zero);f=true;}else lo.Encapsulate(w);}}
sb.AppendLine("house shell verts below 25.2: "+n+" bounds "+lo.min.ToString("F2")+".."+lo.max.ToString("F2"));
var gs=GameObject.Find("D198_Buildings/D198_garage/garage_v5_combo/GARAGE_shell");mf=gs.GetComponent<MeshFilter>();f=false;n=0;float gmin=999;foreach(var v in mf.sharedMesh.vertices){var w=gs.transform.TransformPoint(v);gmin=Mathf.Min(gmin,w.y);}
foreach(var v in mf.sharedMesh.vertices){var w=gs.transform.TransformPoint(v);if(w.y<gmin+.3f){n++;if(!f){lo=new Bounds(w,Vector3.zero);f=true;}else lo.Encapsulate(w);}}
sb.AppendLine("garage shell min y "+gmin+" low verts "+n+" bounds "+lo.min.ToString("F2")+".."+lo.max.ToString("F2"));
foreach(var p in new[]{new Vector2(185,-61),new Vector2(198,-57),new Vector2(225,-78),new Vector2(225,-71.5f),new Vector2(178,-0.4f),new Vector2(221,-54)})sb.AppendLine("at "+p+" cur="+G(cmc,p.x,p.y).ToString("F2")+" 0433="+G(tmc,p.x,p.y).ToString("F2"));
}finally{UnityEngine.Object.DestroyImmediate(tg);}
return sb.ToString();
