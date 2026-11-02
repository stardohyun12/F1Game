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

// 시선이 트인 비탈 자리 찾기: 지형(TownV6_Terrain)·도로 위 1.7 m 에서 남쪽(-z)±35° 로 150 m 가 안 막히고, 바다 쪽(앞 150 m 의 아래 지면 y<2)이 보이는 점. 높이 순.
var res = new System.Collections.Generic.List<(float y, Vector3 p, float yaw, float clear)>();
for (float x = 60; x <= 300; x += 12) for (float z = -230; z <= 110; z += 12) {
  RaycastHit h; if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out h, 400f)) continue;
  string cn = h.collider.name; if (!(cn.Contains("Terrain") || cn.Contains("Road") || cn.Contains("Walk") || cn.Contains("road"))) continue;
  if (h.point.y < 14f) continue;
  var eye = h.point + Vector3.up * 1.7f;
  float best = 0, byaw = 0;
  for (float yaw = 150; yaw <= 210; yaw += 10) {     // 0 = +z, 180 = -z(남쪽)
    var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward; dir = (dir + Vector3.down * 0.06f).normalized;
    RaycastHit hh; float dist = Physics.Raycast(eye, dir, out hh, 300f) ? hh.distance : 300f;
    if (dist > best) { best = dist; byaw = yaw; }
  }
  if (best >= 250f) res.Add((eye.y, eye, byaw, best));
}
res.Sort((a, b) => b.y.CompareTo(a.y));
var sb = new System.Text.StringBuilder("candidates=" + res.Count + "\n");
for (int i = 0; i < Mathf.Min(14, res.Count); i++) sb.Append(res[i].p.ToString("F1") + " yaw=" + res[i].yaw + " clear=" + res[i].clear.ToString("F0") + "\n");
return sb.ToString();
