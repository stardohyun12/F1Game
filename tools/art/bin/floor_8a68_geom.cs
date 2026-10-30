if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Studio_Building") return "FAIL wrong studio";
var sb=new System.Text.StringBuilder("T\t1\n"); int count=0;
var inv=System.Globalization.CultureInfo.InvariantCulture;
foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
 var r=mf.GetComponent<Renderer>(); if(!r || !r.enabled || !r.gameObject.activeInHierarchy || !mf.sharedMesh) continue;
 if(mf.name!="HOUSE_shell" && mf.name!="ST_Ground" && mf.name!="ST_Road" && mf.name!="MAT_01") continue;
 var vs=mf.sharedMesh.vertices;var ts=mf.sharedMesh.triangles;
 for(int i=0;i<ts.Length;i+=3) {
  var p=new Vector3[]{mf.transform.TransformPoint(vs[ts[i]]),mf.transform.TransformPoint(vs[ts[i+1]]),mf.transform.TransformPoint(vs[ts[i+2]])};
  if(p.Any(v=>Mathf.Abs(v.y)>.05f) || p.Max(v=>v.y)-p.Min(v=>v.y)>.0001f) continue;
  sb.Append("F\t"+mf.name+"\t"+(i/3));foreach(var v in p) sb.Append("\t"+v.x.ToString("R",inv)+"\t"+v.y.ToString("R",inv)+"\t"+v.z.ToString("R",inv));sb.Append("\n");count++;
 }
}
return count>0 ? sb.ToString() : "FAIL no studio floor triangles";
