var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var p = car.transform.position;
var s = "v=" + rb.linearVelocity.magnitude.ToString("F2") + " kin=" + rb.isKinematic + " cs=" + rb.constraints + " en=" + car.enabled + " | ";
foreach (var c in Physics.OverlapSphere(p, 4f, ~0, QueryTriggerInteraction.Collide))
    if (!c.transform.IsChildOf(car.transform)) s += c.name + "(" + c.GetType().Name + (c.isTrigger ? ",T" : "") + ",L" + c.gameObject.layer + ") ";
var bh = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(m => m.GetType().Name.Contains("Respawn") || m.GetType().Name.Contains("Guard") || m.GetType().Name.Contains("Bound")).Select(m => m.GetType().Name + ":" + m.enabled);
return s + " || " + string.Join(",", bh);
