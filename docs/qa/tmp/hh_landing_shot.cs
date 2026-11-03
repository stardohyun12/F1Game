// 20261030-2334: 계단참 통로(RAIL_02 끝 ~ 동쪽 벽) 전경 — 임시 카메라(복도 서쪽 위)에서 1280x720. Play 중.
var h = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "house_home" && t.parent != null && t.parent.name == "D198_Buildings");
var go = new GameObject("_tmpLandingCam"); var cam = go.AddComponent<Camera>();
cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
go.transform.position = h.TransformPoint(new Vector3(-1.0f, 3.2f + 2.1f, -0.9f));
go.transform.LookAt(h.TransformPoint(new Vector3(-3.6f, 3.2f + 0.4f, -2.1f)));
var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); cam.targetTexture = rt; cam.Render();
var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/feature1b/02b_landing_view.png", tx.EncodeToPNG());
cam.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(tx); UnityEngine.Object.Destroy(go);
return "ok cam=" + go.transform.position.ToString("F2");
