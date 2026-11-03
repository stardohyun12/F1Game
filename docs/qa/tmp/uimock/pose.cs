// Play: 걷는 몸을 P 에 두고 T 쪽(수평)을 보게, 고개는 PITCH 도
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var P = new Vector3(__PX__f, __PY__f, __PZ__f); var T = new Vector3(__TX__f, __TY__f, __TZ__f);
var to = T - P; float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
d.Player.Teleport(P, yaw);
var fc = Camera.main.GetComponent<OnFootCamera>(); fc.Yaw = yaw; fc.Pitch = __PITCH__f;
return "state=" + d.State + " gaze=" + d.GazeTarget + " prompt=" + d.Prompt + " eye=" + Camera.main.transform.position.ToString("F1");
