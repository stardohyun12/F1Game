// artlive 20261031-1926: 비탈 검은 띠 원인 3 — 그림자만 드리우는(안 보이는) 렌더러·큰 물체 찾기(읽기만)
var sb = new System.Text.StringBuilder();
var fo = GameObject.Find("STUDIO/STUDIO_Slot/forest").transform;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
  var b = r.bounds; bool big = b.size.y > 6 || b.size.x > 25;
  if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly || (big && !r.transform.IsChildOf(fo)))
    sb.Append(r.name + " <" + (r.transform.parent ? r.transform.parent.name : "-") + "> cast " + r.shadowCastingMode + " en " + r.enabled + " " + b.min.ToString("F0") + ".." + b.max.ToString("F0") + "\n"); }
return sb.ToString();
