var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var so = new UnityEditor.SerializedObject(d);
string s = "";
foreach (var n in new[]{"spawn","calendar","bed","cartSpot","boss"}) { var p = so.FindProperty(n); if (p == null) { s += n+"=?; "; continue; } var o = p.objectReferenceValue as Component; s += n + "=" + (o != null ? o.name + " " + o.transform.position.ToString("F2") + " rotY=" + o.transform.eulerAngles.y.ToString("F0") : "null") + "; "; }
foreach (var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) { var nm = go.name.ToLower(); if (nm.Contains("bed") || nm.Contains("침대") || nm.Contains("diary") || nm.Contains("calendar")) s += "\n" + go.name + " " + go.position.ToString("F2") + " parent=" + (go.parent != null ? go.parent.name : "-"); }
return s;
