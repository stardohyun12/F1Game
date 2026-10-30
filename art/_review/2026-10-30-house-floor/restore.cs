if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Studio_Building" || UnityEditor.EditorApplication.isPlaying) return "FAIL: Studio edit mode required";
foreach (var nm in new[] { "ground", "road" }) {
var go = GameObject.Find("STUDIO/" + (nm == "ground" ? "ST_Ground" : "ST_Road"));
if (!go) throw new System.Exception("Missing studio ground");
var mf = go.GetComponent<MeshFilter>();
if (AssetDatabase.GetAssetPath(mf.sharedMesh) != "Assets/Art/Review/house_home/ST_" + nm + "_house_cut.asset") continue;
var original = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Studio_Building_Mats/ST_" + nm + "_mesh.asset");
if (!original) throw new System.Exception("Missing original ground mesh");
mf.sharedMesh = original; var mc = go.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = original;
EditorUtility.SetDirty(go);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "original studio ground restored";
