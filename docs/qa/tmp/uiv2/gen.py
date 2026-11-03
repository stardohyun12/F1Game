# UI 2판 캡처용 exec 파일 만들기(오후 aft / 노을 dusk).
D = "docs/qa/tmp/uiv2/"
E = "D:/Github/F1Game/docs/qa/evidence/ui-v2/"
for n, v in (("aft", "0"), ("dusk", "1")):
    open(D + f"start_{n}.cs", "w", encoding="utf-8").write(f"""Application.runInBackground = true;
if (!UnityEditor.EditorApplication.isPlaying) return "NOT PLAYING";
var sl = SunsetLook.Find(); if (sl != null) {{ sl.dusk = {v}f; if (!sl.IsOn) sl.SetOn(true); else sl.Apply(); }}
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(); ui.dirMode = DotScreenUI.DirMode.Compass;
OnFootRouteBot.GrabDir = "{E}{n}";
return OnFootRouteBot.BeginFirstOrder("{E}{n}/bot", "D:/Github/F1Game/docs/qa/tmp/uiv2/{n}") + " dusk=" + (sl != null ? sl.dusk : -1);
""")
    for mode in ("Road", "Compass"):
        open(D + f"{mode.lower()}_{n}.cs", "w", encoding="utf-8").write(f"""var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
ui.dirMode = DotScreenUI.DirMode.{mode}; ui.SimWarn = true;
var r = PresentGrab.Begin("{E}{n}", "drive_{mode.lower()}", 3, 1.5f, 0.3f);
return r + " tint=" + ui.TintNow + " vis=" + ui.Visible;
""")
open(D + "reset.cs", "w", encoding="utf-8").write("""var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
ui.dirMode = DotScreenUI.DirMode.Compass; ui.SimWarn = false; return "reset pending=" + PresentGrab.Pending;
""")
print("ok")
