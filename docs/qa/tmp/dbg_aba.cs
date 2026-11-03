var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); string o="";
Application.runInBackground=true;
foreach(int i in new int[]{1,4,1}){ string r=DebugWindow.GoPlace(i); var p=d.Player.transform.position; o+=i+" "+r.Substring(0,System.Math.Min(60,r.Length))+" | feet="+p.ToString("F2")+" state="+d.State+"\n"; }
var pl=d.Player; pl.simActive=true; pl.simMove=new Vector2(0f,1f); o+="walk start "+pl.transform.position.ToString("F2")+"\n";
return o;
