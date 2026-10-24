// 활성 배달지 15m 앞(지면 위)에 차를 놓고 그쪽으로 풀스로틀 → 정상 완료. SessionState "qadrive".
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
DeliveryPoint dp=null; foreach(var d in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(UnityEngine.FindObjectsSortMode.None)) if(d.IsActive){dp=d;break;}
if(dp==null) return "no active DP stage="+(Services.Run as RunController).CurrentStage;
Vector3 tgt=dp.transform.position; Vector3 start=tgt+new Vector3(-15,0,0); start.y=0.5f;
rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; c.transform.SetPositionAndRotation(start, Quaternion.LookRotation(new Vector3(tgt.x-start.x,0,tgt.z-start.z))); Physics.SyncTransforms();
int w0=UpgradeSystem.Instance.Wallet;
float t0=Time.time; UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float t=Time.time-t0; var r=Services.Run as RunController; var st=r.CurrentStage.ToString();
 fMove.SetValue(c,new Vector2(0f,1f));
 if(t>10f||st!="Driving"){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qadrive","END stage="+st+" t="+t.ToString("F1")+" reward="+r.LastReward+" done="+r.CompletedCount+" success="+r.LastRunSuccess+" walletBeforeDrive="+w0+" walletAfter="+UpgradeSystem.Instance.Wallet); } };
UnityEditor.SessionState.SetString("qadrive","running"); UnityEditor.EditorApplication.update+=cb; return "driving to "+dp.Label+" "+tgt+" wallet="+w0;
