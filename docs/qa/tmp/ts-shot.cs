// 2124 (Play, 런 준비 뒤): 톤 기준 장면 캡처. SessionState "ts.idx"="1140,1330" (main 중심선 index, 진행 방향=차고 쪽), "ts.out"=하위폴더, "ts.pre"=파일 접두.
// 차를 그 점에 놓고 멈춰 세운 뒤 60프레임(카메라 따라붙기) 기다렸다가 화면과 같은 확대(lr.Present)로 1440×810 저장.
var idxs = UnityEditor.SessionState.GetString("ts.idx", "1140").Split(',').Select(int.Parse).ToArray();
var pre = UnityEditor.SessionState.GetString("ts.pre", "cand");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/tone-slice/" + UnityEditor.SessionState.GetString("ts.out", "")));
System.IO.Directory.CreateDirectory(outDir);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var lr = Camera.main.GetComponent<LowResRenderTest>();
var rt = new RenderTexture(1440, 810, 0); var tex = new Texture2D(1440, 810, TextureFormat.RGB24, false);
int k = 0, wait = 0;
System.Action Place = () =>
{
    var c = main.centerline; int i = idxs[k];
    var p = c[i]; var f = c[Mathf.Min(i + 6, c.Length - 1)] - c[Mathf.Max(i - 6, 0)]; f.y = 0;
    rb.isKinematic = true;
    rb.position = p + Vector3.up * 0.45f; rb.rotation = Quaternion.LookRotation(f.normalized);
    car.transform.SetPositionAndRotation(rb.position, rb.rotation);
};
Place();
UnityEditor.SessionState.SetString("ts.done", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (++wait < 60) return;
    lr.Present(rt);
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, pre + "_" + idxs[k] + ".png"), tex.EncodeToPNG());
    k++; wait = 0;
    if (k >= idxs.Length) { UnityEditor.EditorApplication.update -= cb; rb.isKinematic = false; rt.Release(); UnityEditor.SessionState.SetString("ts.done", pre + " n=" + k); return; }
    Place();
};
UnityEditor.EditorApplication.update += cb;
return "shooting " + string.Join(",", idxs);
