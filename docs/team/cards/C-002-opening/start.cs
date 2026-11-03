// C-002 오프닝 시연(10초). 하네스가 장소 0 "집 방"으로 옮긴 뒤 실행.
// 0s 방에서 눈을 뜸(천장→달력, [E] 달력 보기) → 3s 집 앞(정비소 표식) → 5.5s 사장 앞 대화 3줄(열쇠) → 8.5s 카트 옆 E 타기(표식 → 택배 보관소)
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
d.OpeningSeq.Begin(d);
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
double t0 = UnityEditor.EditorApplication.timeSinceStartup;
int phase = 0;
string seen = "";
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    double t = UnityEditor.EditorApplication.timeSinceStartup - t0;
    if (phase == 0 && t > 2.0) { seen += "방[" + d.Prompt + "] "; phase = 1; }
    if (phase == 1 && t > 3.0)
    {
        DebugWindow.GoPlace(1);
        var cam = d.Player.Cam; if (cam != null && d.Boss != null) { cam.LookAt(d.Boss.position + Vector3.up * 1.5f); cam.Pitch = 0f; }
        phase = 2;
    }
    if (phase == 2 && t > 5.0) { seen += "집앞[" + Opening.Step + " 표식=" + (ui != null ? ui.DestMode : "-") + " " + (d.WalkDest != null ? d.WalkDest.Label : "없음") + "] "; phase = 3; }
    if (phase == 3 && t > 5.5) { DebugWindow.GoPlace(2); phase = 4; }
    if (phase == 4 && t > 5.8) { d.ForceTalk(); phase = 5; }
    if (phase >= 5 && phase <= 7 && t > 5.8 + (phase - 4) * 0.9) { d.SimInteract(); phase++; }
    if (phase == 8 && t > 8.6) { seen += "대화끝[" + Opening.Step + " 열쇠=" + d.OpeningSeq.HasKey + "] "; DebugWindow.GoPlace(3); phase = 9; }
    if (phase == 9 && t > 8.9) { d.SimInteract(); phase = 10; }
    if (phase == 10 && t > 9.6)
    {
        seen += "카트[" + d.State + " 표식=" + (ui != null ? ui.DestMode : "-") + " " + (d.WalkDest != null ? d.WalkDest.Label : "없음") + "]";
        Debug.Log("[Opening] 시연 끝 Step 순서=" + Opening.Trail + " / " + seen);
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "C-002 오프닝 시연 시작: Step=" + Opening.Step + " 시선=" + d.Prompt + " (10초 뒤 콘솔 [Opening] 시연 끝 = 단계 순서)";
