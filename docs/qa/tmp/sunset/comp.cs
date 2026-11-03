// 앉은 자리 구도 시험: 방향 여러 개를 640x360 으로 그려 세로로 붙임
var seat=new Vector3(-9.0f,2.3f,-291.0f); float[] yaws={120f,150f,180f,210f};
var go=new GameObject("_tmpC"); var c=go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView=60; c.farClipPlane=1800; c.nearClipPlane=0.05f;
var rt=new RenderTexture(640,360,24); c.targetTexture=rt; var all=new Texture2D(640,360*yaws.Length,TextureFormat.RGB24,false);
for(int i=0;i<yaws.Length;i++){ c.transform.SetPositionAndRotation(seat, Quaternion.Euler(4f,yaws[i],0)); c.Render(); RenderTexture.active=rt; all.ReadPixels(new Rect(0,0,640,360),0,360*(yaws.Length-1-i)); }
all.Apply(); RenderTexture.active=null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/sunset/comp.png", all.EncodeToPNG());
c.targetTexture=null; UnityEngine.Object.DestroyImmediate(go); rt.Release(); return "ok";
