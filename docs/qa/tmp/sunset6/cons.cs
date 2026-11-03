// 콘솔 에러·예외 수 (LogEntries 리플렉션)
var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var gc = t.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
object[] a = { 0, 0, 0 }; gc.Invoke(null, a);
return "errors=" + a[0] + " warnings=" + a[1] + " logs=" + a[2] + " playing=" + Application.isPlaying + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
