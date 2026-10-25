var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-cam"));
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 4).ToArray();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
var spots = new[] { new Vector2(-50, -141), new Vector2(70, -50), new Vector2(-163, 144) };
var names = new[] { "harbor", "uphill", "coast" };
var presets = new[] { CameraFeelSwitcher.A, CameraFeelSwitcher.B, CameraFeelSwitcher.C };
var log = new System.Text.StringBuilder();
fc.enabled = false; rb.isKinematic = true;
for (int s = 0; s < 3; s++)
{
    RoadSurface br = null; int bi = 0; float bd = 1e9f;
    foreach (var r in roads) for (int i = 2; i < r.centerline.Length - 2; i++) { var c = r.centerline[i]; float d = (new Vector2(c.x, c.z) - spots[s]).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } }
    var P = br.centerline; var p = P[bi]; var f = P[bi + 2] - P[bi - 2]; f.y = 0; f.Normalize();
    if (s == 1 && P[bi + 2].y < P[bi - 2].y) f = -f;
    car.transform.SetPositionAndRotation(p + Vector3.up * 0.35f, Quaternion.LookRotation(f)); rb.position = car.transform.position; rb.rotation = car.transform.rotation;
    log.Append(names[s] + "=" + br.roadId + "#" + bi + " (" + p.x.ToString("F0") + "," + p.y.ToString("F1") + "," + p.z.ToString("F0") + ") ");
    for (int k = 0; k < 3; k++)
    {
        var pr = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(presets[k]);
        var cp = p + Quaternion.LookRotation(f) * new Vector3(0, pr.height, -pr.distance);
        var look = p + Vector3.up * pr.lookHeight + f * pr.lookAhead;
        cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = pr.baseFov;
        var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
        RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "preset_" + names[s] + "_" + "ABC"[k] + ".png"), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release();
        if (s == 0) log.Append(" | " + "ABC"[k] + ": h" + pr.height + " d" + pr.distance + " lookH" + pr.lookHeight + " ahead" + pr.lookAhead + " fov" + pr.baseFov + "-" + pr.maxFov + " pitch=" + Quaternion.LookRotation(look - cp).eulerAngles.x.ToString("F1"));
    }
}
rb.isKinematic = false; fc.enabled = true;
return log.ToString();
