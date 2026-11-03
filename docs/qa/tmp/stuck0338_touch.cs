// 0338: 끼인 차를 건드리지 않고 입력 1 만 — 3 스텝 동안 차가 닿은 것(접점별 법선·충격량)
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var col = car.GetComponent<Collider>(); col.providesContacts = true; int carId = col.GetInstanceID();
var log = new List<string>(); int steps = 0;
Physics.ContactEventDelegate h = null;
h = (PhysicsScene ps, Unity.Collections.NativeArray<ContactPairHeader>.ReadOnly pairs) =>
{
    steps++;
    log.Add($"s{steps} pos{rb.position:F3} v{rb.linearVelocity:F3} pairs{pairs.Length}");
    for (int i = 0; i < pairs.Length; i++) for (int j = 0; j < pairs[i].PairCount; j++)
    {
        var p = pairs[i].GetContactPair(j);
        if (p.ColliderInstanceID != carId && p.OtherColliderInstanceID != carId) continue;
        int other = p.ColliderInstanceID == carId ? p.OtherColliderInstanceID : p.ColliderInstanceID;
        var oc = Resources.InstanceIDToObject(other) as Collider;
        log.Add($" | {(oc != null ? oc.name : other.ToString())} n{p.ContactCount} impSum{p.ImpulseSum:F1} carIsFirst{p.ColliderInstanceID == carId}");
        for (int k = 0; k < p.ContactCount; k++) { var c = p.GetContactPoint(k); log.Add($"    [{c.Position:F2} n{c.Normal:F2} sep{c.Separation:F3} imp{c.Impulse:F1}]"); }
    }
    if (steps >= 2) Physics.ContactEvent -= h;
};
Physics.ContactEvent += h;
car.SetMoveInput(new Vector2(0, 1));
AppDomain.CurrentDomain.SetData("c0338", log);
return "armed at " + rb.position.ToString("F2");
