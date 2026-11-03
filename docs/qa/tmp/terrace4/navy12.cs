// 남색 판 12: 새 카메라엔 없고 SceneView 카메라 CopyFrom 한 카메라에만 있음 → 어느 속성이 다른지 + 그 카메라로 오브젝트 끄기 좁힘
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
try{
var src=UnityEditor.SceneView.lastActiveSceneView.camera;
var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
Color C(){cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(1100,619,1,1),0,0);tex.Apply();RenderTexture.active=null;return tex.GetPixel(0,0);}
bool Navy(){var c=C();return c.r<.15f&&c.b<.3f;}
sb.AppendLine("copy navy="+Navy()+" "+C());
sb.AppendLine("src scene="+(src.scene.IsValid()?src.scene.name:"invalid")+" cam.scene="+(cam.scene.IsValid()?cam.scene.name:"invalid")+" overrideMask="+src.overrideSceneCullingMask+" ortho="+src.orthographic+" depthTex="+src.depthTextureMode+" useOcc="+src.useOcclusionCulling+" clear="+src.clearFlags);
var save=cam.scene;cam.scene=scene;sb.AppendLine("cam.scene=active navy="+Navy());cam.scene=save;
cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.white;sb.AppendLine("solid white navy="+Navy()+" "+C());
var level=scene.GetRootGameObjects().Where(g=>g.activeSelf).Select(g=>g.transform).ToList();
for(int depth=0;depth<8&&level.Count>0;depth++){
 Transform culprit=null;
 foreach(var t in level){t.gameObject.SetActive(false);bool n=Navy();t.gameObject.SetActive(true);if(!n){culprit=t;break;}}
 if(culprit==null){sb.AppendLine("d"+depth+" none of "+level.Count+" removes it alone");break;}
 sb.AppendLine("d"+depth+" off->gone: "+Path(culprit)+" comps="+string.Join(",",culprit.GetComponents<Component>().Select(c=>c.GetType().Name))+" pos="+culprit.position.ToString("F1")+" layer="+culprit.gameObject.layer);
 level=culprit.Cast<Transform>().Where(c=>c.gameObject.activeSelf).ToList();
}
}finally{UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
sb.AppendLine("dirty="+scene.isDirty);
return sb.ToString();
