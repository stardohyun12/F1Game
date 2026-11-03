// terrace6: 차고(D198_garage) 주변 — 뿌리·회전·바닥 사각형, 자식 이름(입구 찾기), 근처 길 메시
var sb=new System.Text.StringBuilder();
var gT=GameObject.Find("D198_Buildings/D198_garage").transform;
sb.Append("garage pos "+gT.position.ToString("F2")+" yaw "+gT.eulerAngles.y.ToString("F1")+"\n");
var b=new Bounds(gT.position,Vector3.zero);foreach(var r in gT.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);sb.Append("bounds "+b.min.ToString("F1")+" "+b.max.ToString("F1")+"\n");
foreach(Transform c in gT){var rb=new Bounds(c.position,Vector3.zero);foreach(var r in c.GetComponentsInChildren<Renderer>())rb.Encapsulate(r.bounds);sb.Append("  "+c.name+" lp "+gT.InverseTransformPoint(c.position).ToString("F1")+" bmin "+rb.min.ToString("F1")+" bmax "+rb.max.ToString("F1")+"\n");}
var hT=GameObject.Find("D198_Buildings/house_home").transform;sb.Append("house pos "+hT.position.ToString("F2")+" yaw "+hT.eulerAngles.y.ToString("F1")+"\n");
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){var mr=ch.GetComponent<MeshRenderer>();if(!mr)continue;var rb=mr.bounds;if(rb.max.x<180||rb.min.x>260||rb.max.z<-95||rb.min.z>-30)continue;var rs=ch.GetComponent<RoadSurface>();sb.Append("road "+ch.name+" "+(rs!=null?rs.kind+" w"+rs.width:"")+" "+rb.min.ToString("F1")+" "+rb.max.ToString("F1")+(ch.gameObject.activeInHierarchy?"":" INACTIVE")+"\n");}
var tb=GameObject.Find("TownV6_Gray/TerraceBlock");sb.Append("terraceBlock "+(tb!=null)+"\n");
return sb.ToString();
