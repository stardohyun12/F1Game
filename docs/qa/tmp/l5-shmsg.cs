var s = Shader.Find("PitCrew/CoastalTown/WorldSurface");
var msgs = UnityEditor.ShaderUtil.GetShaderMessages(s);
return string.Join(" | ", msgs.Select(m => m.severity + " L" + m.line + " " + m.message + " " + m.file));
