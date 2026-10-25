var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
var offer=(System.Collections.Generic.List<OrderJob>)typeof(OrderBoard).GetField("currentOffer",bf).GetValue(ob);
int idx=0; string names=""; for(int i=0;i<offer.Count;i++){ names+=offer[i].name+"("+offer[i].cargoType+") "; if(offer[i].cargoType==CargoType.Box && idx==0 && offer[0].cargoType!=CargoType.Box) idx=i; }
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{idx});
return "picked "+idx+" of: "+names;
