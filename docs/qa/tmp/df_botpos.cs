var d = GameObject.FindFirstObjectByType<OnFootDirector>();
var car = d.Car; var rb = car.GetComponent<Rigidbody>();
var rc = Services.Run as RunController;
return "state=" + d.State + " stage=" + (rc != null ? rc.CurrentStage.ToString() : "?") + " car=" + car.transform.position.ToString("F1") + " v=" + rb.linearVelocity.magnitude.ToString("F1") + " fwd=" + car.transform.forward.ToString("F2") + " player=" + d.Player.transform.position.ToString("F1") + " bot=" + OnFootRouteBot.Status;
