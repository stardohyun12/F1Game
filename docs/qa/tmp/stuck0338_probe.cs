// 0338: 끼인 차 둘레 — 차 앞 3m 안 콜라이더(트리거 포함)·앞으로 쏜 레이·리지드바디 상태
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>(); var t = car.transform;
string r = $"car {t.position:F2} fwd {t.forward:F2} rb.v {rb.linearVelocity:F2} cons {rb.constraints} damp {rb.linearDamping} mass {rb.mass} sleep {rb.IsSleeping()}\n";
foreach (var c in car.GetComponentsInChildren<Collider>(true)) r += $" mycol {c.name} {c.GetType().Name} trig{c.isTrigger} en{c.enabled} b{c.bounds.center:F1}/{c.bounds.size:F1} layer {LayerMask.LayerToName(c.gameObject.layer)}\n";
foreach (var c in Physics.OverlapSphere(t.position + t.forward * 1.5f, 3f, ~0, QueryTriggerInteraction.Collide))
  if (c.GetComponentInParent<ArcadeCarController>() == null) r += $" near {c.transform.root.name}/{c.name} {c.GetType().Name} trig{c.isTrigger} b{c.bounds.center:F1}/{c.bounds.size:F1} layer {LayerMask.LayerToName(c.gameObject.layer)}\n";
foreach (var h in new[] { 0.1f, 0.25f, 0.5f, 0.9f }) { var o = t.position + Vector3.up * h - t.forward * 0.2f; foreach (var hit in Physics.RaycastAll(o, t.forward, 4f, ~0, QueryTriggerInteraction.Collide)) if (hit.collider.GetComponentInParent<ArcadeCarController>() == null) r += $" ray h{h} d{hit.distance:F2} {hit.collider.transform.root.name}/{hit.collider.name} n{hit.normal:F1} trig{hit.collider.isTrigger}\n"; }
return r;
