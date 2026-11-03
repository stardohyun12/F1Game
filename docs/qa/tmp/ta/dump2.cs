var sb = new System.Text.StringBuilder();
var top = GameObject.Find("TownV6_Gray").transform;
System.Action<Transform,int> sum = null;
sum = (t, depth) => {
  int act=0, rend=0, col=0, verts=0; var mats = new System.Collections.Generic.Dictionary<string,int>();
  var names = new System.Collections.Generic.Dictionary<string,int>();
  foreach (Transform c in t) { if (c.gameObject.activeInHierarchy) act++; string k = System.Text.RegularExpressions.Regex.Replace(c.name, "[0-9]+", "#"); names[k] = names.TryGetValue(k, out var q) ? q+1 : 1; }
  foreach (var r in t.GetComponentsInChildren<MeshRenderer>(false)) { rend++; foreach (var m in r.sharedMaterials) { string n = m? m.name : "NULL"; mats[n] = mats.TryGetValue(n, out var v)? v+1 : 1; } var mf=r.GetComponent<MeshFilter>(); if (mf&&mf.sharedMesh) verts+=mf.sharedMesh.vertexCount; }
  foreach (var c in t.GetComponentsInChildren<Collider>(false)) col++;
  sb.Append(new string(' ', depth*2)).Append(t.name).Append(t.gameObject.activeInHierarchy?"":" [off]").Append(" ch=").Append(t.childCount).Append(" act=").Append(act).Append(" rend=").Append(rend).Append(" col=").Append(col).Append(" v=").Append(verts).Append('\n');
  sb.Append(new string(' ', depth*2+2)).Append("names: "); int i=0; foreach (var kv in names) { if (i++>8) break; sb.Append(kv.Key).Append('×').Append(kv.Value).Append(' '); } sb.Append('\n');
  sb.Append(new string(' ', depth*2+2)).Append("mats: "); foreach (var kv in mats) sb.Append(kv.Key).Append('×').Append(kv.Value).Append(' '); sb.Append('\n');
};
foreach (var n in new[]{"Roads","RoadsV2","TerraceBlock","DeadEnds","Piers","D198_Buildings"}) { var t = top.Find(n); sum(t,0); if (n=="TerraceBlock"||n=="RoadsV2") foreach (Transform c in t) if (c.childCount>0 || n=="TerraceBlock") sum(c,1); }
foreach (var n in new[]{"Roadside","Parcels_Plan","TV6_Loop"}) { var g = GameObject.Find(n); if (g) { sum(g.transform,0); foreach (Transform c in g.transform) if (c.childCount>0) sum(c,1); } }
return sb.ToString();
