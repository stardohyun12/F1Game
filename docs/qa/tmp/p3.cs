var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; c.transform.SetPositionAndRotation(new Vector3(72,0.5f,60), Quaternion.identity); Physics.SyncTransforms();
float t0=Time.time; string log=""; float lastLog=0; bool crashed=false; UnityEditor.EditorApplication.CallbackFunction cb=null;
UnityEditor.SessionState.SetString("qadrive","running");
cb=()=>{ float t=Time.time-t0; var r=Services.Run as RunController; var st=r.CurrentStage.ToString();
 fMove.SetValue(c,new Vector2(0f,1f));
 if(!crashed&&t>1.6f){ crashed=true; log+="CRASH v="+(rb.linearVelocity.magnitude*3.6f)+"\n"; rb.linearVelocity=rb.linearVelocity*0.05f; }
 if(t-lastLog>=0.5f){ lastLog=t; log+=string.Format("t={0:F1} z={1:F1} v={2:F0}km/h stage={3}\n",t,c.transform.position.z,rb.linearVelocity.magnitude*3.6f,st); UnityEditor.SessionState.SetString("qadrive",log); }
 if(t>12f||st!="Driving"){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qadrive",log+"END stage="+st+" t="+t+" reward="+r.LastReward+" done="+r.CompletedCount+" integ="+r.LastIntegrityPercent); } };
UnityEditor.EditorApplication.update+=cb; return "driving";
