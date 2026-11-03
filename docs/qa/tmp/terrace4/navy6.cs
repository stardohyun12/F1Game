// 남색 판 6: 한 무리만 켜고 나머지는 forceRenderingOff → 남색이 나오는 무리 = 범인(겹쳐 있어도 찾음). 루트 → 자식 → 렌더러 순으로 좁힘
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
var all=new System.Collections.Generic.List<Renderer>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&r.gameObject.activeInHierarchy)all.Add(r);
try{
var src=UnityEditor.SceneView.lastActiveSceneView.camera;
var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
Color Px(){cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(1100,619,1,1),0,0);tex.Apply();RenderTexture.active=null;return tex.GetPixel(0,0);}
bool Only(System.Collections.Generic.IEnumerable<Renderer> on){var set=new System.Collections.Generic.HashSet<Renderer>(on);foreach(var r in all)r.forceRenderingOff=!set.Contains(r);var c=Px();return c.b<.4f;}
sb.AppendLine("all="+Only(all)+" none="+Only(new Renderer[0]));
// 무리 = 경로 앞부분(깊이 d)
for(int depth=1;depth<=6;depth++){
 var groups=all.GroupBy(r=>{var p=Path(r.transform).Split('/');return string.Join("/",p.Take(depth));});
 foreach(var gr in groups){if(Only(gr))sb.AppendLine("d"+depth+" NAVY "+gr.Key+" n="+gr.Count());}
}
foreach(var r in all){if(Only(new[]{r})){var mf=r.GetComponent<MeshFilter>();sb.AppendLine("SINGLE "+r.GetType().Name+" "+Path(r.transform)+" mesh="+(mf&&mf.sharedMesh?mf.sharedMesh.name+"@"+UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh):"-")+" mat="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+"("+m.shader.name+")@"+UnityEditor.AssetDatabase.GetAssetPath(m):"null"))+" b="+r.bounds.min.ToString("F1")+"~"+r.bounds.max.ToString("F1"));}}
}finally{foreach(var r in all)r.forceRenderingOff=false;UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
return sb.ToString();
