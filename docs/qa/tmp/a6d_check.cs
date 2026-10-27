// 과제 1726: 컴파일·셰이더 확인
System.Type tt = null; foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var x = a.GetType("A6dGrainToggles"); if (x != null) tt = x; }
var look = Shader.Find("PitCrew/SceneA3/Look");
return "type " + (tt != null ? tt.Assembly.GetName().Name : "none") + " grain " + (Shader.Find("PitCrew/A6d/Grain") != null) + " look ok " + look.isSupported + " hasSN " + (look.FindPropertyIndex("_SN") >= 0) + " err " + ShaderUtil.ShaderHasError(look) + " compiling " + EditorApplication.isCompiling;
