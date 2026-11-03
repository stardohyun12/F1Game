var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
  string n=t.name; string l=n.ToLower();
  if (n.StartsWith("OnFoot_")||n.StartsWith("DP_")||n=="FishingSpot"||n=="Cam_Sunset"||n=="house_home"||l.Contains("harbor")||l.Contains("harbour")||n.StartsWith("Cam_FB")||l.Contains("pier")||l.Contains("dock")) sb.AppendLine(n+" "+t.position.ToString("F1")+" yaw"+t.eulerAngles.y.ToString("F0")+" active="+t.gameObject.activeInHierarchy);
}
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
sb.AppendLine("dir="+(d!=null));
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); sb.AppendLine("car "+car.transform.position.ToString("F1"));
var s = SunsetLook.Find(); sb.AppendLine("sunset "+(s!=null)+" dusk="+(s!=null?s.dusk:-1)+" amount="+(s!=null?s.amount:-1));
return sb.ToString();
