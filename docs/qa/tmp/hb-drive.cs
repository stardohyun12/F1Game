// 언덕 슬라이스 합성 주행 (Play, 주행 단계에서). route 0 = 오르막→꼭대기→지름길 헤어핀→문, 1 = 오르막→꼭대기→우회 끝.
// 스플라인을 1m 로 뽑아 7m 앞을 겨눈다. 15~25m 앞 방향차 >35° 면 8m/s, 아니면 vcap. 캡처: s60(오르막 중턱)·s128(꼭대기 넘는 순간)·s162(갈림목).
// 결과: SessionState "hb.drive" — 정지·리스폰(한 프레임 5m 넘게 순간이동)·길 밖(중심선 3.0m 넘음)·최대 횡거리·최저 y.
Application.runInBackground = true;
var route = UnityEditor.SessionState.GetInt("hb.route", 0);
float vcap = UnityEditor.SessionState.GetInt("p.vcap", 15);
bool cap = UnityEditor.SessionState.GetInt("hb.cap", 0) == 1;
var c = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
var path = new System.Collections.Generic.List<Vector3>();
System.Action<UnityEngine.Splines.Spline, bool> add = (sp, stopAtFork) => {
  var fork = UnityEngine.Splines.SplineUtility.EvaluatePosition(c.Splines[1], 0f);
  for (int i = 0; i <= 3000; i++) { var q = UnityEngine.Splines.SplineUtility.EvaluatePosition(sp, i / 3000f); var v = new Vector3(q.x, q.y, q.z);
    if (path.Count == 0 || Vector3.Distance(path[path.Count - 1], v) >= 1f) path.Add(v);
    if (stopAtFork && Vector3.Distance(v, new Vector3(fork.x, fork.y, fork.z)) < 0.3f) break; }
};
if (route == 0) add(c.Splines[0], false); else { add(c.Splines[0], true); add(c.Splines[1], false); }
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var rc = Services.Run as RunController;
UnityEditor.SessionState.SetString("hb.drive", "");
float t0 = Time.time, maxV = 0, minY = 999, lastProg = Time.time, maxLat = 0; int bi = 0, respawn = 0, offroad = 0; bool wasOff = false; Vector3 last = rb.position; string shots = "";
// 2차: 캡처 지점을 길 모양에서 찾는다 — 오르막 중턱(z≥70), 갈림목 8m 전(④), 꼭대기(1,150) = 넘는 순간(⑤), 모서리 우회전(12,167) = 꼭대기 지점(③)
int iUp = 0, iFork = 0, iCorner = 0, iCrest = 0; { var fk = UnityEngine.Splines.SplineUtility.EvaluatePosition(c.Splines[1], 0f); float bf = 1e9f, bc = 1e9f;
  for (int i = 0; i < path.Count; i++) { if (iUp == 0 && path[i].z >= 70f) iUp = i; float df = Vector3.Distance(path[i], new Vector3(fk.x, fk.y, fk.z)); if (df < bf) { bf = df; iFork = i; } float dc = Vector2.Distance(new Vector2(path[i].x, path[i].z), new Vector2(12f, 167f)); if (dc < bc) { bc = dc; iCorner = i; } if (iCrest == 0 && path[i].z >= 150f) iCrest = i; } }
// 3차: ⑥ 깎은 면(z 82~118 오른쪽) 한가운데 z≥96, ③ 꼭대기 = 넘은 직후 z≥158(레이 측정 최대 지점), ⑤ = 모서리
int iBank = path.FindIndex(q => q.z >= 96f), iTop = path.FindIndex(q => q.z >= 158f);
var capAt = new System.Collections.Generic.Dictionary<int, string> { { iUp, "hilltop-2-uphill" }, { iBank, "hilltop-6-cut-slope" }, { iFork - 8, "hilltop-4-fork" }, { iTop, "hilltop-3-crest" }, { iCorner, "hilltop-5-crest-wide" } };
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    var p = rb.position;
    if (Vector3.Distance(p, last) > 5f) respawn++;
    last = p;
    int best = bi; float bd = 1e9f;
    for (int i = Mathf.Max(0, bi - 10); i < Mathf.Min(path.Count, bi + 25); i++) { float dd = new Vector2(path[i].x - p.x, path[i].z - p.z).sqrMagnitude; if (dd < bd) { bd = dd; best = i; } }
    if (best > bi) { bi = best; lastProg = Time.time; }
    float lat = Mathf.Sqrt(bd); maxLat = Mathf.Max(maxLat, lat);
    bool off = lat > 3.0f; if (off && !wasOff) offroad++; wasOff = off;
    var tgt = path[Mathf.Min(path.Count - 1, bi + 7)];
    var to = tgt - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
    float ang = Vector3.SignedAngle(fw, to, Vector3.up);
    Vector3 d0 = path[Mathf.Min(path.Count - 1, bi + 2)] - path[bi], d1 = path[Mathf.Min(path.Count - 1, bi + 25)] - path[Mathf.Min(path.Count - 1, bi + 15)];
    d0.y = 0; d1.y = 0;
    float vT = Vector3.Angle(d0, d1) > 35f ? 8f : vcap, v = rb.linearVelocity.magnitude;
    mi.SetValue(car, new Vector2(Mathf.Clamp(ang / 25f, -1, 1), Mathf.Clamp((vT - v) / 3f, -1f, 1f)));
    maxV = Mathf.Max(maxV, v); minY = Mathf.Min(minY, p.y);
    if (cap) foreach (var kv in capAt) if (bi >= kv.Key && !shots.Contains(kv.Value)) { ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/" + kv.Value + ".png"); shots += kv.Value + " "; }
    bool done = bi >= path.Count - 3 || rc.CurrentStage == RunStage.Result, timeout = Time.time - lastProg > 6f || Time.time - t0 > 90f;
    if (done || timeout)
    {
        mi.SetValue(car, Vector2.zero);
        UnityEditor.EditorApplication.update -= cb;
        UnityEditor.SessionState.SetString("hb.drive", (done ? "REACHED" : "STOPPED") + " route=" + route + " i=" + bi + "/" + path.Count + " time=" + (Time.time - t0).ToString("F1") + "s maxV=" + (maxV * 3.6f).ToString("F0") + "km/h minY=" + minY.ToString("F1") + " maxLat=" + maxLat.ToString("F2") + "m offroad=" + offroad + " respawn=" + respawn + " stage=" + rc.CurrentStage + " shots=" + shots);
    }
};
UnityEditor.EditorApplication.update += cb;
return "driving route=" + route + " path=" + path.Count + " stage=" + rc.CurrentStage;
