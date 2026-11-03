// 편집 모드: 노을 끈 기본 상태로 씬 저장 + 머티리얼 디버그 0 확인
if (Application.isPlaying) return "still playing";
var look = SunsetLook.Find(); if (look.IsOn) look.SetOn(false);
var m = look.seaMaterial; m.SetFloat("_Debug", 0); m.SetFloat("_Style", 1);
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "saved=" + ok + " on=" + look.IsOn + " seaMat=" + look.sea.sharedMaterial.name + " near=" + look.seaNear.enabled + " ripple=" + m.GetVector("_Ripple") + " style=" + m.GetFloat("_Style") + " debug=" + m.GetFloat("_Debug");
