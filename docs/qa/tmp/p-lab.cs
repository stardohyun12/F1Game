var s="";foreach(var d in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None)) s+=d.Label+"@"+d.transform.position+" ";
s+=" | jobs:"+string.Join(",",Resources.FindObjectsOfTypeAll<OrderJob>().Select(j=>j.name+"="+string.Join("/",j.destinationLabels)));
return s;
