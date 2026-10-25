// 프로토 코스 합성 주행 (Play, 주행 단계에서): 중심선 따라 광장까지. 커브 앞(15m 앞 방향차 >35°)은 8m/s, 나머지 18m/s.
// 배달지 4곳 각각 최소 거리, 런 단계 변화, 정산 도달 시각을 SessionState "proto.drive" 에.
Application.runInBackground = true;
var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildPostcardSlice")).First(x => x != null);
var route = UnityEditor.SessionState.GetInt("proto.route", 0);
var total = (float)t.GetMethod("RouteTotal").Invoke(null, new object[] { route }) + 10f;
var frame = t.GetMethod("RouteFrame");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var rc = Services.Run as RunController;
var pts = UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var minD = new float[pts.Length]; for (int i = 0; i < minD.Length; i++) minD[i] = 999;
UnityEditor.SessionState.SetString("proto.drive", "");
float t0 = Time.time, maxV = 0, minY = 999, lastProg = Time.time, bestS = 0, best = 0; string stages = "" + rc.CurrentStage, settle = "";
System.Func<float, object[]> F = q => { var a2 = new object[] { route, q, null, null, null }; frame.Invoke(null, a2); return new object[] { q, a2[2], a2[3], a2[4] }; };
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    var p = rb.position;
    float s = 0; best = 1e9f;
    for (float q = Mathf.Max(-5, bestS - 10); q <= Mathf.Min(total, bestS + 20); q += .5f)
    { var a = F(q); var c = (Vector3)a[1]; if (q > total - 10f) c += (Vector3)a[2] * 0; float dd = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (dd < best) { best = dd; s = q; } }
    if (s > bestS + .5f) { bestS = s; lastProg = Time.time; }
    var ta = F(Mathf.Min(total, s + 7f));
    var tgt = (Vector3)ta[1] + (Vector3)ta[3] * (UnityEditor.SessionState.GetInt("p.off10", 12) / 10f);
    var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    var f0 = (Vector3)F(s)[2]; var f1 = (Vector3)F(Mathf.Min(total, s + 25f))[2];
    float vT = Vector3.Angle(f0, f1) > 35f ? 8f : (float)UnityEditor.SessionState.GetInt("p.vcap", 18), v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((vT - v) / 3f, -1f, 1f)));
    maxV = Mathf.Max(maxV, v); minY = Mathf.Min(minY, p.y);
    for (int i = 0; i < pts.Length; i++) minD[i] = Mathf.Min(minD[i], Vector3.Distance(p, pts[i].transform.position));
    var st = "" + rc.CurrentStage; if (!stages.EndsWith(st)) { stages += ">" + st + "@" + (Time.time - t0).ToString("F1"); }
    bool settled = rc.CurrentStage == RunStage.Result; bool done = settled || bestS >= total - 12f, timeout = Time.time - lastProg > 5f || Time.time - t0 > 120f;
    if (done || timeout)
    {
        mi.SetValue(car, Vector2.zero);
        UnityEditor.EditorApplication.update -= cb;
        var ds = ""; for (int i = 0; i < pts.Length; i++) ds += pts[i].Label + "=" + minD[i].ToString("F1") + "m ";
        UnityEditor.SessionState.SetString("proto.drive", (settled ? "SETTLED" : done ? "REACHED_PLAZA" : "STOPPED") + " s=" + bestS.ToString("F1") + "/" + total.ToString("F1") + " route=" + route + " time=" + (Time.time - t0).ToString("F1") + "s maxV=" + (maxV * 3.6f).ToString("F0") + "km/h minY=" + minY.ToString("F1") + " lateral=" + Mathf.Sqrt(best).ToString("F2") + "m | stages " + stages + " | minDist " + ds);
    }
};
UnityEditor.EditorApplication.update += cb;
return "driving job=" + rc.CurrentJob + " stage=" + rc.CurrentStage;
