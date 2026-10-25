// 창 유리가 아틀라스 몇 번 칸인지: 칸 번호 텍스처로 바꿔 끼운 임시 머티리얼로 프리팹 앞/뒤를 렌더 → docs/qa/tmp/tileid_<id>_<side>.png
var tmp = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp"));
var items = new[] {
  ("Assets/Art/BayLoop/Buildings/shop_house_a.prefab", "id4"), ("Assets/Art/BayLoop/Buildings/terrace_house_a.prefab", "id4"),
  ("Assets/Art/BayLoop/Buildings/harbor_warehouse_a.prefab", "id4"),
  ("Assets/Art/LofiPilot/CoastalHouse.prefab", "id8"), ("Assets/Art/LofiPilot/CoastalHouseC.prefab", "id8") };
var sb = new System.Text.StringBuilder();
foreach (var (path, idt) in items)
{
    var tex = new Texture2D(2, 2); tex.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(tmp, idt + ".png"))); tex.filterMode = FilterMode.Point;
    var mat = new Material(Shader.Find("PitCrew/LofiPilot/Surface")); mat.SetTexture("_MainTex", tex); mat.SetFloat("_FaceShade", 0f); mat.SetFloat("_FogStart", 5000); mat.SetFloat("_FogEnd", 6000);
    var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf); go.transform.position = new Vector3(0, 500, 0);
    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
    var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    float dist = Mathf.Max(b.size.x, b.size.y) * 2.2f + b.extents.z;
    foreach (var side in new[] { 1, -1 })
    {
        var cg = new GameObject("TileCam"); var cam = cg.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray; cam.fieldOfView = 30;
        cg.transform.position = b.center + new Vector3(0, 0, side * dist); cg.transform.LookAt(b.center);
        var rt = new RenderTexture(640, 480, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var o = new Texture2D(640, 480, TextureFormat.RGB24, false); o.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); o.Apply(); RenderTexture.active = null;
        string nm = System.IO.Path.GetFileNameWithoutExtension(path) + (side > 0 ? "_pz" : "_nz");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(tmp, "tileid_" + nm + ".png"), o.EncodeToPNG());
        cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(cg); sb.Append(nm + " ");
    }
    UnityEngine.Object.DestroyImmediate(go);
}
return "tileid " + sb;
