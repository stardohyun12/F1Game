var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
ui.dirMode = DotScreenUI.DirMode.Road; ui.SimWarn = true;
var r = PresentGrab.Begin("D:/Github/F1Game/docs/qa/evidence/ui-v2/dusk", "drive_road", 3, 1.5f, 0.3f);
return r + " tint=" + ui.TintNow + " vis=" + ui.Visible;
