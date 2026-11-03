var names = new[]{"LofiPilot/StreetLamp","LofiPilot/WallLamp","LofiPilot/Planter","LofiPilot/Pots","LofiPilot/Bench","LofiPilot/Wall3m","LofiPilot/Curb","StreetFurniture/Bench","StreetFurniture/Bin","BayLoop/Props/planter_row","BayLoop/Props/street_sign_01","BayLoop/Props/street_sign_02","BayLoop/Props/street_sign_03","BayLoop/Props/small_signs_a","BayLoop/Props/small_signs_c","BayLoop/Props/traffic_mirror_01","LofiPilot/UtilityPole","LofiPilot/Clothesline"};
var sb = new System.Text.StringBuilder();
sb.AppendLine("scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" play="+UnityEditor.EditorApplication.isPlaying);
foreach (var n in names) {
  var g = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/"+n+".prefab");
  if (g==null){sb.AppendLine(n+" MISSING");continue;}
  var i = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(g); i.transform.position=UnityEngine.Vector3.zero; i.transform.rotation=UnityEngine.Quaternion.identity;
  var rs=i.GetComponentsInChildren<UnityEngine.Renderer>(); var b=rs.Length>0?rs[0].bounds:new UnityEngine.Bounds(); foreach(var r in rs) b.Encapsulate(r.bounds);
  int cols=i.GetComponentsInChildren<UnityEngine.Collider>().Length;
  sb.AppendLine(n+" min="+b.min.ToString("F2")+" max="+b.max.ToString("F2")+" cols="+cols+" rends="+rs.Length+" scale="+g.transform.localScale.ToString("F2"));
  UnityEngine.Object.DestroyImmediate(i);
}
var rr = UnityEngine.GameObject.Find("Roadside"); sb.AppendLine("Roadside root=" + (rr!=null));
var top = UnityEngine.GameObject.Find("TownV6_Gray"); sb.AppendLine("TownV6_Gray=" + (top!=null) + " TB=" + (top!=null && top.transform.Find("TerraceBlock")!=null));
if (top!=null){ var tb=top.transform.Find("TerraceBlock"); if(tb!=null) foreach(UnityEngine.Transform c in tb) sb.AppendLine("  TB/"+c.name); }
return sb.ToString();
