// 인스타 캡처 공통 머리 (D-245 후속, 코드·씬 변경 0). 게임 카메라를 360 픽셀 격자 RT(URP renderScale 보정)에 직접 그리고
// 게임이 화면에 올리는 방식(Point 확대 + lr.overlay 역광 테)으로 3배 확대한다. 4:5 = 360×450 → 1080×1350, 1:1 = 360×360 → 1080×1080.
// 픽셀 밀도는 게임 360 과 같게: 세로 시야각을 h/360 배로 넓힌다(tan 비례).
Application.runInBackground = true;
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
const string OUT = "D:/Github/F1Game/art/_review/2026-11-02-insta/";
float rs = 1f; { var rp0 = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; var pr0 = rp0.GetType().GetProperty("renderScale"); if (pr0 != null) rs = (float)pr0.GetValue(rp0); }
RenderTexture MakeRT(int w, int h) { var r = new RenderTexture(Mathf.RoundToInt(w / rs), Mathf.RoundToInt(h / rs), 24) { filterMode = FilterMode.Point, antiAliasing = Mathf.Max(1, lr.msaa) }; r.Create(); return r; }
// 한 장 그린다: (w,h)=렌더 픽셀, k=확대, 포즈 지정 없으면(eye==null) 지금 카메라 포즈 그대로. 반환 = k배 확대 Texture2D(RGB24).
Texture2D Grab(int w, int h, int k, Vector3? eye, Vector3? look, float vfov0) {
  var t0 = cam.targetTexture; var p0 = cam.transform.position; var q0 = cam.transform.rotation; float f0 = cam.fieldOfView;
  if (eye.HasValue) { cam.transform.position = eye.Value; cam.transform.LookAt(look.Value); }
  float vf = vfov0 > 0 ? vfov0 : f0;
  cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(vf * .5f * Mathf.Deg2Rad) * h / 360f) * Mathf.Rad2Deg;
  var rt = MakeRT(w, h); var dst = new RenderTexture(w * k, h * k, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
  cam.targetTexture = rt; cam.ResetAspect(); cam.Render();
  if (lr.overlay != null) Graphics.Blit(rt, dst, lr.overlay); else Graphics.Blit(rt, dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(w * k, h * k, TextureFormat.RGB24, false);
  tx.ReadPixels(new Rect(0, 0, w * k, h * k), 0, 0); tx.Apply(); RenderTexture.active = prev;
  cam.targetTexture = t0; cam.transform.position = p0; cam.transform.rotation = q0; cam.fieldOfView = f0; cam.ResetAspect();
  rt.Release(); dst.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(dst);
  return tx;
}
void Save(Texture2D tx, string rel) { var f = OUT + rel; System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(f)); System.IO.File.WriteAllBytes(f, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); }
// 스틸 한 자리 = 4:5 + 1:1 두 장. eye==null 이면 지금 카메라 그대로.
string Still(string name, Vector3? eye, Vector3? look, float vfov) {
  Save(Grab(360, 450, 3, eye, look, vfov), "stills/" + name + "_1080x1350.png");
  Save(Grab(360, 360, 3, eye, look, vfov), "stills/" + name + "_1080x1080.png");
  return name + "; ";
}

// 비탈 위에서 내려다본 바다 후보 — 지형 능선(z≈106)에서 남쪽. 자유 카메라(걷는 시점 아님): 위치는 지형 위 1.7 m.
var sb = new System.Text.StringBuilder();
DebugWindow.SetDusk(1f);
var cands = new[] { (new Vector3(216, 63.7f, 106), 175f, -0.10f), (new Vector3(252, 63.9f, 106), 185f, -0.10f), (new Vector3(192, 61.9f, 106), 165f, -0.12f), (new Vector3(240, 63.2f, 106), 200f, -0.10f) };
int i = 0;
foreach (var c in cands) {
  var dir = Quaternion.Euler(0, c.Item2, 0) * Vector3.forward; dir = (dir + Vector3.up * c.Item3).normalized;
  sb.Append(Still("cand_overlook" + i, c.Item1, c.Item1 + dir * 100f, 52f)); i++;
}
return sb.ToString();
