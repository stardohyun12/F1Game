using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 차 시각만 교체 (2026-10-13). Race 루트의 자식(body/wheel-*)의 MeshFilter.sharedMesh + 머티리얼을
/// 생성기 FBX(Assets/Model/Generated/race_v2.fbx)의 같은 이름 메시로 바꾼다. transform·물리·카메라는 그대로.
/// 되돌리기: Revert 메뉴 (Kenney race.fbx 로 복귀). 씬 저장은 별도.
/// </summary>
public static class SwapCarVisual
{
    const string NewFbx = "Assets/Model/Generated/race_v2.fbx";
    const string OldFbx = "Assets/Model/kenney_car-kit/Models/FBX format/race.fbx";
    static readonly string[] Names = { "body", "wheel-front-left", "wheel-front-right", "wheel-back-left", "wheel-back-right" };

    /// <summary>생성기가 쓴 색표(.materials.json)로 URP Lit 머티리얼을 만들어 이름으로 매핑. 임포터 색 해석에 안 기댄다.</summary>
    static Dictionary<string, Material> PaletteFor(string fbxPath)
    {
        var dict = new Dictionary<string, Material>();
        string jsonPath = System.IO.Path.ChangeExtension(fbxPath, null) + ".materials.json";
        if (!System.IO.File.Exists(jsonPath)) return dict;
        string dir = "Assets/Rendering/Palette/Car";
        System.IO.Directory.CreateDirectory(dir);
        // 아주 작은 JSON 파서 (이름: [r,g,b])
        var text = System.IO.File.ReadAllText(jsonPath);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, "\"([^\"]+)\"\\s*:\\s*\\[\\s*([0-9.]+)\\s*,\\s*([0-9.]+)\\s*,\\s*([0-9.]+)\\s*\\]"))
        {
            string name = m.Groups[1].Value;
            var col = new Color(float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture));
            string path = $"{dir}/Mat_Car_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", name == "Rim" || name == "Visor" || name == "Helmet" ? 0.5f : 0.1f);
            EditorUtility.SetDirty(mat);
            dict[name] = mat;
        }
        AssetDatabase.SaveAssets();
        return dict;
    }

    static int Apply(string fbxPath)
    {
        var palette = PaletteFor(fbxPath);
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogWarning("[SwapCar] ArcadeCarController 없음"); return 0; }
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (src == null) { Debug.LogWarning("[SwapCar] FBX 없음: " + fbxPath); return 0; }
        var srcByName = new Dictionary<string, (Mesh, Material[])>();
        foreach (var mf in src.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            srcByName[mf.name] = (mf.sharedMesh, mr != null ? mr.sharedMaterials : new Material[0]);
        }
        int n = 0;
        foreach (var t in car.GetComponentsInChildren<Transform>(true))
        {
            if (System.Array.IndexOf(Names, t.name) < 0) continue;
            if (!srcByName.TryGetValue(t.name, out var entry)) { Debug.LogWarning("[SwapCar] 원본에 없음: " + t.name); continue; }
            var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) continue;
            Undo.RecordObjects(new Object[] { mf, mr }, "Swap car visual");
            var mats = entry.Item2;
            if (palette.Count > 0)
            {
                mats = (Material[])mats.Clone();
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && palette.TryGetValue(mats[i].name, out var pm)) mats[i] = pm;
            }
            mf.sharedMesh = entry.Item1; mr.sharedMaterials = mats; n++;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return n;
    }

    [MenuItem("PitCrew/Art/Swap Car Visual (race_v2)")]
    public static void Execute() => Debug.Log("[SwapCar] 교체 " + Apply(NewFbx) + "개 → race_v2");

    [MenuItem("PitCrew/Art/Swap Car Visual Revert (Kenney race)")]
    public static void Revert() => Debug.Log("[SwapCar] 복귀 " + Apply(OldFbx) + "개 → Kenney race");
}
