// Edit 모드: 9판 값 → 씬 전용 바다 머티리얼(TV6_SeaDusk) + 소리 소스 + 저장(노을 꺼짐 그대로)
var sb = new System.Text.StringBuilder();
var sl = SunsetLook.Find();
var m = sl.seaMaterial; sb.Append("seaMat=" + m.name + " path=" + UnityEditor.AssetDatabase.GetAssetPath(m) + "\n");
// ① 가운데 검푸른 나무 반사 덩어리: 장면 반사 섞임 1.0 → 0.6 (끄지 않음 — 나무 반사는 물로 읽히는 단서)
m.SetVector("_SceneRefl", new Vector4(__REFL__f, 0.06f, 0, 0));
// ② 섬 끝 흰 점선 = 물가 거품 띠: 흰색 → 노을 받은 살구색, 먼 거품 폭(안 10→4 m, 띠 20→6 m)
m.SetColor("_Foam", new Color(0.93f, 0.72f, 0.60f));
m.SetVector("_FoamMax", new Vector4(4, 8, 4, 6));
m.SetVector("_Lv", new Vector4(__LV0__f, 0.24f, 0.45f, 0.8f));
UnityEditor.EditorUtility.SetDirty(m);
// ③ 소리: 파도 소스 = 실제 가장 가까운 물가(곶, 자리에서 약 26 m)로. 갈매기 = 35 m 무리 울음이 파도보다 8~10 dB 아래로 들리게
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>(FindObjectsInactive.Include);
amb.waves.transform.position = new Vector3(__WX__f, 0.2f, __WZ__f);
amb.waves.minDistance = __WMIN__f; amb.waves.maxDistance = __WMAX__f;
amb.gulls.minDistance = __GMIN__f; amb.gullVolume = __GVOL__f;
UnityEditor.EditorUtility.SetDirty(amb); UnityEditor.EditorUtility.SetDirty(amb.waves); UnityEditor.EditorUtility.SetDirty(amb.waves.transform); UnityEditor.EditorUtility.SetDirty(amb.gulls);
var bd = UnityEngine.Object.FindFirstObjectByType<DuskBackdrop>(FindObjectsInactive.Include);
bd.cryAt = 0.4f; UnityEditor.EditorUtility.SetDirty(bd);
UnityEditor.AssetDatabase.SaveAssets();
if (sl.IsOn) sl.SetOn(false);
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
sb.Append("saved=" + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc));
sb.Append(" refl=" + m.GetVector("_SceneRefl") + " foam=" + m.GetColor("_Foam") + " foamMax=" + m.GetVector("_FoamMax") + " lv=" + m.GetVector("_Lv"));
sb.Append(" waves=" + amb.waves.transform.position + " min=" + amb.waves.minDistance + " max=" + amb.waves.maxDistance + " gullMin=" + amb.gulls.minDistance + " gullVol=" + amb.gullVolume + " on=" + sl.IsOn);
return sb.ToString();
