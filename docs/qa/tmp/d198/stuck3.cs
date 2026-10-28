// (93,16,-15) 주변 콜라이더(트리거 포함)·차 상태
var c = new Vector3(93, 16.5f, -15);
var sb = new System.Text.StringBuilder();
foreach (var cl in Physics.OverlapSphere(c, 8f, ~0, QueryTriggerInteraction.Collide))
  sb.Append(cl.name + " trig=" + cl.isTrigger + " par=" + (cl.transform.parent ? cl.transform.parent.name : "-") + " b=" + cl.bounds.center.ToString("F1") + cl.bounds.size.ToString("F1") + "\n");
var car = GameObject.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(r => r.mass > 100).Select(r => r.name + " " + r.position.ToString("F1") + " v" + r.linearVelocity.magnitude.ToString("F2") + " fw" + r.transform.forward.ToString("F2")).ToList();
return sb + string.Join("\n", car);
