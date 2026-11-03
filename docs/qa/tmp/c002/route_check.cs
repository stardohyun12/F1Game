// 깨뜨릴 만한 곳: 주문 전 자유 주행에서 경로를 비운 뒤에도 주문 출발(BeginFirstOrder)이 찻집 경로를 다시 세우는가
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var mgr = UnityEngine.Object.FindFirstObjectByType<DeliveryManager>();
string before = mgr.CurrentTarget != null ? mgr.CurrentTarget.Label : "null";
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(OnFootDirector).GetMethod("BeginFirstOrder", bf).Invoke(d, null);
var t = mgr.CurrentTarget;
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
return "state=" + d.State + " 비운뒤 CurrentTarget=" + before + " → 주문출발 뒤 CurrentTarget=" + (t != null ? t.Label + " active=" + t.gameObject.activeInHierarchy + " " + t.transform.position.ToString("F0") : "null") + " OrderStarted=" + d.OrderStarted;
