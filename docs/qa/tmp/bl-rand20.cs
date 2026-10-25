// BayLoop v4 랜덤 주행 50점 (Play, 주행 단계). 시드 고정(SessionState "bl.seed", 기본 42).
// 차길(main/village/shortcut) 중심선 임의 점 + 차로 안 임의 옆 위치, 방향 = 길 방향 ±40°(35점) 또는 ±90° 인도 쪽(15점).
// 각 점 5초: 풀 스로틀 cap 12m/s, 조향은 첫 1.5초 직진 후 길 앞 7m 점으로. 기록:
//  stuck = 5초에 3m 못 감, fall = 시작보다 3m 이상 낮아지거나 y<-3, stop = 0.2초에 5m/s 이상 감속(앞 레이 맞은 콜라이더 이름),
//  curb = ±90° 시작점 중 인도 띠(Band_) 위로 올라선 수.
Application.runInBackground = true;
var rnd = new System.Random(UnityEditor.SessionState.GetInt("bl.seed", 42));
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => (r.kind == "main" || r.kind == "village" || r.kind == "shortcut") && r.centerline.Length > 20).ToList();
float total = roads.Sum(r => r.centerline.Length);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
UnityEditor.SessionState.SetString("bl.rand", "");
int lastFrame = -1, run = 0, frames = 0, stuck = 0, fall = 0, stops = 0, curbTry = 0, curbUp = 0; bool onBand = false;
var log = new List<string>(); RoadSurface rs = null; int ci = 0; Vector3 p0 = Vector3.zero, f0 = Vector3.forward; float vPrev = 0, tRun = 0; float[] vHist = new float[12]; bool side = false;
UnityEditor.EditorApplication.CallbackFunction cb = null;
System.Action Spawn = () =>
{
    double pick = rnd.NextDouble() * total; rs = roads[0];
    foreach (var r in roads) { if (pick < r.centerline.Length) { rs = r; break; } pick -= r.centerline.Length; }
    var P = rs.centerline; int n = P.Length; ci = rnd.Next(3, n - 3);
    var f = P[ci + 2] - P[ci - 2]; f.y = 0; f.Normalize(); var rt = Vector3.Cross(Vector3.up, f);
    float half = (rs.widths.Length > ci ? rs.widths[ci] : rs.width) * 0.5f;
    side = run >= 14; float yaw = side ? (rnd.Next(2) == 0 ? -90f : 90f) : (float)(rnd.NextDouble() * 80 - 40);
    float off = side ? 0f : (float)(rnd.NextDouble() * 2 - 1) * Mathf.Max(0, half - 1.2f);
    f0 = Quaternion.Euler(0, yaw, 0) * f; p0 = P[ci] + rt * off + Vector3.up * 0.5f;
    rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p0; rb.rotation = Quaternion.LookRotation(f0);
    car.transform.SetPositionAndRotation(p0, rb.rotation); frames = 0; tRun = Time.time; onBand = false; vPrev = 0; System.Array.Clear(vHist, 0, vHist.Length);
    if (side) curbTry++;
};
Spawn();
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.frameCount == lastFrame) return;   // EditorApplication.update 는 게임 프레임보다 자주 온다 — 프레임당 한 번만
    lastFrame = Time.frameCount; frames++; var p = rb.position; float v = rb.linearVelocity.magnitude;
    var P = rs.centerline; int n = P.Length;
    // 조향: 처음 90프레임은 시작 방향 유지, 그 뒤 가장 가까운 중심선 점 + 7
    float steer = 0f;
    if (Time.time - tRun > 1.5f && !side)
    {
        int bi = ci; float bd = 1e9f; for (int k = Mathf.Max(0, ci - 30); k < Mathf.Min(n, ci + 60); k++) { float d = (new Vector2(P[k].x - p.x, P[k].z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = k; } }
        ci = bi; var to = P[Mathf.Min(n - 1, bi + 7)] - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
        steer = Mathf.Clamp(Vector3.SignedAngle(fw, to, Vector3.up) / 25f, -1, 1);
    }
    float cap = side ? 6f : 12f;
    mi.SetValue(car, new Vector2(steer, Mathf.Clamp((cap - v) / 3f, -1f, 1f)));
    vHist[frames % 12] = v; float vOld = vHist[(frames + 1) % 12];
    if (Time.time - tRun > 0.5f && vOld - v > 5f && !side)   // 옆으로 출발한 점은 결국 건물에 닿으니 세지 않는다
    {
        stops++; string what = Physics.Raycast(p + Vector3.up * 0.4f, car.transform.forward, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name : "-";
        if (log.Count < 40) log.Add("stop#" + run + " " + rs.roadId + "(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + ") " + what);
        System.Array.Clear(vHist, 0, vHist.Length);
    }
    if (side && !onBand && Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var hb, 3f, ~0, QueryTriggerInteraction.Ignore) && hb.collider.name.StartsWith("Band_")) { onBand = true; curbUp++; }
    bool fell = p.y < p0.y - 3f || p.y < -3f;
    if (Time.time - tRun >= 5f || fell)
    {
        float moved = new Vector2(p.x - p0.x, p.z - p0.z).magnitude;
        if (fell) { fall++; log.Add("fall#" + run + " " + rs.roadId + "(" + p0.x.ToString("F0") + "," + p0.z.ToString("F0") + ")->y" + p.y.ToString("F1")); }
        else if (moved < 3f && !side) { stuck++; log.Add("stuck#" + run + " " + rs.roadId + "(" + p0.x.ToString("F0") + "," + p0.z.ToString("F0") + ") moved " + moved.ToString("F1")); }
        else if (side && moved < 1.5f) log.Add("sideStuck#" + run + " " + rs.roadId + "(" + p0.x.ToString("F0") + "," + p0.z.ToString("F0") + ")");
        run++;
        if (run >= 20)
        {
            mi.SetValue(car, Vector2.zero); UnityEditor.EditorApplication.update -= cb;
            UnityEditor.SessionState.SetString("bl.rand", "DONE runs=20stuck=" + stuck + " fall=" + fall + " hardStops=" + stops + " curbUp=" + curbUp + "/" + curbTry + " | " + string.Join(" ; ", log));
            return;
        }
        Spawn();
    }
};
UnityEditor.EditorApplication.update += cb;
return "rand drive roads=" + roads.Count + " total=" + total.ToString("F0") + " stage=" + (Services.Run as RunController).CurrentStage;
