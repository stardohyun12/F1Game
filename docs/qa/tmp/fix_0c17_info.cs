var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"ROAD_V2e_camo_309378634_165_6","ROAD_V2e_camo_874876277_276_s1","ROAD_camo_794408003_237","ROAD_V2e_camo_309378634_165_5","ROAD_V2e_camo_309378634_165_3+camo_794408002_236_~91ea"}) {
  var g = GameObject.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m=>m.name==n).ToList();
  foreach (var m in g) { var mf = m.GetComponent<MeshFilter>(); sb.AppendLine(n+" active="+m.gameObject.activeInHierarchy+" parent="+m.transform.parent.name+" mat="+m.sharedMaterial.name+" rq="+m.sharedMaterial.renderQueue+" verts="+mf.sharedMesh.vertexCount+" b="+m.bounds.min.ToString("F1")+"-"+m.bounds.max.ToString("F1")); }
}
return sb.ToString();
