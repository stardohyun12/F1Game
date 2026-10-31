Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "status=" + OnFootRouteBot.Status + " frame=" + Time.frameCount + " ts=" + Time.timeScale + (d != null ? " state=" + d.State + " prompt='" + d.Prompt + "' talked=" + d.Talked + " order=" + d.OrderStarted + " delivered=" + d.Delivered : " no director");
