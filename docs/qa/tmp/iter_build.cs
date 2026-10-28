// artlive 1509: Iter_Depot = Map_TownV6 사본 + A7 빛(해 1·주변광·3단 명암·실시간 그림자) + 씬 전용 재질 + ITER_Depot + Cam_IT_* 3대
// 재실행 가능(씬·Iter_Depot_Mats 를 지우고 다시 만든다). Map_TownV6·공유 재질은 안 건드림.
// 단계 ① 형태: 1차 q3_depot.fbx 를 회색 단색 재질 하나로.
const string SRC = "Assets/Scenes/Map_TownV6.unity", S = "Assets/Scenes/Iter_Depot.unity", MD = "Assets/Scenes/Iter_Depot_Mats";
var log = new System.Text.StringBuilder();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SRC);
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset(SRC, S);
if (AssetDatabase.IsValidFolder(MD)) AssetDatabase.DeleteAsset(MD); AssetDatabase.CreateFolder("Assets/Scenes", "Iter_Depot_Mats");
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var a7 = Shader.Find("PitCrew/SceneA7/Lit");
// ---- 1. 씬 전용 재질: LofiPilot/Surface·SceneA3/Look → A7 Lit 사본
var conv = new System.Collections.Generic.Dictionary<Material, Material>(); int nSlots = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var ms = r.sharedMaterials; bool ch = false;
  for (int i = 0; i < ms.Length; i++) { var m = ms[i]; if (!m) continue; var sn = m.shader.name; if (sn != "PitCrew/LofiPilot/Surface" && sn != "PitCrew/SceneA3/Look") continue;
    if (!conv.ContainsKey(m)) { Material n;
      if (sn == "PitCrew/SceneA3/Look") { n = new Material(m); n.shader = a7; }
      else { n = new Material(a7); n.SetTexture("_BaseMap", m.GetTexture("_MainTex")); n.SetTextureScale("_BaseMap", m.GetTextureScale("_MainTex")); n.SetTextureOffset("_BaseMap", m.GetTextureOffset("_MainTex"));
        n.SetColor("_Tint", m.GetColor("_Tint")); n.SetColor("_FogColor", m.GetColor("_FogColor")); n.SetFloat("_FogStart", m.GetFloat("_FogStart")); n.SetFloat("_FogEnd", m.GetFloat("_FogEnd")); n.SetFloat("_FogMax", 1f); n.SetFloat("_Sat", 1f); }
      n.SetFloat("_A7Lit", 1f); n.name = "IT_" + m.name; AssetDatabase.CreateAsset(n, MD + "/" + n.name + "_" + conv.Count + ".mat"); conv[m] = n; }
    ms[i] = conv[m]; ch = true; nSlots++; }
  if (ch) { r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; } }
log.Append("mats converted " + conv.Count + " slots " + nSlots + "\n");
// ---- 2. 빛: 원래 방향광 끄고 ITER_Light(A7LightToggles + A7_Sun)
GameObject.Find("Directional Light").SetActive(false);
var lg = new GameObject("ITER_Light"); var lt = lg.AddComponent<A7LightToggles>();
lt.sceneRP = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Art/SceneA7/A7_RPAsset.asset"); lt.kuwaharaShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/SceneA7/A7Kuwahara.shader");
lt.realtimeShadows = true; lt.painterly = false; lt.Apply();
// ---- 3. ITER_Depot: Q3_Depot 회색 박스 자리. 정면 = 길 쪽(슬롯 yaw 97.71 의 90° 나머지 7.71 → 정면이 포장 트리거·길 쪽 +Z)
var slot = GameObject.Find("TV6_Loop").transform.Find("AssetSlots/Q3_Depot"); slot.gameObject.SetActive(false); Physics.SyncTransforms();
var yaw = Quaternion.Euler(0, slot.eulerAngles.y - 90f, 0); var fw = yaw * Vector3.forward; var rt = yaw * Vector3.right;
System.Func<Vector3, float> Ground = p => { var hs = Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore); float y = -999; foreach (var h in hs) if (h.point.y > y) y = h.point.y; return y; };
var piv = slot.position + fw * 3.5f; piv.y = Ground(piv);   // 본체 폭10·깊이7·높이4m → 앞면 피벗 = 슬롯 중심 + 앞 3.5m (APPLY 후보와 같음)
var root = new GameObject("ITER_Depot").transform; root.SetPositionAndRotation(piv, yaw);
var gray = new Material(a7) { name = "IT_depot_gray" }; gray.SetTexture("_BaseMap", Texture2D.whiteTexture); gray.SetColor("_Tint", new Color(0.62f, 0.62f, 0.62f, 1f)); gray.SetFloat("_A7Lit", 1f); gray.SetFloat("_Sat", 1f);
gray.SetColor("_FogColor", RenderSettings.fogColor); gray.SetFloat("_FogStart", 70f); gray.SetFloat("_FogEnd", 900f); gray.SetFloat("_FogMax", 1f);
// 안개 값은 이웃 재질(Lofi 사본) 하나를 따른다
foreach (var n in conv.Values) if (n.HasProperty("_FogEnd")) { gray.SetColor("_FogColor", n.GetColor("_FogColor")); gray.SetFloat("_FogStart", n.GetFloat("_FogStart")); gray.SetFloat("_FogEnd", n.GetFloat("_FogEnd")); gray.SetFloat("_FogMax", n.GetFloat("_FogMax")); break; }
AssetDatabase.CreateAsset(gray, MD + "/IT_depot_gray.mat");
var dp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PreviewCand/depot/q3_depot.fbx"), root); PrefabUtility.UnpackPrefabInstance(dp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
dp.name = "Depot_s01_form"; dp.transform.localPosition = Vector3.zero;
foreach (var r in dp.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = gray; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
var db = new Bounds(); bool f0 = false; foreach (var r in dp.GetComponentsInChildren<Renderer>()) { if (!f0) { db = r.bounds; f0 = true; } else db.Encapsulate(r.bounds); }
log.Append("slot " + slot.position.ToString("F2") + " yaw " + yaw.eulerAngles.y.ToString("F2") + " pivot " + piv.ToString("F2") + " depot bounds c " + db.center.ToString("F2") + " s " + db.size.ToString("F2") + "\n");
log.Append("ground FL " + Ground(piv - rt * 5f).ToString("F2") + " FR " + Ground(piv + rt * 5f).ToString("F2") + " BL " + Ground(piv - rt * 5f - fw * 4f).ToString("F2") + " BR " + Ground(piv + rt * 5f - fw * 4f).ToString("F2") + "\n");
// ---- 4. 고정 카메라 Cam_IT_*: Main Camera 사본(LowResRenderTest·URP 데이터만 남김), 꺼진 채 저장
var main = Camera.main; var cr = new GameObject("ITER_Cams").transform;
System.Func<string, Vector3, Vector3, Camera> Cam = (nm, pos, look) => { var g = UnityEngine.Object.Instantiate(main.gameObject, cr); g.name = nm; g.tag = "Untagged";
  foreach (var c in g.GetComponents<Component>()) if (c is AudioListener || c is FollowCamera || c is OverviewCamera) UnityEngine.Object.DestroyImmediate(c);
  g.transform.position = pos; g.transform.LookAt(look); g.SetActive(false); var cam = g.GetComponent<Camera>(); log.Append(nm + " " + pos.ToString("F2") + " -> " + look.ToString("F2") + " dist " + Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(piv.x, 0, piv.z)).ToString("F1") + "m fov " + cam.fieldOfView + "\n"); return cam; };
var mid = piv - fw * 3.5f;   // 본체 가운데(바닥)
// road: 길 가운데(슬롯 중심 앞 7m = 앞면 앞 3.5m)에서 길을 따라 오른쪽(오르막 쪽) 24m — 왼쪽은 옆 건물에 가려짐(s01 첫 시도), 카트 높이 1.0m, 보관소 앞면 쪽을 보며 다가옴
var rp = piv + fw * 3.5f + rt * 24f; rp.y = Ground(rp) + 1.0f; Cam("Cam_IT_road", rp, piv + Vector3.up * 2f);
var fp = piv + fw * 8f; fp.y = Ground(fp) + 1.6f; Cam("Cam_IT_front", fp, piv + Vector3.up * 2f);
var d34 = (fw + rt).normalized; var qp = mid + d34 * 12f; qp.y = Ground(qp) + 2.5f; Cam("Cam_IT_34", qp, mid + Vector3.up * 2f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
log.Append("saved " + S + " dirty " + sc.isDirty);
return log.ToString();
