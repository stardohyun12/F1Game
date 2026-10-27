// 과제 0535: Preview_Candidates = Scene_A7_Light 사본 + 후보 4개(정비소 교체·택배 보관소·시험 집·주인공 방) + Cam_PV_* + PreviewCandidatesToggle
// 재실행 가능(씬·PV 재질을 지우고 다시 만든다). 공유 재질은 안 건드림 — 재질은 전부 Assets/Art/PreviewCand/Mats/PV_* 사본.
const string A7 = "Assets/Scenes/Scene_A7_Light.unity", S = "Assets/Scenes/Preview_Candidates.unity", D = "Assets/Art/PreviewCand/", MD = "Assets/Art/PreviewCand/Mats/";
var log = new System.Text.StringBuilder();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(A7);
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset(A7, S);
if (AssetDatabase.IsValidFolder(MD.TrimEnd('/'))) AssetDatabase.DeleteAsset(MD.TrimEnd('/')); AssetDatabase.CreateFolder("Assets/Art/PreviewCand", "Mats");
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform;
System.Func<string, Texture2D> T = p => AssetDatabase.LoadAssetAtPath<Texture2D>(D + p);
var tmplG = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_garage_baked32.mat");
var tmplH = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
System.Func<Material, string, Texture2D, Material> Mat = (tm, nm, tx) => { var m = new Material(tm) { name = "PV_" + nm }; m.SetTexture("_BaseMap", tx); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
  m.SetFloat("_HasR2", 0f); m.SetFloat("_R2", 0f); m.SetFloat("_Sat", 1f); m.SetColor("_Tint", Color.white); m.SetFloat("_A7Lit", 1f); AssetDatabase.CreateAsset(m, MD + m.name + ".mat"); return m; };
var pv = new GameObject("PV_Candidates").transform; pv.SetParent(root, false);
System.Func<string, Transform, GameObject> Inst = (p, par) => { var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p), par); PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); return g; };   // 풀어야 루트 렌더러 재질 교체가 저장된다
System.Action<GameObject, System.Collections.Generic.Dictionary<string, Material>> Remap = (go, map) => { foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) { if (ms[i] && map.ContainsKey(ms[i].name)) ms[i] = map[ms[i].name]; else log.Append("UNMAPPED " + r.name + ":" + (ms[i] ? ms[i].name : "null") + "; "); } r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; } };
System.Func<Vector3, float> Ground = p => { var hs = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore); float y = -999; foreach (var h in hs) if (h.point.y > y) y = h.point.y; return y; };
// ---- 1. 정비소 교체 (같은 원점·회전)
var oldG = root.Find("garage_v5_combo");
var gm = new System.Collections.Generic.Dictionary<string, Material> {
  { "Garage_Albedo", Mat(tmplG, "garage_baked32", T("garage/garage_baked32.png")) }, { "Garage_Sign_Photo", Mat(tmplG, "garage_sign", T("garage/garage_sign.png")) },
  { "Garage_Details_Photo", Mat(tmplG, "garage_details", T("garage/garage_details.png")) }, { "festival_registration_Photo", Mat(tmplG, "garage_festival", T("garage/garage_festival.png")) },
  { "race_photo_0_Photo", Mat(tmplG, "garage_memory_1", T("garage/garage_memory_1.png")) }, { "race_photo_1_Photo", Mat(tmplG, "garage_memory_2", T("garage/garage_memory_2.png")) }, { "race_photo_2_Photo", Mat(tmplG, "garage_memory_3", T("garage/garage_memory_3.png")) } };
var ng = Inst(D + "garage/garage_v5_combo.fbx", pv); ng.name = "PV_Garage_q2"; ng.transform.SetPositionAndRotation(oldG.position, oldG.rotation * ng.transform.localRotation);   // fbx 루트 (270,0,0)·100배 유지 Remap(ng, gm);
foreach (var c in oldG.GetComponentsInChildren<Collider>(true)) { var cg = new GameObject("col_" + c.name); cg.transform.SetParent(ng.transform, false); cg.transform.SetPositionAndRotation(c.transform.position, c.transform.rotation); cg.transform.localScale = Vector3.one;
  UnityEditorInternal.ComponentUtility.CopyComponent(c); UnityEditorInternal.ComponentUtility.PasteComponentAsNew(cg); log.Append("garage col " + c.name + " " + c.GetType().Name + "; "); }
// ---- 2. 택배 보관소: 정비소 앞마당 동쪽, 정면이 서쪽(마당·정비소 쪽), 앞 4m 비움
var km = new System.Collections.Generic.Dictionary<string, Material>(); foreach (var n in new[] { "wall", "trim", "floor", "roof", "decals" }) km["Shared_" + n] = Mat(tmplH, "kit_" + n, T("depot/" + n + ".png"));
km["Shared_decals"].SetFloat("_Cutoff", 0.5f);
var dp = Inst(D + "depot/q3_depot.fbx", pv); dp.name = "PV_Depot_q3"; var df = new Vector3(16f, 0f, 0.5f); df.y = Ground(df); var dYaw = Quaternion.Euler(0, 270, 0); dp.transform.SetPositionAndRotation(df, dYaw * dp.transform.localRotation); Remap(dp, km);
foreach (var mf in dp.GetComponentsInChildren<MeshFilter>()) { if (mf.name == "Q3_decals" || mf.name == "Q3_sign" || mf.name == "Q3_reception_glass") continue; var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
// 앞 4m 안 물체 확인
{ var fw = dYaw * Vector3.forward; var c4 = df + fw * 2.31f + Vector3.up * 1f; var hits = Physics.OverlapBox(c4, new Vector3(5f, 0.9f, 2f), dYaw, ~0, QueryTriggerInteraction.Ignore).Where(h => !h.transform.IsChildOf(dp.transform) && h.name != "Terrain" && h.name != "yard_ground").Select(h => h.name);
  log.Append("\ndepot front " + df.ToString("F2") + " front4m colliders [" + string.Join(",", hits) + "]; "); }
// ---- 3. 공용 키트 시험 집: 동쪽 집 줄 북쪽 끝(보관소와 house_narrow_a 사이), 줄과 같은 방향
var hm = new System.Collections.Generic.Dictionary<string, Material>(); foreach (var n in new[] { "wall", "trim", "roof", "decals" }) hm["Shared_" + n] = km["Shared_" + n];
var kh = Inst(D + "kit/proof.fbx", pv); kh.name = "PV_KitHouse_v2"; var kp = new Vector3(22f, 0f, -12.5f); kp.y = Ground(kp) - 0.05f; kh.transform.SetPositionAndRotation(kp, Quaternion.Euler(0, 256, 0)); Remap(kh, hm);
var kb = kh.AddComponent<BoxCollider>(); kb.center = new Vector3(0, 3.5f, 0); kb.size = new Vector3(6f, 7f, 5f);
// ---- 4. 주인공 방: house_narrow_a 2층 안쪽, 창 = 길 쪽 바깥벽
var host = root.Find("A4b_Houses/house_narrow_a"); var hostR = host.GetComponentsInChildren<Renderer>(); var hb = hostR[0].bounds; foreach (var r in hostR) hb.Encapsulate(r.bounds);
var pvHost = UnityEngine.Object.Instantiate(host.gameObject, host.parent); pvHost.name = "PV_RoomHost_house_narrow_a"; pvHost.transform.SetParent(pv, true);
// 집 메시 안쪽 면(두께 벽·층)이 방 창을 가림 → 이 사본만 레이어 29(이름 없는 칸, ProjectSettings 안 건드림), 방 카메라는 29 를 안 그린다. 그림자도 끔(해가 창으로 들어오게)
const int HostLayer = 29; foreach (var t in pvHost.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = HostLayer;
foreach (var r in pvHost.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
Physics.SyncTransforms();
var toRoad = new Vector3(2.5f, 0, host.position.z) - host.position; toRoad.y = 0; toRoad.Normalize();
float floorY = host.position.y + 3.0f; var probe = host.position + toRoad * 15f; probe.y = floorY + 1.4f;
Vector3 facadeP = Vector3.zero, facadeN = toRoad; bool hitF = false;
foreach (var h in Physics.RaycastAll(probe, -toRoad, 30f).OrderBy(h => h.distance)) if (h.transform.IsChildOf(host)) { facadeP = h.point; facadeN = h.normal; facadeN.y = 0; facadeN.Normalize(); hitF = true; break; }
log.Append("\nhost " + host.position.ToString("F2") + " bounds " + hb.min.ToString("F1") + ".." + hb.max.ToString("F1") + " facade hit " + hitF + " " + facadeP.ToString("F2") + " n " + facadeN.ToString("F2") + "; ");
var rm = Inst(D + "room/room.fbx", pv); rm.name = "PV_Room_q1";
var rmat = Mat(tmplH, "room", T("room/room.png")); Remap(rm, new System.Collections.Generic.Dictionary<string, Material> { { "ROOM_alb", rmat } });
var rq = Quaternion.LookRotation(facadeN, Vector3.up); var ro = facadeP - facadeN * (1.66f + 0.08f); ro.y = floorY;
rm.transform.SetPositionAndRotation(ro, rq);
foreach (var mf in rm.GetComponentsInChildren<MeshFilter>()) if (mf.name == "ROOM_shell" || mf.name == "ROOM_furniture") { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
// 방 옆벽이 집 밖으로 나가는지
{ var corners = new[] { new Vector3(-1.75f, 0, -1.5f), new Vector3(1.75f, 0, -1.5f), new Vector3(-1.75f, 2.4f, 1.66f), new Vector3(1.75f, 2.4f, 1.66f) }; log.Append("room corners in host bounds: " + string.Join(",", corners.Select(c => hb.Contains(rm.transform.TransformPoint(c)) ? "Y" : "N")) + "; "); }
// 창 유리: URP Unlit 투명 판
var gmat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "PV_room_glass" };
gmat.SetFloat("_Surface", 1f); gmat.SetFloat("_Blend", 0f); gmat.SetFloat("_Cull", 0f); gmat.SetFloat("_ZWrite", 0f); gmat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); gmat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
gmat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); gmat.renderQueue = 3000; gmat.SetOverrideTag("RenderType", "Transparent"); gmat.SetColor("_BaseColor", new Color(0.78f, 0.9f, 0.95f, 0.12f)); AssetDatabase.CreateAsset(gmat, MD + gmat.name + ".mat");
var gl = GameObject.CreatePrimitive(PrimitiveType.Quad); gl.name = "PV_Room_glass"; UnityEngine.Object.DestroyImmediate(gl.GetComponent<Collider>()); gl.transform.SetParent(rm.transform, false);
gl.transform.localPosition = new Vector3(0f, 1.4f, 1.58f); gl.transform.localScale = new Vector3(1.8f, 1.0f, 1f); var glr = gl.GetComponent<MeshRenderer>(); glr.sharedMaterial = gmat; glr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
// ---- 토글
var tg = pv.gameObject.AddComponent<PreviewCandidatesToggle>();
// 토글 컴포넌트는 PV_Candidates 에 붙어 있으므로 PV_Candidates 를 끄면 안 된다 → 자식들을 candidates 로
tg.candidates = new[] { ng, dp, kh, pvHost, rm }; tg.replaced = new[] { oldG.gameObject, host.gameObject }; tg.show = true; tg.Apply();
// ---- 카메라 Cam_PV_*
var camT = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "Cam_House");
var camRoot = new GameObject("PV_Cams").transform; camRoot.SetParent(root, false);
System.Func<string, Vector3, Vector3, float, Camera> Cam = (nm, pos, look, fov) => { var c = UnityEngine.Object.Instantiate(camT.gameObject, camRoot).GetComponent<Camera>(); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); c.fieldOfView = fov; c.gameObject.SetActive(false); log.Append("\n" + nm + " " + pos.ToString("F1") + " → " + look.ToString("F1")); return c; };
var bg = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "Cam_B_Game");
var cst = Cam("Cam_PV_street", bg.transform.position, bg.transform.position + bg.transform.forward, bg.fieldOfView);
var cb = root.Find("A4e_Curbs/Curb_house_narrow_a");
Cam("Cam_PV_house", cb.TransformPoint(new Vector3(0f, 1.2f, 7f)), host.position + Vector3.up * 3.5f, camT.fieldOfView);
var gb = new Bounds(); bool f0 = false; foreach (var r in oldG.GetComponentsInChildren<Renderer>(true)) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
Cam("Cam_PV_garage", gb.center + new Vector3(0f, -1.2f, gb.extents.z * 0.7f), gb.center + new Vector3(0f, -1.7f, -gb.extents.z * 0.6f), 70f);
var dfw = dYaw * Vector3.forward; var dlook = df + Vector3.up * 2f - dp.transform.right * 0f;
Cam("Cam_PV_depot15", df + dfw * 15f + Vector3.up * 1.6f, dlook, camT.fieldOfView);
Cam("Cam_PV_depot5", df + dfw * 5f + Vector3.up * 1.6f, dlook, camT.fieldOfView);
var rt = rm.transform; float eye = 1.5f;
System.Func<Camera, Camera> NoHost = c => { c.cullingMask &= ~(1 << HostLayer); return c; };
NoHost(Cam("Cam_PV_room_window", rt.TransformPoint(new Vector3(0f, eye, -1.0f)), rt.TransformPoint(new Vector3(0f, 1.35f, 1.6f)), 70f));
NoHost(Cam("Cam_PV_room_door", rt.TransformPoint(new Vector3(0.3f, eye, 1.1f)), rt.TransformPoint(new Vector3(-0.3f, 1.2f, -1.5f)), 70f));
var fb = rm.transform.Find("ROOM_furniture").GetComponent<Renderer>().bounds;
NoHost(Cam("Cam_PV_room_desk", rt.TransformPoint(new Vector3(-0.9f, eye, -0.9f)), rt.TransformPoint(new Vector3(1.4f, 0.8f, 0.8f)), 70f));
// 정비소 루트 렌더러는 위 Remap 이 저장에 안 남았다(원인 미확인) → 이름으로 한 번 더
{ var r = ng.GetComponent<MeshRenderer>(); var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (gm.ContainsKey(ms[i].name)) ms[i] = gm[ms[i].name]; r.sharedMaterials = ms; EditorUtility.SetDirty(r); log.Append("
garage mats " + string.Join(",", r.sharedMaterials.Select(x => x.name))); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
log.Append("\nsaved " + S + " dirty " + sc.isDirty);
return log.ToString();
