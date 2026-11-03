// C-002 오프닝 시연(10초). 하네스가 장소 0 "집 방"으로 옮긴 뒤 실행.
// 1판 FAIL 원인: 클립(clip.cs)이 이 스크립트보다 ~3초 늦게 시작해 방 장면을 통째로 놓침 → 클립이 시작되는 순간
// (SessionState ev.done 이 "" 로 바뀜)을 기다렸다가 시연을 시작한다. 클립이 없으면(손으로 돌릴 때) 6초 뒤 시작.
// 클립 기준: 0s 방(침대 → 달력 [E]) · 3.0s 집 앞(정비소 깃발 + 안내) · 4.6s 사장 앞 대화 3줄(2줄 "자, 열쇠.")
//            · 7.6s 대화 끝(열쇠 받음 안내 + 보관소 쪽 시선) · 8.5s 카트 E 타기(보관소 표식 + 안내)
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
UnityEditor.SessionState.SetString("ev.done", "WAIT");   // clip.cs 가 시작하면서 "" 로 바꾼다(bundle.sh 는 "{" 로 시작할 때만 끝으로 본다)
UnityEditor.SessionState.SetString("c002.log", "");
double w0 = UnityEditor.EditorApplication.timeSinceStartup, t0 = -1;
int phase = 0;
string seen = "";
System.Func<string> Snap = () => "Step=" + Opening.Step + " 시선=" + d.Prompt + " 표식=" + (ui != null ? ui.DestMode : "-") + "→" + (d.WalkDest != null ? d.WalkDest.Label : "없음") + " 안내=" + d.OpeningSeq.Caption;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    double now = UnityEditor.EditorApplication.timeSinceStartup;
    if (t0 < 0)
    {
        if (UnityEditor.SessionState.GetString("ev.done", "") != "WAIT" || now - w0 > 6.0) { t0 = now; d.OpeningSeq.Begin(d); }
        return;
    }
    double t = now - t0;
    if (phase == 0 && t > 0.4) { seen += "[0.4 방 " + Snap() + "] "; phase = 1; }
    if (phase == 1 && t > 2.6) { seen += "[2.6 방 " + Snap() + "] "; phase = 2; }
    if (phase == 2 && t > 3.0)
    {
        DebugWindow.GoPlace(1);
        var cam = d.Player.Cam; if (cam != null && d.Boss != null) { cam.LookAt(d.Boss.position + Vector3.up * 1.5f); cam.Pitch = 0f; }
        phase = 3;
    }
    if (phase == 3 && t > 4.2) { seen += "[4.2 집앞 " + Snap() + "] "; phase = 4; }
    if (phase == 4 && t > 4.6) { DebugWindow.GoPlace(2); phase = 5; }
    if (phase == 5 && t > 4.8) { d.ForceTalk(); phase = 6; }
    // 대화 3줄: 1줄 4.8~6.0, 2줄(열쇠) 6.0~7.1, 3줄 7.1~7.6, 7.6 끝
    if (phase == 6 && t > 6.0) { d.SimInteract(); phase = 7; }
    if (phase == 7 && t > 6.8) { seen += "[6.8 대화 " + d.TalkWho + ":" + d.TalkText + "] "; phase = 8; }
    if (phase == 8 && t > 7.1) { d.SimInteract(); phase = 9; }
    if (phase == 9 && t > 7.6) { d.SimInteract(); phase = 10; }
    if (phase == 10 && t > 8.2) { seen += "[8.2 대화끝 열쇠=" + d.OpeningSeq.HasKey + " " + Snap() + "] "; phase = 11; }
    if (phase == 11 && t > 8.5) { DebugWindow.GoPlace(3); phase = 12; }
    if (phase == 12 && t > 8.7) { d.SimInteract(); phase = 13; }
    if (phase == 13 && t > 9.6)
    {
        seen += "[9.6 카트 " + d.State + " " + Snap() + "]";
        string line = "Step 순서=" + Opening.Trail + " / " + seen;
        UnityEditor.SessionState.SetString("c002.log", line);
        Debug.Log("[Opening] 시연 끝 " + line);
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "C-002 오프닝 시연 대기: 클립 시작(또는 6초) 뒤 10초 — 끝나면 SessionState c002.log / 콘솔 [Opening] 시연 끝 = Step 순서(Room→Front→Talk→Key→Cart)";
