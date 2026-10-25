var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp"));
var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
var cs = GameObject.Find("TUT_CarStart").transform; var c = cs.position; var f = cs.forward;
var right = Vector3.Cross(Vector3.up, f);
var cands = new Vector3[] { -f * 26 + Vector3.up * 14 + right * 6, -f * 22 + Vector3.up * 12 - right * 10, f * 10 + Vector3.up * 16 - right * 22, -f * 14 + Vector3.up * 18 + right * 16 };
for (int i = 0; i < cands.Length; i++)
{
    var pos = c + cands[i];
    cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(c + f * 20f - pos)); cam.fieldOfView = 50f;
    var rt = new RenderTexture(640, 360, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
    var tex = new Texture2D(640, 360, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "op_s4_" + i + ".png"), tex.EncodeToPNG()); rt.Release();
}
cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
return "ok f=" + f.ToString("F2") + " r=" + right.ToString("F2");
