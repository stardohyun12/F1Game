var s = SunsetLook.Find(); var sb = new System.Text.StringBuilder();
foreach (Transform c in s.backdrop.transform) { sb.Append(c.name + " act=" + c.gameObject.activeSelf + " n=" + c.childCount + " comps="); foreach (var k in c.GetComponents<Component>()) sb.Append(k.GetType().Name + ","); sb.Append("\n"); }
foreach (var r in s.hideWhileOn) sb.Append("hide " + (r ? r.name + " en=" + r.enabled : "-") + "\n");
var fg = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
foreach (var t in fg) if (t.name.Contains("Foreground") || t.name.Contains("Bucket") || t.name.Contains("Dock")) sb.Append("FG " + t.name + " par=" + (t.parent ? t.parent.name : "") + " act=" + t.gameObject.activeInHierarchy + "\n");
return sb.ToString();
