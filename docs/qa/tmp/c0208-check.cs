var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
var sb = new System.Text.StringBuilder();
foreach (var t in race.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("CarVisual_") || t.name.StartsWith("Extra_") || t.name == "ChassisLean") { var p = t.parent; sb.Append(p.name + "/" + t.name + " act=" + t.gameObject.activeSelf + "\n"); }
return sb.ToString();
