var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
string Q(Vector3 p,float r){ var s="@"+p.ToString("F1")+": "; foreach(var c in Physics.OverlapSphere(p,r)){ if(c.attachedRigidbody==rb) continue; if(c.GetComponentInParent<CargoBox>()!=null) continue; var b=c.bounds; if(b.size.x>60||b.size.z>60) continue; s+=c.name+"("+(c.transform.parent?c.transform.parent.name:"")+")c"+b.center.ToString("F1")+"sz"+b.size.ToString("F1")+"; "; } return s+" || "; }
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
string F(float q){ var a2=new object[]{0,q,null,null,null}; t.GetMethod("RouteFrame").Invoke(null,a2); return "s"+q+"="+((Vector3)a2[2]).ToString("F1")+" "; }
return "car="+rb.position.ToString("F1")+" "+Q(rb.position,4f)+Q(new Vector3(8,0,60),4f)+Q(new Vector3(37,-1,84),4f)+F(75)+F(85)+F(95)+F(110)+F(160)+F(165);
