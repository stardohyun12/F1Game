var t = System.Type.GetType("DotScreenUI, Assembly-CSharp");
var f = t != null ? t.GetField("dirMode") : null;
var sh = Shader.Find("PitCrew/UI/DotPalette");
var sb = new System.Text.StringBuilder();
sb.Append("compiled2=" + (f != null) + " shader=" + (sh != null) + " supported=" + (sh != null && sh.isSupported));
if (sh != null) { var msgs = UnityEditor.ShaderUtil.GetShaderMessages(sh); foreach (var m in msgs) sb.Append(" | " + m.severity + ": " + m.message + " L" + m.line); }
sb.Append(" compiling=" + UnityEditor.EditorApplication.isCompiling);
return sb.ToString();
