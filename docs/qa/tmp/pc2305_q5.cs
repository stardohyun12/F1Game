var cam=Camera.main; var sb=new System.Text.StringBuilder();
// 화면 비율로: 파란 상자 중심 (1035/1920, 450/1080 위에서)
var vp=new Vector3(1035f/1920f, 1f-450f/1080f, 0f);
var ray=cam.ViewportPointToRay(vp);
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
 if(!r.enabled||!r.gameObject.activeInHierarchy) continue;
 var b=r.bounds; if(b.size.magnitude>300f) continue;
 if(b.IntersectRay(ray,out float dist) && dist>10f){
   var m=r.sharedMaterial; string col="-"; if(m!=null){ if(m.HasProperty("_BaseColor")) col=m.GetColor("_BaseColor").ToString("F2"); else if(m.HasProperty("_Color")) col=m.GetColor("_Color").ToString("F2"); }
   string path=r.name; var t=r.transform.parent; int n=0; while(t!=null&&n<4){ path=t.name+"/"+path; t=t.parent; n++; }
   sb.AppendLine(dist.ToString("F0")+" "+path+" mat="+(m!=null?m.name+" sh="+m.shader.name:"-")+" col="+col+" c="+b.center.ToString("F1")+" s="+b.size.ToString("F1"));
 }
}
return sb.ToString();
