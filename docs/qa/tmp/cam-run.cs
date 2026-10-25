// Play 중: 본선 s8 에서 정지 출발 → 같은 조향 규칙으로 16초 주행. 5초 뒤부터 9초 동안 CamJitterProbe 기록, 9초 지점 연속 6프레임 캡처.
// SessionState cam.tag = 파일 접두, cam.mode = "" | noshake(프리셋 픽셀 흔들림 0, 복제본) | still(충돌 흔들림도 0)
// 측정 조건 고정: 60fps (에디터는 그냥 두면 200fps)
Application.runInBackground = true; Application.targetFrameRate = 60;
string tag = UnityEditor.SessionState.GetString("cam.tag", "before"), mode = UnityEditor.SessionState.GetString("cam.mode", "");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-cam"));
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main" && r.centerline != null && r.centerline.Length > 0);
var P = rs.centerline; int n = P.Length;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
System.Func<int, Vector3> At = i => P[((i % n) + n) % n];
System.Func<int, Vector3> Fw = i => { var f = At(i + 2) - At(i - 2); f.y = 0; return f.normalized; };
if (mode == "A" || mode == "B" || mode == "C") fc.Preset = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(mode == "A" ? CameraFeelSwitcher.A : mode == "B" ? CameraFeelSwitcher.B : CameraFeelSwitcher.C);
bool kbd = UnityEditor.SessionState.GetInt("cam.kbd", 0) == 1;
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
bool legacy = UnityEditor.SessionState.GetInt("cam.legacy", 0) == 1;
typeof(FollowCamera).GetField("yawSmoothTime", BF).SetValue(fc, legacy ? 0f : 0.12f);
typeof(FollowCamera).GetField("heightSmoothTime", BF).SetValue(fc, legacy ? 0f : 0.15f);
if (mode == "noshake" || mode == "still")
{
    if (fc.Preset != null) { var cl = UnityEngine.Object.Instantiate(fc.Preset); cl.shakeMaxPx = 0; fc.Preset = cl; }
    if (mode == "still") typeof(FollowCamera).GetField("impactShakeAmplitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(fc, 0f);
}
int cur = UnityEditor.SessionState.GetInt("cam.start", 8); float capV = UnityEditor.SessionState.GetFloat("cam.cap", 20f);
rb.position = At(cur) + Vector3.up * 0.5f; rb.rotation = Quaternion.LookRotation(Fw(cur)); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
float t0 = Time.time;
CamJitterProbe.Begin(5f, 9f, 4f, 6, outDir, tag);
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    var p = rb.position;
    int bi = cur; float bd = 1e9f;
    for (int k = -6; k <= 25; k++) { var c = At(cur + k); float d = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = cur + k; } }
    if (bi > cur) cur = bi;
    var tgt = At(cur + 7); var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    float turn = Vector3.Angle(Fw(cur), Fw(cur + 25));
    float vT = turn > 70f ? 6f : turn > 35f ? 9f   : capV, v = rb.linearVelocity.magnitude;
    float st = Mathf.Clamp(ang / 25f, -1, 1); if (kbd) st = Mathf.Abs(st) > 0.25f ? Mathf.Sign(st) : 0f;
    mi.SetValue(car, new Vector2(st, Mathf.Clamp((vT - v) / 3f, -1f, 1f)));
    if (Time.time - t0 > 16f) { mi.SetValue(car, Vector2.zero); UnityEditor.EditorApplication.update -= cb; }
};
UnityEditor.EditorApplication.update += cb;
return "cam-run " + tag + " mode=" + mode;
