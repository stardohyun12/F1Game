// 편집 모드 요소 판정 캡처(__TAG__): 앉은 자리 카메라(pitch __PITCH__, yaw __YAW__) 640x360 Point, 시간 고정 __T__ s.
// 스위치: STYLE(0 A / 1 B) REFL(① 평면 반사) SPEC(②) MICRO(③) HZ(④ 0 = 5판 값, 1 = 150/650/0.8) DASH(⑤ 옆 퍼짐 배수, 1 = 끔). FLK = 1 이면 깜빡임 120프레임(1/115 s 간격)
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset6/raw/"; System.IO.Directory.CreateDirectory(dir);
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var sr = SeaReflection.Find(); int ds0 = sr.downscale; sr.downscale = 1;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(__PITCH__f, __YAW__f, 0));
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false); var t1 = new Texture2D(1, 1, TextureFormat.RGB24, false);
var m = look.seaMaterial;
string[] vn = { "_Spec", "_Micro", "_Dash", "_HorizonFade" }; var v0 = new Vector4[vn.Length]; for (int i = 0; i < vn.Length; i++) v0[i] = m.GetVector(vn[i]);
float style0 = m.GetFloat("_Style");
look.SetOn(true);
m.SetFloat("_Style", __STYLE__f);
var sp = v0[0]; sp.x = __SPEC__f; m.SetVector("_Spec", sp);
var mi = v0[1]; mi.x = __MICRO__f; m.SetVector("_Micro", mi);
m.SetVector("_Dash", new Vector4(__DASH__f, 0, 0, 0));
m.SetVector("_HorizonFade", __HZ__ > 0 ? new Vector4(__HZV__) : new Vector4(300, 650, 0.5f, 0));
System.Action<float> frame = (tt) => {
    Shader.SetGlobalVector("_PCSeaClock", new Vector4(1, tt, 0, 0));
    if (__REFL__ > 0) sr.RenderFor(c); else Shader.SetGlobalVector("_PCSeaReflOn", Vector4.zero);
    c.Render();
};
frame(__T__f); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
System.IO.File.WriteAllBytes(dir + "__TAG__.png", tex.EncodeToPNG());
// 시간: 20번 그리고 1px 읽어 GPU 동기
var sw = System.Diagnostics.Stopwatch.StartNew();
for (int k = 0; k < 20; k++) { frame(__T__f + k / 115f); RenderTexture.active = rt; t1.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); t1.Apply(); }
double ms = sw.Elapsed.TotalMilliseconds / 20;
string flk = "";
if (__FLK__ > 0)
{
    int n = 640 * 360; byte[] prev = null, prev2 = null, cur = new byte[n]; var fl = new System.Collections.Generic.List<float>();
    for (int k = 0; k < 120; k++)
    {
        frame(__T__f + k / 115f); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
        var px = tex.GetPixels32(); for (int i = 0; i < n; i++) cur[i] = (byte)((px[i].r * 54 + px[i].g * 183 + px[i].b * 19) >> 8);
        if (prev2 != null) { int f = 0; for (int i = 0; i < n; i++) { int d0 = prev[i] - prev2[i], d1 = cur[i] - prev[i]; if ((d0 > 24 && d1 < -24) || (d0 < -24 && d1 > 24)) f++; } fl.Add(f / (float)n); }
        prev2 = prev; prev = (byte[])cur.Clone();
    }
    fl.Sort(); flk = ",\"flipMax\":" + fl[fl.Count - 1].ToString("F6") + ",\"flipMed\":" + fl[fl.Count / 2].ToString("F6") + ",\"flipMaxPx\":" + Mathf.RoundToInt(fl[fl.Count - 1] * n);
}
for (int i = 0; i < vn.Length; i++) m.SetVector(vn[i], v0[i]); m.SetFloat("_Style", style0);
look.SetOn(false); Shader.SetGlobalVector("_PCSeaReflOn", Vector4.zero); Shader.SetGlobalVector("_PCSeaClock", Vector4.zero); sr.downscale = ds0;
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
var js = "{\"tag\":\"__TAG__\",\"ms\":" + ms.ToString("F2") + flk + "}";
System.IO.File.AppendAllText(dir + "m6.jsonl", js + "\n");
return js;
