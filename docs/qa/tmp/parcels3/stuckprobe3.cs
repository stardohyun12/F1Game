// 0141: Race 콜라이더 정체 + 136,-93 둘레 + 차 위치
string r = "";
var race = UnityEngine.GameObject.Find("Race");
if (race != null) { r += "Race comps:"; foreach (var c in race.GetComponents<UnityEngine.Component>()) r += " " + c.GetType().Name; foreach (var c in race.GetComponentsInChildren<UnityEngine.Collider>()) r += "\n  col " + c.name + " " + c.GetType().Name + " b=" + c.bounds.center.ToString("F0") + "/" + c.bounds.size.ToString("F0") + " trig=" + c.isTrigger + " layer=" + UnityEngine.LayerMask.LayerToName(c.gameObject.layer); }
r += "\n136,-93:";
foreach (var c in UnityEngine.Physics.OverlapSphere(new UnityEngine.Vector3(136, 12, -93), 5f)) r += " " + c.transform.root.name + "/" + c.name;
return r;
