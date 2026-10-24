var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
float t0=Time.time; string log=""; float lastLog=0; UnityEditor.EditorApplication.CallbackFunction cb=null;
UnityEditor.SessionState.SetString("qadrive","running");
cb=()=>{ float t=Time.time-t0; var st=(Services.Run as RunController).CurrentStage.ToString();
 fMove.SetValue(c,new Vector2(0f,1f));
 if(t-lastLog>=1f){ lastLog=t; log+=string.Format("t={0:F1} pos=({1:F1},{2:F1}) v={3:F0}km/h yaw={4:F0} stage={5}\n",t,c.transform.position.x,c.transform.position.z,rb.linearVelocity.magnitude*3.6f,c.transform.eulerAngles.y,st); UnityEditor.SessionState.SetString("qadrive",log); }
 if(t>14f||st!="Driving"){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qadrive",log+"END stage="+st); } };
UnityEditor.EditorApplication.update+=cb; return "driving fwd="+c.transform.forward;
