Application.runInBackground=true;
var dir=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); var pl=dir.Player;
string spec=UnityEditor.SessionState.GetString("pc.exitwp","271.9,-30.4;269.8,-30.5;269.6,-31.6;270.3,-32.6;272.5,-33.6;277.0,-33.6;277.0,-37.0");
var wps=new System.Collections.Generic.List<Vector2>(); foreach(var s in spec.Split(';')){ var a=s.Split(','); wps.Add(new Vector2(float.Parse(a[0],System.Globalization.CultureInfo.InvariantCulture),float.Parse(a[1],System.Globalization.CultureInfo.InvariantCulture))); }
var start=new Vector3(272.3f,31.1f,-31.8f);
dir.DebugTeleport(start,270f,null,270f+90f);
int i=0; float tWp=-1f; float t0=Time.realtimeSinceStartup; float tBegin=t0+0.8f; string log=""; bool run=false;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float n=Time.realtimeSinceStartup;
 if(!run){ if(n<tBegin) return; run=true; tWp=n; pl.simActive=true; }
 var p=pl.transform.position; var to=new Vector2(wps[i].x-p.x,wps[i].y-p.z);
 if(to.magnitude<0.35f){ log+="ok"+i+"@"+(n-t0).ToString("F1")+" "; i++; tWp=n; if(i>=wps.Count){ UnityEditor.EditorApplication.update-=cb; pl.simMove=Vector2.zero; pl.simActive=false; UnityEditor.SessionState.SetString("pc.exit","DONE "+log+" end="+pl.transform.position.ToString("F1")); return; } return; }
 if(n-tWp>6f){ UnityEditor.EditorApplication.update-=cb; pl.simMove=Vector2.zero; pl.simActive=false; UnityEditor.SessionState.SetString("pc.exit","STUCK wp"+i+" "+wps[i]+" at "+p.ToString("F2")+" "+log); return; }
 pl.Cam.Yaw=Mathf.Atan2(to.x,to.y)*Mathf.Rad2Deg; pl.simMove=new Vector2(0f,1f); };
UnityEditor.EditorApplication.update+=cb; return "go";
