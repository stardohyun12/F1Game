// BayLoop v4 연석 넘기 (Play). 본선 연석 구간 12점: 차로 가운데에서 인도 쪽(±90°)으로 5m/s 4초.
// 바퀴 밑 레이(차 자신 콜라이더 빼고)가 Band_ 를 밟았는지, 몇 m 갔는지, 최대 몇 cm 올라갔는지. 결과 SessionState "bl.curb".
Application.runInBackground = true;
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main" && r.centerline.Length > 0);
var P = rs.centerline; int n = P.Length;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var mine = new HashSet<Collider>(car.GetComponentsInChildren<Collider>());
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
// 연석 구간: 항구길(0~), 상점가, 곶길, 해안도로 — 점 번호는 중심선 비율로 고름(본선 1495점)
var idx = new List<int> { 1260, 1320, 1380, 1430, 1470 }; // 0506: 해안 쪽 5점 × 양쪽 = 10회
var mode = UnityEditor.SessionState.GetString("c.mode", "Box"); car.SetDriveMode(mode == "Wheels" ? ArcadeCarController.DriveMode.Wheels : ArcadeCarController.DriveMode.Box);
var cg = UnityEngine.Object.FindFirstObjectByType<CargoBox>(); float gL = 0, gG = 0, tilt = 0, hp0 = 0; float sum2 = 0; int ns = 0;
UnityEditor.SessionState.SetString("c.res", "");
int k = 0, lastFrame = -1; float tRun = 0, yMax = 0; Vector3 p0 = Vector3.zero; bool band = false; string bandName = ""; var res = new List<string>(); int up = 0;
System.Action Spawn = () =>
{
    int i = Mathf.Clamp(idx[k / 2], 3, n - 3); float sd = k % 2 == 0 ? 90f : -90f;
    var f = P[i + 2] - P[i - 2]; f.y = 0; f.Normalize(); var f0 = Quaternion.Euler(0, sd, 0) * f;
    p0 = P[i] + Vector3.up * 0.5f; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
    rb.position = p0; rb.rotation = Quaternion.LookRotation(f0); car.transform.SetPositionAndRotation(p0, rb.rotation);
    tRun = Time.time; yMax = -9; band = false; bandName = ""; gL = gG = tilt = 0; sum2 = 0; ns = 0; if (cg != null) { cg.Repair(); hp0 = cg.IntegrityHP; }
};
k = 0; Spawn();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
    var p = rb.position; float v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(0f, Mathf.Clamp((5f - v) / 2f, -1f, 1f)));
    if (Time.time - tRun > 0.3f) yMax = Mathf.Max(yMax, p.y - p0.y);
    if (cg != null && Time.time - tRun > 0.6f) { gL = Mathf.Max(gL, Mathf.Abs(cg.SignedLatG)); gG = Mathf.Max(gG, Mathf.Abs(cg.SignedLongG)); var lg = cg.transform.InverseTransformDirection(Physics.gravity) / 9.81f; float tl = new Vector2(lg.x, lg.z).magnitude; tilt = Mathf.Max(tilt, tl); float tot = new Vector2(cg.SignedLatG - lg.x, cg.SignedLongG - lg.z).magnitude; sum2 += tot * tot; ns++; }
    if (!band)
        foreach (var h in Physics.RaycastAll(p + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
        { if (mine.Contains(h.collider)) continue; if (h.collider.name.StartsWith("Band_")) { band = true; bandName = h.collider.name; } break; }
    if (Time.time - tRun >= 4f)
    {
        float moved = new Vector2(p.x - p0.x, p.z - p0.z).magnitude; if (band) up++;
        res.Add("#" + k + "(" + p0.x.ToString("F0") + "," + p0.z.ToString("F0") + ") " + (band ? "UP " + bandName.Replace("Band_main_", "") : "no") + " moved=" + moved.ToString("F1") + " dy=" + yMax.ToString("F2") + " latG" + gL.ToString("F2") + " longG" + gG.ToString("F2") + " tilt" + tilt.ToString("F2") + " rms" + Mathf.Sqrt(sum2 / Mathf.Max(1, ns)).ToString("F2") + " dHP" + (cg != null ? (hp0 - cg.IntegrityHP).ToString("F1") : "-"));
        k++;
        if (k >= idx.Count * 2) { mi.SetValue(car, Vector2.zero); UnityEditor.EditorApplication.update -= cb; UnityEditor.SessionState.SetString("c.res", mode + " DONE up=" + up + "/" + (idx.Count * 2) + " | " + string.Join(" ; ", res)); return; }
        Spawn();
    }
};
UnityEditor.EditorApplication.update += cb;
return mode + " curb test " + idx.Count * 2 + " runs, own colliders=" + mine.Count;
