var b = PitCrew.PackBench.PackBench.I;
var rb = b.Tray().Body;
var before = rb.position;
rb.AddForce(UnityEngine.Vector3.up * 1f, UnityEngine.ForceMode.VelocityChange);
var cs = rb.GetComponentsInChildren<UnityEngine.Collider>();
string cn = ""; foreach (var c in cs) cn += c.name + ":" + c.GetType().Name + (c.enabled ? "" : "(off)") + " ";
var near = UnityEngine.Physics.OverlapBox(rb.position, new UnityEngine.Vector3(0.16f, 0.04f, 0.16f));
string nn = ""; foreach (var c in near) nn += c.name + "(" + (c.attachedRigidbody != null ? c.attachedRigidbody.name + (c.attachedRigidbody.isKinematic ? ",kin" : ",dyn") : "static") + ") ";
return "pos=" + before.ToString("F3") + " cols=" + cn + "\nnear=" + nn + " enabled=" + b.enabled + " SimActive=" + b.SimActive;