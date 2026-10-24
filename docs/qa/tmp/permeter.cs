// SessionState "qaorder" 주문으로 포장 화면을 다시 열고 재료별 m당 비용 + 캡처.
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var n=UnityEditor.SessionState.GetString("qaorder","Order_02_Glass");
OrderJob job=null; foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) if(o.name==n) job=o;
ps.Show(job);
var cpm=ps.GetType().GetMethod("CostPerMeter",bf); float w=(float)ps.GetType().GetProperty("CurrentWidthUV",bf).GetValue(ps);
string s=n+" reward="+job.baseReward+" w="+w+" :";
foreach(var m in Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>("WrapMaterials")) s+=" "+m.name+"="+((float)cpm.Invoke(ps,new object[]{m,w})).ToString("F0")+"/m";
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1536-permeter-"+n+".png");
return s;
