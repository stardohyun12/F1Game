var sb=new System.Text.StringBuilder();
var cat=Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog");
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:OrderJob")){
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid); var o=UnityEditor.AssetDatabase.LoadAssetAtPath<OrderJob>(path);
 string src="cube"; double A=6; float s=1f;
 if(cat!=null && cat.TryGet(o.cargoType,out var prefab,out var scale)){
  var go=(GameObject)UnityEngine.Object.Instantiate(prefab); go.transform.localScale=Vector3.one*scale;
  var mc=go.GetComponentInChildren<MeshCollider>(); Mesh m=mc!=null?mc.sharedMesh:go.GetComponentInChildren<MeshFilter>().sharedMesh; Transform t=mc!=null?mc.transform:go.GetComponentInChildren<MeshFilter>().transform;
  A=0; if(m.isReadable){ var v=m.vertices; var tr=m.triangles; for(int i=0;i<tr.Length;i+=3) A+=Vector3.Cross(v[tr[i+1]]-v[tr[i]],v[tr[i+2]]-v[tr[i]]).magnitude*0.5; } else A=-1;
  s=t.lossyScale.x; src=prefab.name+"(mc="+(mc!=null)+")"; UnityEngine.Object.DestroyImmediate(go); }
 double K=A*s*16384.0;
 sb.Append(o.name+" type="+o.cargoType+" reward="+o.baseReward+" src="+src+" Alocal="+A.ToString("F3")+" s="+s.ToString("F3")+" K="+K.ToString("F0")+" hard(0.18)="+(K*0.18).ToString("F0")+" bubble(0.03)="+(K*0.03).ToString("F0")+"\n");
}
return sb.ToString();
