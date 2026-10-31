Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var car = d.Car; var rb = car.GetComponent<Rigidbody>();
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
car.transform.SetPositionAndRotation(new Vector3(76.7f, 12.9f, -59.6f), Quaternion.Euler(0f, 258f, 0f));
Physics.SyncTransforms();
car.SetMoveInput(new Vector2(0f, 0.4f));
return "placed " + car.transform.position + " hasRb=" + (rb != null);
