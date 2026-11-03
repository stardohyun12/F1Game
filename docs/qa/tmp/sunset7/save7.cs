// 편집 모드: 7판 값 확인·머티리얼 저장 → 노을 끈 상태로 씬 저장
var look = SunsetLook.Find(); var m = look.seaMaterial;
m.SetVector("_Cool", new Vector4(0.55f, 1.5f, 0, 0)); m.SetColor("_CoolCol", new Color(0.16f, 0.32f, 0.46f));
UnityEditor.EditorUtility.SetDirty(m);
if (look.rimMaterial != null) UnityEditor.EditorUtility.SetDirty(look.rimMaterial);
UnityEditor.AssetDatabase.SaveAssets();
if (look.IsOn) look.SetOn(false);
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
string hid = ""; foreach (var r in look.hideWhileOn) hid += r.name + ":" + r.enabled + " ";
return "saved=" + ok + " scene=" + s.name + " on=" + look.IsOn + " backdrop=" + (look.backdrop ? look.backdrop.activeSelf.ToString() : "null") + " rimMat=" + (look.rimMaterial ? look.rimMaterial.name : "null")
    + " hide=" + hid + " seaMat=" + look.sea.sharedMaterial.name + " near=" + look.seaNear.enabled
    + " spot fov=" + spot.seatedFov + " down=" + spot.seatLookDown + " castYaw=" + spot.castYaw + " yawLeft=" + spot.yawLeft
    + " sunAz=" + look.sunAzimuth + " si=" + look.sunIntensity + " rim=" + look.rim + " strips=" + look.cloudStrips + " cool=" + m.GetVector("_Cool");
