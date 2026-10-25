var bg = GameObject.Find("Buildings"); var th = GameObject.Find("TUT_TeaHouse");
var s = "Buildings children=" + (bg != null ? bg.transform.childCount : -1);
if (bg != null) s += " first=" + string.Join(",", bg.transform.Cast<Transform>().Take(5).Select(t => t.name + "@" + t.position.ToString("F0")));
if (th != null) s += "\ntea " + th.transform.position.ToString("F1") + " fwd " + th.transform.forward.ToString("F2");
var lm = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("LANDMARK_")).Select(t => t.name + " " + t.position.ToString("F0") + " yaw" + t.eulerAngles.y.ToString("F0"));
return s + "\n" + string.Join("\n", lm);
