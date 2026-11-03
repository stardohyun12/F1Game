// C-003 첫 배달 시연(그레이박스, 약 10초). 하네스가 장소 11(택배 보관소 앞)로 옮긴 뒤 exec 로 부른다. Play 중에만, 새 Play 에서 한 번.
// 칠판 쪽지 E → (시선이 작업대로) Tab 전표 읽기 자세 0.85초 → 다시 들고 걸어가 작업대 [E] 0.6초 → 포장(PackBench 봇, 14배속) → Q 물러나 [E] 상자 들기 → (시선이 카트로)
// → [컷] 카트 옆(카트 머리를 찻집 쪽으로) E 싣고 타기 → 카트 뒤 화면 1.5초(짐칸 상자 생김·깃발 = 찻집) → [컷] 찻집 앞 길에 카트 → E 내리기 = 도착 → (시선이 문으로) 걸어가 E 두드리기 → 대화 E·E = 건넴.
// 검증 strip 은 클립 약 11초에서 1.45초 간격 8칸 — 기준마다 1.5초 안팎 머물게 시간을 나눴다(2026-11-03 1회 불통과: 카트 0.5초가 strip 사이로 빠짐).
// 컷 두 곳(보관소 안→카트 옆 걷기, 보관소→찻집 운전 300 m)만 순간이동. 단계 값은 전부 실제 E 경로(SimInteract)로 바뀐다.
// 기계 확인 = 감독의 StepTrail(단계가 바뀔 때마다 한 칸): 없음→쪽지→포장→실음→도착→건넴. 결과는 콘솔 [C-003] 한 줄 + SessionState c003.result.
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.Car == null || d.OrderBoard == null || d.PackSpot == null || d.Handover == null) return "FAIL: 감독·칠판·작업대·문 배선 없음 — Map_TownV6 아님?";
if (d.DeliveryStep != "없음") return "FAIL: 이미 배달 단계 " + d.DeliveryStep + " — Play 다시";
// 시작 장면이 칠판 앞이라 오프닝(사장 대화)을 건너뛴다 — 사장 말을 들은 것으로(실제 플레이에선 오프닝이 켠다)
var tf = typeof(OnFootDirector).GetField("<Talked>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (tf != null) tf.SetValue(d, true);
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
float t0 = Time.realtimeSinceStartup, c0 = -1f, tStep = 0f, chipAt = -1f, inCartAt = -1f; int step = 0; bool shotCart = false;
int run = UnityEngine.Random.Range(1, int.MaxValue); UnityEditor.SessionState.SetInt("c003.run", run);
string seen = "";
System.Action<Vector3> walkTo = (target) => {
    Vector3 to = target - pl.transform.position; to.y = 0f;
    if (to.magnitude < 0.05f) { pl.simMove = Vector2.zero; return; }
    Vector3 local = Quaternion.Euler(0f, -fc.Yaw, 0f) * to.normalized;
    pl.simActive = true; pl.simMove = new Vector2(local.x, local.z);
};
// 다음 목표가 화면 가운데에서 몇 도 떨어졌나(스크립트가 시선을 안 건드리는 구간에서 잼 — 게임이 시선을 돌려 줬는지)
System.Func<Vector3, string> Off = (at) => Vector3.Angle(Camera.main.transform.forward, at - Camera.main.transform.position).ToString("F0") + "°";
System.Action stopWalk = () => { pl.simMove = Vector2.zero; pl.simActive = false; };
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
        case 0: // 쪽지 [E] 칩 → E
            fc.LookAt(note.transform.position);
            if (dt > 0.3f) { seen += "칠판[" + d.Prompt + "] "; d.SimInteract(); step = 1; tStep = t; }
            break;
        case 1: // 쪽지를 드는 사이(0.6) 시선이 저절로 작업대로 돈다 → Tab 으로 전표를 읽기 자세(화면 절반)로 — 손에 든 전표가 strip 한 칸에 크게 보이게
            if (dt > 0.6f) { d.SimTab(); step = 16; tStep = t; }
            break;
        case 16: // 읽으며 작업대 쪽으로 걸음(약 0.9초) → Tab 두 번(내려놓기 → 다시 들기, 화면 아래 23%)
            walkTo(ps.Top);
            if (dt > 0.2f && d.Held != null && d.Held.State == HeldPaper.Pose.Up) d.SimTab();   // 아직 Up 이면(앞 Tab 이 버려짐) 다시
            if (dt > 0.85f) { seen += "전표[" + (d.Held != null ? d.Held.State + " 덮음 " + d.Held.CoverY.ToString("P0") : "-") + "]@" + t.ToString("F1") + " "; d.SimTab(); step = 17; tStep = t; }
            break;
        case 17:
            walkTo(ps.Top);
            if (dt > 0.3f) { d.SimTab(); seen += "쪽지후 작업대까지 " + Off(ps.Top) + " "; step = 2; tStep = t; }
            break;
        case 2: // 작업대까지 걸어가 [E] 칩 — 전표를 든 채(Up) 칩이 뜬 화면을 0.7초 보여 준다
            fc.LookAt(ps.Top - Vector3.up * 0.15f);
            if (!(d.Prompt.StartsWith("E 포장") || d.Prompt.StartsWith("E 작업대"))) walkTo(ps.Top);
            else
            {
                stopWalk();
                if (chipAt < 0f) { chipAt = t; seen += "작업대[" + d.Prompt + " 전표=" + (d.Held != null ? d.Held.State.ToString() : "-") + "]@" + t.ToString("F1") + " "; }
                if (t - chipAt > 0.6f) { d.SimInteract(); step = 3; tStep = t; }
            }
            if (step == 2 && dt > 3f) { stopWalk(); seen += "작업대 칩 못 봄[" + d.Prompt + " " + d.GazeTarget + "] "; d.SimInteract(); step = 3; tStep = t; }
            break;
        case 3: // 포장 시점 → 봇 포장(8배속 — 포장 놀이 자체는 C-002 몫)
            if (d.State == OnFootDirector.Mode.OnFoot)
            {   // 누른 프레임에 시선 레이가 작업대를 벗어나면 E 가 버려진다(멈춘 직후 흔들림) — 칩이 남아 있으면 다시 누른다
                fc.LookAt(ps.Top - Vector3.up * 0.15f);
                if (d.Prompt.StartsWith("E 포장") || d.Prompt.StartsWith("E 작업대")) d.SimInteract();
            }
            if (d.State == OnFootDirector.Mode.Pack && !d.PackMoving) { Time.timeScale = 14f; PitCrew.PackBench.PackBench.BotRun("full", 0.6f); step = 4; tStep = t; }
            else if (dt > 2f) { seen += "포장 시점 안 됨[" + d.State + "] "; step = 99; }
            break;
        case 4:
            if (ps.Ready) { Time.timeScale = 1f; d.SimPackLeave(); step = 5; tStep = t; }
            else if (dt > 6f) { Time.timeScale = 1f; seen += "포장 안 끝남[" + PitCrew.PackBench.PackBench.BotStatus + "] "; step = 99; }
            break;
        case 5: // 물러나서 [E] 상자 들기
            if (d.State == OnFootDirector.Mode.OnFoot && !d.PackMoving)
            {
                fc.LookAt(ps.Top - Vector3.up * 0.1f);
                if (d.Prompt == "E 상자 들기") { seen += "작업대[" + d.Prompt + "]@" + t.ToString("F1") + " "; d.SimInteract(); step = 6; tStep = t; }
                else if (dt > 2f) { seen += "상자 들기 칩 없음[" + d.Prompt + "] "; step = 99; }
            }
            break;
        case 6: // 시선이 카트 쪽으로 도는 것(0.5 s) 뒤 [컷] 카트 옆
            if (!d.CarryingBox && d.Prompt == "E 상자 들기") { d.SimInteract(); tStep = t; }   // E 가 버려졌으면 다시
            if (dt > 0.6f)
            {
                seen += "상자후 카트까지 " + Off(car.transform.position) + " "; DebugWindow.GoPlace(3);
                // [컷] 동안 카트 머리를 찻집 쪽으로(실제 플레이에선 도현이 돌린다) — 타자마자 뒤 카메라 화면 안에 깃발이 찻집 위에 선다
                var wd = d.WalkDest;
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
        case 7:
            fc.LookAt(car.transform.position + Vector3.up * 0.5f);
            if (d.Prompt == "E 싣고 타기") { seen += "카트[" + d.Prompt + " 들기=" + d.CarryingBox + "]@" + t.ToString("F1") + " "; d.SimInteract(); step = 8; tStep = t; }
            else if (dt > 1.5f) { seen += "싣기 칩 없음[" + d.Prompt + "] "; step = 99; }
            break;
        case 8: // 실음 — 짐칸에 상자가 생기고, 방향 표시(깃발)가 찻집을 가리키는 카트 뒤 화면을 1.5초(strip 한 칸 이상)
            if (d.State == OnFootDirector.Mode.OnFoot && d.Prompt == "E 싣고 타기") { fc.LookAt(car.transform.position + Vector3.up * 0.5f); d.SimInteract(); tStep = t; }
            if (d.State != OnFootDirector.Mode.InCart && dt > 2f) { seen += "싣기 안 됨[" + d.State + "] "; step = 99; break; }
            if (d.State == OnFootDirector.Mode.InCart && inCartAt < 0f) inCartAt = t;
            if (inCartAt >= 0f && t - inCartAt > 0.8f && !shotCart) { shotCart = true; Shot("1-cart-marker"); }
            if (inCartAt >= 0f && t - inCartAt > 1.5f)
            {
                var mgr = Services.Delivery as DeliveryManager;
                var tgt = mgr != null ? mgr.CurrentTarget : null;
                string bear = tgt != null ? Vector3.SignedAngle(Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up), Vector3.ProjectOnPlane(tgt.transform.position - Camera.main.transform.position, Vector3.up), Vector3.up).ToString("F0") + "°" : "-";
                seen += "실음[" + d.State + " 짐칸상자=" + d.BedBox + " 표식=" + (ui != null ? ui.DestMode + " " + ui.DestDistM.ToString("F0") + "m" : "-") + " 목표=" + (tgt != null ? tgt.Label : "-") + " 카메라→목표 " + bear + "]@" + inCartAt.ToString("F1") + " ";
                // [컷] 운전 300 m — 찻집 앞 길(장소 4 '첫 배달 찻집' 자리)에 카트를 세운다
                var rb = car.GetComponent<Rigidbody>();
                Vector3 p = new Vector3(93.8f, 4.3f, -281.5f); Quaternion r = Quaternion.Euler(0f, 270f, 0f);   // 왼쪽(내리는 쪽)이 문 쪽 — 90° 면 카트가 문을 가린다
                car.transform.SetPositionAndRotation(p, r);
                if (rb != null) { rb.position = p; rb.rotation = r; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms();
                if (follow != null) follow.SnapToTarget();
                step = 9; tStep = t;
            }
            break;
        case 9: // E 내리기 = 도착 → 시선이 문으로
            if (dt > 0.2f && d.Prompt == "E 내리기") { d.SimInteract(); step = 10; tStep = t; }
            else if (dt > 2f) { seen += "내리기 칩 없음[" + d.Prompt + "] "; step = 99; }
            break;
        case 10:
            if (d.State == OnFootDirector.Mode.InCart && d.Prompt == "E 내리기") { d.SimInteract(); tStep = t; }
            if (dt > 0.35f && d.State == OnFootDirector.Mode.OnFoot) { seen += "내림[" + d.DeliveryStep + " 들기=" + d.CarryingBox + " 짐칸상자=" + d.BedBox + " 문까지 " + Off(d.Handover.DoorCenter) + "]@" + t.ToString("F1") + " "; step = 11; tStep = t; }
            break;
        case 11: // 문까지 걸어가 [E] 두드리기
            fc.LookAt(d.Handover.DoorCenter);
            walkTo(d.Handover.DoorCenter);
            if (d.Prompt == "E 두드리기") { stopWalk(); seen += "문[" + d.Prompt + "]@" + t.ToString("F1") + " "; d.SimInteract(); step = 12; tStep = t; }
            else if (dt > 3f) { stopWalk(); seen += "두드리기 칩 없음[" + d.Prompt + " " + d.GazeTarget + "] "; step = 99; }
            break;
        case 12: // 받는 사람 나옴 → 대화 두 줄 E·E (한 줄 최소 0.3초 — 열린 뒤부터 잰다)
            if (!d.InTalk && d.Prompt == "E 두드리기") { fc.LookAt(d.Handover.DoorCenter); d.SimInteract(); }
            if (d.InTalk) { seen += "대화@" + t.ToString("F1") + " "; step = 13; tStep = t; }
            else if (dt > 4f) { seen += "대화 안 열림 "; step = 99; }
            break;
        case 13:
            if (dt > 0.35f) { d.SimInteract(); step = 15; tStep = t; }
            break;
        case 15:
            if (dt > 0.35f) { Shot("2-handover"); d.SimInteract(); step = 14; tStep = t; }
            break;
        case 14:
            if (dt > 0.1f) step = 99;
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
            if (t > 12f) UnityEditor.EditorApplication.update -= cb;
            break;
    }
};
UnityEditor.EditorApplication.update += cb;
return "C-003 시연 시작: " + go + " · 단계 " + d.StepTrail + " · 칠판 쪽지 " + board.NotesLeft + "장 (결과 = 콘솔 [C-003] / SessionState c003.result)";
