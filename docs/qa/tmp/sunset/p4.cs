var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var rig = spot.transform.Find("FishingRig");
var sb = new System.Text.StringBuilder();
sb.Append("state=" + d.State + " seated=" + spot.Seated + " rig=" + (rig != null) + " player=" + d.Player.transform.position.ToString("F2") + " active=" + d.Player.gameObject.activeSelf + " prompt='" + d.Prompt + "'\n");
foreach (var l in d.Log) sb.Append(l + "\n");
return sb.ToString();
