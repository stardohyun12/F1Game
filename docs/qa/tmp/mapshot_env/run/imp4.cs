// mapshot 1856: FBX 하나 — 복사 → 동기 ImportAsset → 임포터 설정 → 재임포트. env_camo_204695467_v01 치환해서 호출.
var sb = new System.Text.StringBuilder();
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Game_shot/F1/Assets")) return "FAIL: 사본 에디터가 아님";
string n = "env_camo_204695467_v01"; string fp = "Assets/_TmpEnv/" + n + ".fbx";
var sw = System.Diagnostics.Stopwatch.StartNew();
System.IO.File.Copy("D:/Github/F1Game/art/export/env-first/" + n + ".fbx", Application.dataPath + "/_TmpEnv/" + n + ".fbx", true);
AssetDatabase.ImportAsset(fp, ImportAssetOptions.ForceSynchronousImport);
var mi = (ModelImporter)AssetImporter.GetAtPath(fp);
mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
mi.SaveAndReimport();
var go = AssetDatabase.LoadAssetAtPath<GameObject>(fp); var names = new System.Collections.Generic.HashSet<string>(); int tris = 0;
foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { foreach (var m in r.sharedMaterials) names.Add(m ? m.name : "null"); var mf = r.GetComponent<MeshFilter>(); if (mf) tris += mf.sharedMesh.triangles.Length / 3; }
return fp + " mats=" + string.Join(",", names) + " tris=" + tris + " " + sw.ElapsedMilliseconds + "ms";
