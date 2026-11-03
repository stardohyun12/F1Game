// 남색 판 11: 렌더러가 아닌데 Default 레이어로 그려짐 → 씬 오브젝트를 무리째 SetActive(false) 해 보며 좁힘(끝나면 원래대로)
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool wasDirty=scene.isDirty;
var touched=new System.Collections.Generic.List<GameObject>();
try{
var cam=go.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.white;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
bool Navy(){cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(1100,619,1,1),0,0);tex.Apply();RenderTexture.active=null;var c=tex.GetPixel(0,0);return c.r<.5f;}
sb.AppendLine("fresh cam navy="+Navy());
cam.cullingMask=1;sb.AppendLine("mask=Default navy="+Navy());cam.cullingMask=-1;
System.Collections.Generic.List<Transform> level=scene.GetRootGameObjects().Where(g=>g.activeSelf).Select(g=>g.transform).ToList();
for(int depth=0;depth<8&&level.Count>0;depth++){
 Transform culprit=null;
 foreach(var t in level){if(!t.gameObject.activeSelf)continue;t.gameObject.SetActive(false);bool n=Navy();t.gameObject.SetActive(true);if(!n){culprit=t;sb.AppendLine("d"+depth+" off->gone: "+Path(t));break;}}
 if(culprit==null){sb.AppendLine("d"+depth+" none of "+level.Count+" removes it alone");break;}
 var comps=culprit.GetComponents<Component>().Select(c=>c.GetType().Name);sb.AppendLine("   comps="+string.Join(",",comps)+" pos="+culprit.position.ToString("F1")+" layer="+culprit.gameObject.layer);
 level=culprit.Cast<Transform>().Where(c=>c.gameObject.activeSelf).ToList();
}
}finally{UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
sb.AppendLine("wasDirty="+wasDirty+" nowDirty="+scene.isDirty);
return sb.ToString();
