// C-002 오프닝 시연(10초). 하네스가 장소 0 "집 방"으로 옮긴 뒤 실행.
// 클립(clip.cs)이 이 스크립트보다 늦게 시작한다 → 클립 시작(SessionState ev.done 이 "" 로 바뀜)을 기다렸다가 시연 시작. 클립이 없으면(손으로 돌릴 때) 6초 뒤.
// 2판 FAIL 원인: 대화 E 를 시각에 맞춰 한 번씩만 눌렀다 → 하네스 실행에서 한 번이 "한 줄 최소 0.3초"에 걸려 버려졌고, 대화가 안 끝난 채
// GoPlace(3) 이 대화를 취소해 열쇠 없이 카트 옆 → "사장님한테 먼저". 이제 상태를 보고 넘긴다: 줄이 넘어갈 때까지 다시 누르고,
// 대화가 끝나 열쇠를 받기 전엔 카트로 안 간다, 탈 때까지 다시 누른다.
// 클립 기준(strip 8칸 ≈ 0 · 1.3 · 2.7 · 4.0 · 5.3 · 6.7 · 8.0 · 9.3 s):
//   0~2.7 방(침대 → 달력 [E]) · 3.0 집 앞(정비소 쪽을 봄 — 깃발 + "정비소 ↑ 50m") · 4.6 사장 앞 대화 · 5.7/6.9 줄 넘김(2줄 "자, 열쇠.")
//   · 7.4 대화 끝(열쇠 받음 안내 + 보관소 쪽 시선) · 8.3 카트 옆(카트 머리를 보관소 쪽으로) · E 타기 → 깃발·화살·거리 + "택배 보관소 ↑"
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
UnityEditor.SessionState.SetString("ev.done", "WAIT");   // clip.cs 가 시작하면서 "" 로 바꾼다(bundle.sh 는 "{" 로 시작할 때만 끝으로 본다)
UnityEditor.SessionState.SetString("c002.log", "");
double w0 = UnityEditor.EditorApplication.timeSinceStartup, t0 = -1, cartAt = -1;
int phase = 0, adv = 0;
float lineAt = -1f;
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
    if (phase == 5 && t > 4.8) { if (d.ForceTalk()) { lineAt = d.TalkLineAt; phase = 6; } }
    if (phase == 6)
    {
        // 줄 넘김: 5.7 s 에 1→2, 6.9 s 에 2→3, 7.4 s 에 끝. 넘어갈 때까지(TalkLineAt 이 바뀔 때까지) 다시 누른다 — 0.3 초 규칙에 버려져도 다음 프레임에 또.
        if (d.InTalk && d.TalkLineAt != lineAt) { adv++; lineAt = d.TalkLineAt; if (adv == 1) seen += "[" + t.ToString("F1") + " 대화 " + d.TalkWho + ":" + d.TalkText + "] "; }
        int want = (t > 5.7 ? 1 : 0) + (t > 6.9 ? 1 : 0) + (t > 7.4 ? 1 : 0);
        if (d.InTalk && adv < want && Time.unscaledTime - d.TalkLineAt > 0.35f) d.SimInteract();
        if (!d.InTalk) { seen += "[" + t.ToString("F1") + " 대화끝 열쇠=" + d.OpeningSeq.HasKey + " " + Snap() + "] "; phase = 7; }
    }
    // 대화가 끝나 열쇠를 받은 뒤에만 카트로 — 대화 중 GoPlace 는 대화를 취소한다(DebugLeaveState).
    if (phase == 7 && t > 8.3)
    {
        if (!d.OpeningSeq.HasKey) { seen += "[열쇠 없음 — 멈춤]"; phase = 99; }
        else
        {
            // [컷] 카트 머리를 보관소 쪽으로(실제 플레이에선 도현이 돌린다) — 타자마자 뒤 카메라 화면 안에 보관소 깃발이 선다
            var car = d.Car; var wd = d.WalkDest;
            if (car != null && wd != null)
            {
                Vector3 to = wd.transform.position - car.transform.position; to.y = 0f;
                var rb = car.GetComponent<Rigidbody>(); Quaternion r = Quaternion.LookRotation(to.normalized, Vector3.up);
                car.transform.rotation = r; if (rb != null) { rb.rotation = r; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms();
            }
            DebugWindow.GoPlace(3);
            phase = 8;
        }
    }
    if (phase == 8)
    {
        if (d.State == OnFootDirector.Mode.OnFoot)
        {
            if (d.Car != null) d.Player.Cam.LookAt(d.Car.transform.position + Vector3.up * 0.5f);
            if (d.Prompt == "E 타기") d.SimInteract();
            if (t > 10.5) { seen += "[타기 안 됨 " + Snap() + "]"; phase = 99; }
        }
        else if (d.State == OnFootDirector.Mode.InCart) { cartAt = t; seen += "[" + t.ToString("F1") + " 탐] "; phase = 9; }
    }
    if (phase == 9 && t - cartAt > 0.7) { seen += "[" + t.ToString("F1") + " 카트 " + d.State + " " + Snap() + "]"; phase = 99; }
    if (phase == 99)
    {
        string line = "Step 순서=" + Opening.Trail + " / " + seen;
        UnityEditor.SessionState.SetString("c002.log", line);
        Debug.Log("[Opening] 시연 끝 " + line);
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "C-002 오프닝 시연 대기: 클립 시작(또는 6초) 뒤 약 9.5초 — 끝나면 SessionState c002.log / 콘솔 [Opening] 시연 끝 = Step 순서(Room→Front→Talk→Key→Cart)";
