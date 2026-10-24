var d=UnityEditor.EditorApplication.update; int n=0; string names="";
if(d!=null) foreach(var h in d.GetInvocationList()){ var tn=h.Method.DeclaringType!=null?h.Method.DeclaringType.FullName:""; var asm=h.Method.DeclaringType!=null?h.Method.DeclaringType.Assembly.GetName().Name:""; names+=asm+":"+tn+"."+h.Method.Name+" | ";
 if(h.Target!=null && h.Target.GetType().GetField("captured")!=null){ UnityEditor.EditorApplication.update-=(UnityEditor.EditorApplication.CallbackFunction)h; n++; } }
var t=System.Type.GetType("LowResRenderTest, PitCrew"); (Camera.main.GetComponent(t) as Behaviour).enabled=false;
return "removed="+n+" ; "+names;
