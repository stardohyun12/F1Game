var c = Camera.main.transform; var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
return "done=" + FrameFlickerProbe.Done + " " + FrameFlickerProbe.Summary + " car=" + car.transform.position.ToString("F1") + " v=" + car.GetComponent<Rigidbody>().linearVelocity.magnitude.ToString("F1")
  + " POSE=" + c.position.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + c.position.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + c.position.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
  + "," + c.eulerAngles.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + c.eulerAngles.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
