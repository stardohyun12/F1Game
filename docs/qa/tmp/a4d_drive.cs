// 과제 1331 주행 봇 (Play 중). Box·Wheels 각각 ① 직선 가속(길 가운데 따라, 풀스로틀) ② 코너(길 따라 + 조향 ±0.8 사인 2초 주기) ③ 오르막(①의 끝에서 반대로 돌려 길 따라) — 10초씩.
// 표본마다: 속도, 미끄럼각(속도 방향 vs 차 앞, 3m/s 이상), 바닥 틈(상자 바닥 − 차 밑 땅), 접지(isGrounded), 기울기(차 위 vs 땅 법선).
// 멈춤 = 1.5초 뒤 풀스로틀인데 1km/h 미만 / 긁힘 = 틈 < 0.02m (Wheels 에서만 의미) / 뜸 = 틈 > 0.25m. 결과 SessionState "a4d.drive"
Application.runInBackground = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>(); var box = car.GetComponent<BoxCollider>();
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var mi = typeof(ArcadeCarController).GetField("moveInput", BF); var dmF = typeof(ArcadeCarController).GetField("driveMode", BF); var grF = typeof(ArcadeCarController).GetField("isGrounded", BF);
var apply = typeof(ArcadeCarController).GetMethod("ApplyDriveMode", BF);
UnityEditor.SessionState.SetString("a4d.drive", "running");
Vector3 p0 = rb.position; Quaternion r0 = rb.rotation; Vector3 upP = p0; Quaternion upR = r0;
int test = 0; float t0 = Time.time; var sb = new System.Text.StringBuilder();
float maxV = 0, t40 = -1, slipSum = 0, slipMax = 0, tiltMax = 0, gapMin = 9, gapMax = -9; int n = 0, nSlip = 0, stops = 0, scrape = 0, floatN = 0, ungr = 0; Vector3 ps = p0;
int ev = 0; float lastEv = -9;
string[] names ={ "직선", "코너", "오르막" };
System.Action<Vector3, Quaternion, int> Reset = (pos, rot, mode) => {
  dmF.SetValue(car, System.Enum.ToObject(dmF.FieldType, mode)); apply.Invoke(car, null);
  rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = pos; rb.rotation = rot; car.transform.SetPositionAndRotation(pos, rot);
  maxV = 0; t40 = -1; slipSum = 0; slipMax = 0; tiltMax = 0; gapMin = 9; gapMax = -9; n = nSlip = stops = scrape = floatN = ungr = 0; ps = pos; t0 = Time.time; ev = 0; lastEv = -9; };
Reset(p0, r0, 0);
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
  int mode = test / 3, kind = test % 3; float t = Time.time - t0; var p = rb.position;
  var ah = p + car.transform.forward * 10f; float xs = 0; int nx = 0;
  for (float x = ah.x - 15; x <= ah.x + 15; x += 0.5f) foreach (var hh in Physics.RaycastAll(new Vector3(x, 30, ah.z), Vector3.down, 80)) if (hh.collider.name == "Road") { xs += x; nx++; }
  var to = (nx > 0 ? new Vector3(xs / nx, p.y, ah.z) : ah) - p; to.y = 0; var fw = car.transform.forward; fw.y = 0;
  float steer = Mathf.Clamp(Vector3.SignedAngle(fw, to, Vector3.up) / 25f, -1, 1); if (kind == 1) steer = Mathf.Clamp(steer + 0.8f * Mathf.Sin(t * Mathf.PI), -1, 1);
  mi.SetValue(car, new Vector2(steer, 1f));
  var v = rb.linearVelocity; var hv = Vector3.ProjectOnPlane(v, Vector3.up); float kmh = hv.magnitude * 3.6f; maxV = Mathf.Max(maxV, kmh); if (t40 < 0 && kmh >= 40) t40 = t;
  if (hv.magnitude > 3f) { float sl = Vector3.Angle(fw, hv); slipSum += sl; nSlip++; slipMax = Mathf.Max(slipMax, sl); }
  if (t > 1.5f && kmh < 1f) stops++;
  float gy = float.NaN; Vector3 gn = Vector3.up; string gTop = ""; foreach (var hh in Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore)) if (hh.collider.attachedRigidbody != rb && (float.IsNaN(gy) || hh.point.y > gy)) { gy = hh.point.y; gn = hh.normal; gTop = hh.collider.name; }
  if (!float.IsNaN(gy)) { float gap = box.bounds.min.y - gy; gapMin = Mathf.Min(gapMin, gap); gapMax = Mathf.Max(gapMax, gap); bool sc0 = false; foreach (var c in Physics.OverlapBox(car.transform.TransformPoint(box.center), Vector3.Scale(box.size, car.transform.lossyScale) * 0.5f + Vector3.one * 0.01f, car.transform.rotation, ~0, QueryTriggerInteraction.Ignore)) if (c.attachedRigidbody != rb && !c.name.StartsWith("Wall")) sc0 = true; if (sc0) scrape++; if (gap > 0.25f) floatN++; tiltMax = Mathf.Max(tiltMax, Vector3.Angle(car.transform.up, gn)); }
  if (!(bool)grF.GetValue(car)) ungr++; n++;
  // 사건 기록(시험당 최대 5): 기울기>25°·틈>0.3·멈춤 시작 — 시각·자리·속도·밑 땅·차에 닿은 것
  float tiltNow = Vector3.Angle(car.transform.up, gn); bool stopNow = t > 1.5f && kmh < 1f;
  if (ev < 5 && t - lastEv > 1f && (tiltNow > 25f || (!float.IsNaN(gy) && box.bounds.min.y - gy > 0.3f) || stopNow)) {
    lastEv = t; ev++; string touch = ""; foreach (var c in Physics.OverlapBox(box.bounds.center, box.bounds.extents + Vector3.one * 0.05f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) if (c.attachedRigidbody != rb && touch.Length < 80) touch += c.name + ",";
    string gname = ""; foreach (var hh in Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore)) if (hh.collider.attachedRigidbody != rb) gname += hh.collider.name + "@" + hh.point.y.ToString("F1") + ",";
    sb.Append("   ! t" + t.ToString("F1") + (stopNow ? " 멈춤" : "") + " 기울기 " + tiltNow.ToString("F0") + " p " + p.ToString("F1") + " " + kmh.ToString("F0") + "km/h 밑 " + gname + " 닿음 " + touch + "\n"); }
  bool onRoad = gTop == "Road";
  if (test == 0 && t <= 8f && t >= 3f && onRoad && tiltNow < 10f && (bool)grF.GetValue(car)) { upP = p + Vector3.up * 0.3f; upR = Quaternion.LookRotation(-fw, Vector3.up); }
  if (t >= 10f) {
    float d = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(ps.x, 0, ps.z));
    sb.Append((mode == 0 ? "Box " : "Wheels ") + names[kind] + ": 거리 " + d.ToString("F0") + "m 최고 " + maxV.ToString("F0") + " 끝 " + kmh.ToString("F0") + "km/h 0-40 " + (t40 < 0 ? "-" : t40.ToString("F1") + "s") + " 미끄럼 평균 " + (nSlip > 0 ? slipSum / nSlip : 0).ToString("F1") + "° 최대 " + slipMax.ToString("F0") + "° 멈춤 " + (100f * stops / n).ToString("F0") + "% 긁힘 " + (100f * scrape / n).ToString("F0") + "% 뜸 " + (100f * floatN / n).ToString("F0") + "% 비접지 " + (100f * ungr / n).ToString("F0") + "% 틈 " + gapMin.ToString("F2") + "~" + gapMax.ToString("F2") + " 기울기최대 " + tiltMax.ToString("F0") + "° 표본 " + n + "\n");
    test++;
    if (test >= 6) { mi.SetValue(car, Vector2.zero); Reset(p0, r0, 0); UnityEditor.EditorApplication.update -= cb; UnityEditor.SessionState.SetString("a4d.drive", sb.ToString()); return; }
    int k2 = test % 3; Reset(k2 == 2 ? upP : p0, k2 == 2 ? upR : r0, test / 3);
  }
};
UnityEditor.EditorApplication.update += cb;
return "driving";
