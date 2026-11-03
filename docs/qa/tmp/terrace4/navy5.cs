// 남색 판 5: Default 레이어 렌더러를 반씩 forceRenderingOff(저장 안 되는 값) → 픽셀이 하늘색이 되는 쪽으로 좁힘
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
var all=new System.Collections.Generic.List<Renderer>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&r.gameObject.activeInHierarchy&&r.gameObject.layer==0)all.Add(r);
try{
var src=UnityEditor.SceneView.lastActiveSceneView.camera;
var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
bool Navy(){cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(1100,619,1,1),0,0);tex.Apply();RenderTexture.active=null;var c=tex.GetPixel(0,0);return c.b<.4f;}
var cand=new System.Collections.Generic.List<Renderer>(all);sb.AppendLine("start n="+cand.Count+" navy="+Navy());
while(cand.Count>1){
 var half=cand.GetRange(0,cand.Count/2);foreach(var r in half)r.forceRenderingOff=true;bool still=Navy();foreach(var r in half)r.forceRenderingOff=false;
 cand=still?cand.GetRange(cand.Count/2,cand.Count-cand.Count/2):half;
}
var f=cand[0];f.forceRenderingOff=true;bool after=Navy();f.forceRenderingOff=false;
var mf=f.GetComponent<MeshFilter>();
sb.AppendLine("found "+f.GetType().Name+" "+Path(f.transform)+" mesh="+(mf&&mf.sharedMesh?mf.sharedMesh.name+" v="+mf.sharedMesh.vertexCount:"-")+" mat="+string.Join(",",f.sharedMaterials.Select(m=>m?m.name+"("+m.shader.name+")":"null"))+" b="+f.bounds.min.ToString("F1")+"~"+f.bounds.max.ToString("F1")+" pos="+f.transform.position.ToString("F1")+" scale="+f.transform.lossyScale.ToString("F2")+" offThenNavy="+after);
var pf=UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(f.gameObject);sb.AppendLine("prefabRoot="+(pf?Path(pf.transform)+" src="+UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(pf)):"none"));
if(mf&&mf.sharedMesh)sb.AppendLine("meshAsset="+UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh));
foreach(var c in f.GetComponents<Component>())sb.AppendLine("  comp "+c.GetType().Name);
}finally{foreach(var r in all)r.forceRenderingOff=false;UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
return sb.ToString();
