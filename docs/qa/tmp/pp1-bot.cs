// 2329 경로 봇: SessionState "pp.queue"="D1_main,D1_short,..." 를 차례로 주행. 경로 = Routes/Route_<id> 중심선(1m).
// 조향: 7m 앞 목표점. 속도 프로파일(가설): 차 밑 길 종류 상한 hw 23.5 / main 19 / dirt 14 / alley 11 / cobble 9 m/s, 앞 25m 꺾임 >70° 7, >35° 10 m/s.
// 화물: 경로 배달 화물로 SetCargoType + Repair. 기록 = 내구도 감소, 차 가속도 피크 G(수평+수직), 충격 합(Σ max(0,G-0.5)·dt), 시뮬 흡수손상.
// 결과 SessionState "pp.result" 에 줄마다 누적, 끝나면 "pp.state"=ALLDONE.
Application.runInBackground = true;
var queue = UnityEditor.SessionState.GetString("pp.queue", "").Split(',').Where(s => s.Length > 0).ToList();
var cargoOf = new Dictionary<string, CargoType> { { "D1", CargoType.Glass }, { "D2", CargoType.Box }, { "D3", CargoType.Eggs }, { "D4", CargoType.Box } };
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var box = UnityEngine.Object.FindFirstObjectByType<CargoBox>();
var sim = UnityEngine.Object.FindFirstObjectByType<CargoShakeSimulator>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var caps = new Dictionary<string, float> { { "hw", 23.5f }, { "main", 19f }, { "dirt", 14f }, { "alley", 11f }, { "cobble", 9f } };
UnityEditor.SessionState.SetString("pp.result", ""); UnityEditor.SessionState.SetString("pp.state", "RUN");
var myId = System.Guid.NewGuid().ToString(); UnityEditor.SessionState.SetString("pp.botId", myId);
int qi = -1; Vector3[] P = null; int n = 0, cur = 0, prog = 0, respawn = 0, settle = 0; string id = "";
float t0 = 0, lastProg = 0, maxV = 0, latMax = 0, gPeak = 0, gSum = 0, hp0 = 1; Vector3 lastP = default, lastVel = default; float lastT = 0;
var kindTime = new Dictionary<string, float>(); var kindLen = new Dictionary<string, float>();
System.Func<int, Vector3> At = i => P[Mathf.Clamp(i, 0, n - 1)];
System.Func<int, Vector3> Fw = i => { var f = At(i + 2) - At(i - 2); f.y = 0; return f.normalized; };
System.Action Next = null;
Next = () =>
{
    qi++;
    if (qi >= queue.Count) { UnityEditor.SessionState.SetString("pp.state", "ALLDONE"); return; }
    id = queue[qi];
    P = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == id && r.kind == "route").centerline; n = P.Length;
    rb.isKinematic = false; rb.position = P[1] + Vector3.up * 0.6f; rb.rotation = Quaternion.LookRotation(Fw(1)); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
    rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
    if (box != null) { box.SetCargoType(cargoOf[id.Substring(0, 2)]); box.Repair(); hp0 = box.IntegrityPercent; }
    if (sim != null) { sim.ResetRunStats(); sim.StartGracePeriod(1.5f); }
    cur = 1; prog = 0; respawn = 0; maxV = latMax = gPeak = gSum = 0; settle = 30; kindTime.Clear(); kindLen.Clear();
    t0 = lastProg = lastT = Time.time; lastP = rb.position; lastVel = Vector3.zero;
};
Next();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying || UnityEditor.SessionState.GetString("pp.botId", "") != myId) { UnityEditor.EditorApplication.update -= cb; return; }
    if (qi >= queue.Count) { UnityEditor.EditorApplication.update -= cb; return; }
    float dt = Time.time - lastT; if (dt <= 0f) return; lastT = Time.time;
    var p = rb.position;
    if (settle > 0) { settle--; t0 = lastProg = Time.time; lastVel = rb.linearVelocity; lastP = p; mi.SetValue(car, Vector2.zero); return; }
    if ((p - lastP).magnitude > 8f) respawn++;
    var acc = (rb.linearVelocity - lastVel) / dt; lastVel = rb.linearVelocity; lastP = p;
    float g = acc.magnitude / 9.81f; if (dt < 0.1f) { gPeak = Mathf.Max(gPeak, g); gSum += Mathf.Max(0f, g - 0.5f) * dt; }
    int bi = cur; float bd = 1e9f;
    for (int k = -6; k <= 25; k++) { var c = At(cur + k); float d = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = Mathf.Clamp(cur + k, 0, n - 1); } }
    if (bi > cur) { prog += bi - cur; cur = bi; lastProg = Time.time; }
    latMax = Mathf.Max(latMax, Mathf.Sqrt(bd));
    string kind = "?";
    if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore))
    { var rs = hit.collider.GetComponent<RoadSurface>(); if (rs != null) kind = rs.kind; else if (hit.collider.name == "Bump" || hit.collider.name == "Hump") kind = "cobble"; }
    if (kind == "?") { var nm = ""; foreach (var h in Physics.RaycastAll(p + Vector3.up * 1.5f, Vector3.down, 4f)) { var rs = h.collider.GetComponent<RoadSurface>(); if (rs != null) { nm = rs.kind; break; } } if (nm != "") kind = nm; }
    kindTime[kind] = (kindTime.ContainsKey(kind) ? kindTime[kind] : 0) + dt;
    var tgt = At(cur + 7); var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    float turn = Vector3.Angle(Fw(cur), Fw(cur + 25));
    float cap = caps.ContainsKey(kind) ? caps[kind] : 12f;
    float vT = Mathf.Min(cap, turn > 70f ? 7f : turn > 35f ? 10f : 99f), v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((vT - v) / 3f, -0.5f, 1f)));   // 브레이크 최대 0.5 — 사람처럼 미리 살살(1차는 -1 이라 유리·계란이 큰길에서도 0%)
    maxV = Mathf.Max(maxV, v);
    bool done = cur >= n - 4, timeout = Time.time - lastProg > 6f || Time.time - t0 > 200f;
    if (done || timeout)
    {
        mi.SetValue(car, Vector2.zero);
        float hp1 = box != null ? box.IntegrityPercent : 0;
        var line = (done ? "DONE " : "STOPPED ") + id + " prog=" + prog + "/" + (n - 5) + "m t=" + (Time.time - t0).ToString("F1") + "s maxV=" + (maxV * 3.6f).ToString("F0") + "kmh lat=" + latMax.ToString("F1")
            + " respawn=" + respawn + " cargo=" + (box != null ? box.Type.ToString() : "-") + " hp " + (hp0 * 100).ToString("F0") + "->" + (hp1 * 100).ToString("F0")
            + " gPeak=" + gPeak.ToString("F2") + " gSum=" + gSum.ToString("F2") + " simAbs=" + (sim != null ? sim.AbsorbedDamageThisRun.ToString("F1") : "-")
            + " kinds{" + string.Join(" ", kindTime.Select(kv => kv.Key + ":" + kv.Value.ToString("F1"))) + "}"
            + (done ? "" : " stuckAt=(" + p.x.ToString("F0") + "," + p.y.ToString("F1") + "," + p.z.ToString("F0") + ")");
        UnityEditor.SessionState.SetString("pp.result", UnityEditor.SessionState.GetString("pp.result", "") + line + "\n");
        Next();
    }
};
UnityEditor.EditorApplication.update += cb;
return "bot queue=" + queue.Count + " first=" + id + " stage=" + (Services.Run as RunController).CurrentStage;
