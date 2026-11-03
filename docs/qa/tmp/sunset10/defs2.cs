var l = SunsetLook.Find(); var sc = l.gameObject.scene; bool was = sc.isDirty;
l.v10 = new SunsetLook.Sunset10(); l.v9 = false; l.Apply();
UnityEditor.EditorUtility.SetDirty(l);
if (!was) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
var v = l.v10;
return "scene=" + sc.path + " wasDirty=" + was + " az=" + v.sunAzimuth + " sil=" + v.silhouette + " pf=" + v.seaPathFocus + " dusk=" + l.dusk;
