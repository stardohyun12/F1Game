// 편집 모드: 앉은 자리 카메라(3,180) 640x360 A 한 장(__TAG__). 반사 켬/끔 = __REFL__, 디버그 = __DBG__
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset6/"; System.IO.Directory.CreateDirectory(dir);
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var sr = SeaReflection.Find();
if (sr == null) { sr = look.gameObject.AddComponent<SeaReflection>(); }
sr.hide = new Renderer[] { look.sea, look.seaNear }; sr.downscale = 1;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(__PITCH__f, __YAW__f, 0));
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
var m = look.seaMaterial; float style0 = m.GetFloat("_Style"), dbg0 = m.GetFloat("_Debug");
look.SetOn(true); m.SetFloat("_Style", 0); m.SetFloat("_Debug", __DBG__f);
if (__REFL__ > 0) sr.RenderFor(c); else Shader.SetGlobalVector("_PCSeaReflOn", Vector4.zero);
c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
System.IO.File.WriteAllBytes(dir + "__TAG__.png", tex.EncodeToPNG());
m.SetFloat("_Style", style0); m.SetFloat("_Debug", dbg0);
look.SetOn(false); Shader.SetGlobalVector("_PCSeaReflOn", Vector4.zero);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok seat=" + seat.position + " planeY=" + look.sea.transform.position.y;
