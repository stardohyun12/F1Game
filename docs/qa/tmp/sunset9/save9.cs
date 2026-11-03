// 편집 모드: 9판 색 정리 값 → 씬·씬 전용 머티리얼, 노을 끈 상태로 저장
var sb = new System.Text.StringBuilder();
var look = SunsetLook.Find();
if (look.IsOn) look.SetOn(false);
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
// 물: 빛길 밖은 한 단 더 차갑게(회청 → 남)
var m = look.seaMaterial;
var c = m.GetVector("_Cool"); c.x = 0.75f; m.SetVector("_Cool", c); m.SetColor("_CoolCol", new Color(0.12f, 0.30f, 0.52f));
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets();
// 판자·소품: 탁한 갈색 → 자줏빛 갈색(실루엣 남보라 계열 쪽), 태클박스 차가운 초록 → 바랜 자두 회색
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>(FindObjectsInactive.Include);
if (__APPLY__ > 0)
{
    var mul = new Color(0.92f, 0.86f, 1.10f);
    fg.paint = new Color[] { new Color(0.386f, 0.275f, 0.297f), new Color(0.423f, 0.301f, 0.308f), new Color(0.359f, 0.267f, 0.297f), new Color(0.405f, 0.292f, 0.330f) };
    fg.bucketColor = new Color(0.33f, 0.30f, 0.37f);
    fg.boxColor = new Color(0.34f, 0.29f, 0.33f);
    UnityEditor.EditorUtility.SetDirty(fg);
}
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>(FindObjectsInactive.Include);
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
var db = look.backdrop ? look.backdrop.GetComponent<DuskBackdrop>() : null;
return "saved=" + ok + " scene=" + s.name + " on=" + look.IsOn + " backdrop=" + (look.backdrop ? look.backdrop.activeSelf.ToString() : "null")
    + " seaMat=" + look.sea.sharedMaterial.name + " cool=" + m.GetVector("_Cool") + " coolCol=" + m.GetColor("_CoolCol") + " refl=" + m.GetVector("_SceneRefl") + " foam=" + m.GetColor("_Foam")
    + " paint0=" + fg.paint[0] + " box=" + fg.boxColor + " rodTipFrac=" + spot.rodTipFrac + " cryAt=" + (db ? db.cryAt : -9)
    + " waves=" + amb.waves.transform.position + "/" + amb.waves.minDistance + "/" + amb.waves.maxDistance + " gull=" + amb.gulls.minDistance + "/" + amb.gullVolume;
