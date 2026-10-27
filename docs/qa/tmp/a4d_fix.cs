// 과제 1331 고침 (에디트 모드, Scene_A4d_Town 만)
//  1 a3_M1_mound 가 길(z −55~−92)을 최대 7.6m 덮어 차가 올라타 날아감 → 길 위·길 옆 R m 안의 mound 정점을 길 면 −0.3m 로 내림(메쉬 복사본, 콜라이더 같은 메쉬)
//  2 카트 속도: 최고 65km/h, accelTime 3.5s (가설)
const float R = 8f, BELOW = 0.3f;
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4d_Town.unity");
if (!AssetDatabase.IsValidFolder("Assets/Art/SceneA4d")) AssetDatabase.CreateFolder("Assets/Art", "SceneA4d");
var m = GameObject.Find("a3_M1_mound"); var road = GameObject.Find("Road").GetComponent<Collider>(); var mf = m.GetComponent<MeshFilter>();
var nm = UnityEngine.Object.Instantiate(mf.sharedMesh); nm.name = "a4d_M1_mound"; var v = nm.vertices; int moved = 0;
for (int i = 0; i < v.Length; i++) { var wp = m.transform.TransformPoint(v[i]); float ry = float.PositiveInfinity;
  for (int a = 0; a < 17; a++) { var o = a == 0 ? Vector3.zero : Quaternion.Euler(0, a * 45, 0) * Vector3.forward * (a <= 8 ? R * 0.5f : R);
    if (road.Raycast(new Ray(new Vector3(wp.x + o.x, 80, wp.z + o.z), Vector3.down), out var h, 200)) ry = Mathf.Min(ry, h.point.y); }
  if (!float.IsInfinity(ry) && wp.y > ry - BELOW) { wp.y = ry - BELOW; v[i] = m.transform.InverseTransformPoint(wp); moved++; } }
nm.vertices = v; nm.RecalculateBounds(); nm.RecalculateNormals(); AssetDatabase.DeleteAsset("Assets/Art/SceneA4d/a4d_M1_mound.asset"); AssetDatabase.CreateAsset(nm, "Assets/Art/SceneA4d/a4d_M1_mound.asset");
mf.sharedMesh = nm; m.GetComponent<MeshCollider>().sharedMesh = nm;
var cso = new SerializedObject(GameObject.Find("Race").GetComponent<ArcadeCarController>());
cso.FindProperty("maxSpeedKmh").floatValue = 65f; cso.FindProperty("accelTime").floatValue = 3.5f; cso.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return "mound verts lowered " + moved + "/" + v.Length;
