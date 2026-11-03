var b = PitCrew.PackBench.PackBench.I;
var tray = b.Tray();
var rb = tray.Body;
var js = tray.GetComponents<UnityEngine.Joint>();
string jn = ""; foreach (var j in js) jn += j.GetType().Name + "->" + (j.connectedBody != null ? j.connectedBody.name : "world") + " ";
return "status=" + PitCrew.PackBench.PackBench.Qa2Status + " held=" + (b.Held != null ? b.Held.name : "none") + " kin=" + rb.isKinematic + " grav=" + rb.useGravity + " cons=" + rb.constraints + " sleep=" + rb.IsSleeping() + " mass=" + rb.mass + " v=" + rb.linearVelocity + " pos=" + rb.position + " parent=" + (tray.transform.parent != null ? tray.transform.parent.name : "-") + " joints=" + jn + " timeScale=" + UnityEngine.Time.timeScale + " autoSim=" + UnityEngine.Physics.simulationMode;