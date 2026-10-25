var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp"));
var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
var c = new Vector3(-40, 0, -120); float h = 170f;
cam.fieldOfView = 60f; cam.transform.SetPositionAndRotation(c + Vector3.up * h, Quaternion.Euler(90, 0, 0));
var rt = new RenderTexture(1000, 1000, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
var tex = new Texture2D(1000, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "op_top.png"), tex.EncodeToPNG()); rt.Release();
cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
return "ok covers +-" + (h * Mathf.Tan(30f * Mathf.Deg2Rad)).ToString("F0") + "m";
