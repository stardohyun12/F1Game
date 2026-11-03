// 씬 뷰 카메라 자리에서 한 장 — 도현 사진과 대조
var sv=UnityEditor.SceneView.lastActiveSceneView; var src=sv.camera;
var go=new GameObject("_tmpSV"); var c=go.AddComponent<Camera>(); c.CopyFrom(src); c.cameraType=CameraType.Game;
c.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation); c.farClipPlane=2000;
var rt=new RenderTexture(1060,648,24); c.targetTexture=rt; c.Render();
RenderTexture.active=rt; var tex=new Texture2D(1060,648,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1060,648),0,0); tex.Apply(); RenderTexture.active=null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/sunset/sv.png", tex.EncodeToPNG());
c.targetTexture=null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "fov="+src.fieldOfView;
