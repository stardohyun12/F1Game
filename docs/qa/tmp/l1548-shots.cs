// 1548 (Play, l1548-place 뒤): 장면 3개 × 그림체 0~3. look-3way/<장면>_<안>.png (1280×720). 끝에 원래 안으로 되돌림.
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/look-3way"));
System.IO.Directory.CreateDirectory(outDir);
var fcam = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var mc = fcam ? fcam.GetComponentInChildren<Camera>() : null; if (mc == null) mc = Camera.allCameras.First(c => c.enabled && c.targetTexture == null); var log = new System.Text.StringBuilder();
var go = new GameObject("l1548cam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(mc); cam.enabled = false;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
System.Action<Camera, string> Grab = (c, file) =>
{
    var keep = c.targetTexture; c.targetTexture = rt; c.Render(); c.targetTexture = keep;
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), tex.EncodeToPNG());
};
// (b) 상점가 인도: 간판·차양이 가장 많이 모인 곳(반경 20m) 앞 인도에 행인 3명을 세우고 길 건너편에서
var signs = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.Contains("Shop") || t.name.Contains("Awning") || t.name.Contains("Sign")).Select(t => t.position).ToList();
var shop = signs.OrderByDescending(p => signs.Count(q => (q - p).sqrMagnitude < 400f)).First();
var road = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0).OrderBy(r => r.centerline.Min(c => (new Vector2(c.x - shop.x, c.z - shop.z)).sqrMagnitude)).First();
int ri = System.Array.IndexOf(road.centerline, road.centerline.OrderBy(c => (new Vector2(c.x - shop.x, c.z - shop.z)).sqrMagnitude).First());
var rp = road.centerline[ri]; var rf = road.centerline[Mathf.Min(ri + 2, road.centerline.Length - 1)] - road.centerline[Mathf.Max(ri - 2, 0)]; rf.y = 0; rf.Normalize();
var toShop = shop - rp; toShop.y = 0; var nrm = Vector3.Cross(Vector3.up, rf); if (Vector3.Dot(nrm, toShop) < 0) nrm = -nrm;
var walk = rp + nrm * (road.width * .5f + 0.9f);
var pobj = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).ToList();
var gc = walk - nrm * 1.2f; gc.y = Physics.Raycast(gc + Vector3.up * 3, Vector3.down, out var g0, 8f) ? g0.point.y : gc.y;
var pb = gc - rf * 9f - nrm * 2.5f + Vector3.up * 1.7f; log.Append("cam " + pb.ToString("F1") + " ");
var fw = gc - pb; fw.y = 0; fw.Normalize(); var rt2 = Vector3.Cross(Vector3.up, fw);
float[] pd = { 5f, 7.5f, 10f }; float[] po = { -1.2f, .9f, -.2f };
for (int k = 0; k < 3 && k < pobj.Count; k++)
{
    var np = pb + fw * pd[k] + rt2 * po[k];
    np.y = Physics.Raycast(np + Vector3.up * 1f, Vector3.down, out var gh, 6f) ? gh.point.y : np.y; log.Append("ped" + k + " " + np.ToString("F1") + " ");
    pobj[k].transform.SetPositionAndRotation(np, Quaternion.LookRotation(k == 1 ? -fw : fw));
}
log.Append("shop " + shop.ToString("F0") + " road " + road.roadId + "#" + ri + " signs20 " + signs.Count(q => (q - shop).sqrMagnitude < 400f) + " | ");
// (c) 위에서 본 해안 비탈 = c1514 1_top_straight 와 같은 자리
var P = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First().centerline;
int i0 = 1240; var p0 = P[i0]; var f0 = P[i0 + 3] - P[i0 - 3]; f0.y = 0; f0.Normalize(); var nL = new Vector3(-f0.z, 0, f0.x);
Vector3 sea = Vector3.zero; float lo = 999; for (int a = 0; a < 16; a++) { var d = Quaternion.Euler(0, a * 22.5f, 0) * Vector3.forward; float h = Physics.Raycast(p0 + d * 30 + Vector3.up * 200, Vector3.down, out var hh, 400f) ? hh.point.y : -50f; if (h < lo) { lo = h; sea = d; } }
var s0 = Vector3.Dot(sea, nL) > 0 ? nL : -nL;
float keepMode = Shader.GetGlobalFloat("_PCLook");
for (int m = 0; m <= 3; m++)
{
    Shader.SetGlobalFloat("_PCLook", m);
    Grab(mc, "a_car_" + m + ".png");
    cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(pb, Quaternion.LookRotation(gc + Vector3.up * 0.8f - pb)); Grab(cam, "b_street_" + m + ".png");
    cam.fieldOfView = 55f; var cp = p0 + s0 * 34 + Vector3.up * 26; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(p0 + s0 * 9 - Vector3.up * 4 - cp)); Grab(cam, "c_coast_" + m + ".png");
}
Shader.SetGlobalFloat("_PCLook", keepMode);
UnityEngine.Object.DestroyImmediate(go); rt.Release();
return log.Append("ok 12 shots, look restored " + keepMode).ToString();
