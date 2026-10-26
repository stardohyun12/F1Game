// 2111 사본: 새 주행이 시작되면 이전 주행 콜백은 스스로 빠진다(l5c.driveId)
// BayLoop 합성 주행 (Play, 주행 단계): SessionState "bl.road" 길의 RoadSurface 중심선을 따라 끝(본선은 한 바퀴)까지.
// 7m 앞 목표점 조향, 25m 앞 방향차 >70° 6m/s, >35° 9m/s, 그 외 cap. 결과 SessionState "bl.drive".
Application.runInBackground = true;
var id = UnityEditor.SessionState.GetString("bl.road", "main");
float cap = UnityEditor.SessionState.GetInt("bl.cap", 20);
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == id && r.centerline != null && r.centerline.Length > 0);
var P = rs.centerline; int n = P.Length; bool closed = rs.closed;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
System.Func<int, Vector3> At = i => closed ? P[((i % n) + n) % n] : P[Mathf.Clamp(i, 0, n - 1)];
System.Func<int, Vector3> Fw = i => { var f = At(i + 2) - At(i - 2); f.y = 0; return f.normalized; };
int startI = 0;
if (closed) { float bd = 1e9f; for (int i = 0; i < n; i++) { float d = (P[i] - rb.position).sqrMagnitude; if (d < bd) { bd = d; startI = i; } } }
else { rb.position = P[2] + Vector3.up * 0.6f; rb.rotation = Quaternion.LookRotation(Fw(2)); car.transform.SetPositionAndRotation(rb.position, rb.rotation); startI = 2; }
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
UnityEditor.SessionState.SetString("bl.drive", "");
float t0 = Time.time, maxV = 0, latMax = 0, belowMax = 0, lastProg = Time.time; int prog = 0, cur = startI, respawn = 0, offRoadFrames = 0, frames = 0;
Vector3 lastP = rb.position; string latAt = "", belowAt = "";
int goal = closed ? n - 3 : n - 1 - startI - 3;
var myId = System.Guid.NewGuid().ToString(); UnityEditor.SessionState.SetString("l5c.driveId", myId);
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying || UnityEditor.SessionState.GetString("l5c.driveId", "") != myId) { UnityEditor.EditorApplication.update -= cb; return; }
    var p = rb.position; frames++;
    if ((p - lastP).magnitude > 8f) respawn++;
    lastP = p;
    int bi = cur; float bd = 1e9f;
    for (int k = -6; k <= 25; k++) { var c = At(cur + k); float d = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = cur + k; } }
    if (bi > cur) { prog += bi - cur; cur = bi; lastProg = Time.time; }
    float lat = Mathf.Sqrt(bd); var cp = At(cur);
    if (lat > latMax) { latMax = lat; latAt = "(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + ")"; }
    if (lat > rs.width * 0.5f + 0.3f) offRoadFrames++;
    float below = cp.y - p.y; if (below > belowMax) { belowMax = below; belowAt = "(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + ")"; }
    var tgt = At(cur + 7); var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    float turn = Vector3.Angle(Fw(cur), Fw(cur + 25));
    float vT = turn > 70f ? 6f : turn > 35f ? 9f : cap, v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((vT - v) / 3f, -1f, 1f)));
    maxV = Mathf.Max(maxV, v);
    bool done = prog >= goal, timeout = Time.time - lastProg > 6f || Time.time - t0 > 240f;
    if (done || timeout)
    {
        mi.SetValue(car, Vector2.zero);
        UnityEditor.EditorApplication.update -= cb;
        UnityEditor.SessionState.SetString("bl.drive", (done ? "DONE" : "STOPPED") + " " + id + " prog=" + prog + "/" + goal + "m time=" + (Time.time - t0).ToString("F1") + "s maxV=" + (maxV * 3.6f).ToString("F0") + "km/h latMax=" + latMax.ToString("F2") + "m" + latAt + " offRoad=" + (100f * offRoadFrames / Mathf.Max(1, frames)).ToString("F1") + "% belowRoadMax=" + belowMax.ToString("F2") + "m" + belowAt + " respawn=" + respawn + " end=(" + p.x.ToString("F0") + "," + p.y.ToString("F1") + "," + p.z.ToString("F0") + ")");
    }
};
UnityEditor.EditorApplication.update += cb;
return "driving " + id + " n=" + n + " start=" + startI + " stage=" + (Services.Run as RunController).CurrentStage;
