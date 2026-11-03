// 8판 씬 설정(에디트 모드): splashClip · DuskForeground · 갈매기 소리 연결 · 줄 색
var sb = new System.Text.StringBuilder();
var sh = Shader.Find("PitCrew/Look/DuskRim"); var msgs = UnityEditor.ShaderUtil.GetShaderMessages(sh);
sb.Append("DuskRim msgs=" + msgs.Length + (msgs.Length > 0 ? " " + msgs[0].message : "") + "\n");
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambience/sfx_splash_01.wav");
var so = new UnityEditor.SerializedObject(spot);
so.FindProperty("splashClip").objectReferenceValue = clip;
so.FindProperty("lineColor").colorValue = new Color(1.00f, 0.84f, 0.66f);
so.ApplyModifiedProperties();
sb.Append("clip=" + (clip ? clip.name + " " + clip.length.ToString("F2") + "s" : "null") + "\n");
var look = SunsetLook.Find(); var bd = look.backdrop; sb.Append("backdrop=" + (bd ? bd.name : "null") + " active=" + (bd ? bd.activeSelf : false) + "\n");
var db = bd.GetComponent<DuskBackdrop>();
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>();
var so2 = new UnityEditor.SerializedObject(db); so2.FindProperty("ambience").objectReferenceValue = amb; so2.ApplyModifiedProperties();
sb.Append("ambience=" + (amb ? amb.name + " clips=" + (amb.gullClips != null ? amb.gullClips.Length : 0) : "null") + "\n");
var fgT = bd.transform.Find("DuskForeground");
if (fgT == null) { var g = new GameObject("DuskForeground"); g.transform.SetParent(bd.transform, false); fgT = g.transform; }
var fg = fgT.GetComponent<DuskForeground>(); if (fg == null) fg = fgT.gameObject.AddComponent<DuskForeground>();
fg.origin = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
UnityEditor.EditorUtility.SetDirty(fg);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fg.gameObject.scene);
sb.Append("fg ok children=" + fgT.childCount);
return sb.ToString();
