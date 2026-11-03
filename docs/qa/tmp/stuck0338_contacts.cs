// 0338: 차가 지금 닿아 있는 것 전부(접촉 이벤트, 충격량) — 스로틀 1 을 주고 몇 스텝 기록
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var col = car.GetComponent<Collider>(); col.providesContacts = true;
var log = new List<string>(); int carId = col.GetInstanceID(); int steps = 0;
Physics.ContactEventDelegate h = null;
h = (PhysicsScene ps, Unity.Collections.NativeArray<ContactPairHeader>.ReadOnly pairs) =>
{
    if (++steps > 6) { Physics.ContactEvent -= h; return; }
    for (int i = 0; i < pairs.Length; i++) for (int j = 0; j < pairs[i].PairCount; j++)
    {
        var p = pairs[i].GetContactPair(j);
        if (p.ColliderInstanceID != carId && p.OtherColliderInstanceID != carId) continue;
        int other = p.ColliderInstanceID == carId ? p.OtherColliderInstanceID : p.ColliderInstanceID;
        var oc = Resources.InstanceIDToObject(other) as Collider;
        string s = $"s{steps} {(oc != null ? oc.transform.root.name + "/" + oc.name + " " + oc.GetType().Name : other.ToString())} n{p.ContactCount} imp{p.ImpulseSum:F1}";
        for (int k = 0; k < Mathf.Min(4, p.ContactCount); k++) { var c = p.GetContactPoint(k); s += $" [{c.Position:F2} n{c.Normal:F2} sep{c.Separation:F3}]"; }
        log.Add(s);
    }
};
Physics.ContactEvent += h;
car.SetMoveInput(new Vector2(0, 1));
AppDomain.CurrentDomain.SetData("c0338", log);
return "armed";
