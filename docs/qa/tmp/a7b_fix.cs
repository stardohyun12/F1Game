// 과제 0236 — Scene_A7_Light 마무리: 씬 전용 URP 사본·Kuwahara 연결, 정비소 바닥 접지 얼룩 끄기, 돌담 축척·대비, 해 방위, 사진 키트 세우기
const string D = "Assets/Art/SceneA7/", S = "Assets/Scenes/Scene_A7_Light.unity";
var log = new System.Text.StringBuilder();
AssetDatabase.Refresh();
// ---- 씬 전용 URP 사본
string rpPath = D + "A7_RPAsset.asset";
if (!AssetDatabase.LoadMainAssetAtPath(rpPath)) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline), rpPath);
var rp = AssetDatabase.LoadMainAssetAtPath(rpPath);
foreach (var kv in new (string, object)[] { ("shadowDistance", 40f), ("mainLightShadowmapResolution", 512), ("shadowCascadeCount", 1) }) { var p = rp.GetType().GetProperty(kv.Item1); if (p != null && p.CanWrite) p.SetValue(rp, kv.Item2); }
var softF = rp.GetType().GetField("m_SoftShadowsSupported", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (softF != null) softF.SetValue(rp, false);
EditorUtility.SetDirty(rp);
foreach (var n in new[] { "shadowDistance", "mainLightShadowmapResolution", "shadowCascadeCount", "supportsSoftShadows", "supportsMainLightShadows" }) log.Append(n + "=" + rp.GetType().GetProperty(n)?.GetValue(rp) + " ");
log.Append("\n");
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var lt = root.GetComponent<A7LightToggles>();
lt.sceneRP = rp; lt.kuwaharaShader = Shader.Find("Hidden/PitCrew/A7Kuwahara"); log.Append("kuwahara " + (lt.kuwaharaShader != null) + "\n");
// ---- 정비소 바닥 얼룩: 접지 얼룩(SceneA3/Contact) 중 정비소 안에 있는 것 끄기
var gb = new Bounds(); bool f0 = false; foreach (var r in root.Find("garage_v5_combo").GetComponentsInChildren<Renderer>()) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
int nc = 0, ncg = 0; var cn = new System.Collections.Generic.HashSet<string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  bool contact = false; foreach (var m in r.sharedMaterials) if (m && m.shader.name.Contains("Contact")) contact = true; if (!contact) continue; nc++;
  if (gb.Contains(r.bounds.center)) { r.enabled = false; EditorUtility.SetDirty(r); ncg++; if (cn.Count < 8) cn.Add(r.transform.parent ? r.transform.parent.name + "/" + r.name : r.name); } }
log.Append("contact blobs " + nc + ", garage 안 끔 " + ncg + " e.g. " + string.Join(",", cn) + "\n");
// ---- 돌담: 대비 낮춘 사본 + 타일 = 실제 2×1.5 m
string sp = D + "Tex/stone_H1_soft.png"; AssetDatabase.ImportAsset(sp);
var ti = (TextureImporter)AssetImporter.GetAtPath(sp); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
var sw = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA7_stonewall.mat"); sw.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(sp));
MeshFilter wmf = null; foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (mr.sharedMaterial == sw) { wmf = mr.GetComponent<MeshFilter>(); break; }
var mesh = wmf.sharedMesh; var uv = mesh.uv; Vector2 umin = Vector2.positiveInfinity, umax = Vector2.negativeInfinity; foreach (var u in uv) { umin = Vector2.Min(umin, u); umax = Vector2.Max(umax, u); }
var wb = wmf.GetComponent<Renderer>().bounds; float len = Mathf.Max(wb.size.x, wb.size.z), hgt = wb.size.y;
float uPerM = (umax.x - umin.x) / Mathf.Max(0.01f, len), vPerM = (umax.y - umin.y) / Mathf.Max(0.01f, hgt);
var st0 = sw.GetTextureScale("_BaseMap"); var st = new Vector2(0.5f / Mathf.Max(1e-4f, uPerM), (1f / 1.5f) / Mathf.Max(1e-4f, vPerM));
sw.SetTextureScale("_BaseMap", st); EditorUtility.SetDirty(sw);
log.Append("stonewall " + wmf.name + " len " + len.ToString("F1") + " h " + hgt.ToString("F2") + " uv " + (umax - umin).ToString("F2") + " u/m " + uPerM.ToString("F2") + " v/m " + vPerM.ToString("F2") + " ST " + st0 + "→" + st.ToString("F3") + "\n");
// ---- 해 방위: 거리 컷(Cam_B_Game)에서 그림자가 비스듬히 앞쪽 오른쪽으로 보이게 = 카메라 방위 + 60
Camera cb = null; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.name == "Cam_B_Game") cb = c;
if (cb) { float yaw = cb.transform.eulerAngles.y; lt.sunAzimuth = Mathf.Repeat(yaw + 60f, 360f); log.Append("Cam_B yaw " + yaw.ToString("F0") + " → sunAzimuth " + lt.sunAzimuth.ToString("F0") + "\n"); }
lt.Apply();
// ---- 사진 키트: 누워 있던 것 세우기 + 바닥에 붙이기. 집 앞 무리는 ROOMS 집(house_narrow_a) 앞으로 옮김
var pk = root.Find("A7_PhotoKit"); var H = new System.Collections.Generic.Dictionary<string, float> { { "vending_G1", 1.83f }, { "vending_G2", 1.83f }, { "postbox_G1", 1.38f }, { "ac_G3", 0.55f } };
var hsA = root.Find("A4b_Houses/house_narrow_a"); var cbA = root.Find("A4e_Curbs/Curb_house_narrow_a"); Vector3 camP = cbA.TransformPoint(new Vector3(0f, 1.2f, 7f));
var hb = new Bounds(); bool f1 = false; foreach (var c in hsA.GetComponentsInChildren<BoxCollider>()) if (c.enabled) { if (!f1) { hb = c.bounds; f1 = true; } else hb.Encapsulate(c.bounds); }
Vector3 toCam = camP - hb.center; toCam.y = 0; toCam.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, toCam);
Vector3 frontP = hb.ClosestPoint(new Vector3(camP.x, hb.center.y, camP.z)) + toCam * 0.6f;
int ix = 0;
foreach (Transform t in pk) {
  string n = t.name.Replace("(Clone)", "").Trim(); float want = H.ContainsKey(n) ? H[n] : 1f;
  if (ix < 3) { var p = frontP + side * (ix == 0 ? 1.4f : ix == 1 ? 2.7f : -2.0f); t.position = p; t.rotation = Quaternion.LookRotation(toCam, Vector3.up); }
  var best = t.rotation; float bestErr = 1e9f; var baseRot = t.rotation;
  foreach (var e in new[] { Vector3.zero, new Vector3(-90, 0, 0), new Vector3(90, 0, 0), new Vector3(0, 0, 90) }) { t.rotation = baseRot * Quaternion.Euler(e); var rb = t.GetComponentInChildren<Renderer>().bounds; float err = Mathf.Abs(rb.size.y - want); if (err < bestErr) { bestErr = err; best = t.rotation; } }
  t.rotation = best; var b = t.GetComponentInChildren<Renderer>().bounds;
  float gy = Physics.Raycast(new Vector3(b.center.x, b.max.y + 3f, b.center.z), Vector3.down, out var hit, 30f) ? hit.point.y : b.min.y;
  t.position += Vector3.up * (gy - b.min.y);
  log.Append(n + " h " + t.GetComponentInChildren<Renderer>().bounds.size.y.ToString("F2") + "/" + want + " at " + t.position.ToString("F1") + "; "); ix++; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
