var sb = new System.Text.StringBuilder();
sb.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
// PCLook.hlsl 을 쓰는 셰이더 에러 검사
int bad = 0;
foreach (var si in UnityEditor.ShaderUtil.GetAllShaderInfo()) if (si.hasErrors && si.name.StartsWith("PitCrew")) { bad++; sb.Append(" ERR:" + si.name); }
foreach (var si in UnityEditor.ShaderUtil.GetAllShaderInfo()) if (si.hasErrors && si.name.StartsWith("SceneA7")) { bad++; sb.Append(" ERR:" + si.name); }
sb.Append(" shaderErrors=" + bad);
PitCrewSunsetMenu.Install();
var r = GameObject.Find("Sunset_D227");
sb.Append(" root=" + (r != null));
if (r != null) { var c = r.GetComponent<CoastAmbience>(); sb.Append(" gullClips=" + (c.gullClips == null ? 0 : c.gullClips.Length) + " waves=" + (c.waves != null && c.waves.clip != null)); var w = r.GetComponentInChildren<WindAmbience>(); sb.Append(" wind=" + (w != null && w.clip != null)); }
return sb.ToString();
