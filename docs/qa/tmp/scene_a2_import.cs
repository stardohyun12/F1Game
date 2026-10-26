// 과제 0546 — SceneA2 임포트: 텍스처 Point·밉맵 끔·비압축, FBX 경계 확인
AssetDatabase.Refresh();
var s = new System.Text.StringBuilder(); int nt = 0;
foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/SceneA2" }))
{
  var p = AssetDatabase.GUIDToAssetPath(g); var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
  if (ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize < 4096)
  { ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
  nt++;
}
s.Append("tex " + nt + "\n");
foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/SceneA2" }))
{
  var p = AssetDatabase.GUIDToAssetPath(g); var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
  var inst = (GameObject)UnityEngine.Object.Instantiate(go); var rs = inst.GetComponentsInChildren<Renderer>();
  if (rs.Length == 0) { s.Append(p + " no renderer\n"); UnityEngine.Object.DestroyImmediate(inst); continue; }
  var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
  s.Append($"{System.IO.Path.GetFileNameWithoutExtension(p)} rot={inst.transform.eulerAngles:F0} n={rs.Length} c={b.center:F2} s={b.size:F2}" + (rs.Length > 1 && rs.Length < 30 ? " [" + string.Join(",", System.Linq.Enumerable.Select(rs, r => r.name)) + "]" : "") + "\n");
  UnityEngine.Object.DestroyImmediate(inst);
}
return s.ToString();
