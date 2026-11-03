// 콘솔 에러 수 + 컴파일 상태 + 최근 에러 3개
var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var gc = t.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
object[] a = { 0, 0, 0 }; gc.Invoke(null, a);
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " seaWaves=" + (System.Type.GetType("SeaWaves, Assembly-CSharp") != null)
    + " errors=" + a[0] + " warnings=" + a[1] + " playing=" + Application.isPlaying + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
    + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty
    + " shaderErr=" + UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("PitCrew/Look/DuskSea"));
