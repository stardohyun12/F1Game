// artlive 2056: 끝 상태 — Play 여부, 씬 저장 여부, 슬롯 켜짐
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (!Application.isPlaying && s.isDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var on = new System.Text.StringBuilder(); foreach (Transform t in slot.transform) if (t.gameObject.activeSelf) on.Append(t.name + " ");
return "playing=" + Application.isPlaying + " scene=" + s.name + " dirty=" + s.isDirty + " on=" + on;
