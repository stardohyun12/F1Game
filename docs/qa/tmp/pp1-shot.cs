// 2329 캡처: SessionState "pp.shots"="name:route:idx[:back],..." (경로 중심선 index, 차를 거기 두고 차 시점), "plan" = 위에서 본 평면(직교, x -280..290, z -235..200).
var spec = UnityEditor.SessionState.GetString("pp.shots", "plan").Split(',');
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/play01"));
System.IO.Directory.CreateDirectory(outDir);
UnityEditor.SessionState.SetString("pp.botId", "");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var lr = Camera.main.GetComponent<LowResRenderTest>();
var rt = new RenderTexture(960, 540, 24); var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
int k = 0, wait = 0; Camera planCam = null;
System.Action Place = () =>
{
    var s = spec[k].Split(':');
    if (s[0] == "plan")
    {
        var go = new GameObject("PP_PlanCam"); planCam = go.AddComponent<Camera>();
        planCam.orthographic = true; planCam.orthographicSize = 218; planCam.transform.position = new Vector3(5, 300, -17); planCam.transform.rotation = Quaternion.Euler(90, 0, 0);
        planCam.farClipPlane = 600; planCam.enabled = false; return;
    }
    var c = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == s[1] && r.kind == "route").centerline;
    int i = Mathf.Clamp(int.Parse(s[2]), 4, c.Length - 5);
    var f = c[i + 4] - c[i - 4]; f.y = 0; if (s.Length > 3) f = -f;
    rb.isKinematic = true; rb.position = c[i] + Vector3.up * 0.45f; rb.rotation = Quaternion.LookRotation(f.normalized);
    car.transform.SetPositionAndRotation(rb.position, rb.rotation);
};
Place();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (++wait < 60) return;
    var name = spec[k].Split(':')[0];
    if (planCam != null) { planCam.targetTexture = rt; planCam.Render(); planCam.targetTexture = null; UnityEngine.Object.Destroy(planCam.gameObject); planCam = null; }
    else {   /* lr.Present 는 평면 뒤 rt 를 안 덮어씀(1차 6장이 전부 평면) → 메인 카메라 직접 렌더. 순간이동 뒤 FollowCamera 가 못 따라오므로 B 자리(뒤 5.6 위 2.8, 앞 6m 0.6 높이 주시)로 직접 */
        var cf = car.transform.forward; cf.y = 0; cf.Normalize(); var cp = car.transform.position;
        Camera.main.transform.position = cp - cf * 5.6f + Vector3.up * 2.8f; Camera.main.transform.LookAt(cp + cf * 6f + Vector3.up * 0.6f); Camera.main.targetTexture = rt; Camera.main.Render(); Camera.main.targetTexture = null; }
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
    k++; wait = 0;
    if (k >= spec.Length) { UnityEditor.EditorApplication.update -= cb; rb.isKinematic = false; rt.Release(); UnityEditor.SessionState.SetString("pp.shotDone", "n=" + k); return; }
    Place();
};
UnityEditor.EditorApplication.update += cb;
return "shooting " + spec.Length + " lr=" + (lr != null);
