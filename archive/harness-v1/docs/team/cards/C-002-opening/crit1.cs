// C-002 기준 1 — 새 게임 시작: 2층 방 침대 옆에서 눈을 뜨고, 방 물건(달력)이 [E] 로 보인다.
// 하네스가 새 Play → 장소 0 "집 방" 뒤 실행. 오프닝을 처음부터(Begin) 다시 시작 → 깨어남 연출(침대 0.9 s → 달력으로 1.4 s)을
// 1.8 s 안에 끝나게 줄여서(침대 0.5 + 1.0) 캡처(2 s) 때 달력 [E] 가 떠 있게 한다. 값은 이 Play 에만(씬 저장 안 함).
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(OpeningSequence).GetField("bedHoldSec", f).SetValue(d.OpeningSeq, 0.5f);
typeof(OpeningSequence).GetField("wakeSec", f).SetValue(d.OpeningSeq, 1.0f);
d.OpeningSeq.Begin(d);
UnityEditor.EditorApplication.CallbackFunction cb = null;
double t0 = UnityEditor.EditorApplication.timeSinceStartup;
cb = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (UnityEditor.EditorApplication.timeSinceStartup - t0 < 1.9) return;
    UnityEditor.SessionState.SetString("c002.crit1", "Step=" + Opening.Step + " 시선=" + d.Prompt + " 발=" + d.Player.transform.position.ToString("F1") + " 안내=" + d.OpeningSeq.Caption);
    UnityEditor.EditorApplication.update -= cb;
};
UnityEditor.EditorApplication.update += cb;
return "기준1: 새 게임 Begin → Step=" + Opening.Step + " (1.8 s 뒤 달력 [E] — 결과 SessionState c002.crit1)";
