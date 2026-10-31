var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var car = d.Car; var rb = car.GetComponent<Rigidbody>();
car.SetMoveInput(new Vector2(0f, 0.4f));
return "car " + car.transform.position + " speed=" + car.CurrentSpeedMps + " yaw=" + car.transform.eulerAngles.y;
