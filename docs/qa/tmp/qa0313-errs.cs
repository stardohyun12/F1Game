// QA 0313: 콘솔 에러 수 + 셰이더 상태
var le = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
int err = 0, warn = 0, log = 0;
le.GetMethod("GetCountsByType").Invoke(null, new object[] { err, warn, log });
var args = new object[] { 0, 0, 0 };
le.GetMethod("GetCountsByType").Invoke(null, args);
var sb = new System.Text.StringBuilder();
sb.Append("console err=" + args[0] + " warn=" + args[1] + "\n");
// 컴파일 에러 메시지 (콘솔에서 error CS 줄)
int total = (int)le.GetMethod("StartGettingEntries").Invoke(null, null);
var entT = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
var ent = System.Activator.CreateInstance(entT);
int cs = 0;
for (int i = 0; i < total; i++)
{
    le.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, ent });
    string msg = (string)entT.GetField("message").GetValue(ent);
    if (msg.Contains("error CS")) { cs++; if (cs <= 5) sb.Append(msg.Split('\n')[0] + "\n"); }
}
le.GetMethod("EndGettingEntries").Invoke(null, null);
sb.Append("errorCS=" + cs + "\n");
sb.Append("hasCompileErrors=" + UnityEditor.EditorUtility.scriptCompilationFailed + "\n");
foreach (var name in new[] { "PitCrew/BayLoop/TerrainSlope", "PitCrew/WorldSurface", "PitCrew/LofiSurface" })
{
    var s = Shader.Find(name);
    if (s == null) { sb.Append(name + " = NOT FOUND\n"); continue; }
    int n = UnityEditor.ShaderUtil.GetShaderMessageCount(s);
    var msgs = UnityEditor.ShaderUtil.GetShaderMessages(s);
    sb.Append(name + " supported=" + s.isSupported + " msgs=" + n + " anyErr=" + UnityEditor.ShaderUtil.ShaderHasError(s) + "\n");
    foreach (var m in msgs) sb.Append("   " + m.severity + " L" + m.line + " " + m.message + "\n");
}
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BayLoop/Terrain/M_BL_TerrainSlope.mat");
sb.Append("M_BL_TerrainSlope=" + (mat == null ? "null" : mat.shader.name));
return sb.ToString();
