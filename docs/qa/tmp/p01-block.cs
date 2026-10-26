var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var p = car.transform.position; var f = car.transform.forward;
var s = "pos=" + p + " fwd=" + f + " | ";
foreach (var h in Physics.RaycastAll(p + Vector3.up * 0.3f, f, 4f)) s += "R:" + h.collider.name + "/" + h.collider.transform.parent?.name + "@" + h.distance.ToString("F2") + " ";
foreach (var c in Physics.OverlapBox(p + f * 1.5f + Vector3.up * 0.5f, new Vector3(1.2f, 0.5f, 1.5f), car.transform.rotation)) if (!c.transform.IsChildOf(car.transform)) s += "O:" + c.name + "/" + c.transform.parent?.name + " ";
return s;
