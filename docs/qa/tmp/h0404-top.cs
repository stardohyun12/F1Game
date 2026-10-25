// 과제 0404: 안개 없이 탑다운(머티리얼 _FogStart/_FogEnd 를 잠깐 멀리 → 되돌림). SessionState "h0404.tag".
string tag = UnityEditor.SessionState.GetString("h0404.tag", "before");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/hilltop-v1/" + tag));
System.IO.Directory.CreateDirectory(outDir);
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
var mats = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.HasProperty("_FogStart")).Distinct().ToList();
var saved = mats.Select(m => (m, m.GetFloat("_FogStart"), m.GetFloat("_FogEnd"))).ToList();
bool rf = RenderSettings.fog; RenderSettings.fog = false;
foreach (var m in mats) { m.SetFloat("_FogStart", 5000f); m.SetFloat("_FogEnd", 6000f); }
fc.enabled = false; float farClip = cam.farClipPlane; cam.farClipPlane = 3000f;
var log = new System.Text.StringBuilder();
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp, Vector3.forward)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release();
    log.Append(name + " ");
};
try
{
    Shot("7_topdown", new Vector3(0f, 700f, 20f), new Vector3(0f, 0f, 20.1f), 60f);          // 위 = +z(북), 오른쪽 = +x(동)
    Shot("6_topdown_east", new Vector3(180f, 520f, 160f), new Vector3(180f, 0f, 160.1f), 60f);
}
finally
{
    foreach (var (m, s, e) in saved) { m.SetFloat("_FogStart", s); m.SetFloat("_FogEnd", e); }
    RenderSettings.fog = rf; cam.farClipPlane = farClip; fc.enabled = true;
}
return log + " mats=" + mats.Count;
