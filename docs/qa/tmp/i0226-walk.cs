var sb = new System.Text.StringBuilder();
var path = "Assets/Art/BayLoop/People/people_young_01.fbx";
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
var go = (GameObject)UnityEngine.Object.Instantiate(model);
var bones = go.GetComponentsInChildren<Transform>(true);
sb.Append("bones: " + string.Join(",", bones.Select(b => b.name)) + "\n");
foreach (var clip in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
{
  var feet = bones.Where(b => b.name.ToLower().Contains("foot") || b.name.ToLower().Contains("toe")).ToArray();
  sb.Append(clip.name + " frames " + (clip.length * clip.frameRate).ToString("F0") + " fps " + clip.frameRate + " root motion? " + clip.hasGenericRootTransform + "\n");
  foreach (var f in feet)
  {
    float minY = 1e9f, maxY = -1e9f, minZ = 1e9f, maxZ = -1e9f; var ys = new System.Collections.Generic.List<Vector3>();
    int N = 40;
    for (int i = 0; i <= N; i++) { clip.SampleAnimation(go, clip.length * i / N); var p = go.transform.InverseTransformPoint(f.position); ys.Add(p); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
    // 접지 구간(바닥 근처 y < min+0.02) 동안 z 속도
    float sumV = 0; int nV = 0; float dt = clip.length / N;
    for (int i = 0; i < N; i++) if (ys[i].y < minY + 0.02f && ys[i + 1].y < minY + 0.02f) { sumV += (ys[i].z - ys[i + 1].z) / dt; nV++; }
    sb.Append($"  {f.name} y {minY:F3}..{maxY:F3} z {minZ:F3}..{maxZ:F3} stanceV {(nV > 0 ? sumV / nV : 0):F3} m/s (n{nV})\n");
  }
}
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
