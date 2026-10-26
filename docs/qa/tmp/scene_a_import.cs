AssetDatabase.Refresh();
var s = new System.Text.StringBuilder(); int nt = 0;
foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/SceneA" }))
{
  var p = AssetDatabase.GUIDToAssetPath(g); var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
  if (ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.alphaIsTransparency == false)
  { ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
  nt++;
}
s.Append("tex " + nt + "\n");
foreach (var p in new[] { "Assets/Art/SceneA/V0/garage_v5.fbx", "Assets/Art/SceneA/V0/kart_v5.fbx", "Assets/Art/SceneA/V0/garage_props_v5.fbx", "Assets/Art/SceneA/V1/env_v1.fbx", "Assets/Art/SceneA/V1/kart_v1.fbx", "Assets/Art/SceneA/V2/garage_v2.fbx" })
{
  var go = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (go == null) { s.Append(p + " NULL\n"); continue; }
  var inst = (GameObject)UnityEngine.Object.Instantiate(go);
  var rs = inst.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
  var mats = new System.Collections.Generic.HashSet<string>();
  foreach (var r in rs) foreach (var m in r.sharedMaterials) if (m != null) { var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null; var e = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null; mats.Add(m.name + ":" + (t ? t.name : "-") + "/" + (e ? e.name : "-")); }
  s.Append($"{p} rot={inst.transform.eulerAngles} rends={rs.Length} b.c={b.center:F2} b.s={b.size:F2}\n  {string.Join(" ", mats)}\n");
  UnityEngine.Object.DestroyImmediate(inst);
}
return s.ToString();
