// 2053 (Play, l1548-place 뒤): Look 5b 캡처 → docs/qa/evidence/look5b. a_car·b_street·c_town 오후(look5) + 시간대 4종. Look 0 은 그대로라 look5-edc 것을 복사(공유 .mat 안 건드리게).
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/look5b"));
var oldDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/look5-edc"));
System.IO.Directory.CreateDirectory(outDir);
foreach (var f in new[] { "a_car_look0.png", "b_street_look0.png", "c_town_look0.png" }) System.IO.File.Copy(System.IO.Path.Combine(oldDir, f), System.IO.Path.Combine(outDir, f), true);
var c = UnityEngine.Object.FindFirstObjectByType<PCLookScene>();
var fcam = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var mc = fcam ? fcam.GetComponentInChildren<Camera>() : null; if (mc == null) mc = Camera.main; var log = new System.Text.StringBuilder();
var go = new GameObject("l5cam"); var cam = go.AddComponent<Camera>(); cam.enabled = false;
var texHi = new Texture2D(1440, 810, TextureFormat.RGB24, false);
var rtLo = new RenderTexture(480, 270, 24); rtLo.filterMode = FilterMode.Point; var texLo = new Texture2D(480, 270, TextureFormat.RGB24, false);
System.Func<Texture2D, float> Lum = t => { var px = t.GetPixels32(); double s = 0; for (int i = 0; i < px.Length; i += 3) s += .2126 * px[i].r + .7152 * px[i].g + .0722 * px[i].b; return (float)(s / (px.Length / 3.0)); };
System.Func<string, float> Grab = file =>
{
    c.ApplySky(cam);
    cam.targetTexture = rtLo; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rtLo; texLo.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); texLo.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file + "_raw480.png"), texLo.EncodeToPNG());
    var src = texLo.GetPixels32(); var dst = new Color32[1440 * 810];
    for (int y = 0; y < 810; y++) for (int x = 0; x < 1440; x++) dst[y * 1440 + x] = src[(y / 3) * 480 + x / 3];
    texHi.SetPixels32(dst); texHi.Apply();
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file + ".png"), texHi.EncodeToPNG());
    return Lum(texLo);
};
System.Func<Vector3, float> Gnd = v => Physics.Raycast(new Vector3(v.x, 300, v.z), Vector3.down, out var h, 600f) ? h.point.y : 0f;
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0).ToList();
var signs = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.Contains("Shop") || t.name.Contains("Awning") || t.name.Contains("Sign")).Select(t => t.position).ToList();
var shop = signs.OrderByDescending(p => signs.Count(q => (q - p).sqrMagnitude < 400f)).First();
var road = roads.OrderBy(r => r.centerline.Min(cc => (new Vector2(cc.x - shop.x, cc.z - shop.z)).sqrMagnitude)).First();
int ri = System.Array.IndexOf(road.centerline, road.centerline.OrderBy(cc => (new Vector2(cc.x - shop.x, cc.z - shop.z)).sqrMagnitude).First());
var rp = road.centerline[ri]; var rf = road.centerline[Mathf.Min(ri + 2, road.centerline.Length - 1)] - road.centerline[Mathf.Max(ri - 2, 0)]; rf.y = 0; rf.Normalize();
var toShop = shop - rp; toShop.y = 0; var nrm = Vector3.Cross(Vector3.up, rf); if (Vector3.Dot(nrm, toShop) < 0) nrm = -nrm;
var walk = rp + nrm * (road.width * .5f + 0.9f);
var pobj = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).ToList();
var gc = walk - nrm * 1.2f; gc.y = Gnd(gc);
var pb = gc - rf * 9f - nrm * 2.5f + Vector3.up * 1.7f;
var fw = gc - pb; fw.y = 0; fw.Normalize(); var rt2 = Vector3.Cross(Vector3.up, fw);
float[] pd = { 5f, 7.5f, 10f }; float[] po = { -1.2f, .9f, -.2f };
for (int k = 0; k < 3 && k < pobj.Count; k++) { var np = pb + fw * pd[k] + rt2 * po[k]; np.y = Gnd(np); pobj[k].transform.SetPositionAndRotation(np, Quaternion.LookRotation(k == 1 ? -fw : fw)); }
System.Action<string, bool> Scenes = (tag, all) =>
{
    c.PushGlobals();
    cam.CopyFrom(mc); cam.targetTexture = null; cam.transform.SetPositionAndRotation(mc.transform.position, mc.transform.rotation);
    log.Append(tag + " car L" + Grab("a_car_" + tag).ToString("F0") + " ");
    if (!all) return;
    cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(pb, Quaternion.LookRotation(gc + Vector3.up * .8f - pb)); log.Append("street L" + Grab("b_street_" + tag).ToString("F0") + " ");
    var tp = shop + new Vector3(-30, 75, -30); cam.transform.SetPositionAndRotation(tp, Quaternion.LookRotation(shop - tp)); log.Append("town L" + Grab("c_town_" + tag).ToString("F0") + " | ");
};
c.look = 5; c.Tick(); c.MarkRenderers(true);
log.Append(MoodSwitcher.Apply(MoodSwitcher.PathAfternoon) + " "); c.Tick(); Scenes("look5", true);
string[] tn = { "1morning", "2afternoon", "3sunset", "4dusk" };
for (int i = 0; i < 4; i++) { MoodSwitcher.Apply(MoodSwitcher.DayPaths[i]); c.Tick(); Scenes("t" + tn[i], i != 1); log.Append("fogP" + Shader.GetGlobalVector("_PCFogParams").ToString("F2") + " | "); }
MoodSwitcher.Apply(MoodSwitcher.PathAfternoon); c.Tick();
var lr = mc.GetComponent<LowResRenderTest>(); log.Append(lr != null ? $" mainLR={lr.RenderSize} int={lr.integerScale} screen={Screen.width}x{Screen.height}" : " mainLR=none");
UnityEngine.Object.DestroyImmediate(go); rtLo.Release();
return log.ToString();
