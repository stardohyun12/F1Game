var sb = new System.Text.StringBuilder();
var g = GameObject.Find("TownV6_Gray");
var shaders = new System.Collections.Generic.Dictionary<string,int>();
foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) foreach (var m in r.sharedMaterials) if (m) { var k = m.shader.name + " | " + m.name; shaders[k] = shaders.ContainsKey(k) ? shaders[k]+1 : 1; }
foreach (var kv in shaders) sb.Append("SH " + kv.Key + " x" + kv.Value + "\n");
foreach (Transform t in g.transform) sb.Append("CH " + t.name + " n=" + t.childCount + "\n");
var race = GameObject.Find("Race"); foreach (var c in race.GetComponents<Component>()) sb.Append("RACE " + c.GetType().Name + "\n");
sb.Append("RACE pos " + race.transform.position + " kids " + race.transform.childCount + "\n");
sb.Append("Drop=" + CameraFramingZone.Drop + "\n");
var ds = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None); foreach (var l in ds) sb.Append("LIGHT " + l.name + " " + l.type + " sh=" + l.shadows + " rot=" + l.transform.eulerAngles + "\n");
return sb.ToString();
