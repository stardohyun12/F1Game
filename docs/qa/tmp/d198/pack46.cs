// D-198 2차: 포장 지점을 마을길 46 위(보관소 문 앞 14 m, 직원 옆)로
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (!sc.path.EndsWith("Map_TownV6.unity")) return "wrong scene " + sc.path;
Transform F(string n) { foreach (var r in sc.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
float x = -44.96f, z = -14.09f;
var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600).Where(h => h.collider.name.StartsWith("ROAD_")).ToList();
float y = hs.Count > 0 ? hs.Max(h => h.point.y) : float.NaN;
var t = F("TUT_PackTrigger"); t.SetPositionAndRotation(new Vector3(x, y + 0.5f, z), Quaternion.Euler(0, 64f, 0));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
var o = F("TUT_MartOwner");
return "PackTrigger " + t.position.ToString("F2") + " road=" + (hs.Count > 0 ? hs[0].collider.name : "none") + " owner dist " + Vector3.Distance(o.position, t.position).ToString("F1");
