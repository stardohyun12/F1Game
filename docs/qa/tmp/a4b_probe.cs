// 과제 1302 탐색: A4 씬의 정비소·흙 재질, 길 중심/높이(z -15..-60)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4_Town.unity");
var sb = new System.Text.StringBuilder();
var mats = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  foreach (var m in r.sharedMaterials) { if (!m) continue; var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null; string k = m.name + "<" + (t ? AssetDatabase.GetAssetPath(t) : "-") + ">";
    if (k.Contains("garage") || k.Contains("dirt")) { mats.TryGetValue(k, out var n); mats[k] = n + 1; } } }
foreach (var kv in mats) sb.Append(kv.Key + " x" + kv.Value + "\n");
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (r.sharedMesh && AssetDatabase.GetAssetPath(r.sharedMesh).Contains("dirt")) sb.Append("dirt obj " + r.name + " " + AssetDatabase.GetAssetPath(r.sharedMesh) + "\n");
for (float z = -14; z >= -64; z -= 6) { float xs = 0, ys = 0, mn = 999, mx = -999; int n = 0;
  for (float x = -25; x <= 45; x += 0.5f) foreach (var h in Physics.RaycastAll(new Vector3(x, 40, z), Vector3.down, 100)) if (h.collider.name == "Road") { xs += x; ys += h.point.y; n++; mn = Mathf.Min(mn, x); mx = Mathf.Max(mx, x); }
  RaycastHit hh; sb.Append("z" + z + " road x " + mn + ".." + mx + " y " + (n > 0 ? ys / n : 0).ToString("F1") + "\n"); }
var b = GameObject.Find("SceneA4").transform.Find("A4_BldgNew/background_building_03_22"); sb.Append("bb03 " + (b ? b.GetComponent<Renderer>().bounds.ToString() : "none"));
return sb.ToString();
