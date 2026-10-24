// 실제 fallRespawnY 로 추락→리스폰 검증. 리스폰=한 프레임 5m 이상 순간이동. 리스폰 지점의 중심선 거리·아래 충돌체 기록.
Application.runInBackground=true; Time.timeScale=1f;
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
var frame=t.GetMethod("RouteFrame"); var tot=t.GetMethod("RouteTotal");
var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var mi=typeof(ArcadeCarController).GetField("moveInput",bf);
var fy=typeof(ArcadeCarController).GetField("fallRespawnY",bf); string fyv=fy!=null?fy.GetValue(car).ToString():"na";
System.Func<int,float,Vector3[]> F=(r,q)=>{ var a=new object[]{r,q,null,null,null}; frame.Invoke(null,a); return new[]{(Vector3)a[2],(Vector3)a[3],(Vector3)a[4]}; };
var cl=new System.Collections.Generic.List<Vector3>(); for(int r=0;r<2;r++){ float T=(float)tot.Invoke(null,new object[]{r}); for(float q=0;q<T;q+=1f) cl.Add(F(r,q)[0]); }
float hq=0,hd=1e9f; float T0=(float)tot.Invoke(null,new object[]{0}); for(float q=0;q<T0;q+=1f){ var p=F(0,q)[0]; float d=new Vector2(p.x-8,p.z-60).magnitude; if(d<hd){hd=d;hq=q;} }
var tests=new System.Collections.Generic.List<(int r,float q,int side)>(); tests.Add((0,hq,1)); tests.Add((0,hq,-1)); foreach(var q in new float[]{300,450,600,800}){ tests.Add((1,q,1)); tests.Add((1,q,-1)); }
int idx=-1; float t0=0, minY=0; Vector3 last=Vector3.zero; bool resp=false; string log="fallRespawnY="+fyv+" hairpinQ="+hq+" | ";
UnityEditor.SessionState.SetString("p.fall","running");
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){UnityEditor.EditorApplication.update-=cb;return;}
 var rc=Services.Run as RunController; if(rc.CurrentStage.ToString()!="Driving"){ UnityEditor.EditorApplication.update-=cb; UnityEditor.SessionState.SetString("p.fall","ABORT stage="+rc.CurrentStage+" "+log); return; }
 var p=rb.position;
 if(idx>=0&&!resp){ minY=Mathf.Min(minY,p.y); if((p-last).magnitude>5f){ resp=true; float bd=1e9f; Vector3 bc=Vector3.zero; foreach(var c in cl){ float d=new Vector2(c.x-p.x,c.z-p.z).magnitude; if(d<bd){bd=d;bc=c;} }
   RaycastHit h; string gn=Physics.Raycast(p+Vector3.up,Vector3.down,out h,10f)?h.collider.name+"@"+h.point.y.ToString("F1"):"none";
   log+="T"+idx+"(r"+tests[idx].r+" q"+tests[idx].q+" s"+tests[idx].side+") RESPAWN after minY="+minY.ToString("F1")+" at "+p.ToString("F1")+" lat="+bd.ToString("F1")+"m dy="+(p.y-bc.y).ToString("F1")+" ground="+gn+"; "; mi.SetValue(car,Vector2.zero); }
  else mi.SetValue(car,new Vector2(0,1)); }
 last=p;
 if(idx<0||resp&&Time.time-t0>0.5f&&false||Time.time-t0>12f){
  if(idx>=0&&!resp) log+="T"+idx+"(r"+tests[idx].r+" q"+tests[idx].q+" s"+tests[idx].side+") noRespawn minY="+minY.ToString("F1")+" end="+p.ToString("F1")+"; ";
  idx++; if(idx>=tests.Count){ UnityEditor.EditorApplication.update-=cb; mi.SetValue(car,Vector2.zero); UnityEditor.SessionState.SetString("p.fall","DONE "+log); return; }
  var f=F(tests[idx].r,tests[idx].q); var od=f[2]*tests[idx].side; od.y=0; od.Normalize();
  rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; car.transform.SetPositionAndRotation(f[0]+Vector3.up*0.8f+od*1.5f,Quaternion.LookRotation(od)); Physics.SyncTransforms();
  last=car.transform.position; t0=Time.time; minY=999; resp=false; }
};
UnityEditor.EditorApplication.update+=cb; return "tests="+tests.Count+" "+log;
