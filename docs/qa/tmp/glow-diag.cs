// Play 중: WindowGlow 첫 삼각형 몇 개 앞 4m 에서 카메라로 찍어 보이는지 확인 → bayloop-mood/test_glow_diag<i>.png
Application.runInBackground = true;
var go = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "WindowGlow").gameObject;
var mf = go.GetComponent<MeshFilter>(); var mr = go.GetComponent<MeshRenderer>(); var m = mf.sharedMesh;
var v = m.vertices; var tw = m.GetTriangles(0);
var sb = new System.Text.StringBuilder();
sb.Append($"active={go.activeInHierarchy} parent={go.transform.parent?.name} pos={go.transform.position} rot={go.transform.rotation.eulerAngles} scale={go.transform.lossyScale} layer={go.layer} bounds={mr.bounds} enabled={mr.enabled}\n");
foreach (var mat in mr.sharedMaterials) sb.Append($"{mat.name} shader={mat.shader.name} q={mat.renderQueue} col={mat.GetColor("_BaseColor")}\n");
var cam0 = Camera.main; sb.Append($"cam mask={cam0.cullingMask} layerVisible={(cam0.cullingMask & (1 << go.layer)) != 0}\n");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-mood"));
for (int s = 0; s < 2; s++)
{
    int ti = (tw.Length / 3 / 2 * s) * 3;
    Vector3 a = go.transform.TransformPoint(v[tw[ti]]), b = go.transform.TransformPoint(v[tw[ti + 1]]), c = go.transform.TransformPoint(v[tw[ti + 2]]);
    var n = Vector3.Cross(b - a, c - a).normalized; var mid = (a + b + c) / 3f;
    var cgo = new GameObject("DiagCam"); var cam = cgo.AddComponent<Camera>(); cam.CopyFrom(cam0);
    cgo.transform.position = mid + n * 5f; cgo.transform.LookAt(mid);
    var rt = new RenderTexture(480, 270, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(480, 270, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "test_glow_diag" + s + ".png"), tex.EncodeToPNG());
    sb.Append($"tri{s} mid={mid} n={n} size={(b - a).magnitude:F2}x{(c - a).magnitude:F2}\n");
    UnityEngine.Object.Destroy(tex); rt.Release(); UnityEngine.Object.Destroy(cgo);
}
return sb.ToString();
