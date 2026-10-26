// 0506-1: 평판(y 800) 위에서 Box / Wheels 성능 비교 — 0→60 km/h, 12 초 최고속, 40·70 km/h 풀조향 회전 반경, 60 km/h 드리프트. 결과 SessionState "perf"
Application.runInBackground = true; Time.timeScale = 1f;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var mi = typeof(ArcadeCarController).GetField("moveInput", BF); var hb = typeof(ArcadeCarController).GetField("qaForceHandbrake", BF);
typeof(ArcadeCarController).GetField("wheelRollDrag", BF).SetValue(car, 3.2f);
var plat = GameObject.Find("QA_PerfPlate"); if (plat == null) { plat = GameObject.CreatePrimitive(PrimitiveType.Cube); plat.name = "QA_PerfPlate"; plat.transform.position = new Vector3(0, 800, 0); plat.transform.localScale = new Vector3(1200, 1, 1200); }
UnityEditor.SessionState.SetString("perf", "");
var modes = new[] { ArcadeCarController.DriveMode.Box, ArcadeCarController.DriveMode.Wheels };
int mIdx = 0, ph = 0, lastFrame = -1; float t0 = 0, t60 = -1, top = 0, yawSum = 0, vSum = 0, slipSum = 0; int n = 0; float yawPrev = 0, vyPrev = 0, azMax = 0;
var res = new System.Text.StringBuilder();
System.Action Spawn = () => { car.SetDriveMode(modes[mIdx]); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; var p = new Vector3(-500, 801.2f, -500); rb.position = p; rb.rotation = Quaternion.LookRotation(new Vector3(1, 0, 1)); car.transform.SetPositionAndRotation(p, rb.rotation); Physics.SyncTransforms(); t0 = Time.time; };
float[] holdKmh = { 0, 0, 40, 70, 60 };
mIdx = 0; ph = 0; Spawn();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
  if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
  float t = Time.time - t0; float kmh = Vector3.Dot(rb.linearVelocity, car.transform.forward) * 3.6f;
  if (ph == 0) // 0→60, 12 s 최고속
  {
    mi.SetValue(car, new Vector2(0, 1)); hb.SetValue(car, false);
    if (t > 0.5f && t60 < 0 && kmh >= 60) t60 = t - 0.5f; // 0.5 s 안착 시간 뺌
    if (t < 0.5f) { mi.SetValue(car, Vector2.zero); }
    if (t > 8f) { float vy = rb.linearVelocity.y; azMax = Mathf.Max(azMax, Mathf.Abs(vy - vyPrev) / Mathf.Max(1e-3f, Time.deltaTime)); vyPrev = vy; }
    top = Mathf.Max(top, kmh);
    if (t >= 12.5f) { res.Append($"{modes[mIdx]}: 0→60 {t60:F2}s · 12s 최고 {top:F1}km/h · 평지 수직가속 최대 {azMax:F1} · y {rb.position.y - 800.5f:F3}\n"); ph = 1; t0 = Time.time; }
    return;
  }
  if (ph >= 1 && ph <= 3) // 1 = 40 km/h 반경, 2 = 70 km/h 반경, 3 = 60 km/h 드리프트
  {
    float tgt = new[] { 40f, 70f, 60f }[ph - 1];
    float thr = Mathf.Clamp((tgt - kmh) / 8f, -1f, 1f);
    bool steer = t > 6f; bool drift = ph == 3 && steer;
    mi.SetValue(car, new Vector2(steer ? 1f : 0f, thr)); hb.SetValue(car, drift);
    float yawNow = car.transform.eulerAngles.y; float dyaw = Mathf.DeltaAngle(yawPrev, yawNow) * Mathf.Deg2Rad / Mathf.Max(1e-4f, Time.deltaTime); yawPrev = yawNow;
    if (t > 8f) { yawSum += dyaw; var hv = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up); vSum += hv.magnitude; slipSum += Vector3.Angle(hv, Vector3.ProjectOnPlane(car.transform.forward, Vector3.up)); n++; }
    if (t >= 10f)
    {
      float w = yawSum / Mathf.Max(1, n), v = vSum / Mathf.Max(1, n);
      res.Append($"  {(ph == 3 ? "드리프트" : "반경")}@{tgt}: 실제 {v * 3.6f:F1}km/h 회전 {w * Mathf.Rad2Deg:F1}°/s 반경 {v / Mathf.Max(1e-3f, Mathf.Abs(w)):F1}m slip {slipSum / Mathf.Max(1, n):F1}°\n");
      yawSum = vSum = slipSum = 0; n = 0; ph++; hb.SetValue(car, false);
      // 다음 단계 전에 다시 세움
      rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; var p = new Vector3(-500, 801.2f, -500); rb.position = p; rb.rotation = Quaternion.LookRotation(new Vector3(1, 0, 1)); Physics.SyncTransforms(); t0 = Time.time;
    }
    return;
  }
  // 다음 모드
  mIdx++; if (mIdx >= modes.Length) { mi.SetValue(car, Vector2.zero); hb.SetValue(car, false); car.SetDriveMode(ArcadeCarController.DriveMode.Box); UnityEditor.EditorApplication.update -= cb; UnityEditor.SessionState.SetString("perf", "DONE\n" + res); return; }
  ph = 0; t60 = -1; top = 0; azMax = 0; Spawn();
};
UnityEditor.EditorApplication.update += cb;
return "perf started";
