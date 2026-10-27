// 과제 1046 캡처(A2e) — 컷 3개(B 게임·D·소개), 1920×1080 PNG. Play 중 LowResRenderTest RT(720) 를 Present(Nearest)로 확대. 파일 이름 뒤에 _tag.txt 붙임
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a2e/")); System.IO.Directory.CreateDirectory(outDir);
var tag = System.IO.File.Exists(outDir + "_tag.txt") ? System.IO.File.ReadAllText(outDir + "_tag.txt").Trim() : "";
var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
foreach (var nm in new[] { "Cam_B_Game", "Cam_D", "Cam_Intro" })
{
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c;
  foreach (var c in cams) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>();
  if (Application.isPlaying) { lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); log.Append($"{nm} play RenderSize={lr.RenderSize} RT={(cam.targetTexture ? cam.targetTexture.width + "x" + cam.targetTexture.height + " " + cam.targetTexture.filterMode : "none")}\n"); }
  else { var rt = new RenderTexture(1280, 720, 24) { filterMode = FilterMode.Point }; cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; Graphics.Blit(rt, dst); rt.Release(); log.Append(nm + " edit 1280x720 point\n"); }
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + nm.Replace("Cam_", "").ToLower() + tag + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
}
foreach (var c in cams) c.gameObject.SetActive(c.name == "Cam_B_Game");
dst.Release();
return log.ToString() + outDir;
