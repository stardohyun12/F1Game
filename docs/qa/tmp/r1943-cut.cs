var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/retro-v1"));
var mc = Camera.allCameras.First(c => c.enabled && c.targetTexture == null); var log = new System.Text.StringBuilder();
var go = new GameObject("r1943cam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(mc); cam.enabled = false; cam.fieldOfView = 55f;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
System.Action<string> Grab = file => { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), tex.EncodeToPNG()); };
System.Func<Vector3, float> Ter = v => { var hs = Physics.RaycastAll(new Vector3(v.x, 300, v.z), Vector3.down, 600f).Where(h => h.collider.name.Contains("Terrain")).ToList(); return hs.Count > 0 ? hs.Max(h => h.point.y) : -99f; };
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.width > 0).ToList();
var found = new List<(Vector3 p, Vector3 n, float dh)>();
foreach (var r in roads)
    for (int i = 4; i < r.centerline.Length - 4; i += 5)
    {
        var p = r.centerline[i]; var f = r.centerline[i + 3] - r.centerline[i - 3]; f.y = 0; f.Normalize(); var n = new Vector3(-f.z, 0, f.x);
        foreach (var sgn in new[] { 1f, -1f })
        {
            var q = p + n * sgn * (r.width * .5f + 7f); float dh = Ter(q) - p.y;
            if (dh > 5f && dh < 16f && true) found.Add((p, n * sgn, dh));
        }
    }
log.Append("cut cands " + found.Count + " | ");
int k = 0;
foreach (var c in found.OrderByDescending(c => c.dh).Where((c, i) => i % Mathf.Max(1, found.Count / 3) == 0).Take(3))
{
    var cp = c.p - c.n * 4 + Vector3.up * 9; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(c.p + c.n * 12 + Vector3.up * (c.dh * .5f) - cp));
    PitCrewLookMenu.Set(0); Grab("e_cut" + k + "_look0.png"); PitCrewLookMenu.Set(4); Grab("e_cut" + k + "_look4.png");
    log.Append(k + ":" + c.p.ToString("F0") + " dh" + c.dh.ToString("F1") + " "); k++;
}
UnityEngine.Object.DestroyImmediate(go); rt.Release();
return log.ToString();
