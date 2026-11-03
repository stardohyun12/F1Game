// 4판 머티리얼을 셰이더 기본값으로(깊이 텍스처는 유지)
UnityEditor.AssetDatabase.Refresh();
var look = SunsetLook.Find(); if (look.IsOn) look.SetOn(false);
var m = look.seaMaterial; var tex = m.GetTexture("_DepthTex"); var rect = m.GetVector("_DepthRect");
var fresh = new Material(Shader.Find("PitCrew/Look/DuskSea"));
m.shader = fresh.shader; m.CopyPropertiesFromMaterial(fresh); m.SetTexture("_DepthTex", tex); m.SetVector("_DepthRect", rect);
UnityEngine.Object.DestroyImmediate(fresh);
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
return "ok shader=" + m.shader.name + " supported=" + m.shader.isSupported + " tex=" + (tex ? tex.name : "null") + " rect=" + rect;
