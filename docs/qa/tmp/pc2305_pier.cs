Application.runInBackground=true;
var dir=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); var pl=dir.Player;
DebugWindow.GoPlace(5);
float t0=Time.realtimeSinceStartup; float tGo=t0+0.8f; float last=t0; float minY=99f; string log=""; bool started=false; int sec=-1; float tStart=0;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float n=Time.realtimeSinceStartup;
 if(!started && n>=tGo){ started=true; tStart=n; pl.simActive=true; pl.simMove=new Vector2(0f,1f); log+="start "+pl.transform.position.ToString("F2")+" "; }
 if(started){ minY=Mathf.Min(minY,pl.transform.position.y); int s=(int)(n-tStart); if(s!=sec){ sec=s; log+="t"+s+":"+pl.transform.position.ToString("F1")+" "; } }
 if(started && n-tStart>8f){ UnityEditor.EditorApplication.update-=cb; pl.simMove=Vector2.zero; pl.simActive=false; UnityEditor.SessionState.SetString("pc.pier","minY="+minY.ToString("F2")+" end="+pl.transform.position.ToString("F2")+" state="+dir.State+" | "+log); } };
UnityEditor.EditorApplication.update+=cb; return "go";
