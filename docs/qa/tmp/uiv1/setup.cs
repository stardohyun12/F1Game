// UI 1판 설치(에디트 모드): Galmuri 가져오기 = Hinted Raster(안티앨리어싱 없음), Map_TownV6 감독에 DotScreenUI, 씬 저장.
var s = "";
foreach (var p in new[]{"Assets/UI/Fonts/Galmuri/Galmuri11.ttf","Assets/UI/Fonts/Galmuri/Galmuri9.ttf"}) {
  var imp = (UnityEditor.TrueTypeFontImporter)UnityEditor.AssetImporter.GetAtPath(p);
  imp.fontRenderingMode = UnityEditor.FontRenderingMode.HintedRaster; imp.fontTextureCase = UnityEditor.FontTextureCase.Dynamic; imp.includeFontData = true;
  imp.SaveAndReimport(); s += p + " ok; ";
}
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (dir == null) return s + "NO DIRECTOR";
var ui = dir.GetComponent<DotScreenUI>(); if (ui == null) ui = dir.gameObject.AddComponent<DotScreenUI>();
var so = new UnityEditor.SerializedObject(ui);
so.FindProperty("director").objectReferenceValue = dir;
so.FindProperty("font").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
so.FindProperty("smallFont").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
so.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dir.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(dir.gameObject.scene);
return s + " added on " + dir.name + " font=" + (so.FindProperty("font").objectReferenceValue != null);
