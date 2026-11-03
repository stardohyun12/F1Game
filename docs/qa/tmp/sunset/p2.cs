var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var sb = new System.Text.StringBuilder();
sb.Append("state=" + d.State + " seated=" + spot.Seated + " landed=" + spot.Landed + " prompt='" + d.Prompt + "' bobber=" + spot.BobberPosition.ToString("F2"));
sb.Append(" cam=" + Camera.main.transform.position.ToString("F2") + " rot=" + Camera.main.transform.eulerAngles.ToString("F1"));
// 화면에 켜진 Canvas(HUD) — LowRes 캔버스 빼고
foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
    if (c.isActiveAndEnabled && c.isRootCanvas && c.name != "LowResRenderTest_Canvas") { int g = 0; foreach (var gr in c.GetComponentsInChildren<UnityEngine.UI.Graphic>(false)) if (gr.enabled && gr.color.a > 0.01f) g++; if (g > 0) sb.Append(" | canvas " + c.name + " graphics=" + g); }
FrameFlickerProbe.Begin(8f, "D:/Github/F1Game/docs/qa/evidence/sunset/", "seated");
return sb.ToString();
