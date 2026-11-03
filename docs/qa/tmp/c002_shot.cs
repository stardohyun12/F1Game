// 수용 기준 사진 1장(D-277 기준별 증거). bundle.sh 가 docs/qa/tmp/c002_shot.png 를 바꿔 exec 로 보낸다 — critN.cs 실행 2초 뒤.
// 화면 = clip.cs.tmpl 과 같은 방식(LowResRenderTest.Present → RenderTexture → PNG = 도현이 보는 Game 화면). Play 중에만.
// 결과: docs/qa/tmp/c002_shot.png 에 PNG, SessionState ev.shot = "ok <경로>" / "FAIL …".
if (!Application.isPlaying) { UnityEditor.SessionState.SetString("ev.shot", "FAIL: Play 아님"); return "FAIL: Play 아님"; }
string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/c002_shot.png"));
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
int W = 960, H = 540;
var cam = Camera.main;
var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
var rt = new RenderTexture(W, H, 24); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
try {
    if (lr != null) lr.Present(rt);
    else if (cam != null) { var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
    else { UnityEditor.SessionState.SetString("ev.shot", "FAIL: 카메라 없음"); return "FAIL: 카메라 없음"; }
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
} catch (System.Exception e) { UnityEditor.SessionState.SetString("ev.shot", "FAIL: " + e.Message); return "FAIL: " + e.Message; }
finally { rt.Release(); UnityEngine.Object.DestroyImmediate(tex); }
UnityEditor.SessionState.SetString("ev.shot", "ok " + outPath);
return "shot " + outPath.Replace("\\", "/") + " src=" + (lr != null ? "LowRes" : "Camera") + " frame=" + Time.frameCount;
