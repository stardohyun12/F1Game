// Play 중: SessionState "bl.shot" = "road|s|name" 목록(; 구분). 각 지점에 차를 놓고(정지) 물리 몇 프레임 뒤
// FollowCamera 가 따라붙으면 Camera.main 을 960x540 RT 로 렌더해서 docs/qa/evidence/bayloop-v1/<name>.png.
// 마지막에 "top" 이 있으면 위에서 내려본 전경 한 장(별도 카메라).
Application.runInBackground = true;
var list = UnityEditor.SessionState.GetString("bl.shot", "").Split(';').Where(x => x.Length > 0).ToList();
// 옵션(4번째 칸부터): rev = 반대 방향, yaw=도 = 차 방향 돌림, hud = 게임 뷰 ScreenCapture(HUD 포함) 한 장 더(<name>_hud.png)
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/" + UnityEditor.SessionState.GetString("bl.outdir", "bayloop-v1")));
System.IO.Directory.CreateDirectory(outDir);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline.Length > 0).ToDictionary(r => r.roadId);
System.Action<Camera, string> Save = (cam, name) =>
{
    var rt = new RenderTexture(960, 540, 24); var prev = cam.targetTexture;
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
    RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
    tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
    UnityEngine.Object.Destroy(tex); rt.Release();
};
int idx = 0, wait = 0; var done = new List<string>();
UnityEditor.SessionState.SetString("bl.shotlog", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (idx >= list.Count) { UnityEditor.EditorApplication.update -= cb; UnityEditor.SessionState.SetString("bl.shotlog", "DONE " + string.Join(" ", done)); return; }
    var a = list[idx].Split('|');
    if (a[0] == "mood") { UnityEditor.SessionState.SetString("bl.moodlog", UnityEditor.SessionState.GetString("bl.moodlog", "") + MoodSwitcher.Apply(a[1]) + "; "); idx++; return; }   // 무드 전환(다음 지점 90프레임 동안 반영)
    if (a[0] == "top")
    {
        var go = new GameObject("TopCam"); var cam = go.AddComponent<Camera>();
        cam.CopyFrom(Camera.main); cam.orthographic = true; cam.orthographicSize = 290f; cam.farClipPlane = 3000f;
        go.transform.position = new Vector3(-8f, 150f, 24f); go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); cam.nearClipPlane = 0.3f;
        bool fog = RenderSettings.fog; RenderSettings.fog = false;   // 위에서 보면 거리 안개·볼륨 연무에 다 묻힌다
        var vols = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None).Where(v => v.enabled).ToList();
        foreach (var v in vols) v.enabled = false;
        var ucd = go.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (ucd != null) ucd.renderPostProcessing = false;
        // 진짜 원인: 월드 셰이더(LofiPilot/Surface·WorldSurface…)가 머티리얼 _FogStart/_FogEnd(30~105m)로 카메라 거리 안개를 직접 칠한다 → 위에서 150m 면 전부 안개색
        var wm = MoodController.WorldMaterials(); var keep = wm.Select(x => (x, x.GetFloat("_FogStart"), x.GetFloat("_FogEnd"))).ToList();
        foreach (var x in wm) { x.SetFloat("_FogStart", 5000f); x.SetFloat("_FogEnd", 6000f); }
        Save(cam, a[2]); RenderSettings.fog = fog; foreach (var v in vols) v.enabled = true;
        foreach (var (x, s0, e0) in keep) { x.SetFloat("_FogStart", s0); x.SetFloat("_FogEnd", e0); } UnityEngine.Object.Destroy(go); done.Add(a[2]); idx++; return;
    }
    var rs = roads[a[0]]; var P = rs.centerline; int n = P.Length;
    // v4: s 칸이 "@x,z" 면 그 길 중심선에서 가장 가까운 점
    int i;
    if (a[1].StartsWith("@")) { var xz = a[1].Substring(1).Split(','); var q = new Vector2(float.Parse(xz[0]), float.Parse(xz[1])); i = Enumerable.Range(0, n).OrderBy(k => (new Vector2(P[k].x, P[k].z) - q).sqrMagnitude).First(); }
    else i = int.Parse(a[1]);
    i = Mathf.Clamp(i, 2, n - 3);
    var f = P[Mathf.Min(n - 1, i + 2)] - P[Mathf.Max(0, i - 2)]; f.y = 0; f.Normalize();
    var opts = a.Skip(3).ToList();
    if (opts.Contains("rev")) f = -f;
    var lk = opts.FirstOrDefault(o => o.StartsWith("look=")); if (lk != null) { var t2 = lk.Substring(5).Split(','); var d2 = new Vector3(float.Parse(t2[0]) - P[i].x, 0, float.Parse(t2[1]) - P[i].z); if (d2.sqrMagnitude > 1) f = d2.normalized; }
    var yo = opts.FirstOrDefault(o => o.StartsWith("yaw=")); if (yo != null) f = Quaternion.Euler(0, float.Parse(yo.Substring(4)), 0) * f;
    bool hud = opts.Contains("hud");
    // v4: eye = 사람 눈높이 1.6m 카메라(차 옆 side=m 만큼 오른쪽(+)/왼쪽(−) 인도 위, eyaw=도 만큼 시선 돌림)
    if (opts.Contains("eye") && wait == 89)
    {
        var so = opts.FirstOrDefault(o => o.StartsWith("side=")); float side = so != null ? float.Parse(so.Substring(5)) : 0f;
        var ey = opts.FirstOrDefault(o => o.StartsWith("eyaw=")); float eyaw = ey != null ? float.Parse(ey.Substring(5)) : 0f;
        var rt2 = Vector3.Cross(Vector3.up, f).normalized; var at = P[i] + rt2 * side - f * 4f;
        float gy = Physics.Raycast(at + Vector3.up * 30f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : P[i].y;
        var go = new GameObject("EyeCam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(Camera.main); cam.fieldOfView = 60f;
        go.transform.position = new Vector3(at.x, gy + 1.6f, at.z); go.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0, eyaw, 0) * f) * Quaternion.Euler(4f, 0, 0);
        Save(cam, a[2] + "_eye"); UnityEngine.Object.Destroy(go);
    }
    if (wait == 0)
    {
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.position = P[i] + Vector3.up * 0.5f; rb.rotation = Quaternion.LookRotation(f);
        car.transform.SetPositionAndRotation(rb.position, rb.rotation);
    }
    // 정지 상태로 카메라가 따라오게 0.4m/s 앞으로 밀어 준다(카메라가 속도 방향을 볼 때 대비)
    rb.linearVelocity = f * 0.4f;
    wait++;
    if (wait == 90) { Save(Camera.main, a[2]); done.Add(a[2] + "@" + P[i].ToString("F0")); if (!hud) { idx++; wait = 0; } else ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, a[2] + "_hud.png")); }
    if (hud && wait >= 150) { idx++; wait = 0; }   // ScreenCapture 는 다음 프레임 끝에 파일이 떨어진다
};
UnityEditor.EditorApplication.update += cb;
return "shooting " + list.Count;
