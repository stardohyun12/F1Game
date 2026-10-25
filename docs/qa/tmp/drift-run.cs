var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>();
var T=typeof(ArcadeCarController); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=T.GetField("moveInput",bf); var fHb=T.GetField("qaForceHandbrake",bf);
var sb=new System.Text.StringBuilder(); float t0=Time.time; float lastLog=-1; float sSum=0,sMax=0,yaw75=0,yaw90=0,slip100=0,slip99=0,spd8=0; int sN=0;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{
 float t=Time.time-t0;
 float steer=0, thr=1; bool hb=false;
 if(t>=6&&t<8){hb=true;steer=1;} else if(t>=8&&t<9.5f){hb=true;steer=0;} else if(t>=9.5f&&t<11){hb=false;steer=0;}
 else if(t>=11){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); fHb.SetValue(c,false); UnityEditor.SessionState.SetString("qalog",sb.ToString()); UnityEditor.SessionState.SetString("qasum","meanAbsSlip(6.5-8)="+(sSum/Mathf.Max(1,sN)).ToString("F1")+" maxSlip="+sMax.ToString("F0")+" yawRate@7.5="+yaw75.ToString("F0")+" yawRate@9.0="+yaw90.ToString("F0")+" slip@10.0="+slip100.ToString("F1")+" slip@9.9="+slip99.ToString("F1")+" spd@8="+spd8.ToString("F0")); return; }
 { var v0=rb.linearVelocity; v0.y=0; float sl=v0.magnitude>1? Mathf.Abs(Vector3.SignedAngle(c.transform.forward,v0,Vector3.up)):0; if(t>=6.5f&&t<8f){sSum+=sl;sN++;sMax=Mathf.Max(sMax,sl);} if(t>=7.5f&&yaw75==0) yaw75=Mathf.Abs(rb.angularVelocity.y*Mathf.Rad2Deg); if(t>=9f&&yaw90==0) yaw90=Mathf.Abs(rb.angularVelocity.y*Mathf.Rad2Deg)+0.001f; if(t>=7.99f&&spd8==0) spd8=v0.magnitude*3.6f; if(t>=9.9f&&slip99==0) slip99=sl+0.001f; if(t>=10f&&slip100==0) slip100=sl+0.001f; }
 fMove.SetValue(c,new Vector2(steer,thr)); fHb.SetValue(c,hb);
 if(t-lastLog>=0.1f && (t>=5.5f || t<1f)){ lastLog=t; var v=rb.linearVelocity; v.y=0; float slip= v.magnitude>1? Vector3.SignedAngle(c.transform.forward,v,Vector3.up):0; sb.AppendLine(t.ToString("F2")+","+(v.magnitude*3.6f).ToString("F0")+","+c.transform.eulerAngles.y.ToString("F0")+","+slip.ToString("F1")+","+(rb.angularVelocity.y*Mathf.Rad2Deg).ToString("F0")+","+(hb?1:0)+","+c.transform.position.x.ToString("F0")+","+c.transform.position.y.ToString("F1")+","+c.transform.position.z.ToString("F0")); }
};
UnityEditor.EditorApplication.update+=cb; return "started";
