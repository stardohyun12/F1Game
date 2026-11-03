var sb=new System.Text.StringBuilder();
var o=new Vector3(175.2f,19.6f,-75.2f);
for(float dy=-0.02f;dy<=0.12f;dy+=0.02f) for(float dz=-0.1f;dz<=0.1f;dz+=0.05f){
 var d=new Vector3(1f,dy,dz).normalized;
 if(Physics.Raycast(o,d,out var h,400f)){ var r=h.collider.GetComponent<Renderer>(); string mn=r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"-";
  if(!h.collider.name.StartsWith("TownV6_Terrain")&&!h.collider.name.StartsWith("ROAD")) sb.AppendLine(dy.ToString("F2")+","+dz.ToString("F2")+" "+h.collider.name+" par="+(h.collider.transform.parent!=null?h.collider.transform.parent.name:"-")+" mat="+mn+" d="+h.distance.ToString("F0")+" p="+h.point.ToString("F1")); }
}
// blue-ish renderers
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){
 var m=r.sharedMaterial; if(m==null) continue;
 Color c=Color.white; if(m.HasProperty("_BaseColor")) c=m.GetColor("_BaseColor"); else if(m.HasProperty("_Color")) c=m.GetColor("_Color");
 if(c.b>0.5f&&c.r<0.25f&&c.g<0.45f){ var b=r.bounds; sb.AppendLine("BLUE "+r.name+" par="+(r.transform.parent!=null?r.transform.parent.name:"-")+" mat="+m.name+" c="+b.center.ToString("F1")+" s="+b.size.ToString("F1")); }
}
return sb.ToString();
