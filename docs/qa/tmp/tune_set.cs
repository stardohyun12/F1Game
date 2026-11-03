UnityEditor.AssetDatabase.Refresh();
Shader.SetGlobalVector("_A7InQ", new Vector4(QX, QY, QZ, 0));
var p = Shader.GetGlobalVector("_A7InP"); p.y = LEV; p.z = FOG; Shader.SetGlobalVector("_A7InP", p);
var oc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>(); oc.LookAt(new Vector3(189.6f, 28.7f, -61.2f));
return "set " + Shader.GetGlobalVector("_A7InQ") + " " + Shader.GetGlobalVector("_A7InP");
