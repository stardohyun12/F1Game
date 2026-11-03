var sb=new System.Text.StringBuilder();
var bs=new[]{GameObject.Find("D198_depot").transform,GameObject.Find("D198_postoffice").transform};
foreach(var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)){ if(rs.centerline==null) continue;
 bool near=false; foreach(var q in rs.centerline) foreach(var b in bs) if(new Vector2(q.x-b.position.x,q.z-b.position.z).magnitude<30) near=true;
 if(!near) continue; sb.AppendLine("== "+rs.name+" w="+rs.width+" n="+rs.centerline.Length);
 foreach(var q in rs.centerline){ float d0=new Vector2(q.x-bs[0].position.x,q.z-bs[0].position.z).magnitude, d1=new Vector2(q.x-bs[1].position.x,q.z-bs[1].position.z).magnitude; if(Mathf.Min(d0,d1)<35) sb.AppendLine(q.ToString("F1")+" dDepot="+d0.ToString("F1")+" dPost="+d1.ToString("F1")); }
}
foreach(var b in bs){ sb.AppendLine(b.name+" rot="+b.eulerAngles.y.ToString("F0")+" fwd="+b.forward.ToString("F2")); foreach(Transform c in b.parent) sb.Append(c.name+" "); sb.AppendLine(); }
return sb.ToString();
