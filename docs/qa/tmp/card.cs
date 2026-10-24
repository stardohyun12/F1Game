var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var n=UnityEditor.SessionState.GetString("qaorder","Order_02_Glass");
OrderJob job=null; foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) if(o.name==n) job=o;
ps.Show(job);
var sb=new System.Text.StringBuilder(); foreach(var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None)) if(t.text!=null && t.text.Contains("다 감으면")) sb.Append(t.text.Replace("\n"," | ")+" ; ");
foreach(var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsSortMode.None)) if(t.text!=null && t.text.Contains("다 감으면")) sb.Append(t.text.Replace("\n"," | ")+" ; ");
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1551-card-"+n+".png");
return n+" "+job.baseReward+" "+sb;
