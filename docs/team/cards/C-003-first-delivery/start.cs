// C-003 첫 배달 시연(그레이박스, 약 10초). 하네스가 장소 11(택배 보관소 앞)로 옮긴 뒤 exec 로 부른다. Play 중에만, 새 Play 에서 한 번.
// 3판(2026-11-03 2회 불통과 고침): 검증 strip 은 12fps 클립에서 16장 간격 8칸 = 0 · 1.33 · 2.67 · 4.0 · 5.33 · 6.67 · 8.0 · 9.33 s.
// 2판은 건네기가 10초 뒤로 밀려 마지막 칸이 문 앞뿐이었다 → 단계마다 "그 칸 전엔 안 넘김" 시각만 두고 나머지는 상태가 되는 즉시 넘긴다.
//   0.0 칠판 [E] 쪽지 집기 → 1.33 손에 든 전표 + 작업대 [E] 포장하기 + 안내 글 → 2.67 포장(봇 14배속) → 4.0 상자 든 채 카트 옆 [E] 싣고 타기
//   → 5.33 카트 뒤 화면: 짐칸 상자 + 깃발 + 안내 글 「언덕 위 찻집 줄리아네 ↑ 310m」 → 6.67 내려서 문 두드림(문 열림)
//   → 8.0 줄리아 반응 줄 + 돈 → 9.33 안내 글 「첫 배달 끝 +1,200원 — 집에 가서 자자」(배달 끝 상태)
// 컷 두 곳(보관소 안→카트 옆, 보관소→찻집 앞 운전 300 m)만 순간이동. 단계 값은 전부 실제 E·Tab 경로(SimInteract)로 바뀐다.
// 시작 장면이 칠판 앞이라 오프닝(사장 대화·카트 열쇠)은 끝난 것으로 둔다 — 실제 플레이에선 C-002 오프닝이 거기까지 데려온다.
// 기계 확인 = 감독의 StepTrail: 없음→쪽지→포장→실음→도착→건넴 + 단계마다 안내 글(DeliveryCaption). 결과 = 콘솔 [C-003] 한 줄 + SessionState c003.result.
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.Car == null || d.OrderBoard == null || d.PackSpot == null || d.Handover == null) return "FAIL: 감독·칠판·작업대·문 배선 없음 — Map_TownV6 아님?";
if (d.DeliveryStep != "없음") return "FAIL: 이미 배달 단계 " + d.DeliveryStep + " — Play 다시";
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(OnFootDirector).GetField("<Talked>k__BackingField", BF)?.SetValue(d, true);
var op = d.OpeningSeq;
if (op != null)
{   // 오프닝 끝(Cart 단계, 카트 안내 시간 지남) — 오프닝 안내 글 「방학 1일째 아침」·「정비소로」가 배달 안내를 덮지 않게
    typeof(OpeningSequence).GetField("<Step>k__BackingField", BF)?.SetValue(op, OpeningSequence.Stage.Cart);
    typeof(OpeningSequence).GetField("<HasKey>k__BackingField", BF)?.SetValue(op, true);
    typeof(OpeningSequence).GetField("stepAt", BF)?.SetValue(op, -99f);
}
string go = DebugWindow.GoPlace(12);   // 택배 보관소 칠판 1.8 m 앞
var fc = d.Player.Cam;
var pl = d.Player;
var board = d.OrderBoard; var ps = d.PackSpot; var car = d.Car;
var note = board.NearestNote(board.Center);
if (note == null) return "FAIL: 칠판 쪽지 없음(HeldPaper.On 꺼짐?)";
fc.LookAt(note.transform.position);
var follow = Camera.main.GetComponent<FollowCamera>();
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();

UnityEditor.SessionState.SetString("ev.done", "c003-wait");
UnityEditor.SessionState.SetString("c003.result", "");
float t0 = Time.realtimeSinceStartup, c0 = -1f, tStep = 0f, chipAt = -1f; int step = 0;
int run = UnityEngine.Random.Range(1, int.MaxValue); UnityEditor.SessionState.SetInt("c003.run", run);
string seen = "";
System.Action<Vector3, bool> walkTo = (target, runFast) => {
    Vector3 to = target - pl.transform.position; to.y = 0f;
    if (to.magnitude < 0.05f) { pl.simMove = Vector2.zero; return; }
    Vector3 local = Quaternion.Euler(0f, -fc.Yaw, 0f) * to.normalized;
    pl.simActive = true; pl.simMove = new Vector2(local.x, local.z);
    pl.simRun = runFast;
};
System.Action stopWalk = () => { pl.simMove = Vector2.zero; pl.simRun = false; pl.simActive = false; };
System.Func<float, string> At = (x) => "@" + x.ToString("F1");
System.Func<string> Cap = () => "「" + d.DeliveryCaption + "」";
// 확인용 캡처 — SessionState c003.shots 에 폴더가 있을 때만(하네스 클립엔 안 찍힘)
string shots = UnityEditor.SessionState.GetString("c003.shots", "");
System.Action<string> Shot = (n) => { if (shots != "") ScreenCapture.CaptureScreenshot(shots + "/" + n + ".png"); };
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c003.run", 0) != run) { UnityEditor.EditorApplication.update -= cb; return; }
    float now = Time.realtimeSinceStartup;
    if (c0 < 0f) { if (UnityEditor.SessionState.GetString("ev.done", "") != "c003-wait" || now - t0 > 20f) { c0 = now; tStep = 0f; } else { fc.LookAt(note.transform.position); return; } }
    float t = now - c0, dt = t - tStep;
    switch (step)
    {
        case 0: // 0.0 칸: 쪽지 [E] 칩 → E
            fc.LookAt(note.transform.position);
            if (dt > 0.25f) { seen += "칠판[" + d.Prompt + "]" + At(t) + " "; d.SimInteract(); step = 1; tStep = t; }
            break;
        case 1: // 쪽지를 손에 들고(0.6 s 들어 올림) 작업대로 뛰어감 → 1.33 칸: 손에 든 전표 + [E] 포장하기 + 안내 글
            fc.LookAt(ps.Top - Vector3.up * 0.15f);
            if (dt < 0.1f) break;
            if (!(d.Prompt.StartsWith("E 포장") || d.Prompt.StartsWith("E 작업대"))) walkTo(ps.Top, true);
            else
            {
                stopWalk();
                if (chipAt < 0f) { chipAt = t; seen += "작업대[" + d.Prompt + " 전표=" + (d.Held != null ? d.Held.State.ToString() : "-") + " " + Cap() + "]" + At(t) + " "; }
                if (t > 1.5f && t - chipAt > 0.2f) { Shot("1-slip-bench"); d.SimInteract(); step = 3; tStep = t; }
            }
            if (step == 1 && dt > 3f) { stopWalk(); seen += "작업대 칩 못 봄[" + d.Prompt + " " + d.GazeTarget + "] "; step = 99; }
            break;
        case 3: // 포장 시점 → 봇 포장(14배속 — 포장 놀이 자체는 C-002 몫)
            if (d.State == OnFootDirector.Mode.OnFoot)
            {   // 누른 프레임에 시선 레이가 작업대를 벗어나면 E 가 버려진다 — 칩이 남아 있으면 다시 누른다
                fc.LookAt(ps.Top - Vector3.up * 0.15f);
                if (d.Prompt.StartsWith("E 포장") || d.Prompt.StartsWith("E 작업대")) d.SimInteract();
            }
            if (d.State == OnFootDirector.Mode.Pack && !d.PackMoving) { Time.timeScale = 14f; PitCrew.PackBench.PackBench.BotRun("full", 0.6f); seen += "포장 시작" + At(t) + " "; step = 4; tStep = t; }
            else if (dt > 2f) { seen += "포장 시점 안 됨[" + d.State + "] "; step = 99; }
            break;
        case 4:
            if (ps.Ready) { Time.timeScale = 1f; d.SimPackLeave(); seen += "포장 끝" + At(t) + " "; step = 5; tStep = t; }
            else if (dt > 6f) { Time.timeScale = 1f; seen += "포장 안 끝남[" + PitCrew.PackBench.PackBench.BotStatus + "] "; step = 99; }
            break;
        case 5: // 물러나서 [E] 상자 들기
            if (d.State == OnFootDirector.Mode.OnFoot && !d.PackMoving)
            {
                fc.LookAt(ps.Top - Vector3.up * 0.1f);
                if (d.Prompt == "E 상자 들기") { seen += "작업대[" + d.Prompt + " " + Cap() + "]" + At(t) + " "; d.SimInteract(); step = 6; tStep = t; }
                else if (dt > 2f) { seen += "상자 들기 칩 없음[" + d.Prompt + "] "; step = 99; }
            }
            break;
        case 6: // 상자를 들었으면(시선이 카트 쪽으로 돈다) [컷] 카트 옆 — 4.0 칸 전에
            if (!d.CarryingBox && d.Prompt == "E 상자 들기") { d.SimInteract(); tStep = t; }
            if (d.CarryingBox && dt > 0.35f)
            {
                seen += "상자 듦[" + Cap() + "]" + At(t) + " "; DebugWindow.GoPlace(3);
                // [컷] 동안 카트 머리를 찻집 쪽으로(실제 플레이에선 도현이 돌린다) — 타자마자 뒤 카메라 화면 안에 깃발이 찻집 위에 선다
                var wd = d.FirstDrop;
                if (wd != null)
                {
                    Vector3 to = wd.transform.position - car.transform.position; to.y = 0f;
                    var rb = car.GetComponent<Rigidbody>(); Quaternion r = Quaternion.LookRotation(to.normalized, Vector3.up);
                    car.transform.rotation = r; if (rb != null) { rb.rotation = r; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                    Physics.SyncTransforms();
                }
                step = 7; tStep = t;
            }
            break;
        case 7: // 4.0 칸: 상자 든 채 카트 옆 [E] 싣고 타기 + 안내 글 → 4.15 뒤 E
            fc.LookAt(car.transform.position + Vector3.up * 0.5f);
            if (d.Prompt == "E 싣고 타기")
            {
                if (chipAt >= 0f) { chipAt = -1f; seen += "카트[" + d.Prompt + " 들기=" + d.CarryingBox + " " + Cap() + "]" + At(t) + " "; }
                if (t > 4.15f) { Shot("2-carry-cart"); d.SimInteract(); step = 8; tStep = t; }
            }
            else if (dt > 2f) { seen += "싣기 칩 없음[" + d.Prompt + "] "; step = 99; }
            break;
        case 8: // 실음 — 5.33 칸: 짐칸 상자 + 찻집 깃발 + 안내 글. 5.45 뒤 [컷] 찻집 앞 길
            if (d.State == OnFootDirector.Mode.OnFoot && d.Prompt == "E 싣고 타기") { fc.LookAt(car.transform.position + Vector3.up * 0.5f); d.SimInteract(); }
            if (d.State != OnFootDirector.Mode.InCart) { if (dt > 2f) { seen += "싣기 안 됨[" + d.State + "] "; step = 99; } break; }
            if (chipAt < 0f) chipAt = t;
            if (t > 5.45f && t - chipAt > 0.3f)
            {
                Shot("3-cart-dest");
                var mgr = Services.Delivery as DeliveryManager;
                var tgt = mgr != null ? mgr.CurrentTarget : null;
                seen += "실음[짐칸상자=" + d.BedBox + " 표식=" + (ui != null ? ui.DestMode + " " + ui.DestDistM.ToString("F0") + "m" : "-") + " 목표=" + (tgt != null ? tgt.Label : "-") + " " + Cap() + "]" + At(chipAt) + " ";
                // [컷] 운전 300 m — 찻집 앞 길, 내리는 쪽(왼쪽)이 문 쪽이고 내린 자리에서 문까지 약 2 m
                var rb = car.GetComponent<Rigidbody>();
                Vector3 p = new Vector3(93.8f, 4.3f, -282.6f); Quaternion r = Quaternion.Euler(0f, 270f, 0f);
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p.y = hit.point.y + 0.3f;
                car.transform.SetPositionAndRotation(p, r);
                if (rb != null) { rb.position = p; rb.rotation = r; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms();
                if (follow != null) follow.SnapToTarget();
                step = 9; tStep = t;
            }
            break;
        case 9: // E 내리기 = 도착 → 상자 들고 문으로
            if (dt > 0.1f && d.State == OnFootDirector.Mode.InCart && d.Prompt == "E 내리기") { seen += "카트[" + d.Prompt + " " + Cap() + "] "; d.SimInteract(); }
            if (d.State == OnFootDirector.Mode.OnFoot) { seen += "내림[" + d.DeliveryStep + " 들기=" + d.CarryingBox + " " + Cap() + "]" + At(t) + " "; step = 11; tStep = t; }
            else if (dt > 2f) { seen += "내리기 안 됨[" + d.Prompt + "] "; step = 99; }
            break;
        case 11: // 문 [E] 두드리기(가까우면 바로, 멀면 걸어감)
            fc.LookAt(d.Handover.DoorCenter);
            if (d.Prompt == "E 두드리기") { stopWalk(); seen += "문[" + d.Prompt + "]" + At(t) + " "; d.SimInteract(); step = 12; tStep = t; }
            else if (dt > 0.15f) walkTo(d.Handover.DoorCenter, false);
            if (step == 11 && dt > 3f) { stopWalk(); seen += "두드리기 칩 없음[" + d.Prompt + " " + d.GazeTarget + "] "; step = 99; }
            break;
        case 12: // 문이 열리고 줄리아가 나옴 → 대화(인사 줄)
            if (!d.InTalk && !d.Knocking && d.Prompt == "E 두드리기") { fc.LookAt(d.Handover.DoorCenter); d.SimInteract(); }
            if (d.InTalk) { seen += "대화[" + d.TalkWho + "]" + At(t) + " "; step = 13; tStep = t; }
            else if (dt > 4f) { seen += "대화 안 열림 "; step = 99; }
            break;
        case 13: // 인사 줄 → (0.35 s 뒤) 반응 줄 = 상자 건넴·돈. 8.0 칸에 반응 줄이 떠 있게 7.6 전엔 안 넘김
            if (dt > 0.35f && t > 7.6f) { d.SimInteract(); step = 14; tStep = t; }
            break;
        case 14: // 줄이 안 넘어갔으면(한 줄 최소 0.3 s 에 걸림) 다시 누름
            if (d.HandoverMoney > 0) { seen += "건넴[" + d.TalkWho + ": " + d.TalkText + " 돈 " + d.MoneyText + "]" + At(t) + " "; step = 15; tStep = t; }
            else if (dt > 0.3f) { d.SimInteract(); tStep = t; }
            break;
        case 15: // 반응 줄을 8.0 칸까지 보여 주고 8.15 뒤 닫음 → 9.33 칸: 배달 끝 안내 글
            if (t > 8.15f && dt > 0.35f) { Shot("4-handover"); d.SimInteract(); step = 16; tStep = t; }
            break;
        case 16:
            if (d.HandedOver) { seen += "끝[" + d.DeliveryStep + " " + Cap() + "]" + At(t) + " "; step = 99; }
            else if (dt > 0.3f) { d.SimInteract(); tStep = t; }
            break;
        case 99:
        {
            stopWalk(); Time.timeScale = 1f;
            string res = "단계 " + d.StepTrail + " · 건넴=" + d.HandedOver + " 돈 " + d.HandoverMoney + " · " + seen + "· 끝 " + t.ToString("F1") + "s";
            UnityEditor.SessionState.SetString("c003.result", res);
            Debug.Log("[C-003] " + res);
            step = 100;
            break;
        }
        case 100:
            if (t > 9.5f && shots != "") { Shot("5-end"); shots = ""; }
            if (t > 12f) UnityEditor.EditorApplication.update -= cb;
            break;
    }
};
UnityEditor.EditorApplication.update += cb;
return "C-003 시연 시작: " + go + " · 단계 " + d.StepTrail + " · 칠판 쪽지 " + board.NotesLeft + "장 · 오프닝 " + (op != null ? op.Step.ToString() : "-") + " (결과 = 콘솔 [C-003] / SessionState c003.result)";
