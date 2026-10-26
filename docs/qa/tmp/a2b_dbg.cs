// 과제 0601 디버그 — 바다·원경 렌더러 월드 범위
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
  var top = r.transform; while (top.parent && top.parent.name != "SceneA2b") top = top.parent;
  if (top.name.StartsWith("sea") || top.name.StartsWith("far_town") || r.name == "Terrain") sb.Append(top.name + "/" + r.name + " c=" + r.bounds.center.ToString("F1") + " s=" + r.bounds.size.ToString("F1") + " " + (r.sharedMaterial ? r.sharedMaterial.name : "null") + "\n");
}
return sb.ToString();
