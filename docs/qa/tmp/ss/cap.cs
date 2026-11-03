// 과제 1428: 카트 추격 카메라(CameraFeel_B_HighRoad: 높이 2.8·거리 5.6·보는 높이 0.6·앞 6m·FOV 52) 자리에서 720p + Kuwahara(반경 2).
// SessionState "ss.seg" = 구간, "ss.mode" = shots | video
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
string seg = UnityEditor.SessionState.GetString("ss.seg", "tomo"), mode = UnityEditor.SessionState.GetString("ss.mode", "shots");
var proxy = GameObject.Find("_SS_PROXY"); if (!proxy) return "FAIL: 대역 없음";
var race = GameObject.Find("Race"); var rb = race.GetComponent<Rigidbody>();
var camGo = GameObject.Find("Main Camera"); var cam = camGo.GetComponent<Camera>(); var fc = camGo.GetComponent<FollowCamera>(); var lr = camGo.GetComponent<LowResRenderTest>();
fc.enabled = false; rb.isKinematic = true; rb.linearVelocity = Vector3.zero;
var ci = System.Globalization.CultureInfo.InvariantCulture;
// 차 원점의 지면 위 높이(처음 한 번 재서 기억)
float carLift = UnityEditor.SessionState.GetFloat("ss.lift", -1f);
System.Func<float, float, float, float> ground = (x, y, z) => {
  float best = float.NegativeInfinity;
  foreach (var h in Physics.RaycastAll(new Vector3(x, y + 4f, z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)) {
    if (h.transform.root == race.transform) continue; if (h.point.y > best) best = h.point.y; }
  return float.IsNegativeInfinity(best) ? y : best; };
if (carLift < 0) { var rp = race.transform.position; carLift = rp.y - ground(rp.x, rp.y, rp.z); UnityEditor.SessionState.SetFloat("ss.lift", carLift); }
var kuwa = new Material(Shader.Find("PitCrew/A7Kuwahara") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/SceneA7/A7Kuwahara.shader")); kuwa.SetFloat("_Radius", 2);
bool s0 = lr.smooth; var m0 = lr.presentMaterial; int h0 = lr.targetHeight; float f0 = cam.fieldOfView;
lr.smooth = true; lr.presentMaterial = kuwa; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false);
cam.fieldOfView = 52f;
// 계획 길(원본 knot)과 씬 V2 길 표면이 어긋난다 → knot 둘레 8m 안 V2 길 표면 점들의 무게중심으로 옮긴다(2회).
System.Func<Vector3, Vector3> snap = k => {
  var c = k;
  for (int it = 0; it < 2; it++) { Vector3 sum = Vector3.zero; int cntH = 0;
    for (int gx = -8; gx <= 8; gx++) for (int gz = -8; gz <= 8; gz++) { if (gx * gx + gz * gz > 64) continue;
      foreach (var h in Physics.RaycastAll(new Vector3(c.x + gx, k.y + 6f, c.z + gz), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)) {
        if (h.collider.name.StartsWith("ROAD_V2")) { sum += h.point; cntH++; break; } } }
    if (cntH > 0) c = new Vector3(sum.x / cntH, k.y, sum.z / cntH); }
  return c; };
System.Action<float, float, float, float> place = (x, y, z, yaw) => {
  float gy = ground(x, y, z); var anchor = new Vector3(x, gy + carLift, z); var q = Quaternion.Euler(0, yaw, 0);
  race.transform.SetPositionAndRotation(anchor, q); rb.position = anchor; rb.rotation = q;
  var cp = anchor + q * new Vector3(0, 2.8f, -5.6f); var look = anchor + Vector3.up * 0.6f + q * Vector3.forward * 6f;
  cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp, Vector3.up)); };
System.Action<string> shoot = path => {
  cam.Render(); lr.Present(dst); var prev = RenderTexture.active; RenderTexture.active = dst;
  tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev; System.IO.File.WriteAllBytes(path, tx.EncodeToPNG()); };
var lines = System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/ss/data.txt")));
string ev = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/streetscape-proxy/" + seg + "/"));
string fr = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/ss/frames_" + seg + "/"));
System.IO.Directory.CreateDirectory(ev); var log = new System.Text.StringBuilder(); bool inSeg = false; int n = 0;
var sw = System.Diagnostics.Stopwatch.StartNew();
if (mode == "video") { if (System.IO.Directory.Exists(fr)) System.IO.Directory.Delete(fr, true); System.IO.Directory.CreateDirectory(fr); }
// VP(0.5m 간격) 전체를 V2 길에 붙이고 ±5m 이동평균 → 방향은 ±4m 앞뒤 차. SHOT 은 VP 중 가장 가까운 점을 쓴다.
var vp = new System.Collections.Generic.List<Vector3>(); var shots = new System.Collections.Generic.List<Vector3>();
foreach (var ln in lines) {
  var p = ln.Split(' ');
  if (p[0] == "SEG") { inSeg = p[1] == seg; continue; }
  if (!inSeg) continue;
  if (p[0] == "VP") vp.Add(new Vector3(float.Parse(p[1], ci), float.Parse(p[2], ci), float.Parse(p[3], ci)));
  if (p[0] == "SHOT") shots.Add(new Vector3(float.Parse(p[2], ci), float.Parse(p[3], ci), float.Parse(p[4], ci)));
}
var sn = new Vector3[vp.Count]; var off = new float[vp.Count];
for (int i = 0; i < vp.Count; i++) { sn[i] = snap(vp[i]); off[i] = Vector2.Distance(new Vector2(sn[i].x, sn[i].z), new Vector2(vp[i].x, vp[i].z)); }
var sm = new Vector3[vp.Count];
for (int i = 0; i < vp.Count; i++) { Vector3 s = Vector3.zero; int c = 0; for (int j = Mathf.Max(0, i - 10); j <= Mathf.Min(vp.Count - 1, i + 10); j++) { s += sn[j]; c++; } sm[i] = s / c; }
System.Func<int, float> yawAt = i => { var d = sm[Mathf.Min(vp.Count - 1, i + 8)] - sm[Mathf.Max(0, i - 8)]; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; };
var offs = (float[])off.Clone(); System.Array.Sort(offs);
log.Append("knot→V2 offset m: median=" + offs[offs.Length / 2].ToString("F2") + " p90=" + offs[(int)(offs.Length * 0.9f)].ToString("F2") + " max=" + offs[offs.Length - 1].ToString("F2") + " | ");
if (mode == "shots") for (int si = 0; si < shots.Count; si++) {
  int best = 0; float bd = 1e9f; for (int i = 0; i < vp.Count; i++) { float d = (vp[i] - shots[si]).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
  place(sm[best].x, sm[best].y, sm[best].z, yawAt(best));
  proxy.SetActive(true); shoot(ev + "with_" + (si + 1).ToString("00") + ".png");
  proxy.SetActive(false); shoot(ev + "without_" + (si + 1).ToString("00") + ".png");
  proxy.SetActive(true); log.Append((si + 1) + ":vp" + best + " off=" + off[best].ToString("F1") + " "); }
if (mode == "video") for (int i = 0; i < vp.Count && n < 300; i++) { place(sm[i].x, sm[i].y, sm[i].z, yawAt(i)); shoot(fr + "f" + n.ToString("0000") + ".png"); n++; }
lr.smooth = s0; lr.presentMaterial = m0; lr.targetHeight = h0; cam.fieldOfView = f0; dst.Release(); UnityEngine.Object.DestroyImmediate(tx);
return "OK " + seg + " " + mode + " lift=" + carLift.ToString("F2") + " frames=" + n + " ms=" + sw.ElapsedMilliseconds + " " + log;
