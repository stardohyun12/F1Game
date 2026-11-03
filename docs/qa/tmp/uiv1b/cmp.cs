Application.runInBackground = true;
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
string dir = "D:/Github/F1Game/docs/qa/evidence/ui-v1b/pairs";
Time.timeScale = 0f;
ui.SimArrowV1 = true;
var r1 = PresentGrab.Begin(dir, "TAG_v1", 1, 0f, 0.3f, () => { ui.SimArrowV1 = false; });
var r2 = PresentGrab.Begin(dir, "TAG_v1b", 1, 0f, 0.8f, () => { Time.timeScale = 1f; });
return r1 + "/" + r2;
