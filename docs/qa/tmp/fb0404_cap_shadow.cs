Shader.SetGlobalColor("_A7Shade", Color.black); Shader.SetGlobalVector("_A7T", Vector4.zero); Shader.SetGlobalFloat("_A7Mid", 0f);
// 사용: 아래 SHOTS 를 고쳐 exec. 각 줄 = 파일, 카메라 위치, 바라볼 점
var shots = new (string f, Vector3 p, Vector3 t)[] {
  ("docs/qa/evidence/fb0344/shadow_before2.png", new Vector3(209.7f,33f,-40.8f), new Vector3(221.7f,29.5f,-54.8f)),
};
var main = Camera.main; var go = new GameObject("_cap"); var cam = go.AddComponent<Camera>();
if (main) { cam.CopyFrom(main); }
cam.fieldOfView = 60; cam.farClipPlane = 1800; cam.enabled = false;
var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
var outp = new System.Text.StringBuilder();
foreach (var s in shots) {
  if (s.f == "__SHOTS__") continue;
  go.transform.position = s.p; go.transform.LookAt(s.t);
  cam.Render(); RenderTexture.active = rt;
  var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
  var path = "D:/Github/F1Game/" + s.f; System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
  System.IO.File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); outp.Append(s.f + "\n");
}
RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
return outp.ToString();
