var sb = new System.Text.StringBuilder();
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
sb.Append("roots: " + string.Join(", ", sc.GetRootGameObjects().Select(r => r.name + "(" + r.transform.childCount + ")")) + "\n");
var P = new (string id, float x, float z, float y, float yaw)[] { ("room", -117.7f, -11.2f, 6.12f, 0.9f), ("garage", -63.2f, -6.7f, 8.58f, -174.8f), ("depot", -43.7f, -26.8f, 8.94f, 2.7f), ("postoffice", -57.7f, -25.6f, 8.61f, -5.3f) };
foreach (var p in P) {
  var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Buildings/" + p.id + "/" + p.id + ".prefab");
  var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf);
  go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
  var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
  int cols = go.GetComponentsInChildren<Collider>().Length;
  UnityEngine.Object.DestroyImmediate(go);
  sb.Append(p.id + " local bounds min=" + b.min.ToString("F2") + " max=" + b.max.ToString("F2") + " colliders=" + cols + "\n");
  var hits = Physics.RaycastAll(new Vector3(p.x, 200, p.z), Vector3.down, 400);
  foreach (var h in hits.OrderBy(h => h.distance)) sb.Append("   hit " + h.collider.name + " y=" + h.point.y.ToString("F2") + " root=" + h.collider.transform.root.name + "\n");
}
return sb.ToString();
