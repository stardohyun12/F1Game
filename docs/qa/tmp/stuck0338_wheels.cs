// 0338: 끼인 차의 주행 방식·바퀴 4개 구 레이(맞은 것·법선·거리)·힘 방향 — ApplyWheels 를 그대로 흉내
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>(); var tr = car.transform;
var T = typeof(ArcadeCarController); var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object F(string n) => T.GetField(n, BF).GetValue(car);
float wr = (float)F("wheelRadius"), ws = (float)F("wheelSpring"), lift = (float)F("bodyLift"), minN = (float)F("wheelMinNormalY");
Vector2 inset = (Vector2)F("wheelInset"); Vector3 c0 = (Vector3)F("bodyBoxCenter0"), s0 = (Vector3)F("bodyBoxSize0"); LayerMask gl = (LayerMask)F("groundLayer");
var box = car.GetComponent<BoxCollider>();
string r = $"mode{car.Mode} pos{rb.position:F2} rot{tr.eulerAngles:F1} fwd{tr.forward:F2} v{rb.linearVelocity:F3} box c{box.center:F2} s{box.size:F2} scale{tr.lossyScale:F2} grounded{F("isGrounded")} smooth{F("smoothedMove")}\n";
float sy = tr.lossyScale.y, bottom = c0.y - s0.y * .5f, startAbove = lift + wr + .1f, travelRest = startAbove - wr + rb.mass * -Physics.gravity.y / 4f / Mathf.Max(1f, ws);
float hx = s0.x * .5f * inset.x, hz = s0.z * .5f * inset.y; Vector3 sum = Vector3.zero;
for (int i = 0; i < 4; i++)
{
    var local = new Vector3(c0.x + (i % 2 == 0 ? -hx : hx), bottom + startAbove / sy, c0.z + (i < 2 ? hz : -hz));
    var start = tr.TransformPoint(local);
    var hits = Physics.SphereCastAll(start, wr, -tr.up, travelRest + .05f, gl, QueryTriggerInteraction.Ignore);
    r += $"w{i} start{start:F2} travelRest{travelRest:F3}";
    if (Physics.SphereCast(start, wr, -tr.up, out var h, travelRest + .05f, gl, QueryTriggerInteraction.Ignore))
        r += $" FIRST {h.collider.name} d{h.distance:F3} n{h.normal:F2} pt{h.point:F2} ok{(h.distance < travelRest && h.normal.y >= minN && !h.collider.attachedRigidbody)}";
    else r += " FIRST none";
    foreach (var hh in hits) r += $"\n    all {hh.collider.name} d{hh.distance:F3} n{hh.normal:F2}";
    var ov = Physics.OverlapSphere(start, wr, gl, QueryTriggerInteraction.Ignore);
    r += $"\n    overlapAtStart{ov.Length}"; foreach (var o in ov) r += " " + o.name;
    r += "\n";
}
foreach (var o in Physics.OverlapBox(box.bounds.center, box.bounds.extents, Quaternion.identity, gl, QueryTriggerInteraction.Ignore)) r += "boxAABBoverlap " + o.name + "\n";
return r;
