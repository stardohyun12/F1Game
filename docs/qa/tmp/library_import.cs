// artlive 1534 (D-191): 건물 라이브러리 Assets/Art/Library/Buildings/<이름>/ — 텍스처·fbx 임포트 설정, 폴더 전용 A7 재질, 프리팹(피벗 = 바닥 앞 가운데, 정면 +Z, BoxCollider).
// Studio_Building 이 열려 있어야 한다. 프리팹 인스턴스는 STUDIO/STUDIO_Slot/<이름> 으로 두고 꺼 둔다. 다시 돌리면 덮어쓴다.
// 자세(모델 자식의 회전·이동)는 아래 POSE 표. 재질 = SA7_house_shop.mat 의 사본(원본은 읽기만), 텍스처만 바꿈.
var log = new System.Text.StringBuilder();
const string L = "Assets/Art/Library/Buildings";
AssetDatabase.Refresh();
// 이름, fbx, 회전 Y, 이동 규칙("keep" = fbx 원점 그대로 / "front" = 가로 가운데·앞면 최대 z 를 0 으로)
var POSE = new (string nm, string fbx, float yaw, string rule)[] {
  ("room", "room.fbx", 0f, "front"), ("garage", "garage_v5_combo.fbx", 0f, "front"), ("depot", "q3_depot.fbx", 0f, "keep"), ("postoffice", "q4_postoffice.fbx", 0f, "keep"), ("house_home", "house_home.fbx", 0f, "keep") };
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
foreach (var p in new[] { "_GroundY", "_FootDark", "_Blotch", "_Lift", "_Sat", "_A7Lit", "_Cutoff", "_FogStart", "_FogEnd", "_FogMax" }) if (tmpl.HasProperty(p)) log.Append(p + "=" + tmpl.GetFloat(p) + " "); log.Append("\n");
System.Func<string, string, string> TexFor = (nm, slot) => { var s = slot.ToLower();
  if (nm == "room") return s.Contains("glass") ? "glass" : "room";
  if (nm == "house_home") return s.Contains("glass") ? "glass" : "house_home";  /* 0351: 창 유리 = 이 집 전용 반투명 M_house_home_glass */  // 0031 주인공 2층집: 아틀라스 하나  // 2303: 방 창 유리(D-189)
  if (nm == "garage") { if (s.Contains("lamp")) return "garage_lamp"; if (s.Contains("props")) return "garage_props"; if (s.Contains("posters")) return "garage_posters"; if (s.Contains("dirt")) return "garage_dirt"; if (s.Contains("albedo")) return "garage_baked32"; if (s.Contains("festival")) return "garage_festival"; if (s.Contains("race_photo_0")) return "garage_memory_1"; if (s.Contains("race_photo_1")) return "garage_memory_2"; if (s.Contains("race_photo_2")) return "garage_memory_3"; if (s.Contains("sign")) return "garage_sign"; if (s.Contains("detail")) return "garage_details"; return null; }
  if (s.Contains("glass")) return "glass";  // 1846: 우체국 창 유리
  if (s.Contains("clock_dial")) return "clock_dial";  // 1857: 보관소 시계 문자판·바늘
  foreach (var k in new[] { "icons", "wall", "trim", "floor", "roof", "decal", "postbox" }) if (s.Contains(k)) return k == "decal" ? "decals" : k == "postbox" ? "postbox_G1" : k; return null; };
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var ONLY = new[] { "depot" };  // 1950 (1914: 끝의 프리팹 강제 재임포트로 다른 건물 재질 NULL 해결)  // 1857: 물건별 오브젝트(정비소·보관소)
foreach (var e in POSE) { if (!ONLY.Contains(e.nm)) continue;
  var dir = L + "/" + e.nm;
  foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { dir })) { var tp = AssetDatabase.GUIDToAssetPath(g); var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
    ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = (tp.EndsWith("/wall.png") || tp.EndsWith("/roof.png")) && !tp.Contains("/depot/") && !tp.Contains("/postoffice/");  /* 2230: 우체국도 규칙대로 끔(build_asset.py postoffice) */  /* 2041: 보관소는 규칙(APPLY.md 밉 0)대로 끔 — 지글거림은 도현 판정 */  /* 1846: 큰 면만 밉맵(Point 유지) — 멀리서 지글거림 */ ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.alphaIsTransparency = tp.Contains("decals") || tp.Contains("icons") || tp.Contains("clock_dial"); ti.SaveAndReimport(); }
  var fp = dir + "/" + e.fbx; var mi = (ModelImporter)AssetImporter.GetAtPath(fp);
  mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport();
  var old = slotT.Find(e.nm); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
  var root = new GameObject(e.nm); root.transform.SetParent(slotT, false);
  var mdl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fp)); PrefabUtility.UnpackPrefabInstance(mdl, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
  mdl.transform.SetParent(root.transform, false); mdl.transform.localRotation = Quaternion.Euler(0, e.yaw, 0) * mdl.transform.localRotation;
  // 재질
  var made = new System.Collections.Generic.Dictionary<string, Material>(); var miss = new System.Collections.Generic.List<string>();
  foreach (var r in mdl.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials;
    for (int i = 0; i < ms.Length; i++) { var sn = ms[i] ? ms[i].name : "null"; var tn = TexFor(e.nm, sn); if (tn == null) { miss.Add(r.name + ":" + sn); continue; }
      if (tn == "glass" && !made.ContainsKey(tn)) { var gp = dir + "/M_" + e.nm + "_glass.mat"; AssetDatabase.DeleteAsset(gp);  // 1846: 옅은 하늘빛 반투명, 양면
        var gm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_" + e.nm + "_glass" }; gm.SetFloat("_Surface", 1); gm.SetFloat("_Blend", 0); gm.SetFloat("_SrcBlend", 5); gm.SetFloat("_DstBlend", 10); gm.SetFloat("_ZWrite", 0); gm.SetFloat("_Cull", 0);
        gm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); gm.renderQueue = 3000; gm.SetOverrideTag("RenderType", "Transparent"); gm.SetColor("_BaseColor", new Color(0.72f, 0.86f, 0.95f, 0.32f)); AssetDatabase.CreateAsset(gm, gp); made[tn] = gm; }
      if (!made.TryGetValue(tn, out var m)) { var mp = dir + "/M_" + e.nm + "_" + tn + ".mat"; AssetDatabase.DeleteAsset(mp); m = new Material(tmpl) { name = "M_" + e.nm + "_" + tn };
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + tn + ".png")); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
        m.SetColor("_Tint", Color.white); m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); m.SetFloat("_Cutoff", tn == "decals" || tn == "icons" || tn == "clock_dial" ? 0.5f : 0f); if (m.HasProperty("_GroundY")) m.SetFloat("_GroundY", 0);
        if (e.nm == "garage") { m.shader = Shader.Find("PitCrew/Library/A7LitPoint"); m.SetFloat("_Emit", tn == "garage_lamp" ? 1f : 0f); }  // 1653: 점광원 받는 사본 셰이더
        AssetDatabase.CreateAsset(m, mp); made[tn] = m; }
      ms[i] = m; }
    r.sharedMaterials = ms; }
  // 경계(루트 기준) → 이동
  System.Func<Bounds> B = () => { var b = new Bounds(); bool f = false; foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) foreach (var v in mf.sharedMesh.vertices) { var q = root.transform.InverseTransformPoint(mf.transform.TransformPoint(v)); if (!f) { b = new Bounds(q, Vector3.zero); f = true; } else b.Encapsulate(q); } return b; };
  var b0 = B(); log.Append(e.nm + " raw box " + b0.min.ToString("F2") + ".." + b0.max.ToString("F2"));
  if (e.rule == "front") mdl.transform.localPosition += new Vector3(-b0.center.x, 0, -b0.max.z);
  var b1 = B(); log.Append(" -> " + b1.min.ToString("F2") + ".." + b1.max.ToString("F2") + " size " + b1.size.ToString("F2") + " mats " + made.Count + (miss.Count > 0 ? " MISS " + string.Join(",", miss.Distinct()) : "") + "\n");
  if (e.nm == "depot" || e.nm == "postoffice") { foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.name.Contains("building") || mf.name.Contains("body")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }  // 1623: 벽 메시만 = 문으로 지나감
  else if (e.nm == "garage") { foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.name == "GARAGE_shell") mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }  // 1857: 몸만 메시 콜라이더, 물건은 아래 낱개
  else if (e.nm == "house_home") { foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.name == "HOUSE_shell" || mf.name == "STAIRS") mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }  // 0351: 집 안으로 들어감 = 껍데기·계단만 메시 콜라이더
  else { var bc = root.AddComponent<BoxCollider>(); bc.center = b1.center; bc.size = b1.size; }
  // 1857: 만질 수 있는 물건 = 낱개 BoxCollider (이름 종류_번호 · CLOCK). 시계엔 WallClock(바늘 축 = 정면 +Z = 문자판 법선)
  int nItem = 0; foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) { var nm = mf.name;
    if (!(System.Text.RegularExpressions.Regex.IsMatch(nm, "^[A-Z]+_\\d+$") || nm == "CLOCK" || (e.nm == "house_home" && System.Text.RegularExpressions.Regex.IsMatch(nm, "^[A-Z]+(_[A-Z]+)?$") && nm != "STAIRS"))) continue; if (nm.StartsWith("GLASS_")) continue;  /* 0351: SINK·TABLE_KITCHEN 같은 이름도 */ mf.gameObject.AddComponent<BoxCollider>(); nItem++; }
  log.Append(" items " + nItem);
  var ck = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CLOCK");
  if (ck) { var wt = System.Type.GetType("WallClock, Assembly-CSharp"); var hh = ck.Find("CLOCK_hand_hour"); var hm = ck.Find("CLOCK_hand_min");
    if (wt != null && hh && hm) { var wc = ck.gameObject.AddComponent(wt); var so = new SerializedObject(wc);
      so.FindProperty("hourHand").objectReferenceValue = hh; so.FindProperty("minuteHand").objectReferenceValue = hm;
      so.FindProperty("axisLocal").vector3Value = hh.InverseTransformDirection(-root.transform.forward).normalized; so.ApplyModifiedPropertiesWithoutUndo();
      log.Append(" clock axis " + hh.InverseTransformDirection(-root.transform.forward).normalized.ToString("F2") + " center " + hh.position.ToString("F2")); }
    else log.Append(" CLOCK MISS type=" + (wt != null) + " hands=" + (hh != null) + "/" + (hm != null)); }
  foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (System.Text.RegularExpressions.Regex.IsMatch(t.name, "^LAMP_\\d$")) {  /* 1857: LAMP_01 등 = 등 메시(빛 아님) */ var lt = t.gameObject.AddComponent<Light>(); lt.type = LightType.Spot; t.rotation = Quaternion.Euler(90f, 0f, 0f); lt.spotAngle = 120f; lt.innerSpotAngle = 50f; lt.color = new Color(1f, 0.76f, 0.46f); lt.range = 6f; lt.intensity = 9f; lt.shadows = LightShadows.None; log.Append(" light " + t.name + " " + t.position.ToString("F2")); }  // 1653: 갓 아래 따뜻한 점광원
  if (e.nm == "house_home") {  // 0031: 2층 = room 프리팹 그대로(중첩 프리팹). room 의 fbx 원점(방 바닥 가운데)을 집 (0, 3.2, 0) 에 맞춤
    var rp = AssetDatabase.LoadAssetAtPath<GameObject>(L + "/room/room.prefab"); var ri = (GameObject)PrefabUtility.InstantiatePrefab(rp); ri.name = "ROOM_2F"; ri.transform.SetParent(root.transform, false);
    var rm = ri.transform.Find("room"); var off = rm ? rm.localPosition : Vector3.zero; ri.transform.localPosition = new Vector3(0, 3.2f, 1.35f) - off; /* 0052: 방 가운데 = 집 가운데에서 앞(+Z) 1.35m (house_home measure.json room_offset_unity) */ log.Append(" room2F offset " + off.ToString("F3"));
    /* 0351 도현 "집 안이 이상함": 방 상자 껍데기(ROOM_shell·ROOM_shellN·방 통 콜라이더)는 버리고 가구·물건만 — 벽·바닥·천장은 house_home.py 의 집 칸막이(방 벽 자리 그대로) */
    PrefabUtility.UnpackPrefabInstance(ri, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
    foreach (var t in ri.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("ROOM_shell")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
    foreach (var c in ri.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
    var lamp = ri.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "LAMP_01"); if (lamp) lamp.position += Vector3.up * 0.75f;  /* 방 천장 2.4 → 집 2층 천장 2.65(+.25) + 1608b 갓 윗면 = 천장(+.50, 줄은 다락으로) — 긴 줄이 "떠 있는 시계"로 읽힘 */
    log.Append(" room shell dropped, lamp " + (lamp ? lamp.position.y.ToString("F2") : "none"));
    /* 1608 fa9f·18e9 [다시]: 2층 = 방 2개(복도·방 뒷벽 없음) → 옛 방 뒷벽(집 z -0.15)에 걸려 있던 물건이 허공에 뜸, 동쪽 창(집 z 1.5..2.4)은 포스터·달력·사진이 가림.
       물건 묶음마다: 첫 이름의 bounds 가운데를 축으로 yaw 돌리고, 그 bounds 가 (x, z) 가운데 · 벽 쪽 면 = 벽면 ± 1cm 이 되게 옮김. 좌표 = 집 로컬(Unity, x = -Blender x, z = -Blender y) */
    System.Func<string, Transform> F = n => ri.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
    System.Action<string[], float, float, float, string, float> Put = (names, yaw, cx, cz, wallSide, wallAt) => {
      var ts = names.Select(F).Where(t => t).ToArray(); if (ts.Length == 0) { log.Append(" PUT MISS " + names[0]); return; }
      var piv = ts[0].GetComponent<Renderer>().bounds.center; foreach (var t in ts) t.RotateAround(piv, Vector3.up, yaw);
      var b = ts[0].GetComponent<Renderer>().bounds; var lmin = root.transform.InverseTransformPoint(b.min); var lmax = root.transform.InverseTransformPoint(b.max);
      var d = new Vector3(cx - (lmin.x + lmax.x) / 2, 0, cz - (lmin.z + lmax.z) / 2);
      if (wallSide == "") d.x = 0;  /* 벽 그대로, 벽 따라 z 만 */
      if (wallSide == "x-") d.x = wallAt - lmin.x; if (wallSide == "x+") d.x = wallAt - lmax.x; if (wallSide == "z+") d.z = wallAt - lmax.z;
      foreach (var t in ts) t.position += root.transform.TransformVector(d); log.Append(" put " + names[0]); };
    var books = new System.Collections.Generic.List<string> { "SHELF_01", "KART_01", "BOOKS_03", "BOX_02" }; for (int i = 1; i <= 20; i++) books.Add("BOOK_" + i.ToString("00"));
    Put(books.ToArray(), 180f, -1.3f, 0, "z+", 2.845f);                                  // 책장 = 앞벽(발코니 쪽, 문 서쪽) 안쪽 면 z 2.85 에 등, 침대 머리 뒤
    Put(new[] { "WALLCLOCK_01" }, 90f, 0, -1.1f, "x-", -4.14f);                           // 시계 = 동쪽 벽 안쪽 면(x -4.15) + 1cm, 책장(SHELF_04) 위 — 1729: 책장이 계단참(z -.65..-1.55)으로 옮겨서 따라감
    Put(new[] { "HOOK_01", "TOWEL_01" }, 90f, 0, -2.35f, "x-", -4.14f);                   // 수건 걸이 = 동쪽 벽 안쪽, 계단참(z -2.85..-1.85) — 1608b: 앞벽에선 문 옆 "떠 있는 시계"로 읽힘
    Put(new[] { "CERT_01" }, -90f, 0, 0.33f, "x+", -0.66f);                              // 상장 = 칸막이 주인공 방 쪽 면(x -0.65) + 1cm, 침대 발치 위
    Put(new[] { "POSTER_01" }, 0, 0, 1.03f, "", 0);                                      // 18e9: 창(z 1.5..2.4) 옆으로 — 포스터 z .70..1.36
    { var po = F("POSTER_01"); if (po) { var pb = po.GetComponent<Renderer>().bounds; po.position += Vector3.up * (root.transform.position.y + 3.2f + 1.22f - pb.min.y); log.Append(" poster1 y " + po.GetComponent<Renderer>().bounds.min.y.ToString("F2")); } }  // 1729 fa9f "천장 근처에 떠 보임": 바닥 +1.22 ~ +2.10(가운데 1.66, 눈높이)
    Put(new[] { "PHOTO_01", "PHOTO_02", "PHOTO_03", "PHOTO_04" }, 0, 0, 0.335f, "", 0);  // 사진 넷(묶음 z .26..0.62) = 포스터 옆 위 — 기준 = PHOTO_01 가운데(1.465 → .335, 다 같이 -1.13)
    Put(new[] { "CALENDAR_01" }, 0, 0, 0.43f, "", 0); }                                   // 달력 = 사진 아래(z .26..0.60), 스위치(z .07..0.15) 옆 11cm
  PrefabUtility.SaveAsPrefabAssetAndConnect(root, dir + "/" + e.nm + ".prefab", InteractionMode.AutomatedAction);
  root.SetActive(false);
}
// 1914: 재질을 지우고 같은 경로에 다시 만들면, 메모리의 프리팹이 옛 재질을 가리켜 NULL(마젠타)이 된다 → 프리팹 강제 재임포트
foreach (var e in POSE) AssetDatabase.ImportAsset(L + "/" + e.nm + "/" + e.nm + ".prefab", ImportAssetOptions.ForceUpdate);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return log.ToString();
