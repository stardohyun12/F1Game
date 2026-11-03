// Edit 모드: Play 미리보기·리뷰 뒤 값 → 씬 직렬화 값 + 바다 마루 선 확인 + 저장(노을 꺼짐 그대로)
var sb = new System.Text.StringBuilder();
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>(FindObjectsInactive.Include);
fg.deckRect = new Vector4(1.3f, 8f, 0.3f, 2.2f);
fg.paint = new Color[] { new Color(0.42f, 0.32f, 0.27f), new Color(0.46f, 0.35f, 0.28f), new Color(0.39f, 0.31f, 0.27f), new Color(0.44f, 0.34f, 0.30f) };
fg.bucketColor = new Color(0.36f, 0.35f, 0.34f);
fg.bucketAt = new Vector3(-1.4f, 2.05f, 0f); fg.boxAt = new Vector3(-2.1f, 1.9f, 20f);
UnityEditor.EditorUtility.SetDirty(fg);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>(FindObjectsInactive.Include);
spot.rodTip = new Vector3(1.07f, -0.24f, 2.36f); spot.lineColor = new Color(0.95f, 0.74f, 0.60f); UnityEditor.EditorUtility.SetDirty(spot);
var bd = UnityEngine.Object.FindFirstObjectByType<DuskBackdrop>(FindObjectsInactive.Include);
bd.gulls = new Vector4(3f, 35f, 8f, 5f); bd.flightArc = new Vector2(-6f, 48f); bd.lampBlink = new Vector3(5f, 1.1f, 4.5f); UnityEditor.EditorUtility.SetDirty(bd);
var sl = SunsetLook.Find();
sl.screenGlow = new Vector4(0.10f, 0.22f, 0.05f, 0.022f); UnityEditor.EditorUtility.SetDirty(sl);
var m = sl.seaMaterial; sb.Append("seaMat=" + m.name + " crest=" + m.GetVector("_Crest") + "\n");
if (m.GetVector("_Crest").w != 0f) { m.SetVector("_Crest", new Vector4(0.72f, 1f, 22f, 0f)); UnityEditor.EditorUtility.SetDirty(m); sb.Append("crest fixed\n"); }
UnityEditor.AssetDatabase.SaveAssets();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
sb.Append("saved=" + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc));
sb.Append(" deck=" + fg.deckRect + " line=" + spot.lineColor + " gulls=" + bd.gulls + " lamp=" + bd.lampBlink + " glow=" + sl.screenGlow);
return sb.ToString();
