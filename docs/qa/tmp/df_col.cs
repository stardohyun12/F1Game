var car = GameObject.Find("Race");
var sb = new System.Text.StringBuilder();
foreach (var c in car.GetComponentsInChildren<Collider>(true))
{
    var p = c.transform; string path = p.name;
    while (p.parent != null && p.parent != car.transform) { p = p.parent; path = p.name + "/" + path; }
    sb.Append(path + " [" + c.GetType().Name + " en=" + c.enabled + " trig=" + c.isTrigger + " act=" + c.gameObject.activeInHierarchy + "]; ");
}
return sb.ToString();
