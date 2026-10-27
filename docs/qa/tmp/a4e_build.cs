// 과제 1429 — Scene_A4e_Town = Scene_A4d_Town 복사(카트 실치수·Wheels 그대로).
//  집 6채 메쉬·텍스처를 e판(Assets/Art/SceneA4e/houses, a4e_import.cs 로 넣음)으로 교체. 빈 부모 house_* 자리·회전·+0.5m·낮춘 길 벽은 A4c 에서 물려받은 그대로.
//  e판 fbx 는 집마다 메쉬 하나(소품까지 합쳐짐) → MeshCollider 대신 몸체 BoxCollider (parts.json W·D·Hw, 메쉬 로컬 = Blender 축, Z 위).
//    몸체 가로세로 중심 = 벽 높이 30~90% 구간 정점의 가운데(소품·차양은 대부분 그 아래). 축대 집(zb>0)은 축대 상자 하나 더(메쉬 전체 바닥면 × zb).
const string D = "Assets/Art/SceneA4e/", S = "Assets/Scenes/Scene_A4e_Town.unity";
var body = new System.Collections.Generic.Dictionary<string, float[]> {   // W, D, Hw, zb (parts.json)
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A4d_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
foreach (Transform t in g) {
  if (!body.ContainsKey(t.name)) { log.Append("skip " + t.name + "; "); continue; }
  var oldInst = t.GetChild(0); var oldMat = oldInst.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  var m = new Material(oldMat) { name = "SA4e_" + t.name }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat");
  mats.Remove(oldMat); mats.Add(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = Vector3.zero;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3];
  foreach (var mf in inst.GetComponentsInChildren<MeshFilter>()) {
    var v = mf.sharedMesh.vertices; float lo = zb + 0.3f * (Hw - zb), hi = zb + 0.9f * (Hw - zb); float x0 = 1e9f, x1 = -1e9f, y0 = 1e9f, y1 = -1e9f;
    foreach (var p in v) if (p.z >= lo && p.z <= hi) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); }
    var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
    log.Append(t.name + " body c " + bc.center.ToString("F2") + " s " + bc.size.ToString("F1") + " midspan " + (x1 - x0).ToString("F1") + "x" + (y1 - y0).ToString("F1"));
    if (zb > 0f) { var mb = mf.sharedMesh.bounds; var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); log.Append(" + plinth s " + bc2.size.ToString("F1")); }
    log.Append("; "); }
}
tg.materials = mats.ToArray(); tg.Apply();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
