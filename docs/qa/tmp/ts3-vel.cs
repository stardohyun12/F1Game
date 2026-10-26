var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
return "v=" + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h pos=" + rb.position.ToString("F0") + " kin=" + rb.isKinematic + " drive=" + UnityEditor.SessionState.GetString("bl.drive", "");
