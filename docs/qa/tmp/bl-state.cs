Application.runInBackground = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rc = Services.Run as RunController;
return "playing=" + Application.isPlaying + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " stage=" + (rc != null ? rc.CurrentStage.ToString() : "null") + " car=" + car.transform.position.ToString("F1") + " v=" + car.GetComponent<Rigidbody>().linearVelocity.magnitude.ToString("F1")
  + " | drive=" + UnityEditor.SessionState.GetString("bl.drive", "") + " | shot=" + UnityEditor.SessionState.GetString("bl.shotlog", "");
