AssetDatabase.Refresh();
var sb=new System.Text.StringBuilder();
sb.Append("scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" play="+Application.isPlaying+"\n");
var pf=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Buildings/field_patch/field_patch.prefab");
var inst=(GameObject)PrefabUtility.InstantiatePrefab(pf);
try{
 var rs=inst.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 sb.Append("bounds c="+b.center.ToString("F3")+" s="+b.size.ToString("F3")+" min="+b.min.ToString("F3")+"\n");
 foreach(Transform t in inst.GetComponentsInChildren<Transform>()){var mf=t.GetComponent<MeshFilter>();sb.Append(t.name+" lp="+t.localPosition.ToString("F3")+" lr="+t.localEulerAngles.ToString("F1")+" ls="+t.localScale.ToString("F3")+(mf?" mesh="+mf.sharedMesh.name+" v="+mf.sharedMesh.vertexCount+" mb="+mf.sharedMesh.bounds.ToString("F3"):"")+" col="+(t.GetComponent<Collider>()?t.GetComponent<Collider>().GetType().Name:"-")+"\n");}
}finally{UnityEngine.Object.DestroyImmediate(inst);}
var h=GameObject.Find("D198_Buildings/house_home");sb.Append("house pos="+h.transform.position.ToString("F3")+" yaw="+h.transform.eulerAngles.y.ToString("F2")+" parent="+(h.transform.parent?h.transform.parent.name:"-")+"\n");
var tb=GameObject.Find("TownV6_Gray/TerraceBlock");sb.Append("terrace="+(tb!=null)+"\n");
foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())sb.Append(go.name+" | ");
int a7=0;foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(m&&m.GetType().Name.Contains("A7Shade"))a7++;
sb.Append("\nA7="+a7);
return sb.ToString();
