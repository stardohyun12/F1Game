// mapshot 2020 (v2): FBX 8개 — 하나씩 복사 → 동기 ImportAsset → 임포터 설정 → 재임포트. (v1 은 FBX 당 ~1초라 한 exec 에 묶어도 60초 안쪽)
var sb = new System.Text.StringBuilder();
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Game_shot/F1/Assets")) return "FAIL: 사본 에디터가 아님";
var all = new[] { "env_camo_204693578_v02", "env_camo_204694937_v02", "env_camo_204695159_v02", "env_camo_204695467_v02", "env_camo_204696009_v02", "env_camo_204697918_v02", "env_camo_204701091_v02", "env_tomo_1355595398_v02" };
var tsw = System.Diagnostics.Stopwatch.StartNew();
foreach (var n in all) {
  if (tsw.ElapsedMilliseconds > 40000) { sb.AppendLine("STOP: 시간 상자, 남은 것은 다시 호출 " + n); break; }
  string fp = "Assets/_TmpEnv/" + n + ".fbx";
  if (AssetDatabase.LoadAssetAtPath<GameObject>(fp) != null) { sb.AppendLine("skip(이미 있음) " + n); continue; }
  var sw = System.Diagnostics.Stopwatch.StartNew();
  System.IO.File.Copy("D:/Github/F1Game/art/export/env-first-v2/" + n + ".fbx", Application.dataPath + "/_TmpEnv/" + n + ".fbx", true);
  AssetDatabase.ImportAsset(fp, ImportAssetOptions.ForceSynchronousImport);
  var mi = (ModelImporter)AssetImporter.GetAtPath(fp);
  mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
  mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
  mi.SaveAndReimport();
  var go = AssetDatabase.LoadAssetAtPath<GameObject>(fp); var names = new System.Collections.Generic.HashSet<string>(); int tris = 0; int nm = 0;
  foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { nm++; foreach (var m in r.sharedMaterials) names.Add(m ? m.name : "null"); var mf = r.GetComponent<MeshFilter>(); if (mf) tris += mf.sharedMesh.triangles.Length / 3; }
  sb.AppendLine(n + " renderers=" + nm + " mats=" + string.Join(",", names) + " tris=" + tris + " rootScale=" + go.transform.localScale + " " + sw.ElapsedMilliseconds + "ms");
}
return sb.ToString();
