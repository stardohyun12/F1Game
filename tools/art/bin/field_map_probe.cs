var scn=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb=new System.Text.StringBuilder();sb.AppendLine("scene="+scn.path+" dirty="+scn.isDirty+" compiling="+UnityEditor.EditorApplication.isCompiling+" playing="+Application.isPlaying);
foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
 if(t.name=="house_home" || t.name=="D198_garage") {var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);sb.AppendLine(t.name+" pos="+t.position+" rot="+t.eulerAngles+" bounds="+b);}
}
var terr=new System.Collections.Generic.List<Collider>();foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled && (c is TerrainCollider || c.name.Contains("Terrain"))) {terr.Add(c);sb.AppendLine("terrain="+c.name);}
for(int z=-64;z<=-48;z+=4)for(int x=188;x<=208;x+=4){float y=float.NaN;foreach(var c in terr){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))y=float.IsNaN(y)?h.point.y:Mathf.Max(y,h.point.y);}sb.AppendLine(x+","+z+"="+y);}
return sb.ToString();
