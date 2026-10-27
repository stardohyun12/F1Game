// 과제 1726 탐색: 토글 목록 재질의 _BaseMap 경로(파이썬 색 분석용)
var tg = GameObject.Find("SceneA4").GetComponent<SceneA3Toggles>(); var sb = new System.Text.StringBuilder();
foreach (var m in tg.materials) { if (!m || !m.HasProperty("_BaseMap")) continue; var t = m.GetTexture("_BaseMap"); sb.Append(m.name + "\t" + (m.HasProperty("_Sat") ? m.GetFloat("_Sat") : 1f) + "\t" + (t ? AssetDatabase.GetAssetPath(t) : "-") + "\n"); }
return sb.ToString();
