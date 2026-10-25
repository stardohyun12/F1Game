// C 를 옛 임포트(Bilinear·mip·DXT1)·틴트 1 로 잠깐 되돌려 "게임 전" 캡처용. 끝나면 CarVisualSwitcher.Apply 가 다시 고친다.
var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Art/BayLoop/Car/C_Atlas.png");
ti.filterMode = FilterMode.Bilinear; ti.mipmapEnabled = true; ti.textureCompression = UnityEditor.TextureImporterCompression.Compressed; ti.maxTextureSize = 2048; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BayLoop/Car/Candidate_C.mat"); m.SetColor("_Tint", Color.white);
UnityEditor.SessionState.SetString("c.tag", "C_before");
var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/BayLoop/Car/C_Atlas.png");
return "C old import " + t.format + " " + t.filterMode + " mips=" + t.mipmapCount;
