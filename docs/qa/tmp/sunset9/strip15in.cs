// Play: 에디터 안에서 게임 시각 1 s 마다 16장(Present 경로 640×360) → raw/w00..w15.png + w.jsonl. 시작 3 s 뒤 갈매기 무리
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset9/raw/";
Application.runInBackground = true;
var look = SunsetLook.Find(); var db = look.backdrop.GetComponent<DuskBackdrop>();
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(DuskBackdrop).GetField("flightNext", bf).SetValue(db, Time.time + 3f);
float t0 = Time.time; int k = 0; var log = new System.Text.StringBuilder();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.time < t0 + k * 1f) return;
    var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
    int w = lr.RenderSize.x, h = lr.RenderSize.y;
    var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    lr.Present(rt);
    var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
    System.IO.File.WriteAllBytes(dir + "w" + k.ToString("00") + ".png", tex.EncodeToPNG()); rt.Release(); UnityEngine.Object.DestroyImmediate(tex);
    string gulls = ""; foreach (Transform c in look.backdrop.transform) if (c.name.StartsWith("Dusk_Gull") && c.gameObject.activeSelf) { var gv = cam.WorldToViewportPoint(c.position); gulls += "(" + gv.x.ToString("F2") + "," + (1 - gv.y).ToString("F2") + ")"; }
    var glint = spot.transform.Find("FishingRig/Glint");
    log.Append("{\"tag\":\"w" + k.ToString("00") + "\",\"t\":" + (Time.time - t0).ToString("F2") + ",\"lamp\":" + (db.LampOn ? 1 : 0) + ",\"glint\":" + (glint && glint.gameObject.activeSelf ? 1 : 0)
        + ",\"gulls\":\"" + gulls + "\",\"calls\":" + amb.GullCalls + ",\"lastCall\":" + (amb.LastCallTime - t0).ToString("F2") + "}\n");
    k++;
    if (k >= 16) { UnityEditor.EditorApplication.update -= cb; System.IO.File.WriteAllText(dir + "w.jsonl", log.ToString()); }
};
UnityEditor.EditorApplication.update += cb;
return "started t0=" + t0;
