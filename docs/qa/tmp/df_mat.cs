var car = GameObject.FindFirstObjectByType<ArcadeCarController>();
var bc = car.GetComponent<BoxCollider>();
string M(PhysicsMaterial m) => m == null ? "null(default 0.6/0.6 avg)" : m.name + " d=" + m.dynamicFriction + " s=" + m.staticFriction + " comb=" + m.frictionCombine;
var sb = new System.Text.StringBuilder("car=" + M(bc.sharedMaterial) + " | ");
RaycastHit h;
if (Physics.Raycast(car.transform.position + Vector3.up, Vector3.down, out h, 5f, ~0, QueryTriggerInteraction.Ignore))
    sb.Append("under=" + h.collider.name + " " + M(h.collider.sharedMaterial) + " | ");
foreach (var p in new[]{ new Vector3(86.16f, 6f, -192.19f), new Vector3(-86.5f, 10f, -77.7f) })
    if (Physics.Raycast(p, Vector3.down, out h, 30f, ~0, QueryTriggerInteraction.Ignore))
        sb.Append(h.collider.name + " " + M(h.collider.sharedMaterial) + " | ");
return sb.ToString();
