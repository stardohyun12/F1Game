var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var item=ps.ActiveItem; var ws=item.GetComponent<PitCrew.Packing.WrapState>();
string s="item="+item.name+" rot="+item.transform.eulerAngles+" fwd="+item.transform.forward+" phase="+ps.GetType().GetField("phase",bf).GetValue(ps)+"\n";
var mats=ps.GetType().GetMethod("GetMaterials",bf).Invoke(ps,null) as System.Array; foreach(var m in mats) s+=m+"\n";
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; s+="cam fwd="+cam.transform.forward+" pos="+cam.transform.position+" itempos="+item.transform.position;
return s;
