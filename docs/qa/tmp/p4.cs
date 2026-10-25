var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; c.transform.SetPositionAndRotation(new Vector3(120,0.5f,66), Quaternion.Euler(0,90,0)); Physics.SyncTransforms();
float t0=Time.time; string log=""; UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float t=Time.time-t0; var r=Services.Run as RunController; var st=r.CurrentStage.ToString();
 fMove.SetValue(c,new Vector2(0f,1f));
 if(t>12f||st!="Driving"){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qadrive","END stage="+st+" t="+t+" x="+c.transform.position.x+" reward="+r.LastReward+" done="+r.CompletedCount+" integ="+r.LastIntegrityPercent+" success="+r.LastRunSuccess+" beatPar="+r.LastBeatPar); } };
UnityEditor.SessionState.SetString("qadrive","running"); UnityEditor.EditorApplication.update+=cb; return "driving";
