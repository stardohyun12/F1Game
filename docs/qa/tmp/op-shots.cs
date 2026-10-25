var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp"));
var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
{
    if (!t.name.StartsWith("Shot_")) continue;
    cam.transform.SetPositionAndRotation(t.position, t.rotation); cam.fieldOfView = t.name.StartsWith("Shot_1") ? 55f : 50f;
    var rt = new RenderTexture(640, 360, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
    var tex = new Texture2D(640, 360, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "op_" + t.name + ".png"), tex.EncodeToPNG()); rt.Release();
    sb.Append(t.name + t.position.ToString("F1") + " ");
}
cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
return sb.ToString();
