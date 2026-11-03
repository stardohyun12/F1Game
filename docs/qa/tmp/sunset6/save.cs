// 편집 모드: 최종 켬 값(① 평면 반사 SeaReflection, ② _Spec.x = 1, ③④⑤ 끔)을 머티리얼·씬에 저장. 노을 끈 상태로.
var look = SunsetLook.Find(); var m = look.seaMaterial;
var sp = m.GetVector("_Spec"); sp.x = __SPEC__f; m.SetVector("_Spec", sp);
var mi = m.GetVector("_Micro"); mi.x = 0; m.SetVector("_Micro", mi);
m.SetVector("_Dash", new Vector4(1, 0, 0, 0));
m.SetVector("_HorizonFade", new Vector4(300, 650, 0.5f, 0));
m.SetFloat("_Style", 1); m.SetFloat("_Debug", 0);
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
if (look.IsOn) look.SetOn(false);
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
return "saved=" + ok + " on=" + look.IsOn + " seaMat=" + look.sea.sharedMaterial.name + " near=" + look.seaNear.enabled + " spec=" + m.GetVector("_Spec") + " sr=" + (look.GetComponent<SeaReflection>() != null);
