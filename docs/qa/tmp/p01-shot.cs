// 2306: 장소 01 캡처. SessionState "p01.shots"="name:idx[:r],..." (P01_switchback 중심선 index, r=거꾸로 봄), "p01.pre"=접두. 960×540. "plan" = 위에서 본 평면(직교).
var spec = UnityEditor.SessionState.GetString("p01.shots", "entry:12").Split(',');
var pre = UnityEditor.SessionState.GetString("p01.pre", "afternoon");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/place01"));
System.IO.Directory.CreateDirectory(outDir);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var road = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "P01_switchback");
var lr = Camera.main.GetComponent<LowResRenderTest>();
var rt = new RenderTexture(960, 540, 24); var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
int k = 0, wait = 0; Camera planCam = null;
System.Action Place = () =>
{
    var s = spec[k].Split(':');
    if (s[0] == "plan")
    {
        var go = new GameObject("P01_PlanCam"); planCam = go.AddComponent<Camera>();
        planCam.orthographic = true; planCam.orthographicSize = 34; planCam.transform.position = new Vector3(8, 160, 196); planCam.transform.rotation = Quaternion.Euler(90, 0, 0);
        planCam.farClipPlane = 400; planCam.enabled = false; return;
    }
    var c = road.centerline; int i = int.Parse(s[1]); bool rev = s.Length > 2 && s[2] == "r";
    var p = c[i]; var f = c[Mathf.Min(i + 4, c.Length - 1)] - c[Mathf.Max(i - 4, 0)]; f.y = 0; if (rev) f = -f;
    rb.isKinematic = true;
    rb.position = p + Vector3.up * 0.45f; rb.rotation = Quaternion.LookRotation(f.normalized);
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
    else lr.Present(rt);
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, pre + "_" + name + ".png"), tex.EncodeToPNG());
    k++; wait = 0;
    if (k >= spec.Length) { UnityEditor.EditorApplication.update -= cb; rb.isKinematic = false; rt.Release(); UnityEditor.SessionState.SetString("p01.done", pre + " n=" + k); return; }
    Place();
};
UnityEditor.EditorApplication.update += cb;
return "shooting " + spec.Length;
