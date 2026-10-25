var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
var s=""; foreach(var q in new float[]{150,170,190,210,230,260,300,330}){ var a2=new object[]{0,q,null,null,null}; t.GetMethod("RouteFrame").Invoke(null,a2); var p=(Vector3)a2[2];
 var hs=Physics.RaycastAll(p+Vector3.up*15f,Vector3.down,40f).Where(h=>h.collider.attachedRigidbody==null).OrderByDescending(h=>h.point.y).Take(3);
 s+="s"+q+" c="+p.ToString("F1")+" hits:"+string.Join(",",hs.Select(h=>h.collider.name+"("+h.collider.transform.parent?.name+")="+h.point.y.ToString("F1")))+" | "; }
var mf=t.GetMethod("RouteTotal"); s+=" total0="+mf.Invoke(null,new object[]{0});
var hs2=Physics.RaycastAll(new Vector3(180,15,107),Vector3.down,40f).OrderByDescending(h=>h.point.y); s+=" old(180,107):"+string.Join(",",hs2.Select(h=>h.collider.name+"="+h.point.y.ToString("F1")));
return s;
