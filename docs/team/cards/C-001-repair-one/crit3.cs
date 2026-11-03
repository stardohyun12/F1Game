// C-001 기준 3: 상처 2단계 → 0.8초 뒤 E(감독 SimInteract = 실제 E 와 같은 길) → 연기 꺼짐 + 「−숫자」 돈 글자(2.6초 떠 있음) — 2초 캡처에 잡힌다.
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.Car == null) return "FAIL: 감독·카트 없음";
var car = d.Car;
if (RepairSpot.Active == null) return "FAIL: 정비 자리(RepairSpot) 없음";
// 정비 자리 서쪽 끝에 서서 동쪽 카트(코 위 연기 자리)를 본다 — start.cs 와 같은 자리·시선
d.DebugTeleport(new Vector3(216.6f, 26.7f, -55.4f), 90f, new Vector3(219.4f, 26.7f, -55.4f), 270f);
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
System.Func<Vector3> lookAt = () => car.transform.position + Vector3.up * 1.1f;
if (fc != null) fc.LookAt(lookAt());
var dm = CartDamage.Of(car);
dm.Clear();
int run = UnityEngine.Random.Range(1, int.MaxValue); UnityEditor.SessionState.SetInt("c001.run", run);
float t0 = Time.realtimeSinceStartup; int step = 0;
UnityEditor.EditorApplication.CallbackFunction cb = null;
DebugWindow.CartHurt(); DebugWindow.CartHurt();
// 캡처 시점 여유: exec 한 번이 컴파일로 2~4초 걸려 「2초 뒤」 캡처가 실제론 4~6초 뒤가 된다 → 이 Play 에서만 돈 글자 표시 2.6→10초(검증 사진용 상태 세팅, 코드 값은 그대로)
var msF = typeof(OnFootDirector).GetField("moneySec", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (msF != null) msF.SetValue(d, 10f);
int r0 = DayLog.TodayRepair, s0 = dm.Stage;
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c001.run", 0) != run || Time.realtimeSinceStartup - t0 > 4f) { UnityEditor.EditorApplication.update -= cb; return; }
    if (fc != null) fc.LookAt(lookAt());
    float t = Time.realtimeSinceStartup - t0;
    if (step == 0 && t >= 0.8f) { d.SimInteract(); step = 1; }
    else if (step == 1 && t >= 1.0f) {
        string r = "상처 " + s0 + "→" + dm.Stage + " · 돈 글자 '" + d.MoneyText + "' · DayLog 수리 −" + (DayLog.TodayRepair - r0);
        UnityEditor.SessionState.SetString("c001.crit3", r); Debug.Log("[C-001 crit3] " + r); step = 2;
    }
};
UnityEditor.EditorApplication.update += cb;
return "상처 " + s0 + "단계 · 정비 자리 준비=" + RepairSpot.Ready(d.Player.transform.position, car) + " · +0.8s 에 E (결과는 SessionState c001.crit3 · 콘솔 [C-001 crit3])";
