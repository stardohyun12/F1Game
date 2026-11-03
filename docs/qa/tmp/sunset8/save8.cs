// 편집 모드: 노을 끈 상태로 씬 저장(8판)
var look = SunsetLook.Find();
if (look.IsOn) look.SetOn(false);
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
string hid = ""; foreach (var r in look.hideWhileOn) hid += r.name + ":" + r.enabled + " ";
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>(FindObjectsInactive.Include);
var db = look.backdrop ? look.backdrop.GetComponent<DuskBackdrop>() : null;
return "saved=" + ok + " scene=" + s.name + " on=" + look.IsOn + " backdrop=" + (look.backdrop ? look.backdrop.activeSelf.ToString() : "null")
    + " hide=" + hid + " seaMat=" + look.sea.sharedMaterial.name + " near=" + look.seaNear.enabled
    + " splash=" + (spot.splashClip ? spot.splashClip.name : "null") + " fg=" + (fg ? fg.origin.name : "null") + " amb=" + (db && db.ambience ? db.ambience.name : "null")
    + " rodTip=" + spot.rodTip + " glow=" + look.screenGlow;
