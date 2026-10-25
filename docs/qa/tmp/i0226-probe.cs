var sb = new System.Text.StringBuilder();
foreach (var path in UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/BayLoop/People", "Assets/Art/BayLoop/Landmarks", "Assets/Art/BayLoop/Buildings", "Assets/Art/BayLoop/Props" }).Select(UnityEditor.AssetDatabase.GUIDToAssetPath))
{
  if (!(path.Contains("People") || path.Contains("lighthouse") || path.Contains("crane") || path.Contains("chapel") || path.Contains("FishMarketRoof") || path.Contains("teahouse") || path.Contains("bus_stop") || path.Contains("kiosk") || path.Contains("farmhouse") || path.Contains("tea_"))) continue;
  var mi = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
  var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var rs = go.GetComponentsInChildren<Renderer>(true);
  var b = new Bounds(); bool first = true; foreach (var r in rs) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
  var clips = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => c.name + ":" + c.length.ToString("F2") + "s loop=" + c.isLooping);
  var mats = rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name + "/" + m.shader.name + "/" + (m.mainTexture ? m.mainTexture.name : "-")).Distinct();
  sb.Append(path + " anim=" + mi.animationType + " rig=" + (mi.avatarSetup) + " scale=" + mi.globalScale + " smr=" + rs.Count(r => r is SkinnedMeshRenderer) + " r=" + rs.Length + " size=" + b.size.ToString("F2") + " center=" + b.center.ToString("F2") + "\n  clips=" + string.Join(",", clips) + "\n  mats=" + string.Join(",", mats) + "\n  kids=" + string.Join(",", go.transform.Cast<Transform>().Select(t => t.name)) + "\n");
}
return sb.ToString();
