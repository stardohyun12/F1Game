var sb = new System.Text.StringBuilder();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
sb.AppendLine("scene=" + sc.path + " dirty=" + sc.isDirty + " playing=" + UnityEditor.EditorApplication.isPlaying);
foreach (var dm in UnityEngine.Object.FindObjectsByType<DeliveryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    var so = new UnityEditor.SerializedObject(dm);
    var pts = so.FindProperty("points");
    sb.AppendLine("DM " + dm.name + " points=" + pts.arraySize + " return=" + (so.FindProperty("startReturn").objectReferenceValue != null ? ((Transform)so.FindProperty("startReturn").objectReferenceValue).position.ToString("F1") : "null"));
}
foreach (var dp in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    sb.AppendLine("DP " + dp.name + " label=" + dp.Label + " pos=" + dp.transform.position.ToString("F1") + " active=" + dp.gameObject.activeInHierarchy);
foreach (var d in UnityEngine.Object.FindObjectsByType<OnFootDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    var so = new UnityEditor.SerializedObject(d);
    var sp = so.FindProperty("spawn").objectReferenceValue as Transform;
    var cs = so.FindProperty("cartSpot").objectReferenceValue as Transform;
    sb.AppendLine("OFD " + d.name + " parent=" + (d.transform.parent ? d.transform.parent.name : "-") + " spawn=" + (sp ? sp.position.ToString("F1") : "-") + " cart=" + (cs ? cs.position.ToString("F1") + " yaw=" + cs.eulerAngles.y.ToString("F0") : "-"));
    foreach (Transform c in d.transform) sb.AppendLine("  child " + c.name + " " + c.position.ToString("F1"));
}
foreach (var n in new[]{"TUT_PackTrigger","TUT_ReturnGarage","TUT_MartOwner","Garage","garage","Teahouse","TeaHouse"})
{
    var g = GameObject.Find(n); if (g) sb.AppendLine("GO " + n + " " + g.transform.position.ToString("F1"));
}
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    string nm = t.name.ToLower();
    if ((nm.Contains("tea") || nm.Contains("garage") || nm.Contains("boss") || nm.Contains("julia")) && t.parent != null && !t.parent.name.ToLower().Contains("tea") && !t.parent.name.ToLower().Contains("garage"))
        sb.AppendLine("T " + t.name + " parent=" + t.parent.name + " " + t.position.ToString("F1"));
}
var rc = UnityEngine.Object.FindFirstObjectByType<RunController>(FindObjectsInactive.Include);
sb.AppendLine("RC=" + (rc ? rc.name : "null") + " OT=" + (UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>(FindObjectsInactive.Include) != null));
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
sb.AppendLine("car=" + (car ? car.name + " " + car.transform.position.ToString("F1") : "null"));
var cb = UnityEngine.Object.FindFirstObjectByType<CargoBox>(FindObjectsInactive.Include);
sb.AppendLine("cargo=" + (cb ? cb.name + " parent=" + (cb.transform.parent ? cb.transform.parent.name : "-") : "null"));
foreach (var h in new[]{"DeliveryHUD","DeliveryProgressHUD","IntegrityHUD","ResultPanel"})
{
    var ty = System.Type.GetType(h + ", Assembly-CSharp");
    if (ty == null) { sb.AppendLine(h + " type?"); continue; }
    var o = UnityEngine.Object.FindFirstObjectByType(ty, FindObjectsInactive.Include) as Component;
    sb.AppendLine(h + "=" + (o ? o.name + " active=" + o.gameObject.activeInHierarchy : "null"));
}
return sb.ToString();
