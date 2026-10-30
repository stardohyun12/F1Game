if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL wrong project";
var sb = new System.Text.StringBuilder();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
sb.AppendLine("scene="+sc.name+" play="+EditorApplication.isPlaying+" dirty="+sc.isDirty+" compiling="+EditorApplication.isCompiling);
foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
 var rr=mf.GetComponent<Renderer>(); if (!rr || !rr.enabled || !rr.gameObject.activeInHierarchy || rr.bounds.min.y > .02f || rr.bounds.max.y < -.02f) continue;
 sb.AppendLine(mf.name+" mesh="+AssetDatabase.GetAssetPath(mf.sharedMesh)+" bounds="+rr.bounds);
 var vv=mf.sharedMesh.vertices;var tt=mf.sharedMesh.triangles;
 for(int i=0;i<tt.Length;i+=3) {
  var a=mf.transform.TransformPoint(vv[tt[i]]);var b=mf.transform.TransformPoint(vv[tt[i+1]]);var c=mf.transform.TransformPoint(vv[tt[i+2]]);
  if (Mathf.Abs(a.y)>.02f || Mathf.Abs(b.y)>.02f || Mathf.Abs(c.y)>.02f) continue;
  if (Mathf.Min(a.x,Mathf.Min(b.x,c.x))>.7f || Mathf.Max(a.x,Mathf.Max(b.x,c.x))<-.7f || Mathf.Min(a.z,Mathf.Min(b.z,c.z))>3f || Mathf.Max(a.z,Mathf.Max(b.z,c.z))<2f) continue;
  sb.AppendLine("tri="+(i/3)+" "+a.ToString("F4")+" "+b.ToString("F4")+" "+c.ToString("F4"));
 }
}
return sb.ToString();
