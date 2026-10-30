if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets"))throw new System.Exception("Studio only");
var r=GameObject.Find("FIELD_REVIEW_ONLY");var log=new System.Text.StringBuilder();
foreach(var t in r.transform.Find("A_aligned_8x6").GetComponentsInChildren<MeshFilter>(true))if(t.name=="olive_ground"||t.name.StartsWith("rounded_ridge")||t.name.StartsWith("crop_mass_0_0")||t.name.StartsWith("support")||t.name.StartsWith("weathered")) log.AppendLine(t.name+" vertices="+t.sharedMesh.vertexCount+" tris="+t.sharedMesh.triangles.Length/3+" bounds="+t.sharedMesh.bounds);
return log.ToString();
