var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bayloop-cam"));
var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
var names = new[] { "TUT_Garage_PitCrew", "TUT_PackingSpot", "TUT_TeaHouse_NPC", "TUT_CoastStraightEnd_Sign" };
var offs = new[] { new Vector3(4, 3, 16), new Vector3(3, 2.2f, -8), new Vector3(3, 2.5f, 8), Vector3.zero };
var log = new System.Text.StringBuilder();
for (int k = 0; k < 4; k++)
{
    var g = GameObject.Find(names[k]); if (g == null) { log.Append(names[k] + " MISSING "); continue; }
    var t = g.transform.position; Vector3 cp;
    if (k == 3) { var b = g.transform.Find("Board"); var f = b.forward; cp = t - f * 18f + Vector3.up * 3f - b.right * 3.5f; }
    else cp = t + offs[k];
    cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(t + Vector3.up * 1.5f - cp));
    var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
    var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "tut_" + k + ".png"), tex.EncodeToPNG()); rt.Release();
    log.Append(names[k] + " " + t.ToString("F0") + " ");
}
cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
return log.ToString();
