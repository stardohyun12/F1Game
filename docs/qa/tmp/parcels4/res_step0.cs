// 0236: 해상도 비교 단계식 — exec 마다 (1) 직전 프레임을 저장(SAVE 가 있으면) (2) 다음 높이로 바꾸고 카메라를 8b98 에 고정. 프레임이 실제로 돌아야 그려진다(URP 에서 cam.Render 즉시 호출은 안 그려짐)
// 사용: 이 파일 앞의 두 줄 SAVE/NEXT 만 바꿔 exec
string SAVE = ""; int NEXT = 720;
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/parcels4/"));
var lr = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>(); var cam = lr.GetComponent<Camera>(); var log = new System.Text.StringBuilder();
if (SAVE != "") {
  var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + SAVE, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); dst.Release();
  log.Append(SAVE + " " + lr.RenderSize + " pos=" + cam.transform.position + "; ");
}
foreach (var b in cam.GetComponents<MonoBehaviour>()) if (b != lr && b.enabled && !(b is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)) { b.enabled = false; log.Append("off " + b.GetType().Name + "; "); }
if (cam.transform.parent) { log.Append("unparent " + cam.transform.parent.name + "; "); cam.transform.SetParent(null, true); }
lr.targetHeight = NEXT; cam.transform.position = new Vector3(183.249f, 37.967f, -79.95f); cam.transform.LookAt(new Vector3(277.145f, -125.962f, 76.22f)); cam.fieldOfView = 60;
log.Append("next h=" + NEXT);
return log.ToString();
