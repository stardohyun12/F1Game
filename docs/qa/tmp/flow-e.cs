var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var fHb=typeof(ArcadeCarController).GetField("qaForceHandbrake",bf);
float t0=Time.time; UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float t=Time.time-t0; if(t>5f){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); fHb.SetValue(c,false); UnityEditor.SessionState.SetString("qadrive","done v="+c.GetComponent<Rigidbody>().linearVelocity.magnitude*3.6f+" pos="+c.transform.position); return; }
 fMove.SetValue(c,new Vector2(t>2.5f&&t<3.5f?0.8f:0f,1f)); fHb.SetValue(c,t>3f&&t<3.8f); };
UnityEditor.EditorApplication.update+=cb; UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-flow-3-driving.png"); return "driving "+(Services.Run as RunController).CurrentStage;
