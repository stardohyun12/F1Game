// 0338: 끼였던 자리에 정지 상태로 다시 놓고 입력 1 — 40 스텝 동안 위치·속도·접촉 기록(컨트롤러 그대로, 입력 1)
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var col = car.GetComponent<Collider>(); col.providesContacts = true; int carId = col.GetInstanceID();
var log = new List<string>(); int steps = 0;
Physics.ContactEventDelegate h = null;
h = (PhysicsScene ps, Unity.Collections.NativeArray<ContactPairHeader>.ReadOnly pairs) =>
{
    steps++;
    string s = $"s{steps} pos{rb.position:F2} v{rb.linearVelocity:F2} pairs{pairs.Length}";
    for (int i = 0; i < pairs.Length; i++) for (int j = 0; j < pairs[i].PairCount; j++)
    {
        var p = pairs[i].GetContactPair(j);
        if (p.ColliderInstanceID != carId && p.OtherColliderInstanceID != carId) continue;
        int other = p.ColliderInstanceID == carId ? p.OtherColliderInstanceID : p.ColliderInstanceID;
        var oc = Resources.InstanceIDToObject(other) as Collider;
        s += $" | {(oc != null ? oc.name : other.ToString())} n{p.ContactCount} imp{p.ImpulseSum:F1}";
        for (int k = 0; k < Mathf.Min(3, p.ContactCount); k++) { var c = p.GetContactPoint(k); s += $" [{c.Position:F2} n{c.Normal:F2} sep{c.Separation:F3}]"; }
    }
    if (steps % 4 == 1 || steps < 4) log.Add(s);
    if (steps >= 40) Physics.ContactEvent -= h;
};
Physics.ContactEvent += h;
var P = new Vector3(-197.14f, 4.49f, 5.06f); var R = Quaternion.LookRotation(new Vector3(0.75f, 0, -0.66f)); rb.position = P; rb.rotation = R; car.transform.SetPositionAndRotation(P, R); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; Physics.SyncTransforms(); rb.WakeUp(); car.SetMoveInput(new Vector2(0, 1));
AppDomain.CurrentDomain.SetData("c0338", log);
return "pushed fwd " + car.transform.forward.ToString("F2") + " from " + rb.position.ToString("F2");
