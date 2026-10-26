// 과제 0429 캡처 — 4판 × 2컷, 1920×1080 PNG. Play 중이면 LowResRenderTest 의 RT(720 렌더)를 Present(Nearest)로 확대, 에디트면 같은 크기 RT 를 직접 만든다.
var sw = UnityEngine.Object.FindFirstObjectByType<SceneAVariant>(FindObjectsInactive.Include);
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a/")); System.IO.Directory.CreateDirectory(outDir);
var tag = System.IO.File.Exists(outDir + "_tag.txt") ? System.IO.File.ReadAllText(outDir + "_tag.txt").Trim() : "";
var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
float scale = 1f; var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; var pr = rp?.GetType().GetProperty("renderScale"); if (pr != null) scale = (float)pr.GetValue(rp);
int keepV = sw.variant, keepC = sw.cut;
for (int v = 0; v < 4; v++) for (int c = 0; c < 2; c++)
{
  sw.variant = v; sw.cut = c; sw.Apply(); var cam = sw.cams[c]; var lr = cam.GetComponent<LowResRenderTest>();
  if (Application.isPlaying && lr != null && lr.isActiveAndEnabled)
  { cam.Render(); lr.Present(dst); log.Append($"V{v}c{c} play RenderSize={lr.RenderSize} RT={cam.targetTexture.width}x{cam.targetTexture.height} {cam.targetTexture.filterMode}\n"); }
  else
  {
    var rt = new RenderTexture(Mathf.RoundToInt(1280 / scale), Mathf.RoundToInt(720 / scale), 24) { filterMode = FilterMode.Point };
    var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep; Graphics.Blit(rt, dst); rt.Release();
    if (v == 0 && c == 0) log.Append($"edit RT={rt.width}x{rt.height} scale={scale}\n");
  }
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + $"V{v}_{(c == 0 ? "game" : "intro")}{tag}.png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
}
sw.variant = keepV; sw.cut = keepC; sw.Apply(); dst.Release();
return log.ToString() + outDir;
