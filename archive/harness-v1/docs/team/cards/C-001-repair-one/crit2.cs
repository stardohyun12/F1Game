// C-001 기준 2: 상처 2단계(연기) 카트 곁 정비 자리에 서면 [E 고치기] 칩. E 는 누르지 않은 순간.
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
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c001.run", 0) != run || Time.realtimeSinceStartup - t0 > 4f) { UnityEditor.EditorApplication.update -= cb; return; }
    if (fc != null) fc.LookAt(lookAt());
    // 칩 글자는 감독 Update 가 다음 프레임에 정한다(새 Play 직후 exec 안에선 아직 빈칸) → 0.5초 뒤 읽어 남김
    if (step == 0 && Time.realtimeSinceStartup - t0 > 0.5f) { string r = "칩 '" + d.Prompt + "' · 상처 " + dm.Stage; UnityEditor.SessionState.SetString("c001.crit2", r); Debug.Log("[C-001 crit2] " + r); step = 1; }
};
UnityEditor.EditorApplication.update += cb;
return "상처 " + dm.Stage + "단계 · 정비 자리 준비=" + RepairSpot.Ready(d.Player.transform.position, car) + " · 칩 글자는 +0.5s 에 SessionState c001.crit2 · 콘솔 [C-001 crit2]";
