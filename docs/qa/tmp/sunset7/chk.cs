// 컴파일 상태 + 새 타입/셰이더 + 셰이더 에러
var sb = new System.Text.StringBuilder();
sb.Append("compiling=" + UnityEditor.EditorApplication.isCompiling + " updating=" + UnityEditor.EditorApplication.isUpdating);
sb.Append(" backdropType=" + (System.Type.GetType("DuskBackdrop, Assembly-CSharp") != null));
foreach (var n in new[] { "PitCrew/Look/DuskSilhouette", "PitCrew/Look/DuskSky", "PitCrew/Look/DuskSea", "PitCrew/Look/Present" })
{
    var s = Shader.Find(n); sb.Append("\n" + n + " found=" + (s != null));
    if (s != null) { var msgs = UnityEditor.ShaderUtil.GetShaderMessages(s); sb.Append(" msgs=" + msgs.Length); foreach (var m in msgs) sb.Append("\n   " + m.severity + " " + m.message + " line " + m.line); }
}
return sb.ToString();
