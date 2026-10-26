// 과제 1448: 인도 진입 시험(Play, 주행 단계). 본선 직선 자리 × 양쪽 × 각도(15/45/90) × 속도(20~40km/h) 5회.
// (1448 v2: 시작 낙하 0.4m 를 빼려고 vy·기울기는 0.35s 뒤부터 잰다)
// 판정: 넘음 = 차 중심이 차로 끝+1.6m 를 넘음 / 멈춤 = 그 전에 속도가 처음의 30% 아래 / 튐 = |vy|>1.5m/s 또는 기울기>15°.
// 감속이 가장 큰 순간 차 상자(+6cm)에 겹친 콜라이더 이름을 모아 원인을 센다. 결과 SessionState "b1448.curb".
Application.runInBackground = true;
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var P = main.centerline; int n = P.Length; float lh = main.width * 0.5f;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>(); var box = car.GetComponent<BoxCollider>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
System.Func<int, Vector3> At = i => P[((i % n) + n) % n];
System.Func<int, Vector3> Fw = i => { var f = At(i + 3) - At(i - 3); f.y = 0; return f.normalized; };
// 자리 고르기: 앞뒤 15점 방향차 <8°, 양쪽 차로 끝+1m 아래가 Band_ 콜라이더(인도)
var sites = new List<(int i, int side, string what)>();
for (int i = 40; i < n - 40 && sites.Count < 10; i += 37)
{
    if (Vector3.Angle(Fw(i - 15), Fw(i + 15)) > 8f) continue;
    var f = Fw(i);
    foreach (int side in new[] { 1, -1 })
    {
        var nrm = side * new Vector3(-f.z, 0, f.x);
        if (Physics.Raycast(At(i) + nrm * (lh + 0.9f) + Vector3.up * 2f, Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith("Band_") && sites.Count(s => s.i == i) == 0)
            sites.Add((i, side, h.collider.name));
    }
}
var angles = new[] { 15f, 45f, 90f }; var speeds = new[] { 20f, 25f, 30f, 35f, 40f };
var ov = UnityEditor.SessionState.GetString("b1448.speed", ""); if (ov != "") speeds = Enumerable.Repeat(float.Parse(ov), 5).ToArray();   // 저속 고정 재시험용
var trials = new List<(int site, float ang, float kmh)>();
for (int s = 0; s < Mathf.Min(5, sites.Count); s++) foreach (var a in angles) trials.Add((s, a, speeds[(s + (int)a) % 5]));
// 위 한 줄 = 각도마다 자리 5곳 × 1회(속도 20~40 돌려씀) → 각도당 5회
var res = new List<string>(); var cause = new Dictionary<string, int>();
int ti = -1; float tStart = 0, v0 = 0, minRatio = 1, maxVy = 0, maxTilt = 0, decelMax = 0, prevV = 0, maxLong = 0, maxLat = 0; string hitAt = ""; Vector3 site0 = Vector3.zero, nrm0 = Vector3.zero;
var cargo = Services.Cargo as CargoBox;
var tally = new Dictionary<string, int>();
System.Action Next = null;
Next = () =>
{
    ti++; if (ti >= trials.Count) return;
    var (si, ang, kmh) = trials[ti]; var (i, side, _) = sites[si];
    var f = Fw(i); nrm0 = side * new Vector3(-f.z, 0, f.x); site0 = At(i);
    var head = (Quaternion.AngleAxis(-side * ang, Vector3.up) * f).normalized;   // side+1 = 왼쪽(−yaw)
    if (Vector3.Dot(head, nrm0) < 0) head = (Quaternion.AngleAxis(side * ang, Vector3.up) * f).normalized;
    float sa = Mathf.Sin(ang * Mathf.Deg2Rad), ca = Mathf.Cos(ang * Mathf.Deg2Rad);
    float ext = box.size.z * car.transform.lossyScale.z * .5f * sa + box.size.x * car.transform.lossyScale.x * .5f * ca;
    float lat = Mathf.Max(-lh + ext, lh - ext - 1.2f);
    var start = site0 + nrm0 * lat - head * 0f;
    // 비스듬하면 부딪는 점이 앞으로 밀린다 → 시작점을 뒤로 당겨 자리(site0) 근처에서 부딪게
    float along = (lh - ext - lat) / Mathf.Max(0.05f, sa) * ca; start -= f * along;
    rb.position = start + Vector3.up * 0.4f; rb.rotation = Quaternion.LookRotation(head);
    car.transform.SetPositionAndRotation(rb.position, rb.rotation); Physics.SyncTransforms();
    v0 = kmh / 3.6f; rb.linearVelocity = head * v0; rb.angularVelocity = Vector3.zero;
    tStart = Time.time; minRatio = 1; maxVy = 0; maxTilt = 0; decelMax = 0; prevV = v0; maxLong = 0; maxLat = 0; hitAt = "";
};
Next();
UnityEditor.SessionState.SetString("b1448.curb", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (ti >= trials.Count) return;
    float t = Time.time - tStart; if (t < 0.05f) return;
    var p = rb.position; float latNow = Vector3.Dot(p - site0, nrm0);
    var hv = rb.linearVelocity; float v = new Vector2(hv.x, hv.z).magnitude;
    if (t > 0.3f) minRatio = Mathf.Min(minRatio, v / v0);
    if (t > 0.35f) maxVy = Mathf.Max(maxVy, Mathf.Abs(hv.y)); if (t > 0.35f) maxTilt = Mathf.Max(maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
    if (cargo != null) { maxLong = Mathf.Max(maxLong, cargo.CurrentLongG); maxLat = Mathf.Max(maxLat, cargo.CurrentLatG); }
    float dec = (prevV - v) / Mathf.Max(1e-3f, Time.deltaTime); prevV = v;
    var (si, ang, kmh) = trials[ti];
    mi.SetValue(car, new Vector2(0f, Mathf.Clamp((v0 - v) / 2f, 0f, 1f)));
    if (dec > decelMax && t > 0.15f)
    {
        decelMax = dec;
        var half = Vector3.Scale(box.size, car.transform.lossyScale) * .5f + Vector3.one * 0.06f;
        var hits = Physics.OverlapBox(car.transform.TransformPoint(box.center), half, car.transform.rotation, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != rb).Select(c => c.name.Length > 28 ? c.name.Substring(0, 28) : c.name).Distinct();
        hitAt = string.Join("+", hits) + "@lat" + latNow.ToString("F2") + " y" + (p.y - site0.y).ToString("F2");
    }
    bool crossed = latNow > lh + 1.6f, stopped = t > 0.4f && v < v0 * 0.3f, timeout = t > 3f;
    if (crossed || stopped || timeout)
    {
        bool bounce = maxVy > 1.5f || maxTilt > 15f;
        string outc = crossed ? (bounce ? "넘음+튐" : "넘음") : stopped ? "멈춤" : "느림";
        tally[ang + " " + outc] = tally.TryGetValue(ang + " " + outc, out var c0) ? c0 + 1 : 1;
        res.Add($"{ang}° {kmh}km/h #{sites[si].i}{(sites[si].side > 0 ? "L" : "R")} {outc} minV={minRatio:P0} vy={maxVy:F2} tilt={maxTilt:F0}° G(long/lat)={maxLong:F2}/{maxLat:F2} 최대감속={decelMax:F0}m/s² 끝lat={latNow:F2} 겹침[{hitAt}]");
        mi.SetValue(car, Vector2.zero);
        Next();
        if (ti >= trials.Count)
        {
            UnityEditor.EditorApplication.update -= cb;
            UnityEditor.SessionState.SetString("b1448.curb", "DONE sites=" + string.Join(",", sites.Take(5).Select(s => s.i + (s.side > 0 ? "L" : "R") + ":" + s.what)) + " lh=" + lh + "\n" + string.Join(" ; ", tally.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)) + "\n" + string.Join("\n", res));
        }
    }
};
UnityEditor.EditorApplication.update += cb;
return "trials=" + trials.Count + " sites=" + sites.Count + " lh=" + lh + " cargo=" + (cargo != null);
