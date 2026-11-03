var t = PackTable2.I;
var bx = t.WorkBox.Box;
var sb = new System.Text.StringBuilder();
sb.Append("box tr=" + bx.transform.position.ToString("F3") + " rb=" + bx.Body.position.ToString("F3") + " work=" + t.workSpot.position.ToString("F3") + " active=" + bx.gameObject.activeInHierarchy);
foreach (var c in bx.GetComponentsInChildren<UnityEngine.Collider>(true)) sb.Append(" | " + c.name + " en=" + c.enabled + " b=" + c.bounds.center.ToString("F3") + "/" + c.bounds.size.ToString("F3") + " trig=" + c.isTrigger);
sb.Append(" | rbs:");
foreach (var r in bx.GetComponentsInChildren<UnityEngine.Rigidbody>(true)) sb.Append(" " + r.name + " kin=" + r.isKinematic);
return sb.ToString();
