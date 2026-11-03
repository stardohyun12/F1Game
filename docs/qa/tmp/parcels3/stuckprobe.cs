// 0141: 봇 끼임 자리 둘레 콜라이더
string r = "";
foreach (var pt in new[] { new UnityEngine.Vector3(142, 13, -86), new UnityEngine.Vector3(-180, 4, -70) }) {
  r += pt + ":\n";
  foreach (var c in UnityEngine.Physics.OverlapSphere(pt, 5f)) { var n = c.transform.root.name; if (n.StartsWith("TownV6_Gray") && c.name.StartsWith("ROAD")) continue; r += "  " + n + "/" + c.name + " d=" + (c.ClosestPoint(pt) - pt).magnitude.ToString("F1") + " trig=" + c.isTrigger + "\n"; }
}
return r;
