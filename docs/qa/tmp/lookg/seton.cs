var s = SunsetLook.Find();
var so = new UnityEditor.SerializedObject(s);
so.FindProperty("dusk").floatValue = __DUSK__f;
so.ApplyModifiedProperties();
s.SetOn(true);
UnityEditor.EditorUtility.SetDirty(s);
var sc = s.gameObject.scene; UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
bool saved = __SAVE__ && UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "on=" + s.IsOn + " dusk=" + s.dusk + " az=" + s.SunAzimuthNow + " seaNear=" + s.seaNear.enabled + " " + s.seaNear.transform.position + " backdrop=" + s.backdrop.activeSelf + " sky=" + RenderSettings.skybox.name + " saved=" + saved + " scene=" + sc.path;
