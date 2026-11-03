ParcelBuild.OldSink2147 = false;
var r = ParcelBuild.Run();
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return r + " | dirty " + s.isDirty;
