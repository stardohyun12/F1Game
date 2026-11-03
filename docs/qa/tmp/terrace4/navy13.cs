// 남색 판 13: 가설 = 캡처 카메라가 SceneView 카메라를 CopyFrom 해 cameraType=SceneView → 씬 뷰처럼 Screen Space Overlay 캔버스(UIRoot, 1920×1080 m, z=0)가 월드에 그려짐
var sb=new System.Text.StringBuilder();
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);
var ui=GameObject.Find("UIRoot");
try{
var src=UnityEditor.SceneView.lastActiveSceneView.camera;
var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.targetTexture=rt;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
Color C(){cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(1100,619,1,1),0,0);tex.Apply();RenderTexture.active=null;return tex.GetPixel(0,0);}
sb.AppendLine("copied cameraType="+cam.cameraType+" px="+C());
var cv=ui.GetComponent<Canvas>();cv.enabled=false;sb.AppendLine("UIRoot canvas off px="+C());cv.enabled=true;
sb.AppendLine("UIRoot rect="+((RectTransform)ui.transform).rect+" pos="+ui.transform.position+" scale="+ui.transform.lossyScale);
cam.cameraType=CameraType.Game;sb.AppendLine("cameraType=Game px="+C());
}finally{UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
sb.AppendLine("dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
