// 과제 0640 — 되돌린 씬 다시 열고 720 Nearest 확인
AssetDatabase.Refresh();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity");
var lr = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>(FindObjectsInactive.Include);
int plates = 0; foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name.StartsWith("ROAD_PLATE_")) plates++;
return lr == null ? "LowRes 없음" : $"LowRes en={lr.enabled} h={lr.targetHeight} smooth={lr.smooth} | ROAD_PLATE {plates}";
