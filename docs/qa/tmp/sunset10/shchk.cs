// 10판 셰이더 두 개 컴파일 오류 확인
var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "Assets/Art/SceneA7/SceneA7Lit.shader", "Assets/Art/Look/DuskSea.shader" })
{
    var sh = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);
    sb.Append(path + " err=" + UnityEditor.ShaderUtil.ShaderHasError(sh) + " msgs=" + UnityEditor.ShaderUtil.GetShaderMessageCount(sh) + "\n");
    foreach (var m in UnityEditor.ShaderUtil.GetShaderMessages(sh)) sb.Append("  " + m.severity + " " + m.message + " L" + m.line + "\n");
}
return sb.ToString();
