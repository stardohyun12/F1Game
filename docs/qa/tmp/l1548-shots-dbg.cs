// 1548 (Play, l1548-place 뒤): 장면 3개 × 그림체 0~3. look-3way/<장면>_<안>.png (1280×720). 끝에 원래 안으로 되돌림.
int ln=0;
try {
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/look-3way")); ln=2;
System.IO.Directory.CreateDirectory(outDir); ln=3;
var fcam = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var mc = fcam ? fcam.GetComponentInChildren<Camera>() : null; if (mc == null) mc = Camera.allCameras.First(c => c.enabled && c.targetTexture == null); var log = new System.Text.StringBuilder(); ln=4;
var go = new GameObject("l1548cam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(mc); cam.enabled = false; ln=5;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); ln=6;
System.Action<Camera, string> Grab = (c, file) =>
{
    var keep = c.targetTexture; c.targetTexture = rt; c.Render(); c.targetTexture = keep;
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), tex.EncodeToPNG());
}; ln=12;
// (b) 상점가 인도: 반경 9m 안 다른 행인이 가장 많은 행인 무리의 가운데를 차도 쪽에서
var peds = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).Select(p => p.transform.position).ToList(); ln=14;
var center = peds.OrderByDescending(p => peds.Count(q => (q - p).sqrMagnitude < 81f)).First(); ln=15;
var grp = peds.Where(q => (q - center).sqrMagnitude < 81f).ToList(); var gc = grp.Aggregate(Vector3.zero, (a, b) => a + b) / grp.Count + Vector3.up * 0.9f; ln=16;
var cl = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0).SelectMany(r => r.centerline).OrderBy(c => (new Vector2(c.x - gc.x, c.z - gc.z)).sqrMagnitude).First(); ln=17;
var away = cl - gc; away.y = 0; away.Normalize(); var along = Vector3.Cross(Vector3.up, away); ln=18;
var pb = gc + away * 7.5f + along * 3.5f + Vector3.up * 1.4f; ln=19;
log.Append("peds " + peds.Count + " group " + grp.Count + " at " + gc.ToString("F0") + " | "); ln=20;
// (c) 위에서 본 해안 비탈 = c1514 1_top_straight 와 같은 자리
var P = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First().centerline; ln=22;
int i0 = 1240; var p0 = P[i0]; var f0 = P[i0 + 3] - P[i0 - 3]; f0.y = 0; f0.Normalize(); var nL = new Vector3(-f0.z, 0, f0.x); ln=23;
Vector3 sea = Vector3.zero; float lo = 999; for (int a = 0; a < 16; a++) { var d = Quaternion.Euler(0, a * 22.5f, 0) * Vector3.forward; float h = BuildBayLoop.H(p0.x + d.x * 30, p0.z + d.z * 30); if (h < lo) { lo = h; sea = d; } }
var s0 = Vector3.Dot(sea, nL) > 0 ? nL : -nL; ln=25;
float keepMode = Shader.GetGlobalFloat("_PCLook"); ln=26;
for (int m = 0; m <= 3; m++)
{
    Shader.SetGlobalFloat("_PCLook", m);
    Grab(mc, "a_car_" + m + ".png");
    cam.fieldOfView = 50f; cam.transform.SetPositionAndRotation(pb, Quaternion.LookRotation(gc - pb)); Grab(cam, "b_street_" + m + ".png");
    cam.fieldOfView = 55f; var cp = p0 + s0 * 34 + Vector3.up * 26; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(p0 + s0 * 9 - Vector3.up * 4 - cp)); Grab(cam, "c_coast_" + m + ".png");
}
Shader.SetGlobalFloat("_PCLook", keepMode); ln=34;
UnityEngine.Object.DestroyImmediate(go); rt.Release(); ln=35;
return log.Append("ok 12 shots, look restored " + keepMode).ToString();

} catch (System.Exception e) { return "line " + ln + " " + e.Message; }