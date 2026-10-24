// Play 중 합성 주행: 풀스로틀 + 중심선 8m 앞을 향해 조향. 결과는 SessionState "slice.drive".
Application.runInBackground = true;
var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildPostcardSlice")).First(x => x != null);
var total = (float)t.GetProperty("Total").GetValue(null);
var frame = t.GetMethod("Frame");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
UnityEditor.SessionState.SetString("slice.drive", "");
float t0 = Time.time, maxV = 0, minY = 999, lastProg = Time.time, bestS = 0; int stuck = 0;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    var p = rb.position;
    // 가장 가까운 s
    float s = 0, best = 1e9f;
    for (float q = Mathf.Max(0, bestS - 10); q <= Mathf.Min(total, bestS + 20); q += .5f)
    { var a = new object[] { q, null, null, null }; frame.Invoke(null, a); var c = (Vector3)a[1]; float dd = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (dd < best) { best = dd; s = q; } }
    if (s > bestS + .5f) { bestS = s; lastProg = Time.time; }
    var ta = new object[] { Mathf.Min(total, s + 8f), null, null, null }; frame.Invoke(null, ta);
    var tgt = (Vector3)ta[1] + (Vector3)ta[3] * 1.2f;
    var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), 1f));
    maxV = Mathf.Max(maxV, rb.linearVelocity.magnitude); minY = Mathf.Min(minY, p.y);
    bool done = bestS >= total - 3f, timeout = Time.time - lastProg > 4f || Time.time - t0 > 40f;
    if (done || timeout)
    {
        mi.SetValue(car, Vector2.zero);
        UnityEditor.EditorApplication.update -= cb;
        UnityEditor.SessionState.SetString("slice.drive", (done ? "REACHED_END" : "STOPPED") + " s=" + bestS.ToString("F1") + "/" + total.ToString("F1") + " time=" + (Time.time - t0).ToString("F1") + "s maxV=" + (maxV * 3.6f).ToString("F0") + "km/h minY=" + minY.ToString("F1") + " lateral=" + Mathf.Sqrt(best).ToString("F2") + "m");
    }
};
UnityEditor.EditorApplication.update += cb;
return "driving";
