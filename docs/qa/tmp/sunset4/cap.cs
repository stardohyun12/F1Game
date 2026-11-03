// 편집 모드: 앉은 자리 카메라(PITCH,YAW) 640x360, 셰이더 판별로 같은 카메라. 판 바꿀 땐 바다 렌더러 머티리얼만 임시로 바꿨다가 되돌림.
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset4/"; System.IO.Directory.CreateDirectory(dir);
string[] vers = { __VERS__ };
float yaw = __YAW__f, pitch = __PITCH__f; string pre = "__PRE__";
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(pitch, yaw, 0));
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
var m = look.seaMaterial; var dtex = m.GetTexture("_DepthTex");
string log = "";
look.SetOn(true);
foreach (var ver in vers)
{
    Material use = m;
    if (ver != "v4")
    {
        use = new Material(Shader.Find("PitCrew/Tmp/DuskSea" + ver.ToUpper()));
        if (ver == "v1") { use.SetFloat("_WaveAmp", 0.08f); use.SetColor("_Deep", new Color(0.2f, 0.16f, 0.32f, 1)); }
        if (ver == "v3") { var bak = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_TmpSunset4/v3.mat"); if (bak) { use.CopyPropertiesFromMaterial(bak); use.shader = Shader.Find("PitCrew/Tmp/DuskSeaV3"); } use.SetTexture("_DepthTex", dtex); }
        look.sea.sharedMaterial = use;
    }
    c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + pre + "_" + ver + ".png", tex.EncodeToPNG());
    if (ver != "v4") { look.sea.sharedMaterial = m; UnityEngine.Object.DestroyImmediate(use); }
    log += ver + " ";
}
look.SetOn(false);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok " + log + " on=" + look.IsOn + " seaMat=" + look.sea.sharedMaterial.name;
