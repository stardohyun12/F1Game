// 1.5초 직진(안전 샘플) → HP 기록 → y=-25 로 순간이동 → 리스폰 후 HP. SessionState "qafall".
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var rb=c.GetComponent<Rigidbody>();
var cargo=Services.Cargo as CargoBox;
float t0=Time.time; int st=0; float hp0=0; Vector3 p0=Vector3.zero; UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float t=Time.time-t0;
 if(st==0){ fMove.SetValue(c,new Vector2(0f,0.5f)); if(t>1.5f){ fMove.SetValue(c,Vector2.zero); rb.linearVelocity=Vector3.zero; hp0=cargo.IntegrityHP; p0=rb.position; rb.position=new Vector3(p0.x,-25f,p0.z); st=1; t0=Time.time; } }
 else if(st==1 && t>0.5f){ UnityEditor.EditorApplication.update-=cb;
  UnityEditor.SessionState.SetString("qafall","hpBefore="+hp0.ToString("F2")+" hpAfter="+cargo.IntegrityHP.ToString("F2")+" delta="+(hp0-cargo.IntegrityHP).ToString("F2")+" posBefore="+p0+" posAfter="+rb.position+" stage="+(Services.Run as RunController).CurrentStage); } };
UnityEditor.SessionState.SetString("qafall","running"); UnityEditor.EditorApplication.update+=cb; return "start hp="+cargo.IntegrityHP+" pos="+rb.position;
