// 과제 0404: Play 중 — 본선 꼭대기 접근·도착(카메라 B) + 홀로 선 집 쪽 + 풀잎 근접. SessionState "h0404.tag".
Application.runInBackground = true;
string tag = UnityEditor.SessionState.GetString("h0404.tag", "before");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/hilltop-v1/" + tag));
System.IO.Directory.CreateDirectory(outDir);
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "V_detour" && r.centerline != null && r.centerline.Length > 20);
var P = main.centerline; int ci = 0; for (int i = 1; i < P.Length; i++) if (P[i].y > P[ci].y) ci = i;
var pr = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.B);
var log = new System.Text.StringBuilder(tag + " crest#" + ci + P[ci] + " ");
fc.enabled = false;
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release();
    log.Append(name + " ");
};
System.Func<int, Vector3> Fw = i => { var f = P[Mathf.Min(i + 2, P.Length - 1)] - P[Mathf.Max(i - 2, 0)]; f.y = 0; return f.normalized; };
System.Action<string, int> CamB = (name, i) => { var p = P[i]; var f = Fw(i); Shot(name, p + Quaternion.LookRotation(f) * new Vector3(0, pr.height, -pr.distance), p + Vector3.up * pr.lookHeight + f * pr.lookAhead, pr.baseFov); };
CamB("8_crest_approach", Mathf.Max(0, ci - 60));
CamB("8_crest_arrival", ci);
CamB("8_crest_over", Mathf.Min(P.Length - 1, ci + 15));
var lh = GameObject.Find("LoneHouse");
if (lh != null)
{
    var eye = P[ci] + Vector3.up * 1.7f; var h = lh.transform.GetChild(0).position;
    Shot("8_crest_lonehouse", eye, h + Vector3.up * 3f, 55f);
    log.Append("house" + h + " ");
}
var eye2 = P[ci] + Vector3.up * 1.2f + Vector3.Cross(Fw(ci), Vector3.up) * 6f;
Shot("8_grass_close", eye2, eye2 + Vector3.Cross(Fw(ci), Vector3.up) * 20f - Vector3.up * 2f, 60f);
fc.enabled = true;
return log.ToString();
