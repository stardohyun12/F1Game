// UI 2판 설치(에디트 모드): DotScreenUI.uiShader = PitCrew/UI/DotPalette, 방향 = 나침반(기본), 씬 저장.
if (UnityEditor.EditorApplication.isPlaying) return "PLAYING - stop first";
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
if (ui == null) return "NO DotScreenUI";
var sh = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/UI/DotUI/DotUIPalette.shader");
var so = new UnityEditor.SerializedObject(ui);
so.FindProperty("uiShader").objectReferenceValue = sh;
so.FindProperty("dirMode").enumValueIndex = (int)DotScreenUI.DirMode.Compass;
so.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ui.gameObject.scene);
var sl = SunsetLook.Find();
return "shader=" + (sh != null) + " saved=" + ok + " on=" + ui.name + " sunset=" + (sl != null ? ("on=" + sl.IsOn + " amount=" + sl.amount + " dusk=" + sl.dusk) : "none");
