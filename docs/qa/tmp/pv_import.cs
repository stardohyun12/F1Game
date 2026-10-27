// 과제 0535: 후보 4개를 Assets/Art/PreviewCand/ 로 복사·임포트(텍스처 None·Point·밉맵 끔, 재질 임포트 끔) + 재질 슬롯 이름 조사
string ex = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/"));
string D = "Assets/Art/PreviewCand/", ad = Application.dataPath + "/Art/PreviewCand/";
var log = new System.Text.StringBuilder();
if (!AssetDatabase.IsValidFolder("Assets/Art/PreviewCand")) AssetDatabase.CreateFolder("Assets/Art", "PreviewCand");
var src = new System.Collections.Generic.Dictionary<string, string[]> {
  { "garage", new[] { "export/q2-garage/garage_v5_combo.fbx", "export/q2-garage/garage_baked32.png", "export/q2-garage/garage_sign.png", "export/q2-garage/garage_details.png", "export/q2-garage/garage_festival.png", "export/q2-garage/garage_memory_1.png", "export/q2-garage/garage_memory_2.png", "export/q2-garage/garage_memory_3.png" } },
  { "depot", new[] { "export/q3-depot/q3_depot.fbx", "export/q3-depot/wall.png", "export/q3-depot/trim.png", "export/q3-depot/floor.png", "export/q3-depot/roof.png", "export/q3-depot/decals.png" } },
  { "kit", new[] { "source/kit-shared-v2/proof.fbx", "export/kit-shared-v2/wall.png", "export/kit-shared-v2/trim.png", "export/kit-shared-v2/floor.png", "export/kit-shared-v2/roof.png", "export/kit-shared-v2/decals.png" } },
  { "room", new[] { "export/q1-room/room.fbx", "export/q1-room/room.png" } } };
foreach (var kv in src) { if (!AssetDatabase.IsValidFolder(D + kv.Key)) AssetDatabase.CreateFolder("Assets/Art/PreviewCand", kv.Key);
  foreach (var f in kv.Value) System.IO.File.Copy(ex + f, ad + kv.Key + "/" + System.IO.Path.GetFileName(f), true); }
AssetDatabase.Refresh();
foreach (var kv in src) foreach (var f in kv.Value) { string p = D + kv.Key + "/" + System.IO.Path.GetFileName(f);
  if (p.EndsWith(".png")) { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = TextureWrapMode.Repeat; ti.alphaIsTransparency = p.Contains("decals"); ti.SaveAndReimport(); }
  else { var mi = (ModelImporter)AssetImporter.GetAtPath(p); mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab; mi.isReadable = true; mi.SaveAndReimport();
    var go = AssetDatabase.LoadAssetAtPath<GameObject>(p); var b = new Bounds(); bool f0 = false; int nr = 0;
    var slots = new System.Collections.Generic.HashSet<string>();
    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { nr++; foreach (var m in r.sharedMaterials) slots.Add(m ? m.name : "null"); var mf = r.GetComponent<MeshFilter>(); }
    var inst = (GameObject)UnityEngine.Object.Instantiate(go); foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!f0) { b = r.bounds; f0 = true; } else b.Encapsulate(r.bounds); }
    log.Append(p + " root " + go.transform.localEulerAngles.ToString("F0") + " sc " + go.transform.localScale.ToString("F2") + " renderers " + nr + " bounds c " + b.center.ToString("F2") + " s " + b.size.ToString("F2") + "\n  slots " + string.Join(",", slots) + "\n  children " + string.Join(",", go.GetComponentsInChildren<Transform>(true).Take(40).Select(t => t.name)) + "\n");
    UnityEngine.Object.DestroyImmediate(inst); } }
return log.ToString();
