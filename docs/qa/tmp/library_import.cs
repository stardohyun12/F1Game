// artlive 1534 (D-191): 건물 라이브러리 Assets/Art/Library/Buildings/<이름>/ — 텍스처·fbx 임포트 설정, 폴더 전용 A7 재질, 프리팹(피벗 = 바닥 앞 가운데, 정면 +Z, BoxCollider).
// Studio_Building 이 열려 있어야 한다. 프리팹 인스턴스는 STUDIO/STUDIO_Slot/<이름> 으로 두고 꺼 둔다. 다시 돌리면 덮어쓴다.
// 자세(모델 자식의 회전·이동)는 아래 POSE 표. 재질 = SA7_house_shop.mat 의 사본(원본은 읽기만), 텍스처만 바꿈.
var log = new System.Text.StringBuilder();
const string L = "Assets/Art/Library/Buildings";
AssetDatabase.Refresh();
// 이름, fbx, 회전 Y, 이동 규칙("keep" = fbx 원점 그대로 / "front" = 가로 가운데·앞면 최대 z 를 0 으로)
var POSE = new (string nm, string fbx, float yaw, string rule)[] {
  ("room", "room.fbx", 0f, "front"), ("garage", "garage_v5_combo.fbx", 0f, "front"), ("depot", "q3_depot.fbx", 0f, "keep"), ("postoffice", "q4_postoffice.fbx", 0f, "keep") };
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
foreach (var p in new[] { "_GroundY", "_FootDark", "_Blotch", "_Lift", "_Sat", "_A7Lit", "_Cutoff", "_FogStart", "_FogEnd", "_FogMax" }) if (tmpl.HasProperty(p)) log.Append(p + "=" + tmpl.GetFloat(p) + " "); log.Append("\n");
System.Func<string, string, string> TexFor = (nm, slot) => { var s = slot.ToLower();
  if (nm == "room") return "room";
  if (nm == "garage") { if (s.Contains("lamp")) return "garage_lamp"; if (s.Contains("props")) return "garage_props"; if (s.Contains("posters")) return "garage_posters"; if (s.Contains("dirt")) return "garage_dirt"; if (s.Contains("albedo")) return "garage_baked32"; if (s.Contains("festival")) return "garage_festival"; if (s.Contains("race_photo_0")) return "garage_memory_1"; if (s.Contains("race_photo_1")) return "garage_memory_2"; if (s.Contains("race_photo_2")) return "garage_memory_3"; if (s.Contains("sign")) return "garage_sign"; if (s.Contains("detail")) return "garage_details"; return null; }
  if (s.Contains("glass")) return "glass";  // 1846: 우체국 창 유리
  if (s.Contains("clock_dial")) return "clock_dial";  // 1857: 보관소 시계 문자판·바늘
  foreach (var k in new[] { "icons", "wall", "trim", "floor", "roof", "decal", "postbox" }) if (s.Contains(k)) return k == "decal" ? "decals" : k == "postbox" ? "postbox_G1" : k; return null; };
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var ONLY = new[] { "garage", "depot" };  // 1857: 물건별 오브젝트(정비소·보관소)
foreach (var e in POSE) { if (!ONLY.Contains(e.nm)) continue;
  var dir = L + "/" + e.nm;
  foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { dir })) { var tp = AssetDatabase.GUIDToAssetPath(g); var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
    ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = tp.EndsWith("/wall.png") || tp.EndsWith("/roof.png");  /* 1846: 큰 면만 밉맵(Point 유지) — 멀리서 지글거림 */ ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.alphaIsTransparency = tp.Contains("decals") || tp.Contains("icons") || tp.Contains("clock_dial"); ti.SaveAndReimport(); }
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
  else { var bc = root.AddComponent<BoxCollider>(); bc.center = b1.center; bc.size = b1.size; }
  // 1857: 만질 수 있는 물건 = 낱개 BoxCollider (이름 종류_번호 · CLOCK). 시계엔 WallClock(바늘 축 = 정면 +Z = 문자판 법선)
  int nItem = 0; foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) { var nm = mf.name;
    if (!(System.Text.RegularExpressions.Regex.IsMatch(nm, "^[A-Z]+_\\d+$") || nm == "CLOCK")) continue; mf.gameObject.AddComponent<BoxCollider>(); nItem++; }
  log.Append(" items " + nItem);
  var ck = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CLOCK");
  if (ck) { var wt = System.Type.GetType("WallClock, Assembly-CSharp"); var hh = ck.Find("CLOCK_hand_hour"); var hm = ck.Find("CLOCK_hand_min");
    if (wt != null && hh && hm) { var wc = ck.gameObject.AddComponent(wt); var so = new SerializedObject(wc);
      so.FindProperty("hourHand").objectReferenceValue = hh; so.FindProperty("minuteHand").objectReferenceValue = hm;
      so.FindProperty("axisLocal").vector3Value = hh.InverseTransformDirection(-root.transform.forward).normalized; so.ApplyModifiedPropertiesWithoutUndo();
      log.Append(" clock axis " + hh.InverseTransformDirection(-root.transform.forward).normalized.ToString("F2") + " center " + hh.position.ToString("F2")); }
    else log.Append(" CLOCK MISS type=" + (wt != null) + " hands=" + (hh != null) + "/" + (hm != null)); }
  foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (System.Text.RegularExpressions.Regex.IsMatch(t.name, "^LAMP_\\d$")) {  /* 1857: LAMP_01 등 = 등 메시(빛 아님) */ var lt = t.gameObject.AddComponent<Light>(); lt.type = LightType.Spot; t.rotation = Quaternion.Euler(90f, 0f, 0f); lt.spotAngle = 120f; lt.innerSpotAngle = 50f; lt.color = new Color(1f, 0.76f, 0.46f); lt.range = 6f; lt.intensity = 9f; lt.shadows = LightShadows.None; log.Append(" light " + t.name + " " + t.position.ToString("F2")); }  // 1653: 갓 아래 따뜻한 점광원
  PrefabUtility.SaveAsPrefabAssetAndConnect(root, dir + "/" + e.nm + ".prefab", InteractionMode.AutomatedAction);
  root.SetActive(false);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return log.ToString();
