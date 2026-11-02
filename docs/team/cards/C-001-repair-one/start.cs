// C-001 시연(그레이박스). 하네스가 장소 9 정비소 안으로 옮긴 뒤 exec 로 부른다. Play 중에만.
// 앞부분(바로 실행) = 기계 확인: 상처 +1 두 번 -> 2단계, 정비 자리 준비, 수리 -> 0단계, DayLog 수리비 증가를 직접 읽어 return 에 찍는다.
// 뒷부분(12초 콜백) = 화면 시연(exec 한 번이 2~3초 걸려 클립이 늦게 시작해도 보이게 늦춤): 3.0초 상처 +1, 5.0초 +1, 7.0초 E 누름(감독 SimInteract, 실제 E 와 같은 길) -> 칩 E 고치기, 표시 사라짐, 돈 글자.
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
float t0 = Time.realtimeSinceStartup; int step = 0; UnityEditor.SessionState.SetFloat("c001.t0", t0);
UnityEditor.SessionState.SetString("c001.e", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    float t = Time.realtimeSinceStartup - t0;
    if (step == 0 && t >= 3.0f) { DebugWindow.CartHurt(); step = 1; }
    else if (step == 1 && t >= 5.0f) { DebugWindow.CartHurt(); step = 2; }
    else if (step == 2 && t >= 6.6f) { UnityEditor.SessionState.SetString("c001.chip", d.Prompt + " / 상처 " + dm.Stage); step = 3; }
    else if (step == 3 && t >= 7.0f) { d.SimInteract(); step = 4; }
    else if (step == 4 && t >= 7.3f) {
        string r = "E 경로: 칩 '" + UnityEditor.SessionState.GetString("c001.chip", "") + "' → 상처 " + dm.Stage + " · 돈 글자 '" + d.MoneyText + "' · 오늘 수리 합 " + DayLog.TodayRepair;
        UnityEditor.SessionState.SetString("c001.e", r); Debug.Log("[C-001] " + r); step = 5;
    }
    else if (step == 5 && t >= 12f) { UnityEditor.EditorApplication.update -= cb; }
};
UnityEditor.EditorApplication.update += cb;
return check + " · 시연 시작(3.0s +1, 5.0s +1, 7.0s E)";
