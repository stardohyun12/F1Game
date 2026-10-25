var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"CoastalHouse","CoastalHouseB","CoastalHouseC","CoastalHouseD","CoastalHouseE","CoastalHouseF","CornerShop","CornerShopB","CornerShopC","RepairGarage","RepairGarageB","Stall","UtilityPole","Bollard","Wall3m"}) {
  var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/LofiPilot/"+n+".prefab");
  var g = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(p);
  var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
  sb.AppendLine(n+" c="+b.center.ToString("F2")+" s="+b.size.ToString("F2")+" cols="+g.GetComponentsInChildren<Collider>().Length);
  UnityEngine.Object.DestroyImmediate(g);
}
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); sb.AppendLine("active="+sc.path+" dirty="+sc.isDirty);
var proto = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Proto_CoastCourse.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
foreach (var r in proto.GetRootGameObjects()) sb.AppendLine("root " + r.name + " ch=" + r.transform.childCount + " comps=" + string.Join(",", System.Linq.Enumerable.Select(r.GetComponents<Component>(), c => c.GetType().Name)));
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(proto, true);
return sb.ToString();
