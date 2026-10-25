// 과제 0404: Play 중 — 언덕 찻집 4방향(hilltop-v0 와 같은 각도) + 끝 모를 길 3곳 + 탑다운. SessionState "h0404.tag" = before/after.
Application.runInBackground = true;
string tag = UnityEditor.SessionState.GetString("h0404.tag", "before");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/hilltop-v1/" + tag));
System.IO.Directory.CreateDirectory(outDir);
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
var tea = GameObject.Find("TUT_TeaHouse_NPC").transform.position;
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 40).ToArray();
RoadSurface br = null; int bi = 0; float bd = 1e9f;
foreach (var r in roads) for (int i = 30; i < r.centerline.Length - 30; i++) { var c = r.centerline[i]; float d = (new Vector2(c.x - tea.x, c.z - tea.z)).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } }
var P = br.centerline;
int from = P[bi - 30].y < P[bi + 30].y ? bi - 30 : bi + 30; int step = from < bi ? 1 : -1;
var pr = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.B);
var log = new System.Text.StringBuilder(tag + " road=" + br.roadId + "#" + bi + " tea=" + tea + " ");
fc.enabled = false; float farClip = cam.farClipPlane;
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release();
    log.Append(name + " cam(" + cp.x.ToString("F0") + "," + cp.y.ToString("F1") + "," + cp.z.ToString("F0") + ") ");
};
System.Func<Vector3[], int, int, Vector3> FwA = (Q, i, s) => { var f = Q[Mathf.Clamp(i + 2 * s, 0, Q.Length - 1)] - Q[Mathf.Clamp(i - 2 * s, 0, Q.Length - 1)]; f.y = 0; return f.normalized; };
System.Action<string, Vector3[], int, int> CamB = (name, Q, i, s) => { var p = Q[i]; var f = FwA(Q, i, s); Shot(name, p + Quaternion.LookRotation(f) * new Vector3(0, pr.height, -pr.distance), p + Vector3.up * pr.lookHeight + f * pr.lookAhead, pr.baseFov); };
CamB("1_approach", P, from, step);
CamB("2_arrival", P, bi, step);
var eye = P[bi] + Vector3.up * 1.7f; var seaDir = new Vector3(-0.74f, 0, -0.67f);
Shot("3_field_side", eye, eye - seaDir * 50f + Vector3.up * 2f, 60f);
Shot("4_sea_side", eye, eye + seaDir * 50f - Vector3.up * 6f, 60f);
// 끝 모를 길 3갈래(json outRoads): 끝에서 60m 앞 길 위 카메라 B, 끝 쪽을 본다
cam.farClipPlane = Mathf.Max(farClip, 2000f);
foreach (var id in new[] { "O_mountain", "O_breakwater", "O_bridge", "V_tunnel" })
{
    var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).FirstOrDefault(r => r.roadId == id && r.centerline != null && r.centerline.Length > 4);
    if (rs == null) { log.Append(id + "=없음 "); continue; }
    var Q = rs.centerline; int i0 = Mathf.Max(2, Q.Length - 1 - Mathf.Min(60, Q.Length / 2));
    CamB("5_end_" + id, Q, i0, 1);
}
// 탑다운: 고리 동쪽 고원 + 산길이 보이게 높이 360m 에서 비스듬히
Shot("6_topdown_east", new Vector3(60f, 360f, -60f), new Vector3(200f, 40f, 140f), 60f);
Shot("7_topdown", new Vector3(0f, 620f, 20f), new Vector3(0f, 0f, 30f), 60f);
cam.farClipPlane = farClip; fc.enabled = true;
return log.ToString();
