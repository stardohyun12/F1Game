// 과제 2050: A6d 는 빛이 없어 렌더러 대부분이 그림자 드리우기·받기 꺼짐 → A7 재질 쓰는 렌더러는 켠다(하늘·접지 얼룩 제외)
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); if (!sc.path.EndsWith("Scene_A7_Light.unity")) return "wrong scene " + sc.path;
int n = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  if (r.name == "Sky" || !(r is MeshRenderer)) continue; bool lit = false; foreach (var m in r.sharedMaterials) if (m && m.shader.name == "PitCrew/SceneA7/Lit") lit = true; if (!lit) continue;
  r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; EditorUtility.SetDirty(r); n++; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "shadows on for " + n;
