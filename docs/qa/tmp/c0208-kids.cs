var sb = new System.Text.StringBuilder();
var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
foreach (var t in race.GetComponentsInChildren<Transform>(true))
{
  if (t.name.StartsWith("wheel") || t.name.StartsWith("Skid") || t.name.StartsWith("Smoke")) continue;
  var r = t.GetComponent<Renderer>();
  string path = t.name; var p = t.parent; while (p != null && p != race.transform) { path = p.name + "/" + path; p = p.parent; }
  var lp = race.transform.InverseTransformPoint(t.position);
  sb.Append(path + " act=" + t.gameObject.activeInHierarchy + " raceLocal=" + lp.ToString("F2") + (r != null ? " b=" + r.bounds.size.ToString("F2") + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.name : "-") : "") + "\n");
}
return sb.ToString();
