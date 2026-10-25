// 과제 0131: Play 중 — 언덕 찻집 4방향(다가옴·도착·들판 쪽·바다 쪽) + 걷는 사람 이동량
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/hilltop-v0"));
System.IO.Directory.CreateDirectory(outDir);
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
var tea = GameObject.Find("TUT_TeaHouse_NPC").transform.position;
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 40).ToArray();
RoadSurface br = null; int bi = 0; float bd = 1e9f;
foreach (var r in roads) for (int i = 30; i < r.centerline.Length - 30; i++) { var c = r.centerline[i]; float d = (new Vector2(c.x - tea.x, c.z - tea.z)).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } }
var P = br.centerline;
// 다가오는 방향: 30m 앞뒤 중 낮은 쪽에서 올라온다
int from = P[bi - 30].y < P[bi + 30].y ? bi - 30 : bi + 30; int step = from < bi ? 1 : -1;
var pr = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.B);
var log = new System.Text.StringBuilder("road=" + br.roadId + "#" + bi + " tea=" + tea + " ");
fc.enabled = false;
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release();
    log.Append(name + " cam(" + cp.x.ToString("F0") + "," + cp.y.ToString("F1") + "," + cp.z.ToString("F0") + ") ");
};
System.Func<int, Vector3> Fw = i => { var f = P[i + 2 * step] - P[i - 2 * step]; f.y = 0; return f.normalized; };
// 1 다가옴: 30m 아래 길 위 카메라 B
{ var p = P[from]; var f = Fw(from); Shot("1_approach", p + Quaternion.LookRotation(f) * new Vector3(0, pr.height, -pr.distance), p + Vector3.up * pr.lookHeight + f * pr.lookAhead, pr.baseFov); }
// 2 도착: 찻집 앞 길 위 카메라 B
{ var p = P[bi]; var f = Fw(bi); Shot("2_arrival", p + Quaternion.LookRotation(f) * new Vector3(0, pr.height, -pr.distance), p + Vector3.up * pr.lookHeight + f * pr.lookAhead, pr.baseFov); }
// 3·4 들판 쪽 / 바다 쪽: 찻집 앞 눈높이 1.7m, 바다(남서) · 반대(북동)
var eye = P[bi] + Vector3.up * 1.7f; var seaDir = new Vector3(-0.74f, 0, -0.67f);
Shot("3_field_side", eye, eye - seaDir * 50f + Vector3.up * 2f, 60f);
Shot("4_sea_side", eye, eye + seaDir * 50f - Vector3.up * 6f, 60f);
fc.enabled = true;
// 걷는 사람: 지금 위치 저장 — 다음 호출에서 이동량을 잰다
var peds = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var prev = UnityEditor.SessionState.GetString("c0131.ped", "");
var now = string.Join(";", peds.Select(p => p.name + "=" + p.transform.position.x.ToString("F2") + "," + p.transform.position.y.ToString("F2") + "," + p.transform.position.z.ToString("F2")));
UnityEditor.SessionState.SetString("c0131.ped", now);
log.Append("\nped " + peds.Length + " agent=" + peds.Count(p => p.GetComponent<UnityEngine.AI.NavMeshAgent>() != null) + "\nprev " + prev + "\nnow  " + now);
return log.ToString();
