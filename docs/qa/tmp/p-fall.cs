Application.runInBackground=true; Time.timeScale=1f;
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
int route=UnityEditor.SessionState.GetInt("proto.route",0); float step=UnityEditor.SessionState.GetInt("p.step",50);
float total=(float)t.GetMethod("RouteTotal").Invoke(null,new object[]{route}); var frame=t.GetMethod("RouteFrame");
var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var mi=typeof(ArcadeCarController).GetField("moveInput",bf);
var fy=typeof(ArcadeCarController).GetField("fallRespawnY",bf); if(fy!=null) fy.SetValue(car,-1000f);
var tests=new System.Collections.Generic.List<(float q,int side)>();
for(float q=20;q<total-5;q+=step){ tests.Add((q,1)); tests.Add((q,-1)); }
float dur=3f; var qs=UnityEditor.SessionState.GetString("p.qs",""); if(qs!=""){ tests.Clear(); foreach(var s in qs.Split(',')){ float qq=float.Parse(s); tests.Add((qq,1)); tests.Add((qq,-1)); } dur=9f; }
int idx=-1; float t0=0, minRel=0; string fails="", summary=""; int falls=0; Vector3 c0=Vector3.zero, outDir=Vector3.zero; float baseY=0, maxOut=0;
UnityEditor.SessionState.SetString("p.fall","running");
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){UnityEditor.EditorApplication.update-=cb;return;}
 var r=Services.Run as RunController; if(r.CurrentStage.ToString()!="Driving"){ UnityEditor.EditorApplication.update-=cb; UnityEditor.SessionState.SetString("p.fall","ABORT stage="+r.CurrentStage+" idx="+idx+"/"+tests.Count+" falls="+falls+" "+fails); return; }
 if(idx>=0){ minRel=Mathf.Min(minRel,rb.position.y-baseY); maxOut=Mathf.Max(maxOut,Vector3.Dot(rb.position-c0,outDir)); mi.SetValue(car,new Vector2(0,1)); }
 if(idx<0||Time.time-t0>dur){
  if(idx>=0){ bool gr=Physics.Raycast(rb.position+Vector3.up*1f,Vector3.down,60f); bool fell=(!gr)||rb.position.y<-20f||rb.linearVelocity.y<-6f; if(fell){falls++; fails+="q"+tests[idx].q+"s"+tests[idx].side+"(y"+rb.position.y.ToString("F0")+" vy"+rb.linearVelocity.y.ToString("F1")+" ground="+gr+" minRel"+minRel.ToString("F0")+") ";} summary+=minRel.ToString("F0")+"/"+rb.position.y.ToString("F0")+","; }
  idx++; if(idx>=tests.Count){ UnityEditor.EditorApplication.update-=cb; mi.SetValue(car,Vector2.zero); UnityEditor.SessionState.SetString("p.fall","DONE tests="+tests.Count+" falls="+falls+" fails="+fails+" minRel list="+summary); return; }
  var a=new object[]{route,tests[idx].q,null,null,null}; frame.Invoke(null,a); var pos=(Vector3)a[2]; var rt=(Vector3)a[4];
  outDir=rt*tests[idx].side; outDir.y=0; outDir.Normalize(); c0=pos; baseY=pos.y+0.8f; minRel=0; maxOut=0;
  rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero;
  car.transform.SetPositionAndRotation(pos+Vector3.up*0.8f+outDir*1.5f,Quaternion.LookRotation(outDir)); Physics.SyncTransforms(); t0=Time.time; }
};
UnityEditor.EditorApplication.update+=cb; return "tests="+tests.Count+" total="+total;
