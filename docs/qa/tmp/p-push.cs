Application.runInBackground=true;
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
DeliveryPoint dp=null; foreach(var d in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(UnityEngine.FindObjectsSortMode.None)) if(d.IsActive){dp=d;break;}
if(dp==null) return "no active DP";
var hp=string.Join(",",UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None).Select(b=>b.name+"="+b.IntegrityHP));
var tgt=dp.transform.position; var to=tgt-rb.position; to.y=0; float dist=to.magnitude;
var start=tgt-to.normalized*6f; start.y=tgt.y+0.6f;
rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; c.transform.SetPositionAndRotation(start,Quaternion.LookRotation(to)); Physics.SyncTransforms();
float t0=Time.time; UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ var r=Services.Run as RunController; var st=r.CurrentStage.ToString(); float t=Time.time-t0;
 fMove.SetValue(c,new Vector2(0f,0.5f));
 if(t>10f||st!="Driving"){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("proto.drive","PUSH stage="+st+" t="+t.ToString("F1")+" hpBefore="+hp); } };
UnityEditor.EditorApplication.update+=cb; return "pushing from dist "+dist.ToString("F1")+" hp="+hp;
