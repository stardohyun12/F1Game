var sb = new System.Text.StringBuilder();
var rv = GameObject.Find("TownV6_Gray/RoadsV2"); int i = 0;
foreach (Transform t in rv.transform) { if (i++ < 8) sb.Append("V2 " + t.name + " kids=" + t.childCount + " mf=" + (t.GetComponent<MeshFilter>()!=null) + " col=" + (t.GetComponent<Collider>()? t.GetComponent<Collider>().GetType().Name : "-") + " act=" + t.gameObject.activeSelf + "\n"); }
foreach (var n in new[]{"tomo_1296983883_62","posi_1567571332_429","camo_309378634_165"}) foreach (Transform t in rv.transform) if (t.name.Contains(n)) sb.Append("HIT " + t.name + " act=" + t.gameObject.activeSelf + "\n");
var rd = GameObject.Find("TownV6_Gray/Roads"); int a = 0; foreach (Transform t in rd.transform) if (t.gameObject.activeSelf) a++; sb.Append("Roads active " + a + "/" + rd.transform.childCount + "\n");
foreach (var n in new[]{"tomo_1296983883_62","posi_1567571332_429","camo_309378634_165"}) foreach (Transform t in rd.transform) if (t.name.Contains(n)) sb.Append("OLD " + t.name + " act=" + t.gameObject.activeSelf + "\n");
return sb.ToString();
