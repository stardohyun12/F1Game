// C-002 오프닝 시연(10초). 하네스가 장소 0 "집 방"으로 옮긴 뒤 실행.
// 클립(clip.cs)이 이 스크립트보다 늦게 시작한다 → 클립 시작(SessionState ev.done 이 "" 로 바뀜)을 기다렸다가 시연 시작. 클립이 없으면(손으로 돌릴 때) 6초 뒤.
// 4판(도현 D-277 B): 걷는 구간 방향 표시 없음 — 집을 나와 마당으로 몇 걸음 나서면 시선이 정비소(아치 지붕) 쪽으로 돈다.
// 3판 FAIL "열쇠 받는 순간 안 보임" → 대화를 일찍 끝내고 "카트 열쇠를 받았다" 안내가 1.2초 이상 화면에 남은 뒤 카트로 간다.
// 클립 기준(strip 8칸 ≈ 0 · 1.3 · 2.7 · 4.0 · 5.3 · 6.7 · 8.0 · 9.3 s):
//   0~2.7 방(침대 → 달력 [E]) · 2.9 집 앞 → 3.0 마당 3 m(정비소 쪽으로 고개, 4.0 = 집 앞에서 정비소 건물, 표식 없음)
//   · 4.4 사장 앞 대화(1줄) · 5.5/5.9 줄 넘김(2줄 "자, 열쇠.") · 6.3 대화 끝 → "카트 열쇠를 받았다" · 7.6 카트 옆 → E 타기 → 깃발·화살 + "택배 보관소 ↑"
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
    if (phase == 2 && t > 2.9) { DebugWindow.GoPlace(1); phase = 3; }
    // 현관 밖에서 마당으로 3 m — 실제 플레이에선 도현이 걸어 나오는 몇 걸음(순간이동은 시간 줄이기). 문을 등지고 선 그대로.
    if (phase == 3 && t > 3.0 && Opening.Step == OpeningSequence.Stage.Front)
    {
        Vector3 p = new Vector3(184.8f, 40f, -69.5f);
        if (Physics.Raycast(p, Vector3.down, out RaycastHit h, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p = h.point;
        d.Player.Teleport(p, 190f);
        phase = 4;
    }
    if (phase == 4 && t > 4.1) { seen += "[4.1 집앞 시선돌림=" + d.OpeningSeq.FrontTurned + " yaw=" + d.Player.Cam.Yaw.ToString("F0") + " " + Snap() + "] "; phase = 5; }
    if (phase == 5 && t > 4.3) { DebugWindow.GoPlace(2); phase = 6; }
    if (phase == 6 && t > 4.4) { if (d.ForceTalk()) { lineAt = d.TalkLineAt; phase = 7; } }
    if (phase == 7)
    {
        // 줄 넘김: 5.5 s 1→2, 5.9 s 2→3, 6.3 s 끝. 넘어갈 때까지(TalkLineAt 이 바뀔 때까지) 다시 누른다 — 0.3 초 규칙에 버려져도 다음 프레임에 또.
        if (d.InTalk && d.TalkLineAt != lineAt) { adv++; lineAt = d.TalkLineAt; if (adv == 1) seen += "[" + t.ToString("F1") + " 대화 " + d.TalkWho + ":" + d.TalkText + "] "; }
        int want = (t > 5.5 ? 1 : 0) + (t > 5.9 ? 1 : 0) + (t > 6.3 ? 1 : 0);
        if (d.InTalk && adv < want && Time.unscaledTime - d.TalkLineAt > 0.32f) d.SimInteract();
        if (!d.InTalk) { seen += "[" + t.ToString("F1") + " 대화끝 열쇠=" + d.OpeningSeq.HasKey + " " + Snap() + "] "; phase = 8; }
    }
    // 열쇠 안내가 화면에 남는 1.2초 뒤에만 카트로 — 대화 중 GoPlace 는 대화를 취소한다(DebugLeaveState).
    if (phase == 8 && t > 7.6)
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
            phase = 9;
        }
    }
    if (phase == 9)
    {
        if (d.State == OnFootDirector.Mode.OnFoot)
        {
            if (d.Car != null) d.Player.Cam.LookAt(d.Car.transform.position + Vector3.up * 0.5f);
            if (d.Prompt == "E 타기") d.SimInteract();
            if (t > 10.5) { seen += "[타기 안 됨 " + Snap() + "]"; phase = 99; }
        }
        else if (d.State == OnFootDirector.Mode.InCart) { cartAt = t; seen += "[" + t.ToString("F1") + " 탐] "; phase = 10; }
    }
    if (phase == 10 && t - cartAt > 0.7) { seen += "[" + t.ToString("F1") + " 카트 " + d.State + " " + Snap() + "]"; phase = 99; }
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
