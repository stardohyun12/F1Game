Application.runInBackground=true;
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
var up0=UnityEngine.Object.FindFirstObjectByType<UrgentCallPanel>(); if(up0!=null) typeof(UrgentCallPanel).GetMethod("OnDeclineClicked",bf).Invoke(up0,null);
var offer=(System.Collections.Generic.List<OrderJob>)typeof(OrderBoard).GetField("currentOffer",bf).GetValue(ob); int pick=offer.FindIndex(j=>j.destinationLabels.Contains("LoneHouse")); if(pick<0){ offer[0]=UnityEditor.AssetDatabase.LoadAssetAtPath<OrderJob>("Assets/Data/Orders/ProtoCourse/Proto_4_LoneHouse.asset"); pick=0; }
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{pick});
typeof(OrderBoard).GetMethod("OnBoardGoClicked",bf).Invoke(ob,null);
var up=UnityEngine.Object.FindFirstObjectByType<UrgentCallPanel>(); if(up!=null) typeof(UrgentCallPanel).GetMethod("OnDeclineClicked",bf).Invoke(up,null);
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var item=ps.ActiveItem; var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera;
var r=Services.Run as RunController;
return "offer="+string.Join(",",offer.Select(j=>j.destinationLabels[0]))+" pick="+pick+" limit="+r.ParTime+" stage="+r.CurrentStage+" job="+r.CurrentJob+" item.fwd="+item.transform.forward+" cam.fwd="+cam.transform.forward+" dot="+Vector3.Dot(item.transform.forward,cam.transform.forward)+" urgentPanel="+(up!=null);
