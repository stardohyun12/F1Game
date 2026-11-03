// C-002 기준 2 — 집 앞에 서면 정비소 건물이 화면 안에 보인다(걷는 구간 방향 표시 없음 — 도현 D-277 B) + 사장 대화 3줄 이내 후 카트 열쇠.
// 사진 1장 = 집 앞 마당(현관 밖 3 m)에서 정비소(아치 지붕) 쪽으로 고개가 돈 순간. 화살·거리·깃발 없음, 안내 글 "정비소로 — …" 만.
// 열쇠: 사진엔 안 담기므로(다른 자리) 아래에서 실제 대화를 끝까지 돌려 결과를 return·SessionState 에 남긴다 — 캡처가 끝난 뒤(ev.shot)에 시작.
// 하네스가 새 Play → 장소 0 "집 방" 뒤 실행.
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
d.OpeningSeq.StopAllCoroutines(); d.Player.Cam.HoldLook = false;   // 깨어남 연출이 남아 있으면 끊는다
DebugWindow.GoPlace(1);                                             // 집 앞(현관 밖) → 다음 프레임 Step=Front
double t0 = UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.SessionState.SetString("ev.shot", "");   // 하네스 shot.cs 가 캡처 끝에 "ok …" 를 쓴다 — 그 뒤에만 사장 대화로 간다
int phase = 0, adv = 0; float lineAt = -1f;
string res = "";
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    double t = UnityEditor.EditorApplication.timeSinceStartup - t0;
    if (phase == 0 && t > 0.15 && Opening.Step == OpeningSequence.Stage.Front)
    {
        // 마당으로 몇 걸음(3 m) — 문을 등지고 선 그대로. 3 m 넘으면 OpeningSequence 가 정비소 쪽으로 고개를 돌린다(0.8 s).
        Vector3 p = new Vector3(184.8f, 40f, -69.5f);
        if (Physics.Raycast(p, Vector3.down, out RaycastHit h, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p = h.point;
        d.Player.Teleport(p, 190f);
        phase = 1;
    }
    if (phase == 1 && t > 1.6)
    {
        res = "집앞: Step=" + Opening.Step + " 시선돌림=" + d.OpeningSeq.FrontTurned + " yaw=" + d.Player.Cam.Yaw.ToString("F0")
            + " 방향표시=" + (ui != null ? ui.DestMode : "-") + " WalkDest=" + (d.WalkDest != null ? d.WalkDest.Label : "없음") + " 안내=" + d.OpeningSeq.Caption;
        phase = 2;
    }
    // 캡처가 끝난 뒤(ev.shot 이 채워짐, 없으면 15 s): 사장 앞 → 실제 대화 3줄 → 열쇠
    if (phase == 2 && (UnityEditor.SessionState.GetString("ev.shot", "").Length > 0 || t > 15.0)) { DebugWindow.GoPlace(2); t0 = UnityEditor.EditorApplication.timeSinceStartup - 100.0; phase = 3; }
    if (phase == 3 && t > 100.15) { if (d.ForceTalk()) { lineAt = d.TalkLineAt; phase = 4; } }
    if (phase == 4)
    {
        if (d.InTalk && d.TalkLineAt != lineAt) { adv++; lineAt = d.TalkLineAt; }
        if (d.InTalk && Time.unscaledTime - d.TalkLineAt > 0.35f) d.SimInteract();
        if (!d.InTalk)
        {
            res += " / 대화 " + (adv + 1) + "줄 끝 → 열쇠=" + d.OpeningSeq.HasKey + " Step=" + Opening.Step + " 안내=" + d.OpeningSeq.Caption + " / 순서=" + Opening.Trail;
            UnityEditor.SessionState.SetString("c002.crit2", res);
            Debug.Log("[Opening] 기준2 " + res);
            UnityEditor.EditorApplication.update -= cb;
        }
    }
};
UnityEditor.EditorApplication.update += cb;
return "기준2: 집 앞 마당 → 정비소 쪽 시선(1.6 s), 캡처 뒤 사장 대화 → 열쇠 (결과 SessionState c002.crit2)";
