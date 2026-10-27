// 과제 0236-1: 정비소 바닥 동그란 얼룩 출처 찾기 — 정비소 안 바닥·천장에 닿는 렌더러와 그 재질 값
var root = GameObject.Find("SceneA4").transform; var log = new System.Text.StringBuilder();
var gb = new Bounds(); bool f0 = false; foreach (var r in root.Find("garage_v5_combo").GetComponentsInChildren<Renderer>()) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
var seen = new System.Collections.Generic.HashSet<Material>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
  var b = r.bounds; if (!b.Intersects(gb)) continue; if (b.size.x * b.size.z < 20f) continue;   // 바닥·천장처럼 넓은 것만
  log.Append(r.transform.parent?.name + "/" + r.name + " size " + b.size.ToString("F1") + " miny " + b.min.y.ToString("F1") + " mat ");
  foreach (var m in r.sharedMaterials) { if (!m) continue; log.Append(m.name + " "); if (seen.Add(m) && m.HasProperty("_Blotch")) log.Append("[Blotch " + m.GetFloat("_Blotch") + " size " + m.GetFloat("_BlotchSize") + " FootDark " + m.GetFloat("_FootDark") + " SN " + m.GetFloat("_SN") + " tex " + (m.GetTexture("_BaseMap") ? m.GetTexture("_BaseMap").name : "-") + "] "); }
  log.Append("\n"); }
return log.ToString();
