// 과제 0422: 찻집 배달점 주변 렌더러·콜라이더 + 카트 v6e 프리팹/에셋 위치
var sb = new System.Text.StringBuilder();
var tp = new Vector3(181.4f, 27.7f, 5.7f);
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if ((r.bounds.center - tp).magnitude > 14f) continue;
    if (r.bounds.size.magnitude < 3f) continue;
    sb.Append(r.name + "<" + (r.transform.parent ? r.transform.parent.name : "") + "> mat=" + (r.sharedMaterial ? r.sharedMaterial.name : "-") + " c=" + r.bounds.center.ToString("F0") + " s=" + r.bounds.size.ToString("F0") + " col=" + (r.GetComponent<Collider>() ? r.GetComponent<Collider>().GetType().Name : "-") + "\n");
}
var th = GameObject.Find("TUT_TeaHouse"); if (th) foreach (var c in th.GetComponentsInChildren<Collider>(true)) sb.Append("TH col " + c.name + " " + c.GetType().Name + " trig=" + c.isTrigger + " b=" + c.bounds.center.ToString("F1") + "/" + c.bounds.size.ToString("F1") + "\n");
foreach (var g in UnityEditor.AssetDatabase.FindAssets("kart_v6e")) sb.Append("asset " + UnityEditor.AssetDatabase.GUIDToAssetPath(g) + "\n");
foreach (var g in UnityEditor.AssetDatabase.FindAssets("kart-v6e")) sb.Append("asset " + UnityEditor.AssetDatabase.GUIDToAssetPath(g) + "\n");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
sb.Append("car " + car.name + " children: ");
foreach (Transform t in car.transform) sb.Append(t.name + (t.GetComponent<Collider>() ? "[" + t.GetComponent<Collider>().GetType().Name + "]" : "") + ", ");
foreach (var c in car.GetComponents<Collider>()) sb.Append(" rootCol " + c.GetType().Name + " " + c.bounds.size.ToString("F2"));
return sb.ToString();
