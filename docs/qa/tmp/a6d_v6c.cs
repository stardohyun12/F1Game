// 과제 1736: A6d 카트 v6b → v6c. 형태·UV 동일(art-20261027-1730 보고: 59 mesh·1,864 tris, 정점·UV 차 ≤1.2e-7) → png 만 교체.
// 씬 전용 재질 SA6d_kart_v6b 의 _BaseMap 만 kart_v6c.png 로(이름은 그대로). Point·밉맵 끔·압축 끔.
const string P = "Assets/Art/SceneA6d/kart/kart_v6c.png";
System.IO.File.Copy(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/kart-v6c/kart_v6c.png")), Application.dataPath + "/Art/SceneA6d/kart/kart_v6c.png", true);
AssetDatabase.ImportAsset(P, ImportAssetOptions.ForceUpdate);
var ti = (TextureImporter)AssetImporter.GetAtPath(P); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6d_Town.unity");
var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA6d/Mats/SA6d_kart_v6b.mat"); var t = AssetDatabase.LoadAssetAtPath<Texture2D>(P);
m.SetTexture("_BaseMap", t); EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
int n = 0; foreach (var r in GameObject.Find("Race").transform.Find("A6_KartVisual").GetComponentsInChildren<MeshRenderer>(true)) if (r.sharedMaterial == m) n++;
return "tex " + t.width + "x" + t.height + " " + t.filterMode + " mip " + t.mipmapCount + " fmt " + t.format + " | kart renderers on mat " + n + " sat " + m.GetFloat("_Sat") + " dirty " + sc.isDirty;
