var sb=new System.Text.StringBuilder();
sb.AppendLine(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
foreach(var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) foreach(var t in r.GetComponentsInChildren<Transform>(false)){
 var p=t.position; if(Mathf.Abs(p.x-283)<45&&Mathf.Abs(p.z+36)<45 && (t.parent==null||t.parent.name.EndsWith("Buildings")||UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) && !t.name.StartsWith("ROAD")) sb.AppendLine(t.name+" par="+(t.parent?t.parent.name:"-")+" "+p.ToString("F2"));
}
foreach(var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)){ if(rs.centerline==null) continue; foreach(var q in rs.centerline){ if(Mathf.Abs(q.x-283)<20&&Mathf.Abs(q.z+36)<25){ sb.AppendLine("road "+rs.name+" w="+rs.width+" @"+q.ToString("F1")); break; } } }
return sb.ToString();
