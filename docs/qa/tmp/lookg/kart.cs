// Play 시작 직후: 시간대(__MODE__ = day|aft|dusk) → 카트 타기 → 8초 주행(조향 __STEER__, 가속 __THR__) 동안 깜빡임·프레임 측정
Application.runInBackground = true;
var s = SunsetLook.Find();
string mode = "__MODE__";
if (mode == "day") s.SetOn(false); else { s.dusk = mode == "dusk" ? 1f : 0f; if (!s.IsOn) s.SetOn(true); else s.Apply(); }
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (d.State.ToString() == "OnFoot")
{
    typeof(OnFootDirector).GetProperty("Talked").SetValue(d, true);
    typeof(OnFootDirector).GetMethod("EnterCart", bf).Invoke(d, null);
}
var car = d.Car; var pi = car.GetComponent<UnityEngine.InputSystem.PlayerInput>(); if (pi != null) pi.enabled = false;
float tEnd = Time.time + 8f;
// 진입로 경유점 추종(마지막 뒤로는 마지막 구간 방향으로 곧게). __STEER__ = 조향 이득 배수
var wp = new[] { new Vector2(233.5f, -48f), new Vector2(238.5f, -46.8f), new Vector2(243f, -51f), new Vector2(245.5f, -57f), new Vector2(244.5f, -64f) };
int wi = 0;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.time < tEnd)
    {
        var p = new Vector2(car.transform.position.x, car.transform.position.z);
        if (wi < wp.Length && (wp[wi] - p).magnitude < 3f) wi++;
        var tgt = wi < wp.Length ? wp[wi] : wp[wp.Length - 1] + (wp[wp.Length - 1] - wp[wp.Length - 2]).normalized * 200f;
        var to = tgt - p; var f = new Vector2(car.transform.forward.x, car.transform.forward.z);
        float ang = Vector2.SignedAngle(to, f);   // + = 목표가 오른쪽
        car.SetMoveInput(new Vector2(Mathf.Clamp(ang / 30f * __STEER__f, -1f, 1f), __THR__f));
    }
    else { car.SetMoveInput(Vector2.zero); UnityEditor.EditorApplication.update -= cb; }
};
UnityEditor.EditorApplication.update += cb;
FrameFlickerProbe.Begin(8f, "D:/Github/F1Game/docs/qa/evidence/look-global/flicker/", "kart_" + mode);
return "mode=" + mode + " on=" + s.IsOn + " state=" + d.State + " car=" + car.transform.position.ToString("F1");
