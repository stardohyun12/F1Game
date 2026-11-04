var d = GameObject.Find("DOOR_BALCONY").GetComponent<Renderer>();
d.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/tmp/nodoorshadow.png");
return "ok";
