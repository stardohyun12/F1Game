// 20261030-2334: Map_TownV6 의 house_home 인스턴스를 새 프리팹으로 갈아끼움. 도현 배치(부모·로컬 위치·회전·크기·형제 순서)는 그대로.
// 옛 인스턴스의 오버라이드는 전부 OnFootSetup 이 붙인 것(DoorHinge 5·난간 MeshCollider 3·BoxCollider 끔 3) — 지우고 Setup 을 다시 돌린다.
var log = new System.Text.StringBuilder(); bool DRY = false;
var pfPath = "Assets/Art/Library/Buildings/house_home/house_home.prefab";
var pf = AssetDatabase.LoadAssetAtPath<GameObject>(pfPath);
var olds = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
  .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == pf).ToArray();
log.Append("instances " + olds.Length + "\n");
foreach (var o in olds) {
  var par = o.parent; int sib = o.GetSiblingIndex(); var lp = o.localPosition; var lr = o.localRotation; var ls = o.localScale; var nm = o.name; bool act = o.gameObject.activeSelf;
  log.Append("old " + nm + " parent=" + (par ? par.name : "-") + " sib=" + sib + " lp=" + lp.ToString("F3") + " eul=" + lr.eulerAngles.ToString("F3") + " ls=" + ls.ToString("F3") + " world=" + o.position.ToString("F3") + "\n");
  if (DRY) continue;
  UnityEngine.Object.DestroyImmediate(o.gameObject);
  var n = (GameObject)PrefabUtility.InstantiatePrefab(pf, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  n.transform.SetParent(par, false); n.transform.SetSiblingIndex(sib); n.transform.localPosition = lp; n.transform.localRotation = lr; n.transform.localScale = ls; n.name = nm; n.SetActive(act);
  log.Append("new " + n.name + " world=" + n.transform.position.ToString("F3") + " eul=" + n.transform.eulerAngles.ToString("F3") + " children=" + n.GetComponentsInChildren<Transform>(true).Length + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return log.ToString();
