var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
var P = new (string id, float x, float z)[] { ("room",-117.7f,-11.2f), ("garage",-63.2f,-6.7f), ("depot",-43.7f,-26.8f), ("postoffice",-57.7f,-25.6f) };
var roots = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
foreach (var r in s.GetRootGameObjects()) foreach (var rd in r.GetComponentsInChildren<UnityEngine.Renderer>(true)) {
  var c = rd.bounds.center;
  foreach (var p in P) if (UnityEngine.Mathf.Abs(c.x-p.x)<14 && UnityEngine.Mathf.Abs(c.z-p.z)<14) {
    var t = rd.transform; var top = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
    var key = top!=null ? top : (t.parent!=null ? t.parent.gameObject : t.gameObject);
    if (roots.Add(key)) { var q=key.transform; string chain=""; while(q!=null){chain=q.name+"/"+chain; q=q.parent;} sb.Append(p.id+": "+chain+" pos="+key.transform.position.ToString("F1")+(top!=null?" PREFAB "+UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(top):"")+"\n"); }
  }
}
return sb.ToString();
