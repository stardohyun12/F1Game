// 과제 0226: 에디트 모드 캡처 — 지형 비탈 2곳(해안 곶·언덕 밭) + 09 탑다운 + 항구 랜드마크. 태그 = SessionState i0226.tag (before/after)
var tag = UnityEditor.SessionState.GetString("f0344.tag", "after");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/fix-0342"));
System.IO.Directory.CreateDirectory(outDir);
var go = new GameObject("_i0226cam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.3f; cam.farClipPlane = 900f;
var main = Camera.main; if (main != null) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
var log = new System.Text.StringBuilder(tag + ": ");
System.Func<float, float, float> H = (x, z) => Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out var h, 800f) ? h.point.y : 0f;
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.orthographic = false; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); rt.Release();
    log.Append(name + " ");
};
// 비탈: 언덕(밭 배달지 5 근처) · 해안(곶 3·4 사이) — 비탈을 옆에서 내려다본다
{ float x = 128, z = 136; var t = new Vector3(x, H(x, z), z); var cp = t + new Vector3(-28f, 18f, -22f); Shot("slope_hill_" + tag, cp, t, 55f); }
{ float x = -100, z = 187; var t = new Vector3(x, H(x, z), z); var cp = t + new Vector3(30f, 20f, 8f); Shot("slope_coast_" + tag, cp, t + new Vector3(-20f, -8f, 0), 60f); }
// 바짝 붙은 1× 질감(픽셀 규칙 비교): 언덕 비탈 10m 앞
{ float x = 110, z = 150; var t = new Vector3(x, H(x, z), z); Shot("slope_close_" + tag, t + new Vector3(-8f, 5f, -6f), t, 50f); }
if (false)
{
    // 09 탑다운(맵 전체, 직교)
    cam.orthographic = true; cam.orthographicSize = 250f; cam.transform.SetPositionAndRotation(new Vector3(0, 400f, 20f), Quaternion.Euler(90f, 0, 0));
    { var rt = new RenderTexture(1600, 1600, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt; var tex = new Texture2D(1600, 1600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1600), 0, 0); tex.Apply(); RenderTexture.active = null; System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "09_topdown.png"), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); rt.Release(); log.Append("09_topdown "); }
    // 랜드마크: 각 LANDMARK_* 앞 길에서 (모델이 있는 넷)
    foreach (var lm in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("LANDMARK_")).OrderBy(t => t.name))
    {
        var r = lm.GetComponentsInChildren<Renderer>(); if (r.Length == 0) continue;
        var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds);
        string nm = lm.name.Replace("LANDMARK_", "");
        bool model = lm.GetComponentsInChildren<Transform>().Any(t => t.name.EndsWith("_mesh"));
        log.Append($"\n  {lm.name} 모델={model} 위치({lm.position.x:F0},{lm.position.y:F1},{lm.position.z:F0}) 크기{b.size.x:F1}x{b.size.y:F1}x{b.size.z:F1}");
        if (!model) continue;
        var f = lm.forward; var cp = b.center + f * (b.size.y * 1.3f + 8f) + Vector3.up * 2f; cp.y = Mathf.Max(cp.y, H(cp.x, cp.z) + 1.8f);
        Shot("landmark_" + (nm.Contains("등대") ? "lighthouse" : nm.Contains("크레인") ? "crane" : nm.Contains("종탑") ? "chapel" : nm.Contains("어시장") ? "fishmarket" : nm.GetHashCode().ToString("x")), cp, b.center, 55f);
    }
    // 찻집(에디트 모드 정지 상태)
    var th = GameObject.Find("TUT_TeaHouse");
    if (th != null) { var t = th.transform; Shot("teahouse_front_edit", t.position + t.forward * 13f + t.right * 3f + Vector3.up * 3.2f, t.position + Vector3.up * 2.2f - t.forward * 1.5f, 50f); log.Append($"\n  찻집 ({t.position.x:F1},{t.position.y:F1},{t.position.z:F1}) 앞 {t.forward.x:F2},{t.forward.z:F2}"); }
}
UnityEngine.Object.DestroyImmediate(go);
return log.ToString();
