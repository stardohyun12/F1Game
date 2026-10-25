// 과제 0344: 건너뛰기 직후(T0) 화물 게이지 보이는지 + 단계·런 상태.
Application.runInBackground = true;
var rc = UnityEngine.Object.FindFirstObjectByType<RunController>();
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>();
var hud = UnityEngine.Object.FindFirstObjectByType<IntegrityHUD>(FindObjectsInactive.Include);
string vis = "-";
if (hud != null)
{
    var m = typeof(IntegrityHUD).GetMethod("ShouldShow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    bool should = (bool)m.Invoke(hud, null);
    var cg = hud.GetComponentsInChildren<CanvasGroup>(true).Select(c => c.name + ":a" + c.alpha.ToString("F1")).ToArray();
    var tmps = hud.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Select(t => "'" + t.text + "' on=" + t.isActiveAndEnabled).ToArray();
    vis = $"ShouldShow={should} goActive={hud.gameObject.activeInHierarchy} cg=[{string.Join(",", cg)}] text=[{string.Join(",", tmps)}]";
}
var fi = ot != null ? typeof(OpeningTutorial).GetField("stageName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
return $"stage={rc.CurrentStage} RunActive={rc.RunActive} ot={(ot != null)} hud: {vis}";
