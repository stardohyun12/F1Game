// Play: 나무(A7) 그늘·판자 그늘 시험 — __A7__ = a7Shade rgb, __FGA__ = 판자 _Ambient rgb
var sl = SunsetLook.Find();
sl.a7Shade = new Color(__A7__); sl.Apply();
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>();
int n = 0; var seen = new System.Collections.Generic.HashSet<Material>();
foreach (var r in fg.GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m && seen.Add(m)) { m.SetColor("_Ambient", new Color(__FGA__)); n++; }
return "a7=" + sl.a7Shade + " fgMats=" + n;
