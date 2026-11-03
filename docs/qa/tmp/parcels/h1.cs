var sb=new System.Text.StringBuilder();
var sc=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); sb.AppendLine(sc.path);
foreach(var r in sc.GetRootGameObjects()){ sb.AppendLine("R "+r.name+" ch="+r.transform.childCount+" act="+r.activeSelf);
 if(r.name=="TownV6_Gray") foreach(UnityEngine.Transform c in r.transform){ sb.AppendLine("  "+c.name+" ch="+c.childCount+" act="+c.gameObject.activeSelf);
   if(c.name=="TerraceBlock"||c.name=="D198_Buildings"||c.name.Contains("Env")) foreach(UnityEngine.Transform d in c){ sb.AppendLine("     "+d.name+" ch="+d.childCount+" "+d.position);} } }
return sb.ToString();
