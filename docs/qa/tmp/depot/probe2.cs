var car = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().Car;
var rb = car.GetComponent<Rigidbody>();
return "ts=" + Time.timeScale + " t=" + Time.time.ToString("F1") + " v=" + (rb != null ? rb.linearVelocity.magnitude.ToString("F2") : "-") + " runActive=" + (Services.Run != null && Services.Run.RunActive) + " bot=" + OnFootRouteBot.Status;
