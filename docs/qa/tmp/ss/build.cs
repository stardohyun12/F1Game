// 과제 1428: 길가 배치 회색 대역(Play 중 임시 — 씬에 저장 안 됨). SessionState "ss.seg" = tomo|posi|ridge
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
string seg = UnityEditor.SessionState.GetString("ss.seg", "tomo");
var old = GameObject.Find("_SS_PROXY"); if (old) UnityEngine.Object.DestroyImmediate(old);
var root = new GameObject("_SS_PROXY").transform;
var bot = GameObject.Find("TownPathBot_loopBox"); if (bot) bot.SetActive(false);
var race = GameObject.Find("Race");
// 재질: 씬의 A7 Lit 재질을 복사해 흰 텍스처 + 회색 3단
Material src = null;
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) { foreach (var m in r.sharedMaterials) if (m && m.shader.name == "PitCrew/SceneA7/Lit") { src = m; break; } if (src) break; }
System.Func<float, Material> gray = v => { var m = new Material(src); m.name = "SS_gray_" + v; m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_Tint", new Color(v, v, v, 1)); return m; };
Material D = gray(0.22f), M = gray(0.48f), L = gray(0.78f);
var wireMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); wireMat.SetColor("_BaseColor", Color.black);
System.Func<float, float, float, float> ground = (x, y, z) => {
  float best = float.NegativeInfinity;
  foreach (var h in Physics.RaycastAll(new Vector3(x, y + 4f, z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)) {
    if (h.transform.root == race.transform || h.transform.root == root) continue;
    if (h.point.y > best) best = h.point.y; }
  return float.IsNegativeInfinity(best) ? y : best; };
System.Func<PrimitiveType, Vector3, Vector3, Quaternion, Material, GameObject> prim = (pt, c, s, q, m) => {
  var g = GameObject.CreatePrimitive(pt); UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
  g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(c, q); g.transform.localScale = s;
  var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; return g; };
// 원기둥: 높이 h(축 = q 의 위), 지름 dia
System.Func<Vector3, float, float, Quaternion, Material, GameObject> cyl = (c, dia, h, q, m) => prim(PrimitiveType.Cylinder, c, new Vector3(dia, h * 0.5f, dia), q, m);
System.Func<Vector3, Vector3, Quaternion, Material, GameObject> box = (c, s, q, m) => prim(PrimitiveType.Cube, c, s, q, m);
var lines = System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/ss/data.txt")));
bool inSeg = false; var cnt = new System.Collections.Generic.Dictionary<string, int>(); var ci = System.Globalization.CultureInfo.InvariantCulture;
var poleTop = new System.Collections.Generic.Dictionary<string, Vector3>();
foreach (var ln in lines) {
  var p = ln.Split(' ');
  if (p[0] == "SEG") { inSeg = p[1] == seg; continue; }
  if (!inSeg) continue;
  if (p[0] == "E") {
    string kind = p[2]; float x = float.Parse(p[3], ci), y = float.Parse(p[4], ci), z = float.Parse(p[5], ci), yaw = float.Parse(p[6], ci); bool left = p[7] == "left"; int extra = int.Parse(p[8]);
    var q = Quaternion.Euler(0, yaw, 0); Vector3 f = q * Vector3.forward, rt = q * Vector3.right, inw = left ? rt : -rt, up = Vector3.up;
    var qIn = Quaternion.LookRotation(inw, up);  // 길 쪽을 보는 회전
    float gy = ground(x, y, z); var b = new Vector3(x, gy, z);
    cnt[kind] = cnt.ContainsKey(kind) ? cnt[kind] + 1 : 1;
    switch (kind) {
      case "utility_pole": cyl(b + up * 4.5f, 0.3f, 9f, Quaternion.identity, D); box(b + up * 8.4f, new Vector3(1.6f, 0.1f, 0.1f), qIn * Quaternion.Euler(0, 90, 0), D); poleTop[p[1]] = b + up * 8.4f; break;
      case "convex_mirror": cyl(b + up * 1.25f, 0.08f, 2.5f, Quaternion.identity, D);
        var qm = Quaternion.LookRotation(Quaternion.AngleAxis(left ? -40 : 40, up) * inw, up) * Quaternion.Euler(90, 0, 0);
        cyl(b + up * 2.5f + inw * 0.1f, 0.6f, 0.05f, qm, D); break;
      case "pot_row": { int n = Mathf.Max(1, extra); for (int i = 0; i < n; i++) { var c = b + f * ((i - (n - 1) * 0.5f) * 0.55f); c.y = ground(c.x, y, c.z); cyl(c + up * 0.25f, 0.5f, 0.5f, Quaternion.identity, L); } } break;
      case "laundry": cyl(b - f * 1.5f + up * 1.1f, 0.05f, 2.2f, Quaternion.identity, D); cyl(b + f * 1.5f + up * 1.1f, 0.05f, 2.2f, Quaternion.identity, D);
        box(b + up * 2.15f, new Vector3(0.02f, 0.02f, 3f), q, D);
        for (int i = 0; i < 4; i++) box(b + f * (-1.05f + i * 0.7f) + up * (2.15f - 0.32f - (i % 2) * 0.06f), new Vector3(0.02f, 0.6f - (i % 2) * 0.12f, 0.45f), q, L); break;
      case "mailbox": box(b + up * 0.6f, new Vector3(0.5f, 1.2f, 0.5f), q, M); break;
      case "bench": box(b + up * 0.45f, new Vector3(0.45f, 0.08f, 1.5f), q, M); box(b + up * 0.2f - f * 0.6f, new Vector3(0.4f, 0.4f, 0.08f), q, M); box(b + up * 0.2f + f * 0.6f, new Vector3(0.4f, 0.4f, 0.08f), q, M); box(b + up * 0.7f - inw * 0.2f, new Vector3(0.05f, 0.4f, 1.5f), q, M); break;
      case "fishing_crates": box(b + up * 0.3f - f * 0.32f, Vector3.one * 0.6f, q, M); box(b + up * 0.3f + f * 0.32f, Vector3.one * 0.6f, q, M); box(b + up * 0.9f, Vector3.one * 0.6f, q * Quaternion.Euler(0, 12, 0), M); break;
      case "bicycle": { var qa = Quaternion.LookRotation(inw, up) * Quaternion.Euler(90, 0, 0);
        cyl(b - f * 0.55f + up * 0.35f, 0.7f, 0.05f, qa, D); cyl(b + f * 0.55f + up * 0.35f, 0.7f, 0.05f, qa, D);
        box(b + up * 0.6f, new Vector3(0.05f, 0.06f, 1.1f), q, M); box(b - f * 0.25f + up * 0.9f, new Vector3(0.1f, 0.05f, 0.25f), q, M); box(b + f * 0.5f + up * 1.0f, new Vector3(0.55f, 0.04f, 0.04f), q, M); } break;
      case "awning": box(b + up * 2.4f, new Vector3(1.0f, 0.05f, 2.5f), q * Quaternion.Euler(0, 0, left ? -15 : 15), M); break;
      case "icon_sign": cyl(b + up * 1.1f, 0.05f, 2.2f, Quaternion.identity, D); box(b + up * 2.3f, new Vector3(0.6f, 0.6f, 0.03f), q, L); break;
      case "cat": box(b + up * 0.13f, new Vector3(0.18f, 0.25f, 0.4f), q * Quaternion.Euler(0, 30, 0), L); box(b + up * 0.3f + q * Quaternion.Euler(0, 30, 0) * Vector3.forward * 0.18f, new Vector3(0.14f, 0.13f, 0.12f), q, L); break;
      case "gull_perch": cyl(b + up * 0.5f, 0.25f, 1.0f, Quaternion.identity, D); box(b + up * 1.1f, new Vector3(0.15f, 0.2f, 0.4f), q, L); break;
      case "speed_hump": box(b + up * 0.05f, new Vector3(0.5f, 0.1f, 0.5f), q, M); break;
      case "curb_segment": for (int i = 10; i < p.Length; i++) { var a0 = p[i - 1].Split(','); var a1 = p[i].Split(',');
          var u = new Vector3(float.Parse(a0[0], ci), 0, float.Parse(a0[1], ci)); var v = new Vector3(float.Parse(a1[0], ci), 0, float.Parse(a1[1], ci));
          u.y = ground(u.x, y, u.z); v.y = ground(v.x, y, v.z); var dv = v - u; if (dv.magnitude < 0.01f) continue;
          box((u + v) * 0.5f + up * 0.05f, new Vector3(0.2f, 0.1f, dv.magnitude), Quaternion.LookRotation(dv, up), M); } break;
    }
  } else if (p[0] == "W") {
    var a = new Vector3(float.Parse(p[2], ci), float.Parse(p[3], ci), float.Parse(p[4], ci)); var c = new Vector3(float.Parse(p[5], ci), float.Parse(p[6], ci), float.Parse(p[7], ci));
    a.y = ground(a.x, a.y, a.z) + 8.4f; c.y = ground(c.x, c.y, c.z) + 8.4f;
    var go = new GameObject("wire_" + p[1]); go.transform.SetParent(root, false); var lr = go.AddComponent<LineRenderer>();
    lr.sharedMaterial = wireMat; lr.widthMultiplier = 0.02f; lr.positionCount = 17; lr.useWorldSpace = true; float sag = Vector3.Distance(a, c) * 0.03f;
    for (int i = 0; i <= 16; i++) { float t = i / 16f; lr.SetPosition(i, Vector3.Lerp(a, c, t) - Vector3.up * sag * 4f * t * (1 - t)); }
    cnt["wire"] = cnt.ContainsKey("wire") ? cnt["wire"] + 1 : 1;
  }
}
var sb = new System.Text.StringBuilder("OK " + seg + " objs=" + root.childCount + " ");
foreach (var kv in cnt) sb.Append(kv.Key + "=" + kv.Value + " ");
return sb.ToString();
