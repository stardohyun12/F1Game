// 편집 모드: 노을 바다 머티리얼(씬 전용 TV6_SeaDusk)에 파문 값을 셰이더 기본값으로 박음
var look = SunsetLook.Find(); var m = look.seaMaterial;
m.SetVector("_Ripple", new Vector4(__RIP__));
m.SetColor("_RippleCol", new Color(1.00f, 0.84f, 0.74f, 1));
m.SetFloat("_Debug", 0); m.SetFloat("_Style", 1);
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
return UnityEditor.AssetDatabase.GetAssetPath(m) + " ripple=" + m.GetVector("_Ripple") + " style=" + m.GetFloat("_Style");
