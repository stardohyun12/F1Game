AssetDatabase.Refresh();
var sh = Shader.Find("PitCrew/SceneA7/Lit");
var t = System.Type.GetType("A7LightToggles, Assembly-CSharp");
return "shader " + (sh ? sh.name + " err " + ShaderUtil.ShaderHasError(sh) : "null") + " | type " + (t != null) + " | compiling " + EditorApplication.isCompiling;
