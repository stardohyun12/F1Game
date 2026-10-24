Application.runInBackground=true; Time.timeScale=1f;
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var lab=UnityEditor.SessionState.GetString("p.label","LoneHouse");
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
var up0=UnityEngine.Object.FindFirstObjectByType<UrgentCallPanel>(); if(up0!=null) typeof(UrgentCallPanel).GetMethod("OnDeclineClicked",bf).Invoke(up0,null);
var fo=typeof(OrderBoard).GetField("currentOffer",bf);
var offer=(System.Collections.Generic.List<OrderJob>)fo.GetValue(ob);
var names=string.Join(",",offer.Select(j=>string.Join("/",j.destinationLabels)));
int pick=offer.FindIndex(j=>j.destinationLabels.Contains(lab));
if(pick<0){ var all=Resources.FindObjectsOfTypeAll<OrderJob>().Where(j=>j.destinationLabels.Contains(lab)).ToList(); if(all.Count>0){ offer[0]=all[0]; pick=0; } }
if(pick<0) return "NO ORDER for "+lab+" offer="+names;
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{pick});
typeof(OrderBoard).GetMethod("OnBoardGoClicked",bf).Invoke(ob,null);
var up=UnityEngine.Object.FindFirstObjectByType<UrgentCallPanel>(); if(up!=null) typeof(UrgentCallPanel).GetMethod("OnDeclineClicked",bf).Invoke(up,null);
var r=Services.Run as RunController;
return "offer="+names+" pick="+pick+" limit="+r.ParTime+" stage="+r.CurrentStage+" job="+r.CurrentJob;
