var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return OnFootRouteBot.Status + " | no director";
return OnFootRouteBot.Status + " | state=" + d.State + " started=" + d.OrderStarted + " picked=" + d.OrderPicked + " packed=" + d.Packed + " delivered=" + d.Delivered
  + " car=" + d.Car.transform.position.ToString("F0") + " me=" + d.Player.transform.position.ToString("F1") + " prompt='" + d.Prompt + "' t=" + Time.time.ToString("F0");
