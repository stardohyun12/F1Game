UnityEditor.AssetDatabase.Refresh();
Shader.SetGlobalVector("_A7InQ", new Vector4(0.5f, 0.15f, 0.15f, 0f));
var p = Shader.GetGlobalVector("_A7InP"); p.y = 10; p.z = 0.15f; Shader.SetGlobalVector("_A7InP", p);
var oc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>(); oc.LookAt(new Vector3(189.6f, 28.7f, -61.2f));
return "set " + Shader.GetGlobalVector("_A7InQ") + " " + Shader.GetGlobalVector("_A7InP");
