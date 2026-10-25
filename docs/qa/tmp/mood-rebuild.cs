// 에디트 모드: 스냅샷 기준(Mood A)으로 되돌린 뒤 창 발광 + 하루 4종 다시 만들고 씬 저장. 컴파일 확인 겸 머티리얼 값 출력.
var sb = new System.Text.StringBuilder();
sb.Append(MoodSwitcher.Revert() + "\n");
sb.Append(BuildWindowGlow.Run() + "\n");
MoodSwitcher.BuildDayProfiles(true); UnityEditor.AssetDatabase.SaveAssets();
var w = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_BayLoop_Mats/BL_WindowWarm.mat");
sb.Append($"warm col={w.GetColor("_BaseColor")} cull={w.GetFloat("_Cull")} q={w.renderQueue}\n");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
sb.Append("saved " + scene.path);
return sb.ToString();
