// 과제 0236 — 정비소 바닥 얼룩 지운 아틀라스 사본 할당 + 돌담 타일 축척(돌 하나 ≈ 30 cm). 씬 재질 사본(SA7_*)만 고친다
const string D = "Assets/Art/SceneA7/", S = "Assets/Scenes/Scene_A7_Light.unity";
var log = new System.Text.StringBuilder();
AssetDatabase.Refresh();
string gp = D + "Tex/garage_tin_F2_atlas_clean.png"; AssetDatabase.ImportAsset(gp);
var ti = (TextureImporter)AssetImporter.GetAtPath(gp); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 4096; ti.SaveAndReimport();
var gm = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA7_garage_baked32.mat");
log.Append("garage " + (gm.GetTexture("_BaseMap") ? gm.GetTexture("_BaseMap").name : "-"));
gm.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(gp)); EditorUtility.SetDirty(gm);
log.Append(" → " + gm.GetTexture("_BaseMap").name + "\n");
// 돌담: 실측 u/m 0.66, v/m 0.64. stone_H1(64×48) 한 장 = 가로 1.0 m × 세로 0.75 m 로 → 돌 한 줄(약 4~5개/장) ≈ 20~30 cm
var sw = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA7_stonewall.mat");
var st0 = sw.GetTextureScale("_BaseMap"); var st = new Vector2(1f / (0.66f * 1.0f), 1f / (0.64f * 0.75f));
sw.SetTextureScale("_BaseMap", st); EditorUtility.SetDirty(sw);
log.Append("stonewall ST " + st0.ToString("F3") + " → " + st.ToString("F3") + " tex " + sw.GetTexture("_BaseMap").name + "\n");
AssetDatabase.SaveAssets();
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); log.Append("open scene " + sc.path);
return log.ToString();
