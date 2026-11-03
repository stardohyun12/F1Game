// 앉은 눈 노을 켬 640x360 한 장 + 3배 점확대(1920x1080). 파일 이름은 아래 tag. 끝나면 노을 끔.
var tag = "test"; float yaw = 180f, pitch = 3f;
var dir = "D:/Github/F1Game/docs/qa/tmp/sunset2/"; System.IO.Directory.CreateDirectory(dir);
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(pitch, yaw, 0));
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
look.SetOn(true);
c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
look.SetOn(false);
System.IO.File.WriteAllBytes(dir + tag + ".png", tex.EncodeToPNG());
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok " + tag;
