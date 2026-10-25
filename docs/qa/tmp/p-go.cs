Application.runInBackground=true;
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var r=Services.Run as RunController;
if(r.CurrentStage.ToString()=="Packing") typeof(PitCrew.Packing.PackingScreen).GetMethod("OnGoClicked",bf).Invoke(ps,null);
var up=UnityEngine.Object.FindFirstObjectByType<UrgentCallPanel>(); if(up!=null) typeof(UrgentCallPanel).GetMethod("OnDeclineClicked",bf).Invoke(up,null);
return "stage="+r.CurrentStage+" elapsed="+r.Elapsed;
