// Play: 사장 대화 끝낸 것으로 → 카트 타기 → 키 입력 끄고 __SEC__ 초 앞으로(조향 __STEER__)
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (d.State.ToString() == "OnFoot")
{
    typeof(OnFootDirector).GetProperty("Talked").SetValue(d, true);
    typeof(OnFootDirector).GetMethod("EnterCart", bf).Invoke(d, null);
}
var car = d.Car; var pi = car.GetComponent<UnityEngine.InputSystem.PlayerInput>(); if (pi != null) pi.enabled = false;
float tEnd = Time.time + __SEC__f;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.time < tEnd) car.SetMoveInput(new Vector2(__STEER__f, __THR__f));
    else { car.SetMoveInput(new Vector2(0f, __HOLD__f)); UnityEditor.EditorApplication.update -= cb; }
};
UnityEditor.EditorApplication.update += cb;
return "state=" + d.State + " pos=" + car.transform.position.ToString("F1") + " drop=" + (d.FirstDrop ? d.FirstDrop.transform.position.ToString("F1") : "-");
