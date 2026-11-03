// C-002 기준 3 — 카트에 타면 방향 표시가 택배 보관소를 가리킨다.
// 하네스가 새 Play → 장소 0 "집 방" 뒤 실행. 사장 앞 → 실제 대화 3줄(약 1.1 s) → 열쇠 → 카트 옆 → E 타기 → 2 s 캡처 때 카트 뒤 화면.
// [컷] 카트 머리를 보관소 쪽으로 돌려 둔다(실제 플레이에선 도현이 돌린다) — 깃발이 화면 안에.
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.OpeningSeq == null) return "FAIL: 감독/오프닝 없음";
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
d.OpeningSeq.StopAllCoroutines(); d.Player.Cam.HoldLook = false;
DebugWindow.GoPlace(2);
int phase = 0; float lineAt = -1f;
double t0 = UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    double t = UnityEditor.EditorApplication.timeSinceStartup - t0;
    if (phase == 0 && t > 0.05) { if (d.ForceTalk()) { lineAt = d.TalkLineAt; phase = 1; } }
    if (phase == 1)
    {
        if (d.InTalk && Time.unscaledTime - d.TalkLineAt > 0.31f) d.SimInteract();
        if (!d.InTalk)
        {
            var car = d.Car; var wd = d.WalkDest;
            if (car != null && wd != null)
            {
                Vector3 to = wd.transform.position - car.transform.position; to.y = 0f;
                var rb = car.GetComponent<Rigidbody>(); Quaternion r = Quaternion.LookRotation(to.normalized, Vector3.up);
                car.transform.rotation = r; if (rb != null) { rb.rotation = r; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms();
            }
            DebugWindow.GoPlace(3);
            phase = 2;
        }
    }
    if (phase == 2)
    {
        if (d.State == OnFootDirector.Mode.OnFoot)
        {
            if (d.Car != null) d.Player.Cam.LookAt(d.Car.transform.position + Vector3.up * 0.5f);
            if (d.Prompt == "E 타기") d.SimInteract();
        }
        else if (d.State == OnFootDirector.Mode.InCart) phase = 3;
    }
    if ((phase == 3 && t > 1.9) || t > 4.0)
    {
        UnityEditor.SessionState.SetString("c002.crit3", "t=" + t.ToString("F1") + " State=" + d.State + " Step=" + Opening.Step + " 열쇠=" + d.OpeningSeq.HasKey
            + " 방향표시=" + (ui != null ? ui.DestMode : "-") + "→" + (d.WalkDest != null ? d.WalkDest.Label : "없음") + " 안내=" + d.OpeningSeq.Caption + " 순서=" + Opening.Trail);
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "기준3: 사장 대화 → 열쇠 → 카트 탑승 (결과 SessionState c002.crit3)";
