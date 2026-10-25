var sb = new System.Text.StringBuilder();
sb.Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " play=" + Application.isPlaying + "\n");
foreach (var t in new System.Type[] { typeof(RunController), typeof(OrderBoard), typeof(DeliveryManager), typeof(ResultPanel), typeof(BayLoopDriveOnly), typeof(MoodController), typeof(DeliveryHUD), typeof(CargoBox), typeof(DeliveryPoint), typeof(Canvas), typeof(TMPro.TextMeshProUGUI), typeof(OverviewCamera) })
{
    var objs = UnityEngine.Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None);
    sb.Append(t.Name + ":" + objs.Length + " ");
    foreach (var o in objs) { var c = o as Component; sb.Append("[" + c.gameObject.name + (c.gameObject.activeInHierarchy ? "" : "(off)") + "] "); if (t == typeof(TMPro.TextMeshProUGUI)) break; }
    sb.Append("\n");
}
var ps = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
foreach (var m in ps) if (m != null && m.GetType().Name.Contains("Packing")) sb.Append("PK " + m.GetType().Name + " @" + m.gameObject.name + "\n");
var tm = UnityEngine.Object.FindFirstObjectByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include);
if (tm != null) sb.Append("font " + (tm.font ? tm.font.name : "null") + "\n");
var mc = UnityEngine.Object.FindFirstObjectByType<MoodController>(); if (mc != null) sb.Append("mood " + (mc.Profile ? mc.Profile.name : "null") + "\n");
return sb.ToString();
