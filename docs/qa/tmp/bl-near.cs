var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var p = car.transform.position; var sb = new System.Text.StringBuilder();
sb.Append("pos " + p.ToString("F2") + " fwd " + car.transform.forward.ToString("F2") + " up " + car.transform.up.ToString("F2") + "\n");
foreach (var c in Physics.OverlapSphere(p, 4f)) { if (c.attachedRigidbody == car.GetComponent<Rigidbody>()) continue; sb.Append(c.name + " parent=" + (c.transform.parent ? c.transform.parent.name : "-") + " closest=" + c.ClosestPoint(p).ToString("F1") + "\n"); }
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
sb.Append("input " + mi.GetValue(car));
return sb.ToString();
