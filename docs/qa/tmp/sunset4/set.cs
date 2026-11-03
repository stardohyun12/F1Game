// 바다 머티리얼 값 바꾸기: __SET__ 자리에 m.SetVector(...) 등
var look = SunsetLook.Find(); var m = look.seaMaterial;
__SET__;
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
return "set ok";
