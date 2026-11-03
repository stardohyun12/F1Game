var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
System.Func<int> missing = () => {
  int m = 0;
  foreach (var r in scene.GetRootGameObjects())
    foreach (var c in r.GetComponentsInChildren<UnityEngine.Component>(true)) {
      if (c == null) { m++; continue; }
      var so = new UnityEditor.SerializedObject(c); var p = so.GetIterator();
      while (p.NextVisible(true))
        if (p.propertyType == UnityEditor.SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0) m++;
    }
  return m;
};
int before = missing();
int n = PitCrewPlan.PlanIO.HoldTutorial();
int after = missing();
var hold = UnityEngine.GameObject.Find("_TUT_HOLD");
var sb = new System.Text.StringBuilder("moved=" + n + " missingBefore=" + before + " missingAfter=" + after + " holdFindable=" + (hold != null) + "\n");
foreach (var r in scene.GetRootGameObjects()) if (r.name == "_TUT_HOLD") { sb.Append("hold active=" + r.activeSelf + ": "); foreach (UnityEngine.Transform t in r.transform) sb.Append(t.name + ", "); }
var tut = UnityEngine.GameObject.Find("TV6_Loop/Tutorial"); sb.Append("\nleft in Tutorial: "); foreach (UnityEngine.Transform t in tut.transform) sb.Append(t.name + ", ");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("\nsaved=" + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
