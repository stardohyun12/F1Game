// artlive 20261031-1926: 비탈 검은 띠 원인 2 — 점광원 위치·세기·범위(읽기만)
var sb = new System.Text.StringBuilder();
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append(l.name + " " + l.type + " pos " + l.transform.position.ToString("F1") + " int " + l.intensity + " col " + l.color + " range " + l.range + " active " + l.gameObject.activeInHierarchy + " path " + (l.transform.parent ? l.transform.parent.name : "-") + "\n");
var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Review/forest/M_forest_slope.mat"); var sh = m.shader;
for (int i = 0; i < sh.GetPropertyCount(); i++) sb.Append(sh.GetPropertyName(i) + "=" + (sh.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Float || sh.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Range ? m.GetFloat(sh.GetPropertyName(i)).ToString() : sh.GetPropertyType(i).ToString()) + " ");
return sb.ToString();
