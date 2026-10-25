// 프리팹 앞모습 확인(간판 글자가 바로 읽히나): 원점 멀리 놓고 +Z 앞 6m 에서 -Z 를 보며 렌더
var ids = new[] { "shop_house_a", "harbor_warehouse_b" };
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp"));
var sb = new System.Text.StringBuilder();
foreach (var id in ids)
{
    var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/BayLoop/Buildings/" + id + ".prefab");
    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf); go.transform.position = new Vector3(0, 500, 0);
    var cg = new GameObject("PrefabCam"); var cam = cg.AddComponent<Camera>();
    cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray; cam.fieldOfView = 22;
    float sy = id.StartsWith("shop") ? 502.8f : 505.3f;
    cg.transform.position = new Vector3(0, sy, 8f); cg.transform.LookAt(new Vector3(0, sy, 0));
    var rt = new RenderTexture(640, 400, 24); cam.targetTexture = rt; cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(640, 400, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 400), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "prefab_" + id + ".png"), tex.EncodeToPNG());
    cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(cg); UnityEngine.Object.DestroyImmediate(go);
    sb.Append(id + " ");
}
return "shot " + sb;
