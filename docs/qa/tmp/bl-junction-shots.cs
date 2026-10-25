var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-cam"));
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 3).ToArray();
var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(outDir, "junction_audit.csv")).Skip(1).ToList();
var extra = UnityEditor.SessionState.GetString("jx.extra", "");
var picks = new List<(string label, Vector3 p)>();
foreach (var e in extra.Split(';')) { if (e.Length < 3) continue; var a = e.Split(','); picks.Add((a[0], new Vector3(float.Parse(a[1]), float.Parse(a[2]), float.Parse(a[3])))); }
foreach (var l in lines) { var a = l.Split(','); var p = new Vector3(float.Parse(a[2]), float.Parse(a[3]), float.Parse(a[4])); if (picks.Count >= 10) break; if (picks.Any(q => (q.p - p).magnitude < 15f)) continue; picks.Add((a[0] + "_" + a[1], p)); }
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>(); fc.enabled = false;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>(); rb.isKinematic = true;
float fov0 = cam.fieldOfView; var log = new System.Text.StringBuilder();
System.Action<string> Save = name => { var rt = new RenderTexture(640, 360, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt; var tex = new Texture2D(640, 360, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = null; System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release(); };
for (int k = 0; k < picks.Count; k++)
{
    var p = picks[k].p; RoadSurface br = null; int bi = 0; float bd = 1e9f;
    foreach (var r in roads) for (int i = 0; i < r.centerline.Length; i++) { float d = (r.centerline[i] - p).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } }
    var P = br.centerline; int i0 = Mathf.Clamp(bi, 6, P.Length - 7); var f = P[i0 + 2] - P[i0 - 2]; f.y = 0; f.Normalize();
    if (bi > P.Length / 2 && !br.closed) f = -f;
    var back = P[Mathf.Clamp(bi + (f == (P[i0 + 2] - P[i0 - 2]).normalized ? -9 : 9), 0, P.Length - 1)];
    car.transform.SetPositionAndRotation(back + Vector3.up * 0.35f, Quaternion.LookRotation(f));
    cam.fieldOfView = 50f; cam.transform.SetPositionAndRotation(p + Vector3.up * 40f - f * 6f, Quaternion.LookRotation(p - (p + Vector3.up * 40f - f * 6f)));
    Save("jx" + k + "_top.png");
    var cp = back - f * 5.6f + Vector3.up * 2.8f; cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(p + Vector3.up * 0.5f - cp));
    Save("jx" + k + "_car.png");
    log.Append("jx" + k + " " + picks[k].label + " " + p.ToString("F0") + " on " + br.roadId + "#" + bi + "\n");
}
cam.fieldOfView = fov0; rb.isKinematic = false; fc.enabled = true;
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "junction_top10.txt"), log.ToString());
return log.ToString();
