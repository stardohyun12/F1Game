// 씬 준비: SunsetLook 오브젝트에 SeaReflection(hide = Sea, SeaNear, 반쪽 해상도) 붙이고 셰이더 메시지 확인
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var look = SunsetLook.Find();
var sr = look.GetComponent<SeaReflection>();
if (sr == null) sr = look.gameObject.AddComponent<SeaReflection>();
sr.hide = new Renderer[] { look.sea, look.seaNear }; sr.downscale = 2; sr.planeFromHide = true;
UnityEditor.EditorUtility.SetDirty(sr); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
var sh = look.seaMaterial.shader;
var msgs = UnityEditor.ShaderUtil.GetShaderMessages(sh);
string mm = ""; foreach (var x in msgs) mm += x.severity + ":" + x.message + " L" + x.line + "; ";
return s.path + " play=" + UnityEditor.EditorApplication.isPlaying + " srOn=" + sr.enabled + " seaY=" + look.sea.transform.position.y + " msgs=" + msgs.Length + " " + mm;
