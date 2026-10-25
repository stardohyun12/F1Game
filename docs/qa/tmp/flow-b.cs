var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
typeof(OrderBoard).GetMethod("OnBoardGoClicked",bf).Invoke(ob,null);
var rp=Services.RoutePlan; string s=(Services.Run as RunController).CurrentStage+" routeplan="+(rp==null?"null":rp.GetType().Name)+" | ";
if(rp!=null) foreach(var m in rp.GetType().GetMethods(bf|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly)) s+=m.Name+",";
return s;
