var ps = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(FindObjectsInactive.Include);
var so = new UnityEditor.SerializedObject(ps);
so.FindProperty("rattleFull").floatValue = 2f;
so.FindProperty("rattleLoss").floatValue = 0.40f;
so.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ps.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ps.gameObject.scene);
return "rattleFull=" + so.FindProperty("rattleFull").floatValue + " rattleLoss=" + so.FindProperty("rattleLoss").floatValue + " scene=" + ps.gameObject.scene.path;
