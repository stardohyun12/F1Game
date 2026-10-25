// 갈림길 입구(217,107) 통과 시 유리 HP. route r 중심선을 입구 40m 전부터 40m 후까지 15m/s 로 주행.
Application.runInBackground=true; Time.timeScale=1f;
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
var frame=t.GetMethod("RouteFrame"); int route=UnityEditor.SessionState.GetInt("proto.route",1);
float T=(float)t.GetMethod("RouteTotal").Invoke(null,new object[]{route});
System.Func<float,Vector3[]> F=q=>{ var a=new object[]{route,q,null,null,null}; frame.Invoke(null,a); return new[]{(Vector3)a[2],(Vector3)a[3]}; };
float fq=0,fd=1e9f; for(float q=0;q<T;q+=1f){ var p=F(q)[0]; float d=new Vector2(p.x-217,p.z-107).magnitude; if(d<fd){fd=d;fq=q;} }
var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
var mi=typeof(ArcadeCarController).GetField("moveInput",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
var box=UnityEngine.Object.FindFirstObjectByType<CargoBox>(); var hpP=typeof(CargoBox).GetProperties().FirstOrDefault(p=>p.Name.Contains("Hp")||p.Name.Contains("HP")||p.Name.Contains("Health"));
System.Func<float> HP=()=>hpP!=null?System.Convert.ToSingle(hpP.GetValue(box)):-1f;
float s0=fq-40, s1=fq+40; var f0=F(s0); car.transform.SetPositionAndRotation(f0[0]+Vector3.up*0.6f,Quaternion.LookRotation(new Vector3(f0[1].x,0,f0[1].z))); rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; Physics.SyncTransforms();
float hp0=HP(), minHp=hp0, t0=Time.time, bs=s0, maxV=0; string drops=""; float prev=hp0;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){UnityEditor.EditorApplication.update-=cb;return;}
 var p=rb.position; float best=1e9f; for(float q=bs-5;q<=bs+15;q+=.5f){ var c=F(q)[0]; float d=new Vector2(c.x-p.x,c.z-p.z).sqrMagnitude; if(d<best){best=d; if(q>bs)bs=q;} }
 var tg=F(bs+7)[0]-p; tg.y=0; var fw=car.transform.forward; fw.y=0; float ang=Vector3.SignedAngle(fw,tg,Vector3.up); float v=rb.linearVelocity.magnitude; maxV=Mathf.Max(maxV,v);
 mi.SetValue(car,new Vector2(Mathf.Clamp(ang/25f,-1,1),Mathf.Clamp((15f-v)/3f,-1,1)));
 float h=HP(); if(prev-h>=1f) drops+=(prev-h).ToString("F0")+"@"+p.ToString("F0")+"v"+(v*3.6f).ToString("F0")+"; "; prev=h; minHp=Mathf.Min(minHp,h);
 if(bs>=s1||Time.time-t0>25f){ mi.SetValue(car,Vector2.zero); UnityEditor.EditorApplication.update-=cb; UnityEditor.SessionState.SetString("p.fork","DONE route="+route+" forkQ="+fq+" fd="+fd.ToString("F1")+" s "+s0+"->"+bs.ToString("F0")+"/"+s1+" hp "+hp0.ToString("F0")+"->"+h.ToString("F0")+" maxV="+(maxV*3.6f).ToString("F0")+" lat="+Mathf.Sqrt(best).ToString("F1")+" drops: "+drops+" hpProp="+(hpP!=null?hpP.Name:"none")); }
};
UnityEditor.EditorApplication.update+=cb; return "fork start q="+fq+" hp="+hp0+" prop="+(hpP!=null?hpP.Name:"none");
