// Play: 지금 화면(Present 경로, 640×360)을 __TAG__.png 로 + 상태 JSON 한 줄
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset8/raw/"; System.IO.Directory.CreateDirectory(dir);
Application.runInBackground = true;
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
int w = lr.RenderSize.x, h = lr.RenderSize.y;
var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
lr.Present(rt);
var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
System.IO.File.WriteAllBytes(dir + "__TAG__.png", tex.EncodeToPNG()); rt.Release();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var look = SunsetLook.Find(); var db = look.backdrop.GetComponent<DuskBackdrop>();
var glint = spot.transform.Find("FishingRig/Glint");
var bv = cam.WorldToViewportPoint(spot.BobberPosition);
var sd = Quaternion.Euler(-look.skySunElevation, look.sunAzimuth, 0) * Vector3.forward; var sv = cam.WorldToViewportPoint(cam.transform.position + sd * 2000f);
string gulls = ""; foreach (Transform c in look.backdrop.transform) if (c.name.StartsWith("Dusk_Gull") && c.gameObject.activeSelf) { var gv = cam.WorldToViewportPoint(c.position); gulls += "(" + gv.x.ToString("F2") + "," + (1 - gv.y).ToString("F2") + ")"; }
var boat = look.backdrop.transform.Find("Dusk_Boat_2"); string bt = ""; foreach (Transform c in look.backdrop.transform) if (c.name.StartsWith("Dusk_Boat")) bt = cam.WorldToViewportPoint(c.position).x.ToString("F4");
return "{\"tag\":\"__TAG__\",\"t\":" + Time.time.ToString("F2") + ",\"lamp\":" + (db.LampOn ? 1 : 0) + ",\"flights\":" + db.Flights + ",\"gulls\":\"" + gulls + "\",\"boatX\":" + bt
  + ",\"inPath\":" + (spot.BobberInPath ? 1 : 0) + ",\"glint\":" + (glint && glint.gameObject.activeSelf ? 1 : 0) + ",\"bobber\":\"" + bv.x.ToString("F3") + "," + (1 - bv.y).ToString("F3") + "\",\"sun\":\"" + sv.x.ToString("F3") + "," + (1 - sv.y).ToString("F3") + "\"}";
