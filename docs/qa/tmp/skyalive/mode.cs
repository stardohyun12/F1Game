// Play: 시간대 __DUSK__(0 오후 · 1 노을) · 옛 하늘 __OLD__ · 자리 __SHOT__(seat = 낚시 앉은 시점 yaw 0 pitch 0 / road = 큰길 하늘 많이)
Application.runInBackground = true;
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
look.dusk = __DUSK__f; SunsetLook.DebugOldSky = __OLD__; look.Apply();
string r;
if ("__SHOT__" == "road")
{
    var p = new Vector3(__RX__f, 90f, __RZ__f); RaycastHit h;
    if (Physics.Raycast(p, Vector3.down, out h, 120f)) p.y = h.point.y + 1.7f; else p.y = 8.5f;
    r = CamPin.Pin(p, new Vector3(__RP__f, __RY__f, 0f)) + " hit=" + (h.collider ? h.collider.name : "-");
}
else
{
    CamPin.Unpin();
    var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
    var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f); typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, 0f);
    r = "seat";
}
return r + " dusk=" + look.dusk + " old=" + SunsetLook.DebugOldSky + " life=" + Shader.GetGlobalVector("_PCSkyLife") + " clouds=" + Shader.GetGlobalVector("_PCSkyClouds") + " cam=" + Camera.main.transform.position.ToString("F1") + " fov=" + Camera.main.fieldOfView.ToString("F0");
