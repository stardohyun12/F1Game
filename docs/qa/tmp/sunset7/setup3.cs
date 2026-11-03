// 7판: 역광 테 소재(씬 전용) 만들어 SunsetLook.rimMaterial 에 꽂고 테 문턱 = (켬, 어둠 __DK__, 하늘 __SK__, 세기 __RW__)
var path = "Assets/Scenes/Map_TownV6_Mats/TV6_DuskRim.mat";
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
if (m == null) { m = new Material(Shader.Find("PitCrew/Look/DuskRim")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
var look = SunsetLook.Find(); look.rimMaterial = m; look.rim = new Vector4(1, __DK__f, __SK__f, __RW__f);
UnityEditor.EditorUtility.SetDirty(look);
return "mat=" + m.shader.name + " rim=" + look.rim;
