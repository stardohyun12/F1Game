// 과제 0032 QA: Play 중 OpeningTutorial 전체 자동 진행. SessionState "op.mode" = full(시네마틱 끝까지) | skip(3초에 건너뛰기).
// 캡처 docs/qa/evidence/fix-0342/"+UnityEditor.SessionState.GetString("op.mode","full")+"/ (ScreenCapture = 게임 뷰 + UI). 결과 SessionState "op.qa".
Application.runInBackground = true;
string mode = UnityEditor.SessionState.GetString("op.mode", "full");
float cap = UnityEditor.SessionState.GetInt("op.cap", 16);
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/fix-0342/"+UnityEditor.SessionState.GetString("op.mode","full")+""));
System.IO.Directory.CreateDirectory(outDir);
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var rc = Services.Run as RunController;
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var go = typeof(PitCrew.Packing.PackingScreen).GetMethod("OnGoClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var all = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 0).ToArray();
System.Func<string, RoadSurface> R = k => all.First(r => r.roadId == k);
var seg = new List<Vector3>();
System.Action<string, int, int> Take = (k, a, b) => { var r = R(k); for (int i = a; i <= Mathf.Min(b, r.centerline.Length - 1); i++) seg.Add(r.centerline[i]); };
Take("main", 0, 167); Take("V_kyoto_back", 1, 150); Take("SC_ridge", 66, 9999); Take("main", 356, 1497); Take("main", 0, 14);
var P = seg.ToArray(); int n = P.Length;
System.Func<int, Vector3> At = i => P[Mathf.Clamp(i, 0, n - 1)];
System.Func<int, Vector3> Fw = i => { var f = At(i + 2) - At(i - 2); f.y = 0; return f.normalized; };
var log = new List<string>();
var shots = new HashSet<string>();
float tStart = Time.realtimeSinceStartup;
System.Action<string> Shot = name => { if (shots.Add(name)) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, name + ".png")); log.Add("shot " + name + " @" + (Time.realtimeSinceStartup - tStart).ToString("F1")); } };
string phase = "cine"; float phaseT = 0f; int cur = 0, goCalls = 0; float lastProg = 0f, maxV = 0f;
float runAt = -1f, deliverAt = -1f, coastAt = -1f, settleAt = -1f, bubbleAt = -1f;
UnityEditor.SessionState.SetString("op.qa", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
System.Action<string> End = res =>
{
    mi.SetValue(car, Vector2.zero);
    UnityEditor.EditorApplication.update -= cb;
    UnityEditor.SessionState.SetString("op.qa", res + " | " + string.Join(" ; ", log) + " || DIRECTOR " + string.Join(" ; ", ot.Log));
};
System.Action<float> Drive = vT =>
{
    var p = rb.position;
    int bi = cur; float bd = 1e9f;
    for (int k = -6; k <= 25; k++) { var c = At(cur + k); float d = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = cur + k; } }
    if (bi > cur) { cur = bi; lastProg = Time.realtimeSinceStartup; }
    var to = At(cur + 7) - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    float turn = Vector3.Angle(Fw(cur), Fw(cur + 25));
    float tv = Mathf.Min(vT, turn > 70f ? 6f : turn > 35f ? 9f : cap), v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((tv - v) / 3f, -1f, 1f)));
    maxV = Mathf.Max(maxV, v);
};
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    float now = Time.realtimeSinceStartup;
    if (now - tStart > 480f) { End("TIMEOUT stage=" + ot.Stage + " phase=" + phase); return; }
    string st = ot.Stage;
    if (phase == "cine")
    {
        float t = ot.CineTime;
        if (mode == "skip") { if (t > 3f && st == "Cinematic" || st.StartsWith("Scene")) { if (t > 3f) { ot.Skip(); log.Add("skip@" + t.ToString("F1")); } } }
        else
        {
            if (t > 6.2f) Shot("cine_1_room");
            if (t > 9.0f) Shot("cine_1b_window");
            if (t > 37.5f) Shot("cine_4a_aerial");
            if (t > 19.5f) Shot("cine_2_garage");
            if (t > 30.5f) Shot("cine_3_mart");
            if (t > 43.0f) Shot("cine_4_ignition");
        }
        if (st.StartsWith("T0")) { phase = "toMart"; phaseT = now; log.Add("cineEnd len=" + ot.CineTime.ToString("F1")); }
        return;
    }
    if (phase == "toMart")
    {
        if (mode == "skip" && now - phaseT > 1.5f) { Shot("skip_after"); End("SKIP_OK stage=" + st + " cineLen=" + ot.CineTime.ToString("F1") + " camFollow=" + Camera.main.GetComponent<FollowCamera>().enabled + " fog=" + RenderSettings.fog + " " + RenderSettings.fogStartDistance.ToString("F0") + "-" + RenderSettings.fogEndDistance.ToString("F0") + " bannerActive=" + UnityEngine.Object.FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include).gameObject.activeInHierarchy); return; }
        var trig = GameObject.Find("TUT_PackTrigger").transform.position; var p = rb.position;
        var to = trig - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
        float ang = Vector3.SignedAngle(fw, to, Vector3.up), v = rb.linearVelocity.magnitude;
        float tv = Mathf.Clamp(to.magnitude * 0.6f, 2f, 8f);
        mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((tv - v) / 3f, -1f, 1f)));
        if (st.StartsWith("T1")) { mi.SetValue(car, Vector2.zero); phase = "pack"; phaseT = now; }
        else if (now - phaseT > 40f) End("STUCK toMart d=" + to.magnitude.ToString("F1"));
        return;
    }
    if (phase == "pack")
    {
        if (now - phaseT > 1.5f) Shot("tut_1_packing");
        if (goCalls == 0 && now - phaseT > 2.0f) { go.Invoke(Services.Packing, null); goCalls = 1; phaseT = now; }
        else if (goCalls >= 1 && goCalls < 6 && !rc.RunActive && now - phaseT > 1.2f) { go.Invoke(Services.Packing, null); goCalls++; phaseT = now; log.Add("go#" + goCalls); }
        if (rc.RunActive) { phase = "drive"; runAt = now; cur = 0; lastProg = now; log.Add("runStart integ=" + (Services.Cargo != null ? Services.Cargo.IntegrityPercent : -1).ToString("F2")); }
        else if (goCalls == 6 && now - phaseT > 5f) End("STUCK pack");
        return;
    }
    if (phase == "drive")
    {
        Drive(cap);
        if (now - runAt > 3f) Shot("tut_2_depart");
        if (ot.BubbleUphillShown && bubbleAt < 0f) bubbleAt = now;
        if (bubbleAt > 0f && now - bubbleAt > 0.4f) Shot("tut_3_uphill_bubble");
        if (st.StartsWith("T5") && deliverAt < 0f) deliverAt = now;
        if (deliverAt > 0f && now - deliverAt > 1.0f) Shot("tut_4_arrive_tea");
        if (ot.CoastSegmentSec > 0f && coastAt < 0f) coastAt = now;
        if (coastAt > 0f && now - coastAt > 0.5f) Shot("tut_5_coast_record");
        if (st.StartsWith("T6")) { mi.SetValue(car, Vector2.zero); phase = "settle"; settleAt = now; log.Add("maxV=" + (maxV * 3.6f).ToString("F0") + "km/h prog=" + cur + "/" + n); }
        else if (now - lastProg > 10f) End("STUCK drive cur=" + cur + "/" + n + " stage=" + st + " p=" + rb.position.ToString("F0"));
        return;
    }
    if (phase == "settle")
    {
        if (now - settleAt > 1.5f) Shot("tut_6_settle");
        if (now - settleAt > 2.5f) { ot.CloseSettle(); phase = "done"; phaseT = now; }
        return;
    }
    if (phase == "done" && now - phaseT > 0.5f) End("DONE stage=" + ot.Stage + " bubbleUphill=" + ot.BubbleUphillShown + " coast=" + ot.CoastSegmentSec.ToString("F1") + " wallet=" + (Services.Upgrade != null ? Services.Upgrade.Wallet : -1) + " total=" + (now - tStart).ToString("F0") + "s");
};
UnityEditor.EditorApplication.update += cb;
return "qa " + mode + " n=" + n + " stage=" + ot.Stage;
