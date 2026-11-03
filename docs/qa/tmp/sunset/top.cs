// 위에서 본 맵(직교) — 자리 찾기용. 격자 100m 마다 눈금은 그림에서 픽셀로 환산: 2048px = 800m
var go=new GameObject("_tmpTop"); var c=go.AddComponent<Camera>();
c.orthographic=true; c.orthographicSize=400; c.transform.position=new Vector3(0,400,0); c.transform.rotation=Quaternion.Euler(90,0,0);
c.clearFlags=CameraClearFlags.SolidColor; c.backgroundColor=Color.black; c.farClipPlane=1000;
var rt=new RenderTexture(2048,2048,24); c.targetTexture=rt; c.Render();
RenderTexture.active=rt; var tex=new Texture2D(2048,2048,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,2048,2048),0,0); tex.Apply(); RenderTexture.active=null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/sunset/top.png", tex.EncodeToPNG());
c.targetTexture=null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok";
