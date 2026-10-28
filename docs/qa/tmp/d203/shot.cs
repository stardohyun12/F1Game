// D-203 캡처 3장 (임시 카메라, 씬 변경 없음)
var main = Camera.main;
var go = new GameObject("D203ShotCam"); go.hideFlags = HideFlags.HideAndDontSave;
var cam = go.AddComponent<Camera>(); if (main) cam.CopyFrom(main); go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
cam.fieldOfView = 60f; cam.farClipPlane = 3000f;
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.aspect = 1280f / 720f;
var fwd = Quaternion.Euler(0, 190, 0) * Vector3.forward;
var bal = new Vector3(185, 24.9f + 4.5f, -61) + fwd * 4.8f;
var shots = new (string n, Vector3 p, Vector3 look)[] {
  ("overview", new Vector3(200, 95, -150), new Vector3(212, 25, -58)),
  ("house_balcony_sea", bal, bal + fwd * 100f + Vector3.down * 9f),
  ("cluster_road", new Vector3(196, 23.5f, -90), new Vector3(212, 27, -58)) };
System.IO.Directory.CreateDirectory("D:/Github/F1Game/docs/qa/evidence/d203");
foreach (var s in shots) {
  cam.transform.position = s.p; cam.transform.rotation = Quaternion.LookRotation(s.look - s.p);
  cam.Render(); RenderTexture.active = rt;
  var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/d203/" + s.n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
}
cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
return "ok dirty=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty;
