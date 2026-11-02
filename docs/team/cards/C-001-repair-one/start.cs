// C-001 시연(그레이박스). 하네스가 장소 9 정비소 안으로 옮긴 뒤 exec 로 부른다. Play 중에만.
// 앞부분(바로 실행) = 기계 확인: 상처 +1 두 번 -> 2단계, 정비 자리 준비, 수리 -> 0단계, DayLog 수리비 증가를 직접 읽어 return 에 찍는다.
// 뒷부분(콜백) = 화면 시연, 클립 시작에 맞춤: 깨끗 → 1 → 2 → 3단계(조각·연기가 늘어남) → E 누름(감독 SimInteract, 실제 E 와 같은 길) -> 표시 사라짐, 돈 글자.
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.Car == null) return "FAIL: 감독·카트 없음";
var car = d.Car;
if (RepairSpot.Active == null) return "FAIL: 정비 자리(RepairSpot) 없음 — Map_TownV6 아님?";
Vector3 feet = new Vector3(216.6f, 26.7f, -55.4f);
d.DebugTeleport(feet, 90f, new Vector3(219.4f, 26.7f, -55.4f), 270f);
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>(); if (fc != null) fc.Pitch = 22f;
var dm = CartDamage.Of(car);
dm.Clear();
DebugWindow.CartHurt(); DebugWindow.CartHurt();
int s1 = dm.Stage;
bool ready = RepairSpot.Ready(d.Player.transform.position, car);
int r0 = DayLog.TodayRepair, m0 = DayLog.TodayMoney;
int cost = RepairSpot.Repair(car);
int s2 = dm.Stage, dr = DayLog.TodayRepair - r0, dmoney = DayLog.TodayMoney - m0;
string check = "상처 " + s1 + "→" + s2 + " · DayLog 수리 −" + dr + " (오늘 번 돈 " + dmoney + ") · 정비 자리 준비=" + ready;
// 시연 시계는 클립이 실제로 시작한 순간부터(clip.cs.tmpl 이 시작할 때 ev.done 을 "" 로 비운다 — 그걸 신호로). 20초 안에 안 오면(손으로 돌릴 때) 그냥 시작.
// (앞 판은 8초였는데 클립 exec 컴파일이 길면 시계가 클립보다 먼저 돌아 1~3단계가 strip 한 칸에 몰렸다.)
// 증거 strip 은 클립을 8칸으로 고르게 자른다(약 1.3~1.4초 간격) → 단계마다 1.8초씩 머물러 칸마다 최소 하나씩 잡힌다:
// 0.0 깨끗 · 0.6 1단계 · 2.4 2단계 · 4.2 3단계 · 6.6 E 수리 → 나머지 칸 −돈 글자·깨끗.
UnityEditor.SessionState.SetString("ev.done", "c001-wait");
float t0 = Time.realtimeSinceStartup, c0 = -1f; int step = 0;
int run = UnityEngine.Random.Range(1, int.MaxValue); UnityEditor.SessionState.SetInt("c001.run", run);   // 같은 Play 에서 다시 부르면 앞 콜백은 빠진다
UnityEditor.SessionState.SetString("c001.e", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c001.run", 0) != run) { UnityEditor.EditorApplication.update -= cb; return; }
    float now = Time.realtimeSinceStartup;
    if (c0 < 0f) { if (UnityEditor.SessionState.GetString("ev.done", "") != "c001-wait" || now - t0 > 20f) c0 = now; else return; }
    float t = now - c0;
    if (step == 0 && t >= 0.6f) { DebugWindow.CartHurt(); step = 1; }
    else if (step == 1 && t >= 2.4f) { DebugWindow.CartHurt(); step = 2; }
    else if (step == 2 && t >= 4.2f) { DebugWindow.CartHurt(); step = 3; }
    else if (step == 3 && t >= 6.4f) { UnityEditor.SessionState.SetString("c001.chip", d.Prompt + " / 상처 " + dm.Stage); step = 4; }
    else if (step == 4 && t >= 6.6f) { d.SimInteract(); step = 5; }
    else if (step == 5 && t >= 6.9f) {
        string r = "E 경로: 칩 '" + UnityEditor.SessionState.GetString("c001.chip", "") + "' → 상처 " + dm.Stage + " · 돈 글자 '" + d.MoneyText + "' · 오늘 수리 합 " + DayLog.TodayRepair;
        UnityEditor.SessionState.SetString("c001.e", r); Debug.Log("[C-001] " + r); step = 6;
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return check + " · 시연 대기(클립 시작 +0.6s 1단계, +2.4s 2단계, +4.2s 3단계, +6.6s E)";
