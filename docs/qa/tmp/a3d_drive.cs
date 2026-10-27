// 과제 1145 Play 확인 — ① 시작 자리에서 앞으로(풀스로틀, 조향 0 → 길 따라 살짝) 10초: 바닥 꺼짐(minY) 확인 + 10초에 GameCam 캡처
//  ② 서쪽 보이지 않는 벽(x=-25) 앞 8m 에 세우고 벽 쪽으로 5초: x 최소값이 벽 안쪽인지. 결과 SessionState "a3d.drive"
Application.runInBackground = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var cam = GameObject.Find("GameCam").GetComponent<Camera>(); var lr = cam.GetComponent<LowResRenderTest>();
string outP = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a3/play_check.png"));
UnityEditor.SessionState.SetString("a3d.drive", "running");
Vector3 p0 = rb.position; float t0 = Time.time, maxV = 0, minY = 999, minX = 999; int phase = 1; var sb = new System.Text.StringBuilder(); float lastLog = 0;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
  var p = rb.position; float t = Time.time - t0;
  if (phase == 1) {
    // 길 중심 쪽으로 약하게: 앞 방향 유지(조향은 차 앞 12m 지점의 길 위 여부로 안 함 — 단순 직진)
    // 길 가운데 따라가기: 차 10m 앞 줄에서 x 를 훑어 Road 가 맞는 구간의 가운데를 겨눈다
    var ah = p + car.transform.forward * 10f; float xs = 0; int nx = 0;
    for (float x = ah.x - 15; x <= ah.x + 15; x += 0.5f) foreach (var hh in Physics.RaycastAll(new Vector3(x, 30, ah.z), Vector3.down, 80)) if (hh.collider.name == "Road") { xs += x; nx++; }
    var to = (nx > 0 ? new Vector3(xs / nx, p.y, ah.z) : ah) - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    mi.SetValue(car, new Vector2(Mathf.Clamp(Vector3.SignedAngle(fw, to, Vector3.up) / 25f, -1, 1), 1f)); maxV = Mathf.Max(maxV, rb.linearVelocity.magnitude); minY = Mathf.Min(minY, p.y);
    if (t - lastLog >= 2f) { lastLog = t; sb.Append("t" + t.ToString("F0") + " " + p.ToString("F1") + " " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h; "); }
    if (t >= 10f) {
      var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
      cam.Render(); lr.Present(dst); var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
      System.IO.File.WriteAllBytes(outP, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); dst.Release();
      sb.Append("| P1 10s 이동 " + Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(p0.x, 0, p0.z)).ToString("F1") + "m maxV " + (maxV * 3.6f).ToString("F0") + "km/h minY " + minY.ToString("F2") + " end " + p.ToString("F1") + " lowres " + lr.RenderSize + " | ");
      // ② 벽 시험
      Vector3 w = new Vector3(-17f, 30f, -30f); RaycastHit h; float gy = 0; if (Physics.Raycast(w, Vector3.down, out h, 80)) gy = h.point.y; sb.Append("P2 바닥 " + (h.collider ? h.collider.name : "none") + " y" + gy.ToString("F1") + "; ");
      rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = new Vector3(-17f, gy + 0.8f, -30f); rb.rotation = Quaternion.Euler(0, 270, 0); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
      phase = 2; t0 = Time.time; minY = 999; }
  } else {
    mi.SetValue(car, new Vector2(0, 1f)); minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y);
    if (t >= 5f) { mi.SetValue(car, Vector2.zero); UnityEditor.EditorApplication.update -= cb;
      sb.Append("P2 5s minX " + minX.ToString("F2") + " (벽 안쪽면 -25.0) minY " + minY.ToString("F2") + " end " + p.ToString("F1") + " v " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h");
      UnityEditor.SessionState.SetString("a3d.drive", sb.ToString()); }
  }
};
UnityEditor.EditorApplication.update += cb;
return "driving";
