// 남색 판 4: c2 카메라로 직접 렌더 → (1100,100 위에서) 픽셀 색. 레이어를 하나씩 끄고 끈 레이어에서 색이 하늘로 바뀌면 그 레이어.
var sb=new System.Text.StringBuilder();
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
try{
var src=UnityEditor.SceneView.lastActiveSceneView!=null?UnityEditor.SceneView.lastActiveSceneView.camera:Camera.main;
var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
Color Px(int mask,int x,int yTop){cam.cullingMask=mask;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(x,720-1-yTop,1,1),0,0);tex.Apply();RenderTexture.active=null;return tex.GetPixel(0,0);}
int full=src.cullingMask;sb.AppendLine("src="+src.name+" mask="+full+" clear="+src.clearFlags);
sb.AppendLine("full "+Px(full,1100,100)+" sky "+Px(full,600,40));
sb.AppendLine("none "+Px(0,1100,100));
for(int l=0;l<32;l++){if((full&(1<<l))==0)continue;var c=Px(full&~(1<<l),1100,100);sb.AppendLine("off L"+l+"("+LayerMask.LayerToName(l)+") "+c);}
}finally{UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
return sb.ToString();
