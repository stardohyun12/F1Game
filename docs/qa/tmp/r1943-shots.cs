// 1943 (Play, l1548-place 뒤): retro-v1 캡처. 1) Look 0/4 × 장면 6 (지금 씬 빛) 2) 옛 시간대 4 → 새 시간대 4 평균 휘도 3) 새 오후·노을 × 차/상점가/위
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/retro-v1"));
System.IO.Directory.CreateDirectory(outDir);
var fcam = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var mc = fcam ? fcam.GetComponentInChildren<Camera>() : null; if (mc == null) mc = Camera.allCameras.First(c => c.enabled && c.targetTexture == null); var log = new System.Text.StringBuilder();
var go = new GameObject("r1943cam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(mc); cam.enabled = false;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
System.Func<Camera, string, float> Grab = (c, file) =>
{
    var keep = c.targetTexture; c.targetTexture = rt; c.Render(); c.targetTexture = keep;
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), tex.EncodeToPNG());
    var px = tex.GetPixels32(); double s = 0; for (int i = 0; i < px.Length; i += 7) s += .2126 * px[i].r + .7152 * px[i].g + .0722 * px[i].b;
    return (float)(s / (px.Length / 7.0));
};
System.Func<Vector3, float> Gnd = v => Physics.Raycast(new Vector3(v.x, 300, v.z), Vector3.down, out var h, 600f) ? h.point.y : 0f;
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0).ToList();
// 상점가(l1548 b 와 같은 방식)
var signs = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.Contains("Shop") || t.name.Contains("Awning") || t.name.Contains("Sign")).Select(t => t.position).ToList();
var shop = signs.OrderByDescending(p => signs.Count(q => (q - p).sqrMagnitude < 400f)).First();
var road = roads.OrderBy(r => r.centerline.Min(c => (new Vector2(c.x - shop.x, c.z - shop.z)).sqrMagnitude)).First();
int ri = System.Array.IndexOf(road.centerline, road.centerline.OrderBy(c => (new Vector2(c.x - shop.x, c.z - shop.z)).sqrMagnitude).First());
var rp = road.centerline[ri]; var rf = road.centerline[Mathf.Min(ri + 2, road.centerline.Length - 1)] - road.centerline[Mathf.Max(ri - 2, 0)]; rf.y = 0; rf.Normalize();
var toShop = shop - rp; toShop.y = 0; var nrm = Vector3.Cross(Vector3.up, rf); if (Vector3.Dot(nrm, toShop) < 0) nrm = -nrm;
var walk = rp + nrm * (road.width * .5f + 0.9f);
var pobj = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).ToList();
var gc = walk - nrm * 1.2f; gc.y = Gnd(gc);
var pb = gc - rf * 9f - nrm * 2.5f + Vector3.up * 1.7f;
var fw = gc - pb; fw.y = 0; fw.Normalize(); var rt2 = Vector3.Cross(Vector3.up, fw);
float[] pd = { 5f, 7.5f, 10f }; float[] po = { -1.2f, .9f, -.2f };
for (int k = 0; k < 3 && k < pobj.Count; k++) { var np = pb + fw * pd[k] + rt2 * po[k]; np.y = Gnd(np); pobj[k].transform.SetPositionAndRotation(np, Quaternion.LookRotation(k == 1 ? -fw : fw)); }
// 길가 깎은 면: 길 중심에서 옆 8m 지형이 5m 넘게 높은 곳(본선 우선)
Vector3 cutP = Vector3.zero, cutN = Vector3.zero; float best = 0;
foreach (var r in roads.OrderBy(r => r.roadId == "main" ? 0 : 1))
    for (int i = 4; i < r.centerline.Length - 4; i += 6)
    {
        var p = r.centerline[i]; var f = r.centerline[i + 3] - r.centerline[i - 3]; f.y = 0; f.Normalize(); var n = new Vector3(-f.z, 0, f.x);
        foreach (var sgn in new[] { 1f, -1f }) { var q = p + n * sgn * (r.width * .5f + 6f); float dh = Gnd(q) - p.y; if (dh > best && dh < 12f) { best = dh; cutP = p; cutN = n * sgn; } }
    }
log.Append("cut " + cutP.ToString("F0") + " dh " + best.ToString("F1") + " | ");
// 풀 비탈: 언덕 꼭대기(V_detour#65) 근처 낮은 쪽
var crest = new Vector3(55, 44.3f, 148);
Vector3 down = Vector3.forward; float lo = 999; for (int a = 0; a < 8; a++) { var d = Quaternion.Euler(0, a * 45, 0) * Vector3.forward; float h = Gnd(crest + d * 30); if (h < lo) { lo = h; down = d; } }
System.Action<string, int> Scenes = (tag, look) =>
{
    var keepT = mc.transform.position;
    float l = Grab(mc, "a_car_" + tag + ".png"); log.Append(tag + " car L" + l.ToString("F0") + " ");
    cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(pb, Quaternion.LookRotation(gc + Vector3.up * .8f - pb)); Grab(cam, "b_street_" + tag + ".png");
    var tp = shop + new Vector3(-30, 75, -30); cam.transform.SetPositionAndRotation(tp, Quaternion.LookRotation(shop - tp)); Grab(cam, "c_town_" + tag + ".png");
    if (look < 0) return;
    var pp = shop + nrm * 4 + Vector3.up * 24 - rf * 10; cam.transform.SetPositionAndRotation(pp, Quaternion.LookRotation(shop + nrm * 6 - pp)); Grab(cam, "d_parcel_" + tag + ".png");
    var cp = cutP - cutN * 7 + Vector3.up * 6; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(cutP + cutN * 10 + Vector3.up * 2 - cp)); Grab(cam, "e_cut_" + tag + ".png");
    var gp = crest + Vector3.up * 5 - down * 4; cam.transform.SetPositionAndRotation(gp, Quaternion.LookRotation(crest + down * 25 + Vector3.up * (lo - crest.y) * .8f - gp)); Grab(cam, "f_grass_" + tag + ".png");
};
// 1) 그림체 전/후 (지금 씬 빛)
PitCrewLookMenu.Set(0); Scenes("look0", 0);
PitCrewLookMenu.Set(4); Scenes("look4", 4);
// 2) 옛 시간대 4종 (에셋 값 그대로) → 새 값으로 다시 만들고 4종
string[] tn = { "1morning", "2afternoon", "3sunset", "4dusk" };
for (int i = 0; i < 4; i++) { MoodSwitcher.Apply(MoodSwitcher.DayPaths[i]); log.Append("old " + tn[i] + " L" + Grab(mc, "m_old_" + tn[i] + ".png").ToString("F0") + " "); }
MoodSwitcher.BuildProfiles(true);
for (int i = 0; i < 4; i++) { MoodSwitcher.Apply(MoodSwitcher.DayPaths[i]); log.Append("new " + tn[i] + " L" + Grab(mc, "m_new_" + tn[i] + ".png").ToString("F0") + " "); if (i == 1 || i == 2) Scenes(tn[i], -1); }
log.Append(MoodSwitcher.Revert());
UnityEngine.Object.DestroyImmediate(go); rt.Release();
return log.ToString();
