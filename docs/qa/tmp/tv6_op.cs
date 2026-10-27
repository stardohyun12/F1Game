// 과제 0311 (h0404-op.cs 사본 개조): Map_TownV6 고리에서 OpeningTutorial 시작→정산 자동 1회. Play 중.
// 시네마틱 3초 뒤 Skip → 정비소에서 고리 점(tv6_loop_pts.txt)을 따라 택배 보관소 포장 → 출발 → 찻집 배달 → (막다른 길: 차를 돌려 놓음) → 해안 → 정비소 정산.
// 캡처 docs/qa/evidence/townv6-loop/tut_*.png (ScreenCapture = 게임 뷰 + UI). 결과 SessionState "op.qa".
Application.runInBackground = true;
float cap = 16f;
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/townv6-loop"));
System.IO.Directory.CreateDirectory(outDir);
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var rc = Services.Run as RunController;
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var go = typeof(PitCrew.Packing.PackingScreen).GetMethod("OnGoClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var seg = new List<Vector3>();
foreach (var l in System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/tv6_loop_pts.txt"))))
{ if (l.Trim() == "") continue; var a = l.Split(' '); var q = new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); if (Physics.Raycast(new Vector3(q.x, 200, q.z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore)) q.y = h.point.y; seg.Add(q); }
var P = seg.ToArray(); int n = P.Length;
int rev = -1; for (int i = 1; i < n - 1; i++) { var u = P[i] - P[i - 1]; var v = P[i + 1] - P[i]; u.y = 0; v.y = 0; if (Vector3.Angle(u, v) > 150f) { rev = i; break; } }
System.Func<int, Vector3> At = i => P[Mathf.Clamp(i, 0, n - 1)];
System.Func<int, Vector3> Fw = i => { var f = At(i + 2) - At(i - 2); f.y = 0; return f.normalized; };
var log = new List<string>();
var shots = new HashSet<string>();
float tStart = Time.realtimeSinceStartup;
System.Action<string> Shot = name => { if (shots.Add(name)) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, name + ".png")); log.Add("shot " + name + " @" + (Time.realtimeSinceStartup - tStart).ToString("F1")); } };
string phase = "cine"; float phaseT = 0f; int cur = 0, goCalls = 0; float lastProg = 0f, maxV = 0f; bool turned = false; int recov = 0;
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
    int hi = (!turned && rev > 0) ? rev : n - 1;   // 되돌이 전엔 되돌이 점을 넘어 찾지 않는다(같은 길 되밟기라 인덱스가 튐)
    for (int k = -6; k <= 25; k++) { int ii = Mathf.Min(cur + k, hi); var c = At(ii); float d = (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude; if (d < bd) { bd = d; bi = ii; } }
    if (bi > cur) { cur = bi; lastProg = Time.realtimeSinceStartup; }
    float turn = Vector3.Angle(Fw(cur), Fw(Mathf.Min(cur + 25, hi)));
    int la = turn > 35f ? 4 : 7;   // 0422: 급한 굽이에선 앞 8m 만 본다(14m 를 보면 안쪽 모서리를 깎아 벽·턱에 걸림)
    var to = At(Mathf.Min(cur + la, hi)) - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    float tv = Mathf.Min(vT, turn > 70f ? 6f : turn > 35f ? 9f : cap), v = rb.linearVelocity.magnitude;
    if (!turned && rev > 0 && cur >= rev - 8) tv = Mathf.Min(tv, 4f);
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((tv - v) / 3f, -1f, 1f)));
    maxV = Mathf.Max(maxV, v);
};
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    float now = Time.realtimeSinceStartup;
    if (now - tStart > 600f) { End("TIMEOUT stage=" + ot.Stage + " phase=" + phase + " cur=" + cur + "/" + n); return; }
    string st = ot.Stage;
    if (phase == "cine")
    {
        float t = ot.CineTime;
        if (t > 1.5f) Shot("tut_0_cine_garage");
        if (t > 3f && !st.StartsWith("T")) { ot.Skip(); log.Add("skip@" + t.ToString("F1")); }
        if (st.StartsWith("T0")) { phase = "toDepot"; phaseT = now; lastProg = now; log.Add("cineEnd len=" + ot.CineTime.ToString("F1")); }
        return;
    }
    if (phase == "toDepot")
    {
        if (now - phaseT > 1.5f) Shot("tut_1_garage_start");
        Drive(8f);
        if (st.StartsWith("T1")) { mi.SetValue(car, Vector2.zero); phase = "pack"; phaseT = now; log.Add("packAt cur=" + cur); }
        else if (now - lastProg > 15f) End("STUCK toDepot cur=" + cur + " p=" + rb.position.ToString("F0") + " trig=" + GameObject.Find("TUT_PackTrigger").transform.position.ToString("F0"));
        return;
    }
    if (phase == "pack")
    {
        if (now - phaseT > 1.5f) Shot("tut_2_depot_packing");
        if (goCalls == 0 && now - phaseT > 2.0f) { go.Invoke(Services.Packing, null); goCalls = 1; phaseT = now; }
        else if (goCalls >= 1 && goCalls < 6 && !rc.RunActive && now - phaseT > 1.2f) { go.Invoke(Services.Packing, null); goCalls++; phaseT = now; log.Add("go#" + goCalls); }
        if (rc.RunActive) { phase = "drive"; runAt = now; lastProg = now; log.Add("runStart integ=" + (Services.Cargo != null ? Services.Cargo.IntegrityPercent : -1).ToString("F2")); }
        else if (goCalls == 6 && now - phaseT > 5f) End("STUCK pack");
        return;
    }
    if (phase == "drive")
    {
        if (!turned && rev > 0 && cur >= rev - 1 && st.StartsWith("T5"))
        {   // 막다른 찻집 앞: 플레이어라면 여기서 차를 돌린다 — 봇은 돌려 놓는다
            var q = At(rev + 3); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = q + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(Fw(rev + 6)); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
            turned = true; cur = rev + 3; lastProg = now; log.Add("turnAround@" + rev + " t=" + (now - runAt).ToString("F0"));
        }
        Drive(cap);
        if (now - runAt > 3f) Shot("tut_3_depart");
        if (cur > 50 && cur < 60) Shot("tut_4_postoffice_plaza");
        if (ot.BubbleUphillShown && bubbleAt < 0f) bubbleAt = now;
        if (bubbleAt > 0f && now - bubbleAt > 0.4f) Shot("tut_5_uphill_bubble");
        if (st.StartsWith("T5") && deliverAt < 0f) { deliverAt = now; log.Add("delivered cur=" + cur); }
        if (deliverAt > 0f && now - deliverAt > 1.0f) Shot("tut_6_arrive_tea");
        if (ot.CoastSegmentSec > 0f && coastAt < 0f) coastAt = now;
        if (ot.Stage.Contains("Coast") && coastAt < 0f) Shot("tut_7_coast");
        if (coastAt > 0f && now - coastAt > 0.5f) Shot("tut_7_coast_record");
        if (st.StartsWith("T6")) { mi.SetValue(car, Vector2.zero); phase = "settle"; settleAt = now; log.Add("maxV=" + (maxV * 3.6f).ToString("F0") + "km/h prog=" + cur + "/" + n); }
        else if (now - lastProg > 6f && recov < 3)
        {   // 막힘 복구(기록): 4점 앞에 다시 놓음
            log.Add("RECOVER cur=" + cur + " p=" + rb.position.ToString("F0") + " stage=" + st); recov++;
            var q = At(cur + 4); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = q + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(Fw(cur + 4)); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
            cur += 4; lastProg = now;
        }
        else if (now - lastProg > 15f) End("STUCK drive cur=" + cur + "/" + n + " stage=" + st + " p=" + rb.position.ToString("F0"));
        return;
    }
    if (phase == "settle")
    {
        if (now - settleAt > 1.5f) Shot("tut_8_garage_settle");
        if (now - settleAt > 2.5f) { ot.CloseSettle(); phase = "done"; phaseT = now; }
        return;
    }
    if (phase == "done" && now - phaseT > 0.5f) End("DONE stage=" + ot.Stage + " bubbleUphill=" + ot.BubbleUphillShown + " coast=" + ot.CoastSegmentSec.ToString("F1") + " wallet=" + (Services.Upgrade != null ? Services.Upgrade.Wallet : -1) + " total=" + (now - tStart).ToString("F0") + "s");
};
UnityEditor.EditorApplication.update += cb;
return "qa n=" + n + " rev=" + rev + " stage=" + ot.Stage;
