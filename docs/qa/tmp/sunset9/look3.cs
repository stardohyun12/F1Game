// Play: 판자·소품 칠을 __MUL__ (rgb 배수) 만큼 옮겨 시험 — 원래 색은 DuskForeground.paint 에 그대로
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>();
var mul = new Color(__MUL__); int n = 0; var seen = new System.Collections.Generic.HashSet<Material>();
foreach (var r in fg.GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m && seen.Add(m)) { var c = m.GetColor("_Tint"); m.SetColor("_Tint", new Color(c.r * mul.r, c.g * mul.g, c.b * mul.b)); n++; }
return "mats=" + n;
