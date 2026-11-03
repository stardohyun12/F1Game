// C-001 기준 1: 상처 표시 = 연기(도현 11:39 「연기나는것만」). 3단계(가장 심함)로 올려 2초 뒤 짙은 연기가 카트 코 위로 오르는 순간.
// 단계별 차이는 return 값(단계마다 연기 양·크기·짙기)으로 함께 찍는다.
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
DebugWindow.CartHurt(); ParticleSystem ps = null; foreach (var p in car.GetComponentsInChildren<ParticleSystem>(true)) if (p.name.StartsWith("CartDamage_Smoke")) ps = p;
if (ps == null) return "FAIL: 연기 없음";
string s1 = "1단계 rate=" + ps.emission.rateOverTime.constant + " size≤" + ps.main.startSize.constantMax.ToString("F2") + " a=" + ps.main.startColor.colorMin.a.ToString("F2");
DebugWindow.CartHurt();
string s2 = "2단계 rate=" + ps.emission.rateOverTime.constant + " size≤" + ps.main.startSize.constantMax.ToString("F2");
DebugWindow.CartHurt();
string s3 = "3단계 rate=" + ps.emission.rateOverTime.constant + " size≤" + ps.main.startSize.constantMax.ToString("F2") + " a=" + ps.main.startColor.colorMin.a.ToString("F2");
int panels = 0; foreach (var t in car.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("CartDamage_Part")) panels++;
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c001.run", 0) != run || Time.realtimeSinceStartup - t0 > 4f) { UnityEditor.EditorApplication.update -= cb; return; }
    if (fc != null) fc.LookAt(lookAt());
};
UnityEditor.EditorApplication.update += cb;
return "상처 " + dm.Stage + "단계 · 연기 재생=" + ps.isPlaying + " · 회색 조각 " + panels + "개 · " + s1 + " / " + s2 + " / " + s3;
