// 과제 1331 캡처 (Play 중, 주행 봇 끝난 뒤 = 차가 시작 자리). b_game · car_close · size_person(1.7m 막대 옆, 옆에서) · run_1~3(달리는 중 연속 프레임, 옆 따라가기)
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a4d/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>(); var ct = car.transform;
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
Camera camD = null; foreach (var c in cams) if (c.name == "Cam_D") camD = c; var dPos = camD.transform.position; var dRot = camD.transform.rotation;
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; "); };
Cap("Cam_B_Game", "b_game.png"); Cap("Cam_D", "car_close.png");
// 사람 막대 1.7m (캡슐 반지름 0.25) — 차 오른쪽 1.3m, 땅 위
var bar = GameObject.CreatePrimitive(PrimitiveType.Capsule); UnityEngine.Object.Destroy(bar.GetComponent<Collider>()); bar.name = "QA_Person1.7";
bar.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f); var bp = ct.position + ct.right * 1.3f; if (Physics.Raycast(bp + Vector3.up * 3, Vector3.down, out var gh, 10)) bp.y = gh.point.y; bar.transform.position = bp + Vector3.up * 0.85f;
var bm = new Material(bar.GetComponent<Renderer>().sharedMaterial); bm.color = new Color(0.75f, 0.2f, 0.15f); bar.GetComponent<Renderer>().sharedMaterial = bm;
camD.transform.position = ct.position + ct.right * 0.65f - ct.forward * 5.5f + Vector3.up * 0.9f; camD.transform.LookAt(ct.position + ct.right * 0.65f + Vector3.up * 0.6f);
Cap("Cam_D", "size_person.png");
camD.transform.position = ct.position - ct.right * 2.8f + ct.forward * 0.2f + Vector3.up * 0.8f; camD.transform.LookAt(ct.position + ct.right * 0.65f + Vector3.up * 0.6f);
Cap("Cam_D", "size_person_side.png");
UnityEngine.Object.Destroy(bar);
// 달리기: 스로틀 0.6 · 조향 0 으로 2.5초 뒤 연속 3프레임(업데이트마다 1장), 옆 3m 따라가기
float t0 = Time.time; int shot = 0; var wRL = ct.Find("A4_KartVisual/Wheel_RL"); var wFL = ct.Find("A4_KartVisual/Wheel_FL");
UnityEditor.SessionState.SetString("a4d.caps", log.ToString() + "running");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
  mi.SetValue(car, new Vector2(0f, 0.6f));
  if (Time.time - t0 < 2.5f) return;
  camD.transform.position = ct.position - ct.right * 3f + ct.forward * 0.3f + Vector3.up * 0.7f; camD.transform.LookAt(ct.position + Vector3.up * 0.3f);
  shot++; Cap("Cam_D", "run_" + shot + ".png"); log.Append("  t" + (Time.time - t0).ToString("F3") + " " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h RL " + wRL.localEulerAngles.ToString("F0") + " FL " + wFL.localEulerAngles.ToString("F0") + "\n");
  if (shot >= 3) { mi.SetValue(car, Vector2.zero); UnityEditor.EditorApplication.update -= cb; camD.transform.SetPositionAndRotation(dPos, dRot);
    foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release(); UnityEditor.SessionState.SetString("a4d.caps", log.ToString()); }
};
UnityEditor.EditorApplication.update += cb;
return "capturing";
