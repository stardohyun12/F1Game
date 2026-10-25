// 과제 0032: 차 외형 A/C 를 마을 2지점에서 실제 렌더. 끝나면 현재 차 + 원래 자리로 되돌려 저장.
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/opening-v0"));
var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>().transform;
var cp0 = car.position; var cr0 = car.rotation;
var all = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 0).ToArray();
var main = all.First(r => r.roadId == "main").centerline; var vk = all.First(r => r.roadId == "V_kyoto_back").centerline;
var spots = new[] { new { name = "harbor", a = main[40], b = main[44] }, new { name = "hill", a = vk[100], b = vk[104] } };
var sb = new System.Text.StringBuilder();
foreach (var which in new[] { "Current", "A", "C" })
{
    sb.Append(CarVisualSwitcher.Apply(which == "Current" ? null : which) + "\n");
    foreach (var s in spots)
    {
        var f = s.b - s.a; f.y = 0; f.Normalize(); var right = Vector3.Cross(Vector3.up, f);
        RaycastHit h; var pos = s.a; if (Physics.Raycast(s.a + Vector3.up * 3f, Vector3.down, out h, 8f)) pos.y = h.point.y;
        car.SetPositionAndRotation(pos + Vector3.up * 0.0f, Quaternion.LookRotation(f));
        var cpos = pos + right * 3.6f + f * 3.2f + Vector3.up * 1.6f;
        cam.transform.SetPositionAndRotation(cpos, Quaternion.LookRotation(pos + Vector3.up * 0.5f - cpos)); cam.fieldOfView = 45f;
        var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "car_" + which + "_" + s.name + ".png"), tex.EncodeToPNG()); rt.Release();
    }
}
car.SetPositionAndRotation(cp0, cr0);
cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
sb.Append(CarVisualSwitcher.Apply(null));
return sb.ToString();
