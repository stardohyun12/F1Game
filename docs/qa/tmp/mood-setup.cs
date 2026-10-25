// 에디트 모드: 창 발광 메시 생성 + 하루 4종 프로파일 생성(force) + 씬에 MoodController(Mood, 자동 적용 끔, 기본 = 가을 오후) → 씬 저장
var sb = new System.Text.StringBuilder();
sb.Append(BuildWindowGlow.Run() + "\n");
MoodSwitcher.BuildDayProfiles(true); UnityEditor.AssetDatabase.SaveAssets();
foreach (var p in MoodSwitcher.DayPaths) sb.Append(p + " " + (UnityEditor.AssetDatabase.LoadAssetAtPath<MoodProfile>(p) != null) + "\n");
sb.Append(MoodSwitcher.Apply(MoodSwitcher.PathA) + " (스냅샷 먼저)\n");   // 지금 씬 = Mood A. OnValidate 가 프로파일을 바로 적용하므로 기본값도 A 로 둬서 씬 기준을 안 바꾼다
var mc = UnityEngine.Object.FindFirstObjectByType<MoodController>(FindObjectsInactive.Include);
if (mc == null) { var go = new GameObject("Mood"); mc = go.AddComponent<MoodController>(); }
var so = new UnityEditor.SerializedObject(mc);
so.FindProperty("applyOnAwake").boolValue = false;   // 판 순서 전환은 다음 과제 — 지금은 메뉴/SetProfile 수동
so.FindProperty("profile").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodSwitcher.PathA);
so.ApplyModifiedPropertiesWithoutUndo();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
sb.Append("MoodController on " + mc.name + " saved " + scene.path);
return sb.ToString();
